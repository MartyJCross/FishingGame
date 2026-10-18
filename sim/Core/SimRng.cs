namespace TheLedger.Sim.Core;

/// <summary>
/// Deterministic random numbers for the sim (PCG32, O'Neill 2014). Never use System.Random in sim code:
/// its algorithm isn't guaranteed stable across runtimes. Each subsystem owns its own SimRng, seeded from the
/// world seed and a stream id, so adding random draws in one system doesn't change another.
/// </summary>
public struct SimRng
{
    private ulong _state;
    private readonly ulong _inc;

    public SimRng(ulong seed, ulong stream = 1)
    {
        _state = 0;
        _inc = (stream << 1) | 1UL;
        NextUInt();
        _state += seed;
        NextUInt();
    }

    public uint NextUInt()
    {
        ulong old = _state;
        _state = old * 6364136223846793005UL + _inc;
        uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
    }

    /// <summary>Uniform float in [0, 1).</summary>
    public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

    /// <summary>Uniform float in [min, max).</summary>
    public float Range(float min, float max) => min + (max - min) * NextFloat();

    /// <summary>Uniform int in [min, maxExclusive).</summary>
    public int RangeInt(int min, int maxExclusive)
    {
        if (maxExclusive <= min) return min;
        uint span = (uint)(maxExclusive - min);
        // Lemire-style rejection keeps the distribution unbiased.
        uint threshold = (uint)(-span) % span;
        while (true)
        {
            uint r = NextUInt();
            if (r >= threshold) return min + (int)(r % span);
        }
    }

    /// <summary>True with probability p.</summary>
    public bool Chance(float p) => NextFloat() < p;

    /// <summary>Standard normal sample (Box-Muller).</summary>
    public float NextGaussian()
    {
        float u1 = MathF.Max(NextFloat(), 1e-7f);
        float u2 = NextFloat();
        return MathF.Sqrt(-2f * MathF.Log(u1)) * MathF.Cos(2f * MathF.PI * u2);
    }

    /// <summary>Mixes a seed and a stream id into a well-spread 64-bit seed (SplitMix64).</summary>
    public static ulong Mix(ulong seed, ulong stream)
    {
        ulong z = seed + stream * 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
