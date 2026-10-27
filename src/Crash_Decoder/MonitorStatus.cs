// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox.Decoder;

internal sealed class MonitorStatus
{
    public string State { get; init; } = "not-running";
    public string Detail { get; init; } = "The external recorder has not published status.";
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public uint? LastFrameTimestamp { get; init; }
    public int? BufferedFrames { get; init; }
    public int? BufferCapacity { get; init; }
    public bool IsPurging { get; init; }
    public Guid? SessionId { get; init; }
    public string? AircraftType { get; init; }
    public TelemetrySample? LatestInput { get; init; }
    public DateTimeOffset? LastFrameReceivedAtUtc { get; init; }
    public double? IndicatedAirspeedKnots { get; init; }
    public double? AltitudeFeet { get; init; }
    public RecorderPerformanceSnapshot? Performance { get; init; }
}

internal sealed class RecorderPerformanceSnapshot
{
    public long ReceivedFrames { get; init; }
    public long AcceptedFrames { get; init; }
    public long RateLimitedFrames { get; init; }
    public long EstimatedSamplingGaps { get; init; }
    public double MaxInputHandlerMs { get; init; }
}

internal static class MonitorStatusReader
{
    private static readonly string StatusPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSFS-Input-Blackbox", "monitor-status.json");

    public static MonitorStatus Read()
    {
        try
        {
            if (!File.Exists(StatusPath)) return new MonitorStatus();
            var status = JsonSerializer.Deserialize<MonitorStatus>(File.ReadAllText(StatusPath));
            if (status is null) return new MonitorStatus { State = "unknown", Detail = "The status file is empty or invalid." };
            if ((status.State is "connected" or "connecting") && DateTimeOffset.UtcNow - status.UpdatedAtUtc > TimeSpan.FromSeconds(6))
                return new MonitorStatus { State = "unknown", Detail = "Recorder heartbeat expired; live session cannot be confirmed." };
            if (status.State == "connected" && (status.LastFrameReceivedAtUtc is not { } lastFrame ||
                DateTimeOffset.UtcNow - lastFrame > TimeSpan.FromSeconds(3)))
                return new MonitorStatus { State = "unknown", Detail = "Telemetry frames have stopped; live input cannot be confirmed." };
            return status;
        }
        catch (IOException) { return new MonitorStatus { State = "unknown", Detail = "Recorder status is being updated." }; }
        catch (UnauthorizedAccessException) { return new MonitorStatus { State = "unknown", Detail = "Recorder status is not readable." }; }
        catch (JsonException) { return new MonitorStatus { State = "unknown", Detail = "Recorder status cannot be decoded." }; }
    }
}
