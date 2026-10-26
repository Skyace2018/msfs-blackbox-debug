// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox.Decoder;

internal sealed class EventCaptureResult
{
    public Guid Id { get; init; }
    public bool Success { get; init; }
    public string? OutputPath { get; init; }
    public string? Error { get; init; }
}

internal static class EventCaptureClient
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSFS-Input-Blackbox");

    public static Guid Queue(Guid sessionId, string outputPath, string? aircraftProfile, string? aircraftType, string? aircraftSource)
    {
        var id = Guid.NewGuid();
        var directory = Path.Combine(DataDirectory, "event-requests");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, id.ToString("N") + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            Id = id, SessionId = sessionId, MarkedAtUtc = DateTimeOffset.UtcNow,
            OutputPath = outputPath, AircraftProfile = aircraftProfile, AircraftType = aircraftType,
            AircraftSource = aircraftSource
        }));
        File.WriteAllText(Path.ChangeExtension(path, ".ready"), "");
        return id;
    }

    public static EventCaptureResult? TakeResult(Guid id)
    {
        var path = Path.Combine(DataDirectory, "event-results", id.ToString("N") + ".json");
        var ready = Path.ChangeExtension(path, ".ready");
        if (!File.Exists(ready)) return null;
        var result = JsonSerializer.Deserialize<EventCaptureResult>(File.ReadAllText(path));
        File.Delete(path);
        File.Delete(ready);
        return result;
    }
}
