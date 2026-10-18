using TheLedger.Sim.Core;

namespace TheLedger.Sim.Water;

/// <summary>
/// A simulated strip of water surface: a 1D heightfield (Müller, "Fast Water Simulation for Games Using Height
/// Fields", GDC 2008) layered on top of the ambient waves (docs/04_PHYSICS_SPEC.md §7.1). Splashes, rain, floats,
/// fish and boats push it; it carries ripples away and absorbs them at sponge edges so nothing reflects back.
/// Up to three patches follow the angler/boat, the lure and a hooked fish.
/// </summary>
public sealed class SurfacePatch
{
    private float[] _h;
    private float[] _v;
    private float[] _foam;
    private float[] _scratch;

    public SurfacePatch(int columns, float spacing, float originX)
    {
        if (columns < 16) throw new ArgumentOutOfRangeException(nameof(columns));
        Columns = columns;
        Spacing = spacing;
        OriginX = originX;
        _h = new float[columns];
        _v = new float[columns];
        _foam = new float[columns];
        _scratch = new float[columns];
    }

    public int Columns { get; }
    /// <summary>Column spacing dx, m.</summary>
    public float Spacing { get; }
    /// <summary>World x of column 0, m. Moves in whole columns when the patch follows its target.</summary>
    public float OriginX { get; private set; }
    public float EndX => OriginX + (Columns - 1) * Spacing;
    public bool Active { get; set; } = true;

    public ReadOnlySpan<float> Heights => _h;
    public ReadOnlySpan<float> Velocities => _v;
    public ReadOnlySpan<float> Foam => _foam;

    public bool Contains(float x) => x >= OriginX && x <= EndX;

    /// <summary>Patch height at world x (linear between columns; 0 outside).</summary>
    public float HeightAt(float x)
    {
        float f = (x - OriginX) / Spacing;
        if (f < 0f || f > Columns - 1) return 0f;
        int i = Math.Min((int)f, Columns - 2);
        float t = f - i;
        return _h[i] + (_h[i + 1] - _h[i]) * t;
    }

    public float SlopeAt(float x)
    {
        float f = (x - OriginX) / Spacing;
        if (f < 0f || f > Columns - 1) return 0f;
        int i = Math.Min((int)f, Columns - 2);
        return (_h[i + 1] - _h[i]) / Spacing;
    }

    public float VelocityAt(float x)
    {
        float f = (x - OriginX) / Spacing;
        if (f < 0f || f > Columns - 1) return 0f;
        int i = Math.Min((int)f, Columns - 2);
        float t = f - i;
        return _v[i] + (_v[i + 1] - _v[i]) * t;
    }

    public float FoamAt(float x)
    {
        float f = (x - OriginX) / Spacing;
        if (f < 0f || f > Columns - 1) return 0f;
        return _foam[Math.Min((int)(f + 0.5f), Columns - 1)];
    }

    /// <summary>
    /// Pushes the surface down (positive <paramref name="impulse"/>) or up around x with a Gaussian footprint.
    /// <paramref name="impulse"/> is a vertical velocity change at the centre column, m/s.
    /// </summary>
    public void AddImpulse(float x, float impulse, float radius)
    {
        if (!Active) return;
        float r = MathF.Max(radius, Spacing);
        int lo = Math.Max(1, (int)((x - 3f * r - OriginX) / Spacing));
        int hi = Math.Min(Columns - 2, (int)((x + 3f * r - OriginX) / Spacing) + 1);
        float inv2r2 = 1f / (2f * r * r);
        // Mean-free footprint (a "Mexican hat") so the push conserves water volume: down in the middle, up around it.
        float sumW = 0f, sumRing = 0f;
        for (int i = lo; i <= hi; i++)
        {
            float d = OriginX + i * Spacing - x;
            float w = MathF.Exp(-d * d * inv2r2);
            float ring = MathF.Exp(-d * d * inv2r2 * 0.25f);
            sumW += w;
            sumRing += ring;
        }
        if (sumW <= 0f || sumRing <= 0f) return;
        float ringScale = sumW / sumRing;
        for (int i = lo; i <= hi; i++)
        {
            float d = OriginX + i * Spacing - x;
            float w = MathF.Exp(-d * d * inv2r2) - ringScale * MathF.Exp(-d * d * inv2r2 * 0.25f);
            _v[i] -= impulse * w;
        }
    }

    /// <summary>
    /// Displaces water by a submerged volume change (per metre of width, m²) at x: a float sinking in pushes the
    /// surface up around it (Müller 2008). Volume is conserved.
    /// </summary>
    public void AddDisplacement(float x, float deltaArea, float halfWidth)
    {
        if (!Active) return;
        float w = MathF.Max(halfWidth, Spacing);
        int lo = Math.Max(1, (int)((x - 2f * w - OriginX) / Spacing));
        int hi = Math.Min(Columns - 2, (int)((x + 2f * w - OriginX) / Spacing) + 1);
        int n = hi - lo + 1;
        if (n <= 0) return;
        float dh = deltaArea / (n * Spacing);
        for (int i = lo; i <= hi; i++) _h[i] += dh;
    }

    /// <summary>Moves the patch so <paramref name="targetX"/> is near its centre, shifting in whole columns.</summary>
    public void Follow(float targetX)
    {
        float centre = OriginX + 0.5f * (Columns - 1) * Spacing;
        int shift = (int)MathF.Round((targetX - centre) / Spacing);
        if (Math.Abs(shift) < Columns / 8) return;
        Shift(shift);
    }

    private void Shift(int columns)
    {
        ShiftArray(_h, columns);
        ShiftArray(_v, columns);
        ShiftArray(_foam, columns);
        OriginX += columns * Spacing;
    }

    private void ShiftArray(float[] a, int s)
    {
        int n = a.Length;
        if (Math.Abs(s) >= n)
        {
            Array.Clear(a);
            return;
        }
        if (s > 0)
        {
            Array.Copy(a, s, _scratch, 0, n - s);
            Array.Clear(_scratch, n - s, s);
        }
        else
        {
            Array.Copy(a, 0, _scratch, -s, n + s);
            Array.Clear(_scratch, 0, -s);
        }
        Array.Copy(_scratch, a, n);
    }

    /// <summary>
    /// Advances the heightfield by dt using the wave equation with damping, substepping to stay under the CFL limit.
    /// Sponge zones at both ends absorb outgoing ripples.
    /// </summary>
    public void Step(float dt, float waveSpeed, float damping, int spongeColumns, float foamGain, float foamDecay)
    {
        if (!Active) return;
        float c2 = waveSpeed * waveSpeed;
        float invDx2 = 1f / (Spacing * Spacing);
        // CFL: c·h/dx must stay below 0.5 for this explicit scheme.
        int sub = Math.Max(1, (int)MathF.Ceiling(waveSpeed * dt / (0.45f * Spacing)));
        float h = dt / sub;
        int n = Columns;
        int sponge = Math.Clamp(spongeColumns, 1, n / 3);

        for (int s = 0; s < sub; s++)
        {
            for (int i = 1; i < n - 1; i++)
            {
                float lap = (_h[i - 1] + _h[i + 1] - 2f * _h[i]) * invDx2;
                float edge = SpongeFactor(i, n, sponge);
                float damp = damping + edge * 40f;
                _v[i] = (_v[i] + h * c2 * lap) / (1f + h * damp);
            }
            for (int i = 1; i < n - 1; i++)
            {
                _h[i] += h * _v[i];
                float edge = SpongeFactor(i, n, sponge);
                if (edge > 0f) _h[i] *= 1f - MathF.Min(1f, edge * 12f * h);
            }
            _h[0] = 0f; _h[n - 1] = 0f;
            _v[0] = 0f; _v[n - 1] = 0f;
        }

        // Foam: rises where the surface is steep or moving fast, fades over time.
        float decay = MathF.Exp(-foamDecay * dt);
        for (int i = 1; i < n - 1; i++)
        {
            float slope = MathF.Abs(_h[i + 1] - _h[i - 1]) / (2f * Spacing);
            float excite = MathF.Max(0f, slope - 0.25f) + MathF.Max(0f, MathF.Abs(_v[i]) - 0.35f);
            _foam[i] = MathF.Min(1f, _foam[i] * decay + excite * foamGain * dt);
        }
    }

    private static float SpongeFactor(int i, int n, int sponge)
    {
        int d = Math.Min(i, n - 1 - i);
        if (d >= sponge) return 0f;
        float t = 1f - d / (float)sponge;
        return t * t;
    }

    /// <summary>Wave energy per metre of width (kinetic + potential), for tests.</summary>
    public double Energy(float waveSpeed)
    {
        double e = 0;
        double c2 = waveSpeed * waveSpeed;
        for (int i = 0; i < Columns - 1; i++)
        {
            double slope = (_h[i + 1] - _h[i]) / Spacing;
            e += 0.5 * _v[i] * _v[i] + 0.5 * c2 * slope * slope;
        }
        return e * Spacing;
    }

    public bool IsFinite()
    {
        for (int i = 0; i < Columns; i++)
            if (!float.IsFinite(_h[i]) || !float.IsFinite(_v[i])) return false;
        return true;
    }

    public void Reset()
    {
        Array.Clear(_h);
        Array.Clear(_v);
        Array.Clear(_foam);
    }

    public void Hash(ref StateHasher hasher)
    {
        hasher.Add(OriginX);
        hasher.Add(_h);
        hasher.Add(_v);
    }
}
