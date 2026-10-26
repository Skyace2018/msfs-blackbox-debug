// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox.Decoder;

internal sealed class LivePreviewSnapshot
{
    public string Schema { get; init; } = "";
    public Guid SessionId { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public TelemetrySample[] Frames { get; init; } = [];
}

internal static class LivePreviewReader
{
    private const string ExpectedSchema = "msfs-input-blackbox/live-preview/v1";
    private static readonly string PreviewPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MSFS-Input-Blackbox", "live-preview.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static LivePreviewSnapshot? Read(Guid? expectedSessionId) => ReadFromPath(PreviewPath, expectedSessionId);

    internal static LivePreviewSnapshot? ReadFromPath(string path, Guid? expectedSessionId)
    {
        if (expectedSessionId is null) return null;
        try
        {
            if (!File.Exists(path)) return null;
            var preview = JsonSerializer.Deserialize<LivePreviewSnapshot>(File.ReadAllText(path), JsonOptions);
            var age = preview is null ? TimeSpan.MaxValue : DateTimeOffset.UtcNow - preview.UpdatedAtUtc;
            if (preview is null || preview.Schema != ExpectedSchema || preview.SessionId != expectedSessionId ||
                preview.Frames is null || preview.Frames.Length is 0 or > 240 ||
                age < TimeSpan.FromSeconds(-1) || age > TimeSpan.FromSeconds(2))
                return null;
            if (preview.Frames.Any(frame => !float.IsFinite(frame.YokeX) || !float.IsFinite(frame.YokeY) ||
                !float.IsFinite(frame.Rudder) || !float.IsFinite(frame.Throttle))) return null;
            return preview;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { return null; }
    }
}
