namespace HectorBravo.Gadgets.Core;

/// <summary>Fixed-capacity ring of doubles — O(1) append, no List.RemoveAt(0) churn.</summary>
public sealed class Ring
{
    private readonly double[] _buf;
    private int _head;      // index of the oldest sample
    private int _count;

    public int Capacity => _buf.Length;
    public int Count => _count;

    public Ring(int capacity)
    {
        if (capacity < 2) capacity = 2;
        _buf = new double[capacity];
    }

    public void Add(double v)
    {
        if (_count < _buf.Length)
        {
            _buf[(_head + _count) % _buf.Length] = v;
            _count++;
        }
        else
        {
            _buf[_head] = v;                 // overwrite oldest
            _head = (_head + 1) % _buf.Length;
        }
    }

    /// <summary>Oldest→newest sample at index i.</summary>
    public double this[int i] => _buf[(_head + i) % _buf.Length];

    /// <summary>Copy oldest→newest into a fresh array (for plotting).</summary>
    public double[] Snapshot()
    {
        var a = new double[_count];
        for (int i = 0; i < _count; i++) a[i] = this[i];
        return a;
    }

    public void Clear() { _head = 0; _count = 0; }
}
