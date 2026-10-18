using TheLedger.Sim.Angler;
using TheLedger.Sim.Core;
using TheLedger.Sim.Fish;
using TheLedger.Sim.Input;
using TheLedger.Sim.Tackle;
using TheLedger.Sim.Water;

namespace TheLedger.Sim.World;

/// <summary>
/// The whole simulation (docs/02_TECH_STACK.md §3). Step it at the fixed tick with one InputFrame per angler.
/// Rules: no frame time, no System.Random, no allocation inside Step, fixed iteration order.
/// </summary>
public sealed class SimWorld
{
    private readonly AnglerState[] _anglers;

    public SimWorld(SimConfig config, ulong seed, int anglerCount = 1, Spot? spot = null)
    {
        if (anglerCount < 1) throw new ArgumentOutOfRangeException(nameof(anglerCount));
        Config = config;
        Seed = seed;
        Rng = new SimRng(SimRng.Mix(seed, 1));
        Spot = spot ?? Spot.TestPond();
        _anglers = new AnglerState[anglerCount];
        for (int i = 0; i < anglerCount; i++)
            _anglers[i] = new AnglerState(i, Spot.AnglerStart - new Vec2(1.5f * i, 0f), config);

        _lureViews = new LureView[anglerCount];
        Water = new WaterSurface(seed);
        Water.Conditions = ConditionsFromConfig();
        // Patch 0 covers the water in front of the angler (M3 adds the lure patch, M6 the fish patch).
        float span = (config.PatchColumns - 1) * config.PatchSpacing;
        Water.CreatePatch(0, config.PatchColumns, config.PatchSpacing, -1f + 0.5f * span);
    }

    private readonly List<Fish.Fish> _fish = new();
    private readonly LureView[] _lureViews;
    private readonly SplashView[] _splashViews = new SplashView[32];
    private int _splashCount;

    /// <summary>The fish in this spot.</summary>
    public IReadOnlyList<Fish.Fish> Fish => _fish;

    /// <summary>Who lives here and how clear the water is.</summary>
    public SpotStock Stock { get; private set; } = new();

    /// <summary>Hour of day, 0..24 (from <see cref="SimConfig.StartHour"/> and <see cref="SimConfig.TimeScale"/>).</summary>
    public float Hour => (float)((Config.StartHour + Time * Config.TimeScale / 3600.0) % 24.0);

    /// <summary>
    /// Stocks the spot (setup only; allocates). Sizes come from each species' growth curve: a random age, von
    /// Bertalanffy length with ±10% spread, clamped to the species' record.
    /// </summary>
    public void Populate(SpotStock stock, IReadOnlyList<SpeciesSpec> species)
    {
        Stock = stock;
        _fish.Clear();
        var rng = new SimRng(SimRng.Mix(Seed, 77));
        int id = 0;
        foreach (var entry in stock.Stock)
        {
            SpeciesSpec? sp = null;
            foreach (var s in species) if (s.Id == entry.Species) { sp = s; break; }
            if (sp == null) continue;
            for (int i = 0; i < entry.Count; i++)
            {
                var g = sp.Size.Growth;
                float age = rng.Range(1f, MathF.Max(2f, (g.MaxAgeYears ?? 8f) * 0.7f));
                float lenCm = g.Linf_cm * (1f - MathF.Exp(-g.K * age)) * rng.Range(0.9f, 1.1f);
                lenCm = Math.Clamp(lenCm, sp.Size.LengthCm.Typical[0] * 0.6f, sp.Size.LengthCm.Max);
                float x = rng.Range(stock.RangeX[0] + 1f, stock.RangeX[1] - 1f);
                float surface = Water.Height(x, 0.0), ground = Spot.GroundHeight(x);
                float y = rng.Range(ground + 0.2f, surface - 0.2f);
                _fish.Add(new Fish.Fish(id, sp, lenCm * 0.01f, new Vec2(x, y), rng.Chance(0.5f), Seed + (ulong)id * 7919UL));
                id++;
            }
        }
    }

    /// <summary>The spot being fished (terrain now; structure and fish later).</summary>
    public Spot Spot { get; }

    /// <summary>Wind as a velocity, m/s (from the debug conditions until weather exists).</summary>
    public Vec2 Wind => new(Config.DebugWindSpeed * (Config.DebugWindDir < 0f ? -1f : 1f), 0f);

    /// <summary>The spot's water surface.</summary>
    public WaterSurface Water { get; }

    /// <summary>Until spots and weather exist (M8+), water conditions come from the debug tunables.</summary>
    public bool ConditionsFromDebugConfig = true;

    private WaterConditions ConditionsFromConfig() => new()
    {
        WindSpeed = Config.DebugWindSpeed,
        WindDir = Config.DebugWindDir,
        Fetch = Config.DebugFetch,
        Depth = Config.DebugDepth,
        SwellHeight = Config.DebugSwellHeight,
        SwellPeriod = Config.DebugSwellPeriod,
        SwellDir = 1f,
        RainRate = Config.DebugRainRate,
        Density = 1000f,
    };

    /// <summary>
    /// Something hits the water (a debug click now; lures, fish and boats later). Returns the impact energy, J,
    /// and queues a Splash event.
    /// </summary>
    public float Splash(float x, float speed, float mass, float radius, float horizontalSpeed = 0f)
    {
        float e = Water.Splash(Config, x, speed, mass, radius, horizontalSpeed);
        Events.Push(new SimEvent
        {
            Type = SimEventType.Splash, Tick = Tick, Angler = -1,
            Position = new Vec2(x, Water.Height(x, Time)), Value = e,
        });
        return e;
    }

    public SimConfig Config { get; }
    public ulong Seed { get; }

    /// <summary>World-level random stream. Subsystems get their own via SimRng.Mix(Seed, streamId).</summary>
    public SimRng Rng;

    /// <summary>Ticks stepped so far.</summary>
    public long Tick { get; private set; }

    /// <summary>Sim time in seconds (Tick × dt, computed to avoid float drift).</summary>
    public double Time => Tick / (double)Config.TickHz;

    public IReadOnlyList<AnglerState> Anglers => _anglers;
    public int AnglerCount => _anglers.Length;

    public SimEventQueue Events { get; } = new();

    /// <summary>Times a NaN/Inf was caught and repaired. Tests require 0.</summary>
    public int FaultCount { get; private set; }

    /// <summary>Advances the world by one fixed tick. <paramref name="inputs"/> holds one frame per angler.</summary>
    public void Step(ReadOnlySpan<InputFrame> inputs)
    {
        if (inputs.Length != _anglers.Length)
            throw new ArgumentException($"Expected {_anglers.Length} input frames, got {inputs.Length}");

        float dt = Config.Dt;
        var env = new TackleEnvironment(Water, Spot, Wind, Time);
        StepFish(dt);
        _splashCount = 0;
        for (int i = 0; i < _anglers.Length; i++)
        {
            _anglers[i].Step(Config, dt, inputs[i], env);
            var rig = _anglers[i].Rig;
            for (int e = 0; e < rig.EntryCount; e++)
            {
                // Entries, skips and pops splash the surface; fish will hear these (docs/05 §3.4).
                var w = rig.Entries[e];
                float energy = Water.Splash(Config, w.X, MathF.Abs(w.VY), w.Mass, MathF.Max(w.Radius, 0.01f), w.VX);
                if (_splashCount < _splashViews.Length)
                    _splashViews[_splashCount++] = new SplashView { Pos = new Vec2(w.X, Water.Height(w.X, Time)), Energy = energy };
                Events.Push(new SimEvent
                {
                    Type = SimEventType.Splash, Tick = Tick, Angler = i, Id = (int)w.Kind,
                    Position = new Vec2(w.X, Water.Height(w.X, Time)), Value = energy,
                });
            }
        }

        LineForcesOnFish();

        if (ConditionsFromDebugConfig) Water.Conditions = ConditionsFromConfig();
        Water.Step(Config, dt, Time);

        Tick++;
        GuardFinite();
    }

    /// <summary>Fish see the lures, think and swim; a fish holding a lure pins it to its mouth for the tackle step.</summary>
    private void StepFish(float dt)
    {
        if (_fish.Count == 0) return;
        for (int i = 0; i < _anglers.Length; i++)
        {
            var rig = _anglers[i].Rig;
            var l = rig.Line;
            int t = l.TerminalIndex;
            _lureViews[i] = new LureView
            {
                Angler = i,
                Pos = l.TerminalPos,
                Vel = new Vec2(l.VX[t], l.VY[t]),
                State = rig.Lure,
                Spec = l.Terminal,
                Tension = l.Tension[t],
                Taken = l.Pinned,
            };
        }
        var ctx = new FishContext(Water, Spot, Stock, Time, Hour, _lureViews, _splashViews.AsSpan(0, _splashCount), _fish);
        for (int f = 0; f < _fish.Count; f++)
        {
            var fish = _fish[f];
            var before = fish.State;
            fish.Step(Config, dt, ctx);
            if (fish.State == FishState.Strike && before != FishState.Strike)
                Events.Push(new SimEvent { Type = SimEventType.FishStrike, Tick = Tick, Angler = fish.LureAngler, Id = fish.Id, Position = fish.Mouth });
            if (fish.State == FishState.Hold && before != FishState.Hold)
                Events.Push(new SimEvent { Type = SimEventType.Bite, Tick = Tick, Angler = fish.LureAngler, Id = fish.Id, Position = fish.Mouth });
            if (before == FishState.Hold && fish.State != FishState.Hold && fish.State != FishState.Hooked)
                Events.Push(new SimEvent { Type = SimEventType.FishSpit, Tick = Tick, Angler = -1, Id = fish.Id, Position = fish.Mouth });
        }
        // Pins: one fish per lure.
        for (int i = 0; i < _anglers.Length; i++)
        {
            Fish.Fish? holder = null;
            for (int f = 0; f < _fish.Count; f++)
                if (_fish[f].Holding && _fish[f].LureAngler == i) { holder = _fish[f]; break; }
            var line = _anglers[i].Rig.Line;
            if (holder != null) line.Pin(holder.Mouth);
            else line.Unpin();
        }
    }

    /// <summary>The line pulls on the mouth of whatever fish holds the lure (tension along the last segment).</summary>
    private void LineForcesOnFish()
    {
        for (int f = 0; f < _fish.Count; f++)
        {
            var fish = _fish[f];
            if (!fish.Holding || fish.LureAngler < 0 || fish.LureAngler >= _anglers.Length) { fish.LineForce = Vec2.Zero; continue; }
            var l = _anglers[fish.LureAngler].Rig.Line;
            int t = l.TerminalIndex;
            Vec2 toward = t > 0 ? new Vec2(l.X[t - 1] - l.X[t], l.Y[t - 1] - l.Y[t]) : Vec2.Zero;
            float len = toward.Length;
            fish.LineForce = len > 1e-6f ? toward * (l.Tension[t] / len) : Vec2.Zero;
        }
    }

    private void GuardFinite()
    {
        if (!Water.IsFinite())
        {
            Water.ResetDynamic();
            FaultCount++;
            Events.Push(new SimEvent { Type = SimEventType.SimFault, Tick = Tick, Angler = -1, Id = 1 });
        }
        for (int i = 0; i < _anglers.Length; i++)
        {
            var a = _anglers[i];
            if (a.IsFinite) continue;
            // Repair: reset the broken angler's hands and rig. Later systems may restore a last good snapshot instead.
            a.Hands.Angle = a.Hands.PrevAngle = 0.6f;
            a.Hands.AngularVelocity = 0f;
            a.Hands.Reach = a.Hands.PrevReach = 0f;
            a.Rig.Reset(a.Grip, a.Hands.Angle, MathF.Max(Config.MinLineOut, 1.2f));
            FaultCount++;
            Events.Push(new SimEvent { Type = SimEventType.SimFault, Tick = Tick, Angler = i });
        }
    }

    /// <summary>Hash of the full sim state, for determinism tests.</summary>
    public ulong ComputeHash()
    {
        var h = new StateHasher();
        h.Add(Tick);
        for (int i = 0; i < _anglers.Length; i++) _anglers[i].Hash(ref h);
        for (int f = 0; f < _fish.Count; f++) _fish[f].Hash(ref h);
        Water.Hash(ref h);
        return h.Value;
    }
}
