// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
namespace MsfsInputBlackbox;

// File I/O is kept off the SimConnect message-loop thread. Export work and
// preview/status writes have separate workers so a slow ZIP does not stale the heartbeat.
internal sealed class RecorderIoWorkers : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _exports;
    private readonly Task _previewAndStatus;
    private Action? _pendingStatus;
    private int _disposed;

    public RecorderIoWorkers(Action exportTick, Action previewTick, Action<Exception>? onError = null)
    {
        _exports = Task.Run(() => RunLoop(exportTick, onError, _stop.Token));
        _previewAndStatus = Task.Run(() => RunLoop(() =>
        {
            Interlocked.Exchange(ref _pendingStatus, null)?.Invoke();
            previewTick();
        }, onError, _stop.Token));
    }

    public void PostStatus(Action publish)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        Interlocked.Exchange(ref _pendingStatus, publish);
    }

    private static async Task RunLoop(Action tick, Action<Exception>? onError, CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try { tick(); }
            catch (Exception ex)
            {
                // A failed preview/export must not terminate the receive loop.
                try { onError?.Invoke(ex); } catch { /* Keep this worker alive. */ }
            }
            try { await Task.Delay(250, stop).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _stop.Cancel();
        Task.WaitAll(_exports, _previewAndStatus);
        _stop.Dispose();
    }
}
