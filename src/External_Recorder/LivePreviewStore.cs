// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox;

// A small local display feed. The full-rate evidence remains in TelemetryRingBuffer.
internal sealed class LivePreviewStore
{
    public const string Schema = "msfs-input-blackbox/live-preview/v1";
    private const int MaxFrames = 240; // 60 seconds at the host's 4 Hz display cadence.
    private static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };
    private readonly Queue<TelemetryFrame> _frames = new();
    private readonly string _path;
    private uint? _lastTimestamp;

    public LivePreviewStore(string dataDirectory)
    {
        _path = Path.Combine(dataDirectory, "live-preview.json");
    }

    public int Count => _frames.Count;

    public void Publish(Guid sessionId, TelemetryFrame frame)
    {
        if (_lastTimestamp == frame.Timestamp) return;
        _lastTimestamp = frame.Timestamp;
        _frames.Enqueue(frame);
        while (_frames.Count > MaxFrames) _frames.Dequeue();
        var body = new { schema = Schema, sessionId, updatedAtUtc = DateTimeOffset.UtcNow, frames = _frames.ToArray() };
        var json = JsonSerializer.Serialize(body, JsonOptions);
        try
        {
            var temporaryPath = _path + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, _path, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Some restricted filesystems permit writes but not rename/replace.
                // The viewer tolerates a partially written preview and retries next tick.
                File.WriteAllText(_path, json);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { /* A display feed failure must not interrupt evidence capture. */ }
    }
}
