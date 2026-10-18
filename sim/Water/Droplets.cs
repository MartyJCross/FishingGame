using TheLedger.Sim.Core;

namespace TheLedger.Sim.Water;

/// <summary>
/// Splash droplets (docs/04_PHYSICS_SPEC.md §7.2): cheap ballistic particles thrown by impacts. When one falls back
/// it taps the surface patch with a tiny impulse. Fixed capacity; when full, new droplets are skipped.
/// The renderer draws them; the count is bounded so they never cost much.
/// </summary>
public sealed class Droplets
{
    private readonly float[] _x, _y, _vx, _vy, _size;
    private int _count;

    public Droplets(int capacity = 768)
    {
        _x = new float[capacity];
        _y = new float[capacity];
        _vx = new float[capacity];
        _vy = new float[capacity];
        _size = new float[capacity];
    }

    public int Count => _count;
    public int Capacity => _x.Length;
    public ReadOnlySpan<float> X => _x.AsSpan(0, _count);
    public ReadOnlySpan<float> Y => _y.AsSpan(0, _count);
    public ReadOnlySpan<float> VX => _vx.AsSpan(0, _count);
    public ReadOnlySpan<float> VY => _vy.AsSpan(0, _count);
    public ReadOnlySpan<float> Size => _size.AsSpan(0, _count);

    public void Spawn(float x, float y, float vx, float vy, float size)
    {
        if (_count >= _x.Length) return;
        _x[_count] = x;
        _y[_count] = y;
        _vx[_count] = vx;
        _vy[_count] = vy;
        _size[_count] = size;
        _count++;
    }

    /// <summary>Moves droplets; ones that fall back into the water tap the surface and are removed.</summary>
    public void Step(float dt, float gravity, WaterSurface water, double time, float tapImpulse)
    {
        int i = 0;
        while (i < _count)
        {
            _vy[i] -= gravity * dt;
            _vx[i] *= 1f - 0.4f * dt; // light air drag
            _x[i] += _vx[i] * dt;
            _y[i] += _vy[i] * dt;
            if (_vy[i] < 0f && _y[i] <= water.Height(_x[i], time))
            {
                water.ImpulseAt(_x[i], tapImpulse * _size[i] * MathF.Min(1f, -_vy[i] / 3f), 0.03f);
                RemoveAt(i);
                continue;
            }
            i++;
        }
    }

    private void RemoveAt(int i)
    {
        int last = _count - 1;
        _x[i] = _x[last];
        _y[i] = _y[last];
        _vx[i] = _vx[last];
        _vy[i] = _vy[last];
        _size[i] = _size[last];
        _count--;
    }

    public void Clear() => _count = 0;

    public void Hash(ref StateHasher h)
    {
        h.Add(_count);
        h.Add(X);
        h.Add(Y);
    }
}
