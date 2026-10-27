// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Diagnostics;

namespace MsfsInputBlackbox;

internal sealed record RecorderPerformanceSnapshot(
    long ReceivedFrames, long AcceptedFrames, long RateLimitedFrames,
    long EstimatedSamplingGaps, double MaxInputHandlerMs);

// In-memory counters only. The status publisher samples these at its existing 2-second cadence.
internal sealed class RecorderPerformance
{
    private readonly int _expectedPeriodMs;
    private long _received;
    private long _accepted;
    private long _rateLimited;
    private long _estimatedGaps;
    private long _maxHandlerTicks;
    private uint? _lastReceivedTimestamp;

    public RecorderPerformance(int acceptedSampleRateHz)
    {
        _expectedPeriodMs = Math.Max(1, 1000 / acceptedSampleRateHz);
    }

    public void Received(uint timestamp)
    {
        Interlocked.Increment(ref _received);
        if (_lastReceivedTimestamp is { } previous)
        {
            var elapsed = unchecked(timestamp - previous);
            // A timestamp interval is a sampling-gap estimate, not proof of a dropped packet.
            // Ignore pauses and timestamp resets longer than 30 seconds.
            if (elapsed > 2 * _expectedPeriodMs && elapsed <= 30_000)
                Interlocked.Add(ref _estimatedGaps, Math.Max(0, (long)elapsed / _expectedPeriodMs - 1));
        }
        _lastReceivedTimestamp = timestamp;
    }

    public void Accepted() => Interlocked.Increment(ref _accepted);
    public void RateLimited() => Interlocked.Increment(ref _rateLimited);

    public void HandlerFinished(long startedAtTicks)
    {
        var elapsed = Stopwatch.GetTimestamp() - startedAtTicks;
        long previous;
        do
        {
            previous = Interlocked.Read(ref _maxHandlerTicks);
            if (elapsed <= previous) return;
        } while (Interlocked.CompareExchange(ref _maxHandlerTicks, elapsed, previous) != previous);
    }

    public RecorderPerformanceSnapshot Snapshot() => new(
        Interlocked.Read(ref _received), Interlocked.Read(ref _accepted),
        Interlocked.Read(ref _rateLimited), Interlocked.Read(ref _estimatedGaps),
        Math.Round(Interlocked.Read(ref _maxHandlerTicks) * 1000.0 / Stopwatch.Frequency, 4));
}
