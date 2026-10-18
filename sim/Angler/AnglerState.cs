using TheLedger.Sim.Core;
using TheLedger.Sim.Input;

namespace TheLedger.Sim.Angler;

/// <summary>
/// One angler in the world: body position, hands, and (from M2 on) their own rod, line, reel and rig.
/// The world holds a list of these; nothing assumes there is only one (CLAUDE.md rule 15).
/// </summary>
public sealed class AnglerState
{
    public AnglerState(int index, Vec2 position)
    {
        Index = index;
        Position = PrevPosition = position;
    }

    public int Index { get; }

    /// <summary>Feet position in world space, metres.</summary>
    public Vec2 Position;
    public Vec2 PrevPosition;

    /// <summary>Shoulder height above the feet, metres.</summary>
    public float ShoulderHeight = 1.45f;

    public Hands Hands { get; } = new();

    /// <summary>The last input applied (kept for debug display).</summary>
    public InputFrame LastInput;

    public Vec2 Shoulder => Position + new Vec2(0f, ShoulderHeight);

    public void Step(SimConfig c, float dt, in InputFrame input)
    {
        LastInput = input;
        PrevPosition = Position;
        Hands.Step(c, dt, input.RodTargetAngle, input.RodReach);
    }

    public void Hash(ref StateHasher h)
    {
        h.Add(Position);
        Hands.Hash(ref h);
    }

    public bool IsFinite => Position.IsFinite && Hands.IsFinite;
}
