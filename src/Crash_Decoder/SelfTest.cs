// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;
using System.IO.Compression;

namespace MsfsInputBlackbox.Decoder;

internal static class SelfTest
{
    public static bool Run()
    {
        var dumpPath = Path.Combine(Path.GetTempPath(), "msfs-blackbox-decoder-self-test.json");
        var payload = new
        {
            schema = TelemetryDumpReader.SchemaV1, reason = "synthetic-self-test", capturedAtUtc = DateTimeOffset.UtcNow,
            frames = new[]
            {
                new { Timestamp = 10u, YokeX = 0.0f, YokeY = 0.1f, Rudder = 0.2f, Throttle = 0.3f, Switches0 = 1u, Switches1 = 0u, Switches2 = 0u, Switches3 = 0u },
                new { Timestamp = 20u, YokeX = 0.4f, YokeY = 0.5f, Rudder = 0.6f, Throttle = 0.7f, Switches0 = 2u, Switches1 = 0u, Switches2 = 0u, Switches3 = 0u }
            }
        };
        File.WriteAllText(dumpPath, JsonSerializer.Serialize(payload));
        var configPath = Path.Combine(Path.GetTempPath(), "msfs-blackbox-usercfg-self-test.opt");
        File.WriteAllText(configPath, "InstalledPackagesPath \"D:\\MSFS Packages\"");
        var dump = TelemetryDumpReader.Read(dumpPath);
        var previewPath = Path.Combine(Path.GetTempPath(), "msfs-blackbox-preview-self-test.json");
        var sessionId = Guid.NewGuid();
        File.WriteAllText(previewPath, JsonSerializer.Serialize(new
        {
            schema = "msfs-input-blackbox/live-preview/v1", sessionId,
            updatedAtUtc = DateTimeOffset.UtcNow, frames = payload.frames
        }));
        var preview = LivePreviewReader.ReadFromPath(previewPath, sessionId);
        var wrongSessionRejected = LivePreviewReader.ReadFromPath(previewPath, Guid.NewGuid()) is null;
        File.WriteAllText(previewPath, JsonSerializer.Serialize(new
        {
            schema = "msfs-input-blackbox/live-preview/v1", sessionId,
            updatedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-10), frames = payload.frames
        }));
        var staleRejected = LivePreviewReader.ReadFromPath(previewPath, sessionId) is null;
        var diagnosticsPath = Path.Combine(Path.GetTempPath(), $"msfs-blackbox-diagnostics-self-test-{Guid.NewGuid():N}.zip");
        DiagnosticLog.Write("synthetic_self_test");
        DeveloperExport.CreateDiagnosticsBundle(diagnosticsPath, new MonitorStatus { State = "not-running" });
        using var diagnosticsZip = ZipFile.OpenRead(diagnosticsPath);
        var diagnosticsBundleValid = diagnosticsZip.GetEntry("viewer-diagnostics.log") is not null &&
            diagnosticsZip.GetEntry("recorder-status.json") is not null &&
            diagnosticsZip.GetEntry("telemetry.json") is null;
        return dump.Frames.Count == 2 && dump.Frames[1].Timestamp == 20
            && string.Equals(CommunityFolderDiscovery.ExtractPackagesPath(File.ReadAllText(configPath)), "D:\\MSFS Packages", StringComparison.OrdinalIgnoreCase)
            && preview?.Frames.Length == 2 && wrongSessionRejected && staleRejected && diagnosticsBundleValid;
    }
}
