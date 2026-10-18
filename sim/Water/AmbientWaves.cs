using TheLedger.Sim.Core;

namespace TheLedger.Sim.Water;

/// <summary>
/// Analytic background waves everywhere on the surface (docs/04_PHYSICS_SPEC.md §7.1): a handful of wave trains
/// sized from wind speed and fetch, plus optional swell. Real numbers: wind sea height and period follow the
/// fetch-limited JONSWAP growth relations, capped at a fully developed Pierson-Moskowitz sea, so a 60 m pond in a
/// 5 m/s breeze gets ~2 cm ripples with ~0.4 s period and open water gets proper waves.
/// </summary>
public sealed class AmbientWaves
{
    public const int MaxTrains = 8;

    private readonly float[] _amp = new float[MaxTrains];
    private readonly float[] _k = new float[MaxTrains];      // wavenumber, rad/m (signed: + travels toward +x)
    private readonly float[] _omega = new float[MaxTrains];  // angular frequency, rad/s
    private readonly float[] _phase = new float[MaxTrains];
    private int _count;

    /// <summary>Significant wave height of the wind sea, m.</summary>
    public float WindSeaHs { get; private set; }
    /// <summary>Peak period of the wind sea, s.</summary>
    public float WindSeaTp { get; private set; }
    public int TrainCount => _count;

    /// <summary>
    /// Rebuilds the wave trains. <paramref name="windSpeed"/> m/s at 10 m, <paramref name="windDir"/> +1 blows toward
    /// +x, −1 toward −x. <paramref name="fetch"/> m of open water upwind. <paramref name="depth"/> m (for dispersion).
    /// </summary>
    public void Configure(float windSpeed, float windDir, float fetch, float depth, float swellHeight, float swellPeriod,
        float swellDir, ulong seed, float gravity = 9.81f)
    {
        var rng = new SimRng(SimRng.Mix(seed, 0xA1B1));
        _count = 0;
        float g = gravity;
        float u = MathF.Max(windSpeed, 0f);

        WindSeaHs = 0f;
        WindSeaTp = 0f;
        if (u > 0.3f)
        {
            float u2 = u * u;
            float fetchless = g * MathF.Max(fetch, 1f) / u2;                       // dimensionless fetch
            float hsFetch = 1.6e-3f * MathF.Sqrt(fetchless) * u2 / g;              // JONSWAP fetch-limited height
            float hsFull = 0.21f * u2 / g;                                          // fully developed (PM)
            float tpFetch = 0.286f * MathF.Cbrt(fetchless) * u / g;                // JONSWAP fetch-limited period
            float tpFull = 2f * MathF.PI * u / (0.877f * g);                       // PM peak period
            WindSeaHs = MathF.Min(hsFetch, hsFull);
            WindSeaTp = MathF.Min(tpFetch, tpFull);

            // Five trains spread around the peak frequency, energy weighted toward the peak.
            ReadOnlySpan<float> freqMul = stackalloc float[] { 0.75f, 0.9f, 1.0f, 1.3f, 1.8f };
            ReadOnlySpan<float> weight = stackalloc float[] { 0.5f, 1.0f, 1.0f, 0.55f, 0.25f };
            float sumW = 0f;
            foreach (float w in weight) sumW += w;
            // Significant height Hs = 4·sqrt(m0), and for sinusoids m0 = Σ a²/2.
            float m0 = WindSeaHs * WindSeaHs / 16f;
            float omegaP = 2f * MathF.PI / MathF.Max(WindSeaTp, 0.05f);
            for (int i = 0; i < freqMul.Length; i++)
            {
                float omega = omegaP * freqMul[i];
                float a = MathF.Sqrt(2f * m0 * weight[i] / sumW);
                // Mostly downwind; the shortest trains get some reverse spread.
                float dir = windDir >= 0 ? 1f : -1f;
                if (i >= 3 && rng.Chance(0.35f)) dir = -dir;
                Add(a, omega, dir, depth, g, rng.Range(0f, 2f * MathF.PI));
            }
        }

        if (swellHeight > 0.001f && swellPeriod > 0.5f)
        {
            float omega = 2f * MathF.PI / swellPeriod;
            float a = swellHeight * 0.5f;
            float dir = swellDir >= 0 ? 1f : -1f;
            Add(a * 0.8f, omega, dir, depth, g, rng.Range(0f, 2f * MathF.PI));
            Add(a * 0.45f, omega * 1.12f, dir, depth, g, rng.Range(0f, 2f * MathF.PI));
        }
    }

    private void Add(float amp, float omega, float dir, float depth, float g, float phase)
    {
        if (_count >= MaxTrains) return;
        _amp[_count] = amp;
        _omega[_count] = omega;
        _k[_count] = dir * WaveNumber(omega, depth, g);
        _phase[_count] = phase;
        _count++;
    }

    /// <summary>Solves the dispersion relation ω² = g·k·tanh(k·h) for k (Newton iterations).</summary>
    public static float WaveNumber(float omega, float depth, float g = 9.81f)
    {
        float k = omega * omega / g; // deep-water guess
        if (depth <= 0f || k * depth > 6f) return k;
        for (int i = 0; i < 12; i++)
        {
            float th = MathF.Tanh(k * depth);
            float f = g * k * th - omega * omega;
            float df = g * th + g * k * depth * (1f - th * th);
            k -= f / df;
        }
        return k;
    }

    /// <summary>Surface elevation at x, time t (m).</summary>
    public float Height(float x, double t)
    {
        float h = 0f;
        for (int i = 0; i < _count; i++)
            h += _amp[i] * MathF.Cos(_k[i] * x - (float)(_omega[i] * t % (2.0 * Math.PI)) + _phase[i]);
        return h;
    }

    /// <summary>Surface slope dh/dx at x, time t.</summary>
    public float Slope(float x, double t)
    {
        float s = 0f;
        for (int i = 0; i < _count; i++)
            s -= _amp[i] * _k[i] * MathF.Sin(_k[i] * x - (float)(_omega[i] * t % (2.0 * Math.PI)) + _phase[i]);
        return s;
    }

    /// <summary>Vertical surface velocity dh/dt at x, time t.</summary>
    public float VerticalVelocity(float x, double t)
    {
        float w = 0f;
        for (int i = 0; i < _count; i++)
            w += _amp[i] * _omega[i] * MathF.Sin(_k[i] * x - (float)(_omega[i] * t % (2.0 * Math.PI)) + _phase[i]);
        return w;
    }

    /// <summary>
    /// Water particle velocity below the waves at depth <paramref name="belowSurface"/> (m, ≥ 0), deep-water linear
    /// theory: orbits shrink as e^(−k·depth). Floats bob and lures surge with this.
    /// </summary>
    public Vec2 OrbitalVelocity(float x, float belowSurface, double t)
    {
        float u = 0f, w = 0f;
        float d = MathF.Max(belowSurface, 0f);
        for (int i = 0; i < _count; i++)
        {
            float k = MathF.Abs(_k[i]);
            float decay = MathF.Exp(-k * d);
            float theta = _k[i] * x - (float)(_omega[i] * t % (2.0 * Math.PI)) + _phase[i];
            float speed = _amp[i] * _omega[i] * decay;
            u += MathF.Sign(_k[i]) * speed * MathF.Cos(theta);
            w += speed * MathF.Sin(theta);
        }
        return new Vec2(u, w);
    }

    /// <summary>Horizontal Gerstner offset for rendering sharper crests (not used by physics).</summary>
    public float GerstnerOffset(float x, double t, float steepness)
    {
        float dx = 0f;
        for (int i = 0; i < _count; i++)
            dx -= steepness * MathF.Sign(_k[i]) * _amp[i] *
                  MathF.Sin(_k[i] * x - (float)(_omega[i] * t % (2.0 * Math.PI)) + _phase[i]);
        return dx;
    }
}
