// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
namespace MsfsInputBlackbox;

public sealed class TelemetryRingBuffer
{
    private readonly TelemetryFrame[] _items;
    private readonly object _gate = new();
    private int _next;
    private int _count;
    private bool _capacityWarningRaised;

    public event Action<int, int>? CapacityWarning;

    public TelemetryRingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _items = new TelemetryFrame[capacity];
    }

    public int Count { get { lock (_gate) return _count; } }
    public int Capacity => _items.Length;
    public double UsageFraction { get { lock (_gate) return _count / (double)_items.Length; } }

    public void Add(in TelemetryFrame frame)
    {
        var raiseWarning = false;
        lock (_gate)
        {
            _items[_next] = frame;
            _next = (_next + 1) % _items.Length;
            _count = Math.Min(_count + 1, _items.Length);
            if (!_capacityWarningRaised && _count >= Math.Ceiling(_items.Length * 0.9))
            {
                _capacityWarningRaised = true;
                raiseWarning = true;
            }
        }
        if (raiseWarning) CapacityWarning?.Invoke(Count, Capacity);
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

