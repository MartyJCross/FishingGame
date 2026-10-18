using TheLedger.Sim.Core;

namespace TheLedger.Sim.Tackle;

/// <summary>
/// The rod as an XPBD chain (docs/04_PHYSICS_SPEC.md §4). Node 0 is the butt cap and node 1 the fore grip; both
/// are kinematic and follow the hands. Nodes 2.. are the blank, tapering in stiffness, mass and diameter to the tip.
/// Bending constraints use compliance segLen/EI(s), so the rod loads, whips and recovers like the real blank, and
/// report the bending moment at every joint (for haptics, the overlay and breakage).
/// </summary>
public sealed class Rod
{
    public Rod(RodSpec spec)
    {
        Spec = spec;
        int n = 2 + spec.BlankNodes;
        Count = n;
        X = new float[n]; Y = new float[n];
        PX = new float[n]; PY = new float[n];
        VX = new float[n]; VY = new float[n];
        W = new float[n];
        Mass = new float[n];
        S = new float[n];
        Diameter = new float[n];
        FX = new float[n]; FY = new float[n];
        RestLen = new float[n - 1];
        BendCompliance = new float[n];
        BendLambda = new float[n];
        Moment = new float[n];
        MomentLimit = new float[n];
        _momentAccum = new float[n];
        InContact = new bool[n];
        IsGuide = new bool[n];

        float blankLen = spec.LengthM - spec.HandleLengthM;
        int nb = spec.BlankNodes;
        // Segment lengths shrink toward the tip (∝ 1 − 0.5·s) so the flexible tip gets more resolution.
        Span<float> weights = stackalloc float[nb];
        float sumW = 0f;
        for (int k = 0; k < nb; k++)
        {
            float sMid = (k + 0.5f) / nb;
            weights[k] = 1f - 0.5f * sMid;
            sumW += weights[k];
        }
        RestLen[0] = spec.HandleLengthM;
        float along = 0f;
        S[0] = 0f; S[1] = 0f;
        for (int k = 0; k < nb; k++)
        {
            RestLen[k + 1] = blankLen * weights[k] / sumW;
            along += RestLen[k + 1];
            S[k + 2] = along / blankLen;
        }
        for (int i = 0; i < n; i++) Diameter[i] = spec.Diameter(S[i]);

        // Mass along the blank ∝ diameter (a tube with roughly constant wall), lumped to nodes.
        float massNorm = 0f;
        for (int k = 1; k < n - 1; k++) massNorm += RestLen[k] * 0.5f * (Diameter[k] + Diameter[k + 1]);
        for (int k = 1; k < n - 1; k++)
        {
            float segMass = spec.BlankMassKg * RestLen[k] * 0.5f * (Diameter[k] + Diameter[k + 1]) / massNorm;
            Mass[k] += 0.5f * segMass;
            Mass[k + 1] += 0.5f * segMass;
        }
        Mass[0] += spec.HandleMassKg * 0.5f;
        Mass[1] += spec.HandleMassKg * 0.5f;
        for (int i = 0; i < n; i++) W[i] = i < 2 ? 0f : 1f / Mass[i];

        for (int j = 1; j < n - 1; j++)
        {
            float ell = 0.5f * (RestLen[j - 1] + RestLen[j]);
            BendCompliance[j] = ell / spec.EI(S[j]);
            MomentLimit[j] = spec.MaxMoment(S[j]);
        }
        foreach (int g in spec.GuideNodes)
            if (g >= 1 && g <= nb) IsGuide[g + 1] = true;
        IsGuide[n - 1] = true;

        SetStraight(Vec2.Zero, 0.6f);
    }

    public RodSpec Spec { get; }
    public int Count { get; }
    public int Tip => Count - 1;

    public readonly float[] X, Y, PX, PY, VX, VY, W, Mass, S, Diameter;
    /// <summary>External forces for the next substep (guide forces, test loads), N. Cleared after use.</summary>
    public readonly float[] FX, FY;
    public readonly float[] RestLen;
    public readonly float[] BendCompliance, BendLambda;
    /// <summary>Bending moment at each joint, averaged over the last tick, N·m.</summary>
    public readonly float[] Moment;
    public readonly float[] MomentLimit;
    private readonly float[] _momentAccum;
    public readonly bool[] InContact;
    public readonly bool[] IsGuide;

    /// <summary>Kinematic handle pose at the start and end of the current tick.</summary>
    private Vec2 _gripFrom, _gripTo;
    private float _angleFrom, _angleTo;

    /// <summary>Highest moment / limit ratio over the joints last tick (1 = at the breaking point).</summary>
    public float PeakLoadRatio { get; private set; }

    /// <summary>The load the rod breaks on: <see cref="PeakLoadRatio"/> averaged over ~40 ms (see Line.BreakLoad: the
    /// solver's heavy line nodes make brief tension spikes real line wouldn't).</summary>
    public float BreakLoadRatio { get; private set; }

    public void TrackBreakLoad(float dt) => BreakLoadRatio += (PeakLoadRatio - BreakLoadRatio) * MathF.Min(1f, dt / 0.04f);

    public Vec2 Node(int i) => new(X[i], Y[i]);
    public Vec2 TipPos => new(X[Tip], Y[Tip]);
    public Vec2 TipPrev => new(PX[Tip], PY[Tip]);

    /// <summary>Places the whole rod straight from the grip at an angle (no velocity).</summary>
    public void SetStraight(Vec2 grip, float angle)
    {
        var dir = Vec2.FromAngle(angle);
        var butt = grip - dir * Spec.HandleLengthM;
        X[0] = butt.X; Y[0] = butt.Y;
        float d = 0f;
        for (int i = 1; i < Count; i++)
        {
            d += RestLen[i - 1];
            var p = butt + dir * d;
            X[i] = p.X; Y[i] = p.Y;
        }
        for (int i = 0; i < Count; i++)
        {
            PX[i] = X[i]; PY[i] = Y[i];
            VX[i] = 0f; VY[i] = 0f;
        }
        _gripFrom = _gripTo = grip;
        _angleFrom = _angleTo = angle;
    }

    /// <summary>Sets where the hands take the handle over this tick.</summary>
    public void SetHandleTarget(Vec2 grip, float angle)
    {
        _gripFrom = _gripTo;
        _angleFrom = _angleTo;
        _gripTo = grip;
        _angleTo = angle;
    }

    /// <summary>Moves the two kinematic handle nodes to fraction <paramref name="f"/> of the tick.</summary>
    public void DriveHandle(float f, float h)
    {
        var grip = Vec2.Lerp(_gripFrom, _gripTo, f);
        float a = _angleFrom + Angles.Wrap(_angleTo - _angleFrom) * f;
        var dir = Vec2.FromAngle(a);
        var butt = grip - dir * Spec.HandleLengthM;
        VX[0] = (butt.X - X[0]) / h; VY[0] = (butt.Y - Y[0]) / h;
        VX[1] = (grip.X - X[1]) / h; VY[1] = (grip.Y - Y[1]) / h;
        X[0] = butt.X; Y[0] = butt.Y;
        X[1] = grip.X; Y[1] = grip.Y;
    }

    public void BeginTick()
    {
        Array.Clear(_momentAccum);
    }

    /// <summary>Adds this substep's bending moments (|λ|/h²) to the tick average.</summary>
    public void AccumulateMoments(float h, int substeps)
    {
        float inv = 1f / (h * h * substeps);
        for (int j = 1; j < Count - 1; j++) _momentAccum[j] += MathF.Abs(BendLambda[j]) * inv;
    }

    public void EndTick()
    {
        float peak = 0f;
        for (int j = 1; j < Count - 1; j++)
        {
            Moment[j] = _momentAccum[j];
            float r = Moment[j] / MathF.Max(MomentLimit[j], 1e-6f);
            if (r > peak) peak = r;
        }
        PeakLoadRatio = peak;
    }

    public bool IsFinite()
    {
        for (int i = 0; i < Count; i++)
            if (!float.IsFinite(X[i]) || !float.IsFinite(Y[i]) || !float.IsFinite(VX[i]) || !float.IsFinite(VY[i])) return false;
        return true;
    }

    public void Hash(ref StateHasher h)
    {
        h.Add(X); h.Add(Y); h.Add(VX); h.Add(VY);
    }
}

public static class Angles
{
    public static float Wrap(float a)
    {
        while (a > MathF.PI) a -= 2f * MathF.PI;
        while (a < -MathF.PI) a += 2f * MathF.PI;
        return a;
    }
}
