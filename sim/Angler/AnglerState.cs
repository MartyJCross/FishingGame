using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.Tackle;

namespace TheLedger.Sim.Angler;

/// <summary>
/// One angler in the world: body position, hands, and (from M2 on) their own rod, line, reel and rig.
/// The world holds a list of these; nothing assumes there is only one (CLAUDE.md rule 15).
/// </summary>
public sealed class AnglerState
{
    public AnglerState(int index, Vec2 position, SimConfig config)
    {
        Index = index;
        Position = PrevPosition = position;
        SetTackle(config, RodSpec.MediumSpinning(), ReelSpec.Spinning2500(), LineSpec.Mono10(), TerminalSpec.HalfOunceSinker(), 1.2f);
    }

    /// <summary>This angler's rod, line and terminal tackle.</summary>
    public Rig Rig { get; private set; } = null!;
    public Reel Reel { get; private set; } = null!;

    /// <summary>Where the hands hold the rod, world space.</summary>
    public Vec2 Grip => Shoulder + Hands.GripOffset;

    /// <summary>Replaces the tackle (allocates: setup and gear changes only).</summary>
    public void SetTackle(SimConfig config, RodSpec rod, ReelSpec reel, LineSpec line, TerminalSpec terminal, float lineOut)
    {
        Reel = new Reel(reel, reel.CapacityM - lineOut);
        Rig = new Rig(rod, line, terminal, config.MaxLineNodes);
        Rig.Line.SegmentLength = config.LineSegmentLength;
        Hands.Step(config, 0f, Hands.Angle, Hands.Reach);
        Rig.Reset(Grip, Hands.Angle, lineOut);
    }

    public int Index { get; }

    /// <summary>Terminals this angler can tie on (lures, bait rigs). R / X cycles them while the line is in.</summary>
    public List<TerminalSpec> TackleBox { get; } = new() { TerminalSpec.HalfOunceSinker() };
    public int TackleIndex;
    private bool _rigHeld;

    /// <summary>Feet position in world space, metres.</summary>
    public Vec2 Position;
    public Vec2 PrevPosition;

    /// <summary>Shoulder height above the feet, metres.</summary>
    public float ShoulderHeight = 1.45f;

    public Hands Hands { get; } = new();

    /// <summary>The last input applied (kept for debug display).</summary>
    public InputFrame LastInput;

    public Vec2 Shoulder => Position + new Vec2(0f, ShoulderHeight);

    public void Step(SimConfig c, float dt, in InputFrame input, in TackleEnvironment env)
    {
        LastInput = input;
        PrevPosition = Position;
        Hands.Step(c, dt, input.RodTargetAngle, input.RodReach);

        bool rigButton = input.Has(InputButtons.RigMenu);
        if (rigButton && !_rigHeld && TackleBox.Count > 1 && Rig.Line.LineOut <= c.RetieMaxLineOut)
        {
            TackleIndex = (TackleIndex + 1) % TackleBox.Count;
            Rig.Line.SetTerminal(TackleBox[TackleIndex]);
        }
        _rigHeld = rigButton;

        Reel.BeginTick(c, dt, input, Rig.Line.Spec, Rig.Line.TipTension);
        Rig.Line.SegmentLength = MathF.Max(c.LineSegmentLength, Rig.Line.LineOut / MathF.Max(c.LineNodesTarget, 1f));
        Rig.Step(c, dt, env, Grip, Hands.Angle, Reel.Retrieve, Reel.SlipForce);
        Reel.EndTick(dt, Rig.SlippedThisTick, Rig.TakenThisTick);
    }

    public void Hash(ref StateHasher h)
    {
        h.Add(Position);
        Hands.Hash(ref h);
        Rig.Hash(ref h);
        Reel.Hash(ref h);
    }

    public bool IsFinite => Position.IsFinite && Hands.IsFinite && Rig.IsFinite();
}
