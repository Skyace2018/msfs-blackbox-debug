// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO.Compression;

namespace MsfsInputBlackbox;

internal static class Program
{
    private const int SampleRateHz = 30;
    private const int HistorySeconds = 60;

    private static int Main(string[] args)
    {
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase)) return RunSelfTest();
#if MSFS_SIMCONNECT
        return LiveRecorderHost.Run();
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
        var eventDirectory = Path.Combine(Path.GetTempPath(), "msfs-blackbox-event-test-" + Guid.NewGuid().ToString("N"));
        var capture = new EventCaptureService(3, eventDirectory);
        foreach (var frame in snapshot) capture.AddFrame(frame);
        capture.SetAircraftState(new AircraftState("Synthetic SimConnect aircraft", 123.5, 4567.0, DateTimeOffset.UtcNow));
        var preview = new LivePreviewStore(eventDirectory);
        foreach (var frame in snapshot) preview.Publish(capture.SessionId, frame);
        using (var previewDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(eventDirectory, "live-preview.json"))))
        {
            var previewRoot = previewDocument.RootElement;
            if (previewRoot.GetProperty("schema").GetString() != LivePreviewStore.Schema ||
                previewRoot.GetProperty("frames").GetArrayLength() != 3 ||
                previewRoot.GetProperty("frames")[2].GetProperty("Timestamp").GetUInt32() != 5)
                return Fail("Live preview did not preserve the ordered input samples.");
        }
        var id = Guid.NewGuid();
        var eventPath = Path.Combine(eventDirectory, "event.zip");
        var requestPath = Path.Combine(eventDirectory, "event-requests", id.ToString("N") + ".json");
        File.WriteAllText(requestPath, JsonSerializer.Serialize(new EventCaptureRequest
        {
            Id = id, SessionId = capture.SessionId, MarkedAtUtc = DateTimeOffset.UtcNow, OutputPath = eventPath,
            AircraftProfile = "Synthetic profile", AircraftType = "Synthetic aircraft", AircraftSource = "user-entered"
        }));
        File.WriteAllText(Path.ChangeExtension(requestPath, ".ready"), "");
        capture.ProcessRequests();
        if (!File.Exists(eventPath))
        {
            var resultPath = Path.Combine(eventDirectory, "event-results", id.ToString("N") + ".json");
            return Fail("Event request did not create a developer ZIP: " + (File.Exists(resultPath) ? File.ReadAllText(resultPath) : "no result"));
        }
        using (var zip = ZipFile.OpenRead(eventPath))
        {
            if (zip.GetEntry("telemetry.json") is null || zip.GetEntry("event.json") is null || zip.GetEntry("manifest.json") is null)
                return Fail("Event export is missing telemetry, event, or manifest data.");
            using var eventDocument = JsonDocument.Parse(zip.GetEntry("event.json")!.Open());
            var eventRoot = eventDocument.RootElement;
            if (eventRoot.GetProperty("lastInputs").GetProperty("Timestamp").GetUInt32() != 5 ||
                eventRoot.GetProperty("aircraftType").GetString() != "Synthetic SimConnect aircraft" ||
                eventRoot.GetProperty("aircraftVariables").GetProperty("IndicatedAirspeedKnots").GetDouble() != 123.5 ||
                eventRoot.GetProperty("variablesCaptured").GetArrayLength() != 10 ||
                !eventRoot.TryGetProperty("markedAtUtc", out _))
                return Fail("Event export is missing the marker, last input, variables, or aircraft label.");
        }
        File.WriteAllText(Path.Combine(eventDirectory, "settings.json"),
            "{\"HistorySeconds\":3600,\"SampleRateHz\":60,\"LoggingEnabled\":true,\"KeepCrashDumps\":true}");
        var preferences = RecorderPreferences.Load(eventDirectory);
        if (preferences.HistorySeconds != 3600 || preferences.SampleRateHz != 60)
            return Fail("Recorder did not load the selected one-hour history and 60 Hz sample rate.");
        Console.WriteLine($"SELF-TEST PASS: layout=36 bytes, retained={snapshot.Length}, live preview and event ZIP created, one-hour preferences loaded, dump={dumpPath}");
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
