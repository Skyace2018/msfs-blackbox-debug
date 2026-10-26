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
            privacy = "Contains the selected telemetry only; local Community paths, settings, and unrelated files are excluded."
        });
        using var notes = new StreamWriter(archive.CreateEntry("README.txt").Open());
        notes.WriteLine("MSFS Input Blackbox developer export");
        notes.WriteLine("This archive contains selected control telemetry and a machine-readable manifest.");
        notes.WriteLine("It does not include Community-folder paths, account information, or simulator files.");
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

