// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
namespace MsfsInputBlackbox;

public sealed class TelemetryRingBuffer
{
    private readonly TelemetryFrame[] _items;
    private readonly object _gate = new();
    private int _next;
    private int _count;

    public TelemetryRingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _items = new TelemetryFrame[capacity];
    }

    public int Count { get { lock (_gate) return _count; } }

    public void Add(in TelemetryFrame frame)
    {
        lock (_gate)
        {
            _items[_next] = frame;
            _next = (_next + 1) % _items.Length;
            _count = Math.Min(_count + 1, _items.Length);
        }
    }

    public TelemetryFrame[] SnapshotOldestFirst()
    {
        lock (_gate)
        {
            var copy = new TelemetryFrame[_count];
            var first = (_next - _count + _items.Length) % _items.Length;
            for (var i = 0; i < _count; i++) copy[i] = _items[(first + i) % _items.Length];
            return copy;
        }
    }
}

