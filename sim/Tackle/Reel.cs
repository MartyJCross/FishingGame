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

    private bool _bailHeld;

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

    /// <summary>Reads this tick's input: bail, drag clicks, line hold and cranking. Sets SlipForce and Retrieve.</summary>
    public void BeginTick(SimConfig c, float dt, in InputFrame input, LineSpec line, float tipTension)
    {
        bool bail = input.Has(InputButtons.Bail);
        if (bail && !_bailHeld) BailOpen = !BailOpen;
        _bailHeld = bail;
        DragClick = Math.Clamp(DragClick + input.DragDelta, 0, Spec.DragDetents);

        float crank = MathF.Max(input.CrankRate, 0f);
        Retrieve = 0f;
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
            SlipForce = hold >= 0.98f ? float.PositiveInfinity : lip + hold * hold * Spec.FingerHoldN;
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
    public void EndTick(float dt, float slipped, float taken)
    {
        LineOnSpool -= slipped;
        LineOnSpool += taken;
        DragSlipping = !BailOpen && slipped > 1e-4f;
        LineSpeed = (slipped - taken) / dt;
    }

    public void Hash(ref StateHasher h)
    {
        h.Add(LineOnSpool);
        h.Add(BailOpen ? 1 : 0);
        h.Add(DragClick);
    }
}
