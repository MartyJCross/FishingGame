using TheLedger.Sim.Angler;
using TheLedger.Sim.Core;
using TheLedger.Sim.Input;

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

        Tick++;
        GuardFinite();
    }

    private void GuardFinite()
    {
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
        return h.Value;
    }
}
