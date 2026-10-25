// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MsfsInputBlackbox;

internal static class Program
{
    private const int SampleRateHz = 30;
    private const int HistorySeconds = 60;

    private static int Main(string[] args)
    {
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase)) return RunSelfTest();
#if MSFS_SIMCONNECT
        Console.Error.WriteLine("Live SimConnect mode must be wired to the SDK event loop after the target SDK transport is verified; see docs/LIVE_TEST_PLAN.md.");
        return 2;
#else
        Console.Error.WriteLine("Build with --self-test for a simulator-free test, or set EnableSimConnect=true and SimConnectInteropPath for the official interop assembly.");
        return 2;
#endif
    }

    private static int RunSelfTest()
    {
        if (Marshal.SizeOf<TelemetryFrame>() != TelemetryFrame.SizeInBytes)
            return Fail($"TelemetryFrame is {Marshal.SizeOf<TelemetryFrame>()} bytes; expected {TelemetryFrame.SizeInBytes}.");

        var ring = new TelemetryRingBuffer(3);
        for (uint timestamp = 1; timestamp <= 5; timestamp++) ring.Add(new TelemetryFrame { Timestamp = timestamp, Throttle = timestamp / 10f });
        var snapshot = ring.SnapshotOldestFirst();
        if (snapshot.Length != 3 || snapshot[0].Timestamp != 3 || snapshot[2].Timestamp != 5)
            return Fail("Ring buffer did not preserve the three newest frames in chronological order.");

        var dumpPath = Path.Combine(Path.GetTempPath(), "msfs-blackbox-self-test.json");
        Dump(dumpPath, "synthetic-self-test", snapshot);
        if (!File.Exists(dumpPath)) return Fail("Self-test dump was not created.");
        using var document = JsonDocument.Parse(File.ReadAllText(dumpPath));
        var dumpedFrames = document.RootElement.GetProperty("frames");
        if (dumpedFrames.GetArrayLength() != 3 || dumpedFrames[0].GetProperty("Timestamp").GetUInt32() != 3)
            return Fail("Self-test dump did not preserve telemetry fields.");
        Console.WriteLine($"SELF-TEST PASS: layout=36 bytes, retained={snapshot.Length}, dump={dumpPath}");
        return 0;
    }

    internal static void Dump(string path, string reason, TelemetryFrame[] frames)
    {
        var payload = new { schema = "msfs-input-blackbox/v1", reason, capturedAtUtc = DateTimeOffset.UtcNow, frames };
        File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        }));
    }

    private static int Fail(string message) { Console.Error.WriteLine($"SELF-TEST FAIL: {message}"); return 1; }
}

