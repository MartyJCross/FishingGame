using TheLedger.Sim.Core;

namespace TheLedger.Sim.Tackle;

/// <summary>
/// Fishing line from the rod tip to the terminal tackle (docs/04_PHYSICS_SPEC.md §3). Nodes are lumped masses joined
/// by segments that only resist stretching (slack line sags and piles freely), with compliance r/EA so the line
/// stretches like its material. The first segment (tip → node 0) changes length as line pays out or is retrieved;
/// nodes are inserted and merged at the tip end. The last node is the terminal tackle (a weight for now).
///
/// The line is solved together with the rod by <see cref="TackleSolver"/> (a direct banded solve), so a featherweight
/// line between a rod tip and a heavy sinker or fish stays exactly as stretchy as its material, whatever the mass
/// ratio. That's what lets tension (and drag, breaks, hooksets) be trusted.
/// </summary>
public sealed class Line
{
    private readonly int _capacity;

    public Line(LineSpec spec, TerminalSpec terminal, int capacity)
    {
        Spec = spec;
        Terminal = terminal;
        _capacity = Math.Max(4, capacity);
        int n = _capacity;
        X = new float[n]; Y = new float[n]; PX = new float[n]; PY = new float[n];
        VX = new float[n]; VY = new float[n]; W = new float[n]; Mass = new float[n];
        SolverW = new float[n];
        Rest = new float[n];
        Lambda = new float[n + 1];
        Tension = new float[n + 1];
        _tensionAccum = new float[n + 1];
        Submerged = new bool[n];
        InContact = new bool[n];
    }

    public LineSpec Spec { get; }
    public TerminalSpec Terminal { get; private set; }

    /// <summary>Line node carrying the bobber (−1 = none): the node about FloatDepthM above the terminal.</summary>
    public int FloatIndex { get; private set; } = -1;

    /// <summary>A fish's mouth holds the terminal: it moves where the fish takes it, and the line pulls on the fish.</summary>
    public bool Pinned { get; private set; }
    public float PinX, PinY, PinPrevX, PinPrevY;

    /// <summary>Holds the terminal at <paramref name="p"/> at the end of the next tick (moving there smoothly).</summary>
    public void Pin(Vec2 p)
    {
        int t = Count - 1;
        if (!Pinned) { PinPrevX = X[t]; PinPrevY = Y[t]; }
        else { PinPrevX = PinX; PinPrevY = PinY; }
        PinX = p.X; PinY = p.Y;
        Pinned = true;
        SolverW[t] = 0f;
    }

    public void Unpin()
    {
        if (!Pinned) return;
        Pinned = false;
        RecomputeMasses();
    }

    /// <summary>Swaps what's tied on (gear change; allocation-free).</summary>
    public void SetTerminal(TerminalSpec terminal)
    {
        Terminal = terminal;
        RecomputeMasses();
    }

    /// <summary>Nodes in use; node Count−1 is the terminal tackle.</summary>
    public int Count { get; private set; }
    public int Capacity => _capacity;

    public readonly float[] X, Y, PX, PY, VX, VY, W, Mass;

    /// <summary>
    /// Inverse masses the constraint solver uses. Real line nodes weigh milligrams; when they're thousands of times
    /// lighter than what hangs on the line, their vibration outruns the timestep and the chain goes chaotic. The
    /// solver therefore uses at least <see cref="SolverMassFloor"/> per node; gravity and drag keep the real mass.
    /// </summary>
    public readonly float[] SolverW;

    /// <summary>Minimum node mass inside the solver, kg (set by the rig from tuning and the terminal mass).</summary>
    public float SolverMassFloor { get; set; } = 5e-5f;
    /// <summary>Rest length of the segment from node k to node k+1, m.</summary>
    public readonly float[] Rest;
    /// <summary>Rest length of the first segment, rod tip → node 0, m. Pay-out and retrieve change it.</summary>
    public float FirstRest;
    /// <summary>XPBD multipliers per chain segment (0 = tip→node0, k = node k−1 → node k).</summary>
    public readonly float[] Lambda;
    /// <summary>Tension per chain segment averaged over the last tick, N (index as Lambda).</summary>
    public readonly float[] Tension;
    private readonly float[] _tensionAccum;
    public readonly bool[] Submerged;
    public readonly bool[] InContact;

    /// <summary>Target spacing between nodes, m.</summary>
    public float SegmentLength { get; set; } = 0.1f;

    public int TerminalIndex => Count - 1;
    public Vec2 TerminalPos => new(X[Count - 1], Y[Count - 1]);
    public float TipTension => Tension[0];
    public float MaxTension { get; private set; }

    /// <summary>Unstretched line out from the tip to the terminal, m.</summary>
    public float LineOut
    {
        get
        {
            float s = FirstRest;
            for (int k = 0; k < Count - 1; k++) s += Rest[k];
            return s;
        }
    }

    /// <summary>Lays the line straight from the tip in a direction, with <paramref name="length"/> m out.</summary>
    public void Reset(Vec2 tip, Vec2 direction, float length)
    {
        var dir = direction.Normalized();
        int segs = Math.Clamp((int)MathF.Floor(length / SegmentLength), 1, _capacity - 1);
        Count = segs + 1 > _capacity ? _capacity : Math.Max(2, segs);
        float rest = SegmentLength;
        FirstRest = length - (Count - 1) * rest;
        if (FirstRest < 0.25f * rest)
        {
            rest = length / (Count - 1 + 0.5f);
            FirstRest = 0.5f * rest;
        }
        float d = FirstRest;
        for (int k = 0; k < Count; k++)
        {
            var p = tip + dir * d;
            X[k] = PX[k] = p.X;
            Y[k] = PY[k] = p.Y;
            VX[k] = VY[k] = 0f;
            if (k < Count - 1)
            {
                Rest[k] = rest;
                d += rest;
            }
        }
        RecomputeMasses();
        Array.Clear(Lambda);
        Array.Clear(Tension);
    }

    private float NodeLineMass(int k)
    {
        float lenAround = 0.5f * ((k == 0 ? FirstRest : Rest[k - 1]) + (k < Count - 1 ? Rest[k] : 0f));
        return MathF.Max(1e-7f, Spec.MassPerMetre * lenAround);
    }

    /// <summary>Share of this tick's peak tension that also counts at once (covers the first moments of a fight).</summary>
    private const float InstantShare = 0.5f;

    /// <summary>Peak line tension averaged over ~1.5 s: a cast's brief load stays low, a fight's steady pull counts.</summary>
    public float SustainedTension { get; private set; }

    /// <summary>
    /// The load the line breaks on: peak tension averaged over ~40 ms. The solver's stabilising node mass (tens of
    /// times the real line's) makes a sudden rod movement send a tension wave down the line that real line, being
    /// that much lighter, wouldn't have; it passes in a couple of ticks. Real overloads (a locked drag, a big fish)
    /// last far longer and still break the line.
    /// </summary>
    public float BreakLoad { get; private set; }

    /// <summary>Updates <see cref="SustainedTension"/> and <see cref="BreakLoad"/> from this tick's peak tension.</summary>
    public void TrackSustainedTension(float dt)
    {
        SustainedTension += (MaxTension - SustainedTension) * MathF.Min(1f, dt / 1.5f);
        BreakLoad += (MaxTension - BreakLoad) * MathF.Min(1f, dt / 0.04f);
    }

    /// <summary>Per-node extra solver mass per newton of sustained tension (keeps taut segments from zigzagging).</summary>
    public float TensionMassCoef { get; private set; }

    /// <summary>Sets the solver mass floor (absolute, and per newton of each node's own segment tension) and refreshes.</summary>
    public void SetSolverMassFloor(float floor, float tensionCoef = 0f)
    {
        if (floor == SolverMassFloor && tensionCoef == 0f && TensionMassCoef == 0f) return;
        SolverMassFloor = floor;
        TensionMassCoef = tensionCoef;
        RecomputeMasses();
    }

    private void RecomputeMasses()
    {
        FloatIndex = -1;
        if (Terminal.FloatDepthM > 0f)
        {
            float along = 0f;
            int k = Count - 1;
            while (k > 0 && along + Rest[k - 1] * 0.5f < Terminal.FloatDepthM) { along += Rest[k - 1]; k--; }
            FloatIndex = Math.Max(k, 0);
        }
        for (int k = 0; k < Count; k++)
        {
            Mass[k] = NodeLineMass(k);
            if (k == Count - 1) Mass[k] += Terminal.MassKg;
            if (k == FloatIndex) Mass[k] += Terminal.FloatMassKg;
            W[k] = 1f / Mass[k];
            float local = TensionMassCoef * MathF.Max(SustainedTension, InstantShare * MaxTension);
            SolverW[k] = 1f / MathF.Max(Mass[k], MathF.Max(SolverMassFloor, local));
        }
        if (Pinned) SolverW[Count - 1] = 0f;
    }

    /// <summary>
    /// Changes the line out at the tip by <paramref name="delta"/> m (+ pays out, − retrieves), inserting or merging
    /// nodes as needed. Line out never goes below <paramref name="minimum"/>.
    /// </summary>
    public void PayOut(float delta, float minimum, Vec2 tip, Vec2 tipVelocity)
    {
        float min = MathF.Max(minimum, 0.02f);
        float total = LineOut;
        if (total + delta < min) delta = min - total;
        FirstRest += delta;

        // Insert nodes when the first segment gets long.
        while (FirstRest > 1.5f * SegmentLength && Count < _capacity)
        {
            InsertAtTip(tip, tipVelocity);
        }
        // Merge when it gets short (always keep one free node plus the terminal).
        while (FirstRest < 0.5f * SegmentLength && Count > 2)
        {
            FirstRest += Rest[0];
            RemoveFirst();
        }
        if (FirstRest < 0.005f) FirstRest = 0.005f;
        RecomputeMasses();
    }

    private void InsertAtTip(Vec2 tip, Vec2 tipVelocity)
    {
        for (int k = Count; k > 0; k--)
        {
            X[k] = X[k - 1]; Y[k] = Y[k - 1]; PX[k] = PX[k - 1]; PY[k] = PY[k - 1];
            VX[k] = VX[k - 1]; VY[k] = VY[k - 1];
            Rest[k] = Rest[k - 1];
            Lambda[k + 1] = Lambda[k];
        }
        Count++;
        // New node sits on the line between the tip and the old first node, one segment from the old node.
        var old = new Vec2(X[1], Y[1]);
        var toTip = tip - old;
        float dist = toTip.Length;
        var p = dist > 1e-6f ? old + toTip * MathF.Min(1f, SegmentLength / dist) : old;
        X[0] = PX[0] = p.X; Y[0] = PY[0] = p.Y;
        float f = dist > 1e-6f ? MathF.Min(1f, SegmentLength / dist) : 0f;
        VX[0] = VX[1] + (tipVelocity.X - VX[1]) * f;
        VY[0] = VY[1] + (tipVelocity.Y - VY[1]) * f;
        Rest[0] = SegmentLength;
        FirstRest -= SegmentLength;
        Lambda[1] = Lambda[0];
    }

    private void RemoveFirst()
    {
        for (int k = 0; k < Count - 1; k++)
        {
            X[k] = X[k + 1]; Y[k] = Y[k + 1]; PX[k] = PX[k + 1]; PY[k] = PY[k + 1];
            VX[k] = VX[k + 1]; VY[k] = VY[k + 1];
            if (k < Count - 2) Rest[k] = Rest[k + 1];
            Lambda[k + 1] = Lambda[k + 2];
        }
        Count--;
    }

    public void BeginTick() => Array.Clear(_tensionAccum);

    /// <summary>Adds this substep's segment tensions (−λ/h², pulling only) to the tick average.</summary>
    public void AccumulateTension(float h, int substeps)
    {
        float inv = 1f / (h * h * substeps);
        for (int s = 0; s < Count; s++) _tensionAccum[s] += MathF.Max(0f, -Lambda[s]) * inv;
    }

    public void EndTick()
    {
        float max = 0f;
        for (int s = 0; s < Count; s++)
        {
            Tension[s] = _tensionAccum[s];
            if (Tension[s] > max) max = Tension[s];
        }
        MaxTension = max;
    }

    /// <summary>Current (stretched) length along the nodes from the tip, m.</summary>
    public float CurrentLength(Vec2 tip)
    {
        float s = Vec2.Distance(tip, new Vec2(X[0], Y[0]));
        for (int k = 0; k < Count - 1; k++) s += Vec2.Distance(new Vec2(X[k], Y[k]), new Vec2(X[k + 1], Y[k + 1]));
        return s;
    }

    public bool IsFinite()
    {
        for (int k = 0; k < Count; k++)
            if (!float.IsFinite(X[k]) || !float.IsFinite(Y[k]) || !float.IsFinite(VX[k]) || !float.IsFinite(VY[k])) return false;
        return true;
    }

    public void Hash(ref StateHasher h)
    {
        h.Add(Count);
        h.Add(FirstRest);
        h.Add(X.AsSpan(0, Count));
        h.Add(Y.AsSpan(0, Count));
        h.Add(VX.AsSpan(0, Count));
        h.Add(VY.AsSpan(0, Count));
    }
}
