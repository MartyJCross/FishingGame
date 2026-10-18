using TheLedger.Sim.Angler;
using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.Water;

namespace TheLedger.Sim.World;

/// <summary>
/// The whole simulation (docs/02_TECH_STACK.md §3). Step it at the fixed tick with one InputFrame per angler.
/// Rules: no frame time, no System.Random, no allocation inside Step, fixed iteration order.
/// </summary>
public sealed class SimWorld
{
    private readonly AnglerState[] _anglers;

    public SimWorld(SimConfig config, ulong seed, int anglerCount = 1)
    {
        if (anglerCount < 1) throw new ArgumentOutOfRangeException(nameof(anglerCount));
        Config = config;
        Seed = seed;
        Rng = new SimRng(SimRng.Mix(seed, 1));
        _anglers = new AnglerState[anglerCount];
        for (int i = 0; i < anglerCount; i++)
            _anglers[i] = new AnglerState(i, new Vec2(-2f - 1.5f * i, 0.6f));

        Water = new WaterSurface(seed);
        Water.Conditions = ConditionsFromConfig();
        // Patch 0 covers the water in front of the angler (M3 adds the lure patch, M6 the fish patch).
        float span = (config.PatchColumns - 1) * config.PatchSpacing;
        Water.CreatePatch(0, config.PatchColumns, config.PatchSpacing, -1f + 0.5f * span);
    }

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
        for (int i = 0; i < _anglers.Length; i++)
            _anglers[i].Step(Config, dt, inputs[i]);

        if (ConditionsFromDebugConfig) Water.Conditions = ConditionsFromConfig();
        Water.Step(Config, dt, Time);

        Tick++;
        GuardFinite();
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
            // M0 repair: reset the broken angler's hands. Later systems restore their last good snapshot.
            a.Hands.Angle = a.Hands.PrevAngle = 0.6f;
            a.Hands.AngularVelocity = 0f;
            a.Hands.Reach = a.Hands.PrevReach = 0f;
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
        Water.Hash(ref h);
        return h.Value;
    }
}
