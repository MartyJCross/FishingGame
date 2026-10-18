namespace TheLedger.Sim.Core;

/// <summary>
/// FNV-1a 64-bit hash over raw float bits, for determinism checks (docs/02_TECH_STACK.md §3.4).
/// Hashing the bits (not rounded values) means any drift at all is caught.
/// </summary>
public struct StateHasher
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    private ulong _hash;
    private bool _started;

    public ulong Value => _started ? _hash : Offset;

    public void Add(ulong value)
    {
        if (!_started)
        {
            _hash = Offset;
            _started = true;
        }
        for (int i = 0; i < 8; i++)
        {
            _hash ^= (value >> (i * 8)) & 0xFF;
            _hash *= Prime;
        }
    }

    public void Add(int value) => Add((ulong)(uint)value);
    public void Add(long value) => Add((ulong)value);
    public void Add(float value) => Add((ulong)(uint)BitConverter.SingleToInt32Bits(value));
    public void Add(double value) => Add((ulong)BitConverter.DoubleToInt64Bits(value));

    public void Add(Vec2 v)
    {
        Add(v.X);
        Add(v.Y);
    }

    public void Add(ReadOnlySpan<float> values)
    {
        foreach (float f in values) Add(f);
    }
}
