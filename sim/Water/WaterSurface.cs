using TheLedger.Sim.Core;

namespace TheLedger.Sim.Water;

/// <summary>Wind, swell and rain acting on a spot's water. Set by the spot and (later) the weather system.</summary>
public struct WaterConditions
{
    /// <summary>Wind speed at 10 m, m/s.</summary>
    public float WindSpeed;
    /// <summary>+1 blows toward +x, −1 toward −x.</summary>
    public float WindDir;
    /// <summary>Open water upwind, m (a pond ~60, a lake ~2000, open sea 100000).</summary>
    public float Fetch;
    /// <summary>Typical depth for wave dispersion, m.</summary>
    public float Depth;
    public float SwellHeight;
    public float SwellPeriod;
    public float SwellDir;
    /// <summary>Rain rate, mm/h (light 1, moderate 4, heavy 10+, violent 50+).</summary>
    public float RainRate;
    /// <summary>Water density, kg/m³ (fresh 1000, brackish ~1010, sea ~1025).</summary>
    public float Density;

    public static WaterConditions CalmPond => new()
    {
        WindSpeed = 2f, WindDir = 1f, Fetch = 60f, Depth = 3f, SwellPeriod = 8f, SwellDir = 1f, Density = 1000f,
    };

    public readonly bool SameWaves(in WaterConditions o) =>
        WindSpeed == o.WindSpeed && WindDir == o.WindDir && Fetch == o.Fetch && Depth == o.Depth &&
        SwellHeight == o.SwellHeight && SwellPeriod == o.SwellPeriod && SwellDir == o.SwellDir;
}

/// <summary>
/// The water surface of the current spot: tide level + ambient waves + up to three simulated patches + droplets
/// (docs/04_PHYSICS_SPEC.md §7). Rule: if something touches the water in the sim, the water responds.
/// </summary>
public sealed class WaterSurface
{
    public const int MaxPatches = 3;

    private readonly SurfacePatch?[] _patches = new SurfacePatch?[MaxPatches];
    private WaterConditions _configured;
    private bool _hasConfigured;
    private float _rainCarry;

    public WaterSurface(ulong seed)
    {
        Seed = seed;
        Rng = new SimRng(SimRng.Mix(seed, 0x57A7E5));
    }

    public ulong Seed { get; }
    public SimRng Rng;

    public AmbientWaves Ambient { get; } = new();
    public Droplets Droplets { get; } = new();

    /// <summary>Recent points where something touched the surface, for the renderer's ripple rings.</summary>
    public ImpactLog Impacts { get; } = new();
    public WaterConditions Conditions;

    /// <summary>Still-water level from the tide, m (0 = mean level for the spot).</summary>
    public float TideLevel;

    public float Density => Conditions.Density > 0f ? Conditions.Density : 1000f;

    /// <summary>Sim time of the last Step, s.</summary>
    public double Time { get; private set; }

    public SurfacePatch? Patch(int i) => _patches[i];

    /// <summary>Creates (or replaces) patch slot <paramref name="slot"/> centred on x. Allocates: call at setup only.</summary>
    public SurfacePatch CreatePatch(int slot, int columns, float spacing, float centreX)
    {
        var p = new SurfacePatch(columns, spacing, centreX - 0.5f * (columns - 1) * spacing);
        _patches[slot] = p;
        return p;
    }

    private SurfacePatch? PatchAt(float x)
    {
        for (int i = 0; i < MaxPatches; i++)
        {
            var p = _patches[i];
            if (p != null && p.Active && p.Contains(x)) return p;
        }
        return null;
    }

    /// <summary>Surface elevation at x (m, world y).</summary>
    public float Height(float x, double t) => TideLevel + Ambient.Height(x, t) + (PatchAt(x)?.HeightAt(x) ?? 0f);

    public float Slope(float x, double t) => Ambient.Slope(x, t) + (PatchAt(x)?.SlopeAt(x) ?? 0f);

    public float VerticalVelocity(float x, double t) => Ambient.VerticalVelocity(x, t) + (PatchAt(x)?.VelocityAt(x) ?? 0f);

    public float FoamAt(float x) => PatchAt(x)?.FoamAt(x) ?? 0f;

    /// <summary>Water velocity from waves at a point below the surface (currents are added by the flow field later).</summary>
    public Vec2 WaveVelocity(float x, float y, double t)
    {
        float below = Height(x, t) - y;
        return Ambient.OrbitalVelocity(x, below, t);
    }

    /// <summary>Raw impulse into whichever patch covers x (no droplets, no event). Logged for ripple rings.</summary>
    public void ImpulseAt(float x, float impulse, float radius)
    {
        var p = PatchAt(x);
        if (p == null) return;
        p.AddImpulse(x, impulse, radius);
        Impacts.Add(x, Time, impulse, radius);
    }

    /// <summary>Displacement into whichever patch covers x (floating bodies, boats).</summary>
    /// <summary>
    /// A body moving at or just under the surface pushes the water every tick: down where it moves down, and a bow
    /// wave and trough from moving along (wakes, a bobbing float, a fish boiling near the top). <paramref name="near"/>
    /// is 1 at the surface, 0 once it's deep enough not to disturb it. <paramref name="ring"/> also logs a ripple
    /// ring (callers throttle it).
    /// </summary>
    public void Disturb(SimConfig c, float x, float vx, float vy, float radius, float near, bool ring)
    {
        if (near <= 0f) return;
        var p = PatchAt(x);
        if (p == null) return;
        float size = MathF.Min(3f, MathF.Max(radius, 0.005f) / 0.015f);
        float push = c.WakeGain * near * MathF.Sqrt(size) * (0.8f * MathF.Abs(vx) + MathF.Max(0f, -vy) - 0.4f * MathF.Max(0f, vy));
        if (MathF.Abs(push) < 1e-5f) return;
        float r = MathF.Max(radius * 1.5f, 0.03f);
        p.AddImpulse(x, push, r);
        // The bow wave runs ahead of the body.
        if (MathF.Abs(vx) > 0.15f) p.AddImpulse(x + MathF.Sign(vx) * r * 2f, -0.5f * push, r);
        if (ring) Impacts.Add(x, Time, MathF.Min(0.6f, push * 6f), r);
    }

    public void DisplaceAt(float x, float deltaArea, float halfWidth) => PatchAt(x)?.AddDisplacement(x, deltaArea, halfWidth);

    /// <summary>
    /// Something hits the water at x: pushes the surface and throws droplets in proportion to the impact.
    /// <paramref name="speed"/> m/s downward, <paramref name="mass"/> kg, <paramref name="radius"/> m.
    /// Returns the impact energy (J), which callers use for sound and for spooking fish.
    /// </summary>
    public float Splash(SimConfig c, float x, float speed, float mass, float radius, float horizontalSpeed = 0f)
    {
        float energy = 0.5f * mass * (speed * speed + horizontalSpeed * horizontalSpeed);
        // Impulse grows with momentum but saturates: a dropped boulder doesn't make an infinite hole.
        float momentum = mass * speed;
        float impulse = c.SplashImpulseScale * 3f * MathF.Tanh(momentum / 0.15f) * MathF.Min(1f, speed / 2f + 0.2f);
        ImpulseAt(x, impulse, MathF.Max(radius, 0.03f));

        // Small fast bodies still throw a visible crown of spray.
        int drops = Math.Min(64, (int)(c.DropletsPerJoule * energy + 120f * MathF.Max(radius, 0.005f) * speed));
        float y = Height(x, Time);
        for (int i = 0; i < drops; i++)
        {
            float ang = Rng.Range(0.25f, MathF.PI - 0.25f);
            float v = Rng.Range(0.4f, 1.0f) * MathF.Min(4.5f, 0.6f + 0.35f * speed);
            Droplets.Spawn(x + Rng.Range(-radius, radius), y, MathF.Cos(ang) * v + 0.2f * horizontalSpeed,
                MathF.Sin(ang) * v, Rng.Range(0.3f, 1f));
        }
        return energy;
    }

    public void Step(SimConfig c, float dt, double time)
    {
        Time = time;
        if (!_hasConfigured || !_configured.SameWaves(Conditions))
        {
            Ambient.Configure(Conditions.WindSpeed, Conditions.WindDir, Conditions.Fetch, Conditions.Depth,
                Conditions.SwellHeight, Conditions.SwellPeriod, Conditions.SwellDir, Seed, c.Gravity);
            _configured = Conditions;
            _hasConfigured = true;
        }

        StepRain(c, dt, time);

        for (int i = 0; i < MaxPatches; i++)
            _patches[i]?.Step(dt, c.RippleSpeed, c.RippleDamping, c.SpongeColumns, c.FoamGain, c.FoamDecay);

        Droplets.Step(dt, c.Gravity, this, time, c.DropletTapImpulse);
    }

    private void StepRain(SimConfig c, float dt, double time)
    {
        if (Conditions.RainRate <= 0f) return;
        for (int p = 0; p < MaxPatches; p++)
        {
            var patch = _patches[p];
            if (patch == null || !patch.Active) continue;
            float width = patch.EndX - patch.OriginX;
            // Drops per tick: a representative count, not every real raindrop.
            _rainCarry += Conditions.RainRate * c.RainDropsPerMmPerMetre * width * dt;
            int n = (int)_rainCarry;
            _rainCarry -= n;
            for (int i = 0; i < n; i++)
            {
                float x = Rng.Range(patch.OriginX, patch.EndX);
                float size = Rng.Range(0.5f, 1f) * MathF.Min(1.5f, 0.6f + Conditions.RainRate / 25f);
                patch.AddImpulse(x, c.RainDropImpulse * size, 0.02f);
                Impacts.Add(x, time, c.RainDropImpulse * size, 0.02f);
            }
        }
    }

    public bool IsFinite()
    {
        for (int i = 0; i < MaxPatches; i++)
            if (_patches[i] is { } p && !p.IsFinite()) return false;
        return true;
    }

    /// <summary>Clears patches and droplets (used to recover from a fault).</summary>
    public void ResetDynamic()
    {
        for (int i = 0; i < MaxPatches; i++) _patches[i]?.Reset();
        Droplets.Clear();
    }

    public void Hash(ref StateHasher h)
    {
        h.Add(TideLevel);
        for (int i = 0; i < MaxPatches; i++) _patches[i]?.Hash(ref h);
        Droplets.Hash(ref h);
    }
}

/// <summary>
/// Fixed-size ring of recent surface impacts (position, time, strength). The sim writes; renderers read the entries
/// newer than the last one they saw, by sequence number. Not part of the hashed state: it's a log, not physics.
/// </summary>
public sealed class ImpactLog
{
    public struct Impact
    {
        public long Sequence;
        public float X;
        public double Time;
        public float Strength;
        public float Radius;
    }

    private readonly Impact[] _items = new Impact[512];

    /// <summary>Sequence number of the newest impact (0 = none yet).</summary>
    public long Latest { get; private set; }

    public void Add(float x, double time, float strength, float radius)
    {
        Latest++;
        _items[Latest % _items.Length] = new Impact { Sequence = Latest, X = x, Time = time, Strength = strength, Radius = radius };
    }

    /// <summary>Gets the impact with this sequence number if it's still in the ring.</summary>
    public bool TryGet(long sequence, out Impact impact)
    {
        impact = _items[sequence % _items.Length];
        return sequence > 0 && impact.Sequence == sequence;
    }

    public int Capacity => _items.Length;
}
