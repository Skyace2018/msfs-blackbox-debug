// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.IO.Compression;
using System.Text.Json;

namespace MsfsInputBlackbox.Decoder;

internal static class DeveloperExport
{
    public static void CreateBundle(string destinationPath, TelemetryDump dump, IReadOnlyList<TelemetrySample> frames)
    {
        if (frames.Count == 0) throw new InvalidDataException("There are no frames to export.");
        using var archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create);
        WriteJson(archive, "telemetry.json", new { schema = dump.Schema, reason = dump.Reason, capturedAtUtc = dump.CapturedAtUtc, frames });
        WriteJson(archive, "manifest.json", new
        {
            schema = "msfs-input-blackbox/developer-export/v1",
            exportedAtUtc = DateTimeOffset.UtcNow,
            frameCount = frames.Count,
            firstTimestamp = frames[0].Timestamp,
            lastTimestamp = frames[^1].Timestamp,
            privacy = "Contains selected telemetry and path-free viewer diagnostics; local Community paths, settings, and unrelated files are excluded."
        });
        WriteDiagnostics(archive);
        using var notes = new StreamWriter(archive.CreateEntry("README.txt").Open());
        notes.WriteLine("MSFS Input Blackbox developer export");
        notes.WriteLine("This archive contains selected control telemetry, viewer diagnostics, and a machine-readable manifest.");
        notes.WriteLine("It does not include Community-folder paths, account information, or simulator files.");
    }

    public static void CreateDiagnosticsBundle(string destinationPath, MonitorStatus status)
    {
        using var archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create);
        WriteJson(archive, "manifest.json", new
        {
            schema = "msfs-input-blackbox/developer-export/v1",
            exportedAtUtc = DateTimeOffset.UtcNow,
            includesTelemetry = false,
            privacy = "Contains viewer diagnostics and path-free recorder status; no Community paths or settings."
        });
        WriteJson(archive, "recorder-status.json", new
        {
            status.State, status.UpdatedAtUtc, status.SessionId, status.LastFrameTimestamp,
            status.BufferedFrames, status.BufferCapacity
        });
        WriteDiagnostics(archive);
        using var notes = new StreamWriter(archive.CreateEntry("README.txt").Open());
        notes.WriteLine("MSFS Input Blackbox diagnostics-only export. No telemetry dump was open.");
    }

    private static void WriteDiagnostics(ZipArchive archive)
    {
        using var writer = new StreamWriter(archive.CreateEntry("viewer-diagnostics.log").Open());
        writer.Write(DiagnosticLog.ReadForExport());
    }

    public static void WriteSnapshot(string destinationPath, TelemetryDump dump, TelemetrySample frame)
    {
        File.WriteAllText(destinationPath, JsonSerializer.Serialize(new
        {
            schema = "msfs-input-blackbox/snapshot/v1",
            sourceSchema = dump.Schema,
            reason = dump.Reason,
            capturedAtUtc = dump.CapturedAtUtc,
            snapshot = frame
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void WriteJson(ZipArchive archive, string name, object value)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open());
        writer.Write(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }
}
