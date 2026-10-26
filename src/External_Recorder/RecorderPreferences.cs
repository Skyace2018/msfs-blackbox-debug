// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox;

internal sealed class RecorderPreferences
{
    public int HistorySeconds { get; private init; } = 60;
    public int SampleRateHz { get; private init; } = 30;
    public bool LoggingEnabled { get; private init; } = true;
    public bool KeepCrashDumps { get; private init; } = true;

    public static RecorderPreferences Load(string dataDirectory)
    {
        try
        {
            var path = Path.Combine(dataDirectory, "settings.json");
            if (!File.Exists(path)) return new RecorderPreferences();
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            var root = json.RootElement;
            var seconds = root.TryGetProperty("HistorySeconds", out var history) && history.TryGetInt32(out var value)
                ? Math.Clamp(value, 60, 3600) : 60;
            var rate = root.TryGetProperty("SampleRateHz", out var sample) && sample.TryGetInt32(out var hertz)
                ? Math.Clamp(hertz, 15, 60) : 30;
            return new RecorderPreferences
            {
                HistorySeconds = seconds, SampleRateHz = rate,
                LoggingEnabled = root.TryGetProperty("LoggingEnabled", out var logging) ? logging.GetBoolean() : true,
                KeepCrashDumps = root.TryGetProperty("KeepCrashDumps", out var dumps) ? dumps.GetBoolean() : true
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        { return new RecorderPreferences(); }
    }
}
