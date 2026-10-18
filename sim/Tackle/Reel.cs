using TheLedger.Sim.Core;
using TheLedger.Sim.Input;

namespace TheLedger.Sim.Tackle;

/// <summary>A reel as data (game/data/gear/reels/*.json). docs/04_PHYSICS_SPEC.md §5.</summary>
public sealed class ReelSpec
{
    public string Id { get; set; } = "reel";
    public string Name { get; set; } = "Reel";
    /// <summary>spinning, spincast, baitcaster, conventional, fly (only spinning is simulated so far).</summary>
    public string Type { get; set; } = "spinning";
    /// <summary>Spool turns per handle turn.</summary>
    public float GearRatio { get; set; } = 5.2f;
    /// <summary>Radius of the bare spool arbor, m.</summary>
    public float SpoolCoreRadiusM { get; set; } = 0.016f;
    /// <summary>Width of the line bed, m.</summary>
    public float SpoolWidthM { get; set; } = 0.012f;
    /// <summary>How much line the spool holds when full, m (for its rated line).</summary>
    public float CapacityM { get; set; } = 140f;
    /// <summary>Maximum drag, N (sold as kg: 1 kg ≈ 9.8 N).</summary>
    public float MaxDragN { get; set; } = 40f;
    /// <summary>Drag knob clicks from loosest to tightest.</summary>
    public int DragDetents { get; set; } = 16;
    /// <summary>Static / kinetic drag ratio: 1.02-1.08 smooth, 1.3-1.5 sticky and jerky.</summary>
    public float DragStickRatio { get; set; } = 1.1f;
    /// <summary>Resistance of line peeling off a full spool's lip during a cast, N (rises as the spool empties).</summary>
    public float LipFrictionN { get; set; } = 0.012f;
    /// <summary>How hard a finger pressed on the line at the spool can hold it, N.</summary>
    public float FingerHoldN { get; set; } = 25f;
    public float MassKg { get; set; } = 0.25f;
    public float Price { get; set; }

    // Baitcasters (docs/04 §5.4): the spool turns during the cast.
    /// <summary>Spool moment of inertia when empty, kg·m² (a 10 g aluminium spool is ~1.5e-6).</summary>
    public float SpoolInertia { get; set; } = 1.5e-6f;
    /// <summary>Bearing friction torque, N·m.</summary>
    public float BearingTorque { get; set; } = 1.5e-5f;
    /// <summary>Magnetic brake torque per rad/s at full setting, N·m·s.</summary>
    public float MagBrakeMax { get; set; } = 8.0e-7f;
    /// <summary>Brake knob clicks.</summary>
    public int BrakeClicks { get; set; } = 10;
    /// <summary>Loose line on the spool that becomes a bird's nest, m.</summary>
    public float BacklashLooseM { get; set; } = 1.0f;

    public bool IsBaitcaster => Type == "baitcaster";

    public static ReelSpec Spinning2500() => new();
}

/// <summary>
/// The reel's state for one angler: line on the spool, bail, drag knob, and what it does each tick.
/// The reel acts on the line in two ways: it limits the tension at the rod (above <see cref="SlipForce"/> line leaves
/// the spool: an open bail's lip friction and finger, or the drag), and cranking takes line back in at
/// gear ratio × spool circumference, stalling when the line pulls harder than the hand can winch.
/// </summary>
public sealed class Reel
{
    public Reel(ReelSpec spec, float lineOnSpool)
    {
        Spec = spec;
        LineOnSpool = lineOnSpool;
        DragClick = spec.DragDetents / 3;
    }

    public ReelSpec Spec { get; }
    public float LineOnSpool;
    public bool BailOpen;
    public int DragClick;
    /// <summary>Line left the spool last tick against a closed drag.</summary>
    public bool DragSlipping;
    /// <summary>Line pay-out speed last tick (casting or drag slip), m/s; negative when retrieving.</summary>
    public float LineSpeed;
    /// <summary>Tension limit at the rod for this tick, N (∞ = locked).</summary>
    public float SlipForce = float.PositiveInfinity;
    /// <summary>Line to take in this tick (≤ 0, metres).</summary>
    public float Retrieve;
    /// <summary>Times the line has spooled off completely (the knot at the arbor took the strain).</summary>
    public bool Spooled => LineOnSpool <= 0f;

    private bool _bailHeld, _brakeUpHeld, _brakeDownHeld;
    private float _clearHold, _lastLineSpeed;

    // Baitcaster spool state.
    /// <summary>Spool speed in free spool, rad/s.</summary>
    public float SpoolOmega;
    /// <summary>Line that has come off the spool faster than the lure took it (overrun), m.</summary>
    public float Loose;
    /// <summary>Bird's nest: the spool is locked until the tangle is picked out (hold Keep).</summary>
    public bool Backlashed;
    /// <summary>Raised on the tick a backlash happens (the world turns it into an event).</summary>
    public bool BacklashedNow;
    /// <summary>Brake knob, 0..BrakeClicks.</summary>
    public int BrakeClick = 5;
    /// <summary>Thumb pressure on the spool this tick, 0..1.</summary>
    public float Thumb;
    /// <summary>Progress picking out a backlash, 0..1.</summary>
    public float ClearProgress => Math.Clamp(_clearHold / 1.5f, 0f, 1f);

    /// <summary>The spool is free and spinning line off (a baitcaster in free spool, not thumbed to a stop, not tangled).</summary>
    public bool SpoolDriving => Spec.IsBaitcaster && BailOpen && !Backlashed;

    public float Fill => Math.Clamp(LineOnSpool / MathF.Max(Spec.CapacityM, 1f), 0f, 1.5f);

    /// <summary>Effective spool radius with the line wound on it (docs/04 §5.1).</summary>
    public float SpoolRadius(LineSpec line)
    {
        float rc = Spec.SpoolCoreRadiusM;
        float d = line.DiameterM;
        return MathF.Sqrt(rc * rc + MathF.Max(LineOnSpool, 0f) * d * d / (Spec.SpoolWidthM * MathF.PI * 0.8f));
    }

    public float DragForce => DragClick <= 0 ? 0.3f : Spec.MaxDragN * DragClick / MathF.Max(Spec.DragDetents, 1);

    /// <summary>Line pull at which the hand can no longer turn the handle, N.</summary>
    public float CrankStallForce(SimConfig c, LineSpec line) => c.HandCrankTorque / (Spec.GearRatio * SpoolRadius(line));

    public float SpoolInertiaNow(LineSpec line)
    {
        float r = SpoolRadius(line), rc = Spec.SpoolCoreRadiusM;
        // The line on the spool adds a ring of mass between the core and the current radius.
        float lineMass = MathF.Max(LineOnSpool, 0f) * line.MassPerMetre;
        return Spec.SpoolInertia + 0.5f * lineMass * (r * r + rc * rc);
    }

    /// <summary>
    /// The hardest a finger or thumb can pin the line: the line slips under it before it breaks (a hard backswing with
    /// the bail open must never snap the line, owner 2026-10-18).
    /// </summary>
    private float MaxHold(LineSpec line) => MathF.Min(Spec.FingerHoldN, 0.75f * line.BreakingForceN * line.KnotFactor);

    /// <summary>Spool resistance felt by the line in free spool: bearings, the magnetic brake (∝ spool speed), the thumb, N.</summary>
    public float SpoolResistance(LineSpec line)
    {
        float r = SpoolRadius(line);
        float b = Spec.MagBrakeMax * BrakeClick / MathF.Max(Spec.BrakeClicks, 1);
        return (Spec.BearingTorque + b * SpoolOmega) / r + Thumb * Thumb * Spec.FingerHoldN;
    }

    /// <summary>
    /// Baitcaster spool after the tick (docs/04 §5.4): while the line goes out at least as fast as the spool turns,
    /// the line drives the spool (and first draws any loose loops back out). When the line slows (splash-down, wind,
    /// a stalling lure) the spool keeps turning on its inertia, slowed only by bearings, brake and thumb; the line it
    /// feeds that nobody takes loops up as loose line. Too much loose line is a bird's nest.
    /// </summary>
    private void StepSpool(float dt, float lineOut, LineSpec line)
    {
        float r = SpoolRadius(line);
        float vLine = lineOut / dt;
        _lastLineSpeed = vLine;
        float vSpool = SpoolOmega * r;
        if (Thumb >= 0.98f) { SpoolOmega = 0f; return; }
        if (vLine >= vSpool)
        {
            float pull = (vLine - vSpool) * dt;
            float fromLoose = MathF.Min(Loose, pull);
            Loose -= fromLoose;
            SpoolOmega = MathF.Max(SpoolOmega, (vLine - fromLoose / dt) / r);
        }
        else
        {
            float torque = SpoolResistance(line) * r;
            SpoolOmega = MathF.Max(0f, SpoolOmega - torque / SpoolInertiaNow(line) * dt);
            Loose += MathF.Max(0f, SpoolOmega * r - vLine) * dt;
        }
        if (!Backlashed && Loose > Spec.BacklashLooseM)
        {
            Backlashed = true;
            BacklashedNow = true;
            SpoolOmega = 0f;
        }
    }

    /// <summary>Reads this tick's input: bail, drag clicks, line hold and cranking. Sets SlipForce and Retrieve.</summary>
    public void BeginTick(SimConfig c, float dt, in InputFrame input, LineSpec line, float tipTension)
    {
        bool bail = input.Has(InputButtons.Bail);
        if (bail && !_bailHeld && !Backlashed) { BailOpen = !BailOpen; SpoolOmega = 0f; Loose = 0f; }
        _bailHeld = bail;
        DragClick = Math.Clamp(DragClick + input.DragDelta, 0, Spec.DragDetents);
        bool up = input.Has(InputButtons.BrakeUp), down = input.Has(InputButtons.BrakeDown);
        if (up && !_brakeUpHeld) BrakeClick = Math.Min(Spec.BrakeClicks, BrakeClick + 1);
        if (down && !_brakeDownHeld) BrakeClick = Math.Max(0, BrakeClick - 1);
        _brakeUpHeld = up; _brakeDownHeld = down;
        BacklashedNow = false;
        Thumb = Math.Clamp(input.LineHold, 0f, 1f);

        float crank = MathF.Max(input.CrankRate, 0f);
        Retrieve = 0f;

        if (Backlashed)
        {
            // Picking out a bird's nest: hold Keep (K / Y) for a second and a half. Until then nothing moves.
            _clearHold = input.Has(InputButtons.Interact) ? _clearHold + dt : 0f;
            if (_clearHold >= 1.5f) { Backlashed = false; Loose = 0f; BailOpen = false; _clearHold = 0f; }
            SlipForce = float.PositiveInfinity;
            return;
        }
        if (Spec.IsBaitcaster)
        {
            // Free spool: line only leaves by the spool turning (Rig drives it); the clutch re-engages on cranking.
            if (BailOpen && crank > 0.05f) { BailOpen = false; SpoolOmega = 0f; Loose = 0f; crank = 0f; }
            if (BailOpen)
            {
                // Line is drawn off against the spool's resistance; loose loops come off for nothing.
                // Spinning the spool up takes force too (its inertia as a mass at the line: I / r²).
                float r = SpoolRadius(line);
                float spinUp = SpoolInertiaNow(line) / (r * r) * MathF.Max(0f, _lastLineSpeed - SpoolOmega * r) / dt;
                SlipForce = Thumb >= 0.98f ? MaxHold(line) : Loose > 0.01f ? 0.002f : MathF.Max(0.002f, SpoolResistance(line) + spinUp);
                return;
            }
        }
        if (BailOpen)
        {
            // Turning the handle flips the bail shut (it takes a moment; that tick retrieves nothing).
            if (crank > 0.05f) BailOpen = false;
        }

        if (Spooled)
        {
            SlipForce = float.PositiveInfinity;
        }
        else if (BailOpen)
        {
            // Line peels off the lip; an emptier spool drags harder (underfilled spools cast short).
            float empty = 1f - Math.Clamp(Fill, 0f, 1f);
            float lip = Spec.LipFrictionN * (1f + 3f * empty * empty);
            float hold = Math.Clamp(input.LineHold, 0f, 1f);
            SlipForce = MathF.Min(lip + hold * hold * Spec.FingerHoldN, MaxHold(line));
        }
        else
        {
            float drag = DragForce;
            SlipForce = DragSlipping ? drag / MathF.Max(Spec.DragStickRatio, 1f) : drag;
            if (!DragSlipping && crank > 0f)
            {
                float stall = CrankStallForce(c, line);
                float ease = Math.Clamp(1f - tipTension / stall, 0f, 1f);
                Retrieve = -crank * Spec.GearRatio * 2f * MathF.PI * SpoolRadius(line) * ease * dt;
            }
        }
    }

    /// <summary>Books what actually happened: <paramref name="slipped"/> m left the spool, <paramref name="taken"/> m came in.</summary>
    public void EndTick(float dt, float slipped, float taken, LineSpec line)
    {
        if (SpoolDriving) StepSpool(dt, slipped, line);
        LineOnSpool -= slipped;
        LineOnSpool += taken;
        DragSlipping = !BailOpen && !Backlashed && slipped > 1e-4f;
        LineSpeed = (slipped - taken) / dt;
    }

    public void Hash(ref StateHasher h)
    {
        h.Add(LineOnSpool);
        h.Add(BailOpen ? 1 : 0);
        h.Add(DragClick);
        h.Add(SpoolOmega);
        h.Add(Backlashed ? 1 : 0);
    }
}
