// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox;

// A small, local status contract consumed by the separate desktop decoder.
// It deliberately contains no flight telemetry or user paths.
internal static class MonitorStatusStore
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSFS-Input-Blackbox");
    private static readonly string StatusPath = Path.Combine(DirectoryPath, "monitor-status.json");

    public static void Report(string state, string detail, uint? lastFrameTimestamp = null)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var body = JsonSerializer.Serialize(new { State = state, Detail = detail, UpdatedAtUtc = DateTimeOffset.UtcNow, LastFrameTimestamp = lastFrameTimestamp });
            var temporaryPath = StatusPath + ".tmp";
            File.WriteAllText(temporaryPath, body);
            File.Move(temporaryPath, StatusPath, true);
        }
        catch (IOException) { /* Status reporting must never interrupt telemetry capture. */ }
        catch (UnauthorizedAccessException) { /* The UI will show an unknown/not-running state. */ }
    }
}

