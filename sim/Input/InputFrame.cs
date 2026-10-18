namespace TheLedger.Sim.Input;

/// <summary>Digital inputs, packed as bit flags.</summary>
[Flags]
public enum InputButtons : ushort
{
    None = 0,
    /// <summary>Open/close the bail (spinning) or press free-spool (baitcaster).</summary>
    Bail = 1 << 0,
    /// <summary>Net / grip / gaff / tail rope: the landing tool.</summary>
    Land = 1 << 1,
    RigMenu = 1 << 2,
    Interact = 1 << 3,
}

/// <summary>
/// Everything one angler's input means for one sim tick. This is the only way input reaches the sim
/// (docs/02_TECH_STACK.md §3.1), so recording these frames replays a session exactly.
/// Hooksets, twitches and casts are rod motion, not buttons. Plain fixed-size data; keep it that way.
/// </summary>
public struct InputFrame
{
    /// <summary>Where the player wants the rod to point, radians CCW from +x (facing right).</summary>
    public float RodTargetAngle;
    /// <summary>How far the hands reach out/up, 0..1 (W/S, stick deflection).</summary>
    public float RodReach;
    /// <summary>Line hold: finger on the line / thumb on the spool, 0 = free, 1 = fully held.</summary>
    public float LineHold;
    /// <summary>Reel handle speed in turns per second (wheel, RMB, RT). Negative is not allowed on most reels.</summary>
    public float CrankRate;
    /// <summary>Drag detents to add this tick (+ tighter, − looser).</summary>
    public sbyte DragDelta;
    /// <summary>Walk along the bank/deck, or row/steer a boat, −1..1.</summary>
    public float Move;
    /// <summary>Boat throttle, −1..1.</summary>
    public float Throttle;
    public InputButtons Buttons;

    public readonly bool Has(InputButtons b) => (Buttons & b) != 0;

    /// <summary>Bytes per frame in a recording.</summary>
    public const int RecordSize = 4 * 6 + 1 + 2;

    public readonly void Write(BinaryWriter w)
    {
        w.Write(RodTargetAngle);
        w.Write(RodReach);
        w.Write(LineHold);
        w.Write(CrankRate);
        w.Write(DragDelta);
        w.Write(Move);
        w.Write(Throttle);
        w.Write((ushort)Buttons);
    }

    public static InputFrame Read(BinaryReader r) => new()
    {
        RodTargetAngle = r.ReadSingle(),
        RodReach = r.ReadSingle(),
        LineHold = r.ReadSingle(),
        CrankRate = r.ReadSingle(),
        DragDelta = r.ReadSByte(),
        Move = r.ReadSingle(),
        Throttle = r.ReadSingle(),
        Buttons = (InputButtons)r.ReadUInt16(),
    };
}
