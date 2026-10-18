using TheLedger.Sim.Core;
using TheLedger.Sim.Tackle;
using TheLedger.Sim.Water;
using TheLedger.Sim.World;

namespace TheLedger.Sim.Tests.Tackle;

public static class TackleTestUtil
{
    /// <summary>Dry, still air, ground 100 m below: for static rod and line tests.</summary>
    public static TackleEnvironment DryEnv(double time = 0)
    {
        var water = new WaterSurface(1) { TideLevel = -200f };
        water.Conditions = new WaterConditions { Density = 1000f, Depth = 1f, Fetch = 1f };
        var spot = new Spot("void", new[] { new Vec2(-1000f, -100f), new Vec2(1000f, -100f) });
        return new TackleEnvironment(water, spot, Vec2.Zero, time);
    }

    public sealed class StaticResult
    {
        public float TipDropFraction;      // (grip.y − tip.y) / rod length
        public float TopThirtyBendFraction; // share of total bend angle in the top 30% of the blank
        public float TipTension;
        public float PeakLoadRatio;
        public float TipAngleDeg;
    }

    /// <summary>
    /// Clamps the handle horizontal, hangs <paramref name="loadKg"/> on a short line from the tip, lets it settle,
    /// and measures the bend.
    /// </summary>
    public static StaticResult HangLoad(RodSpec rod, float loadKg, SimConfig? config = null, float seconds = 6f, bool guides = true)
    {
        var c = config ?? new SimConfig { TackleSettleDamping = 6f };
        var terminal = new TerminalSpec { MassKg = loadKg, VolumeM3 = 0f, DiameterM = 0.02f, DragCoefficient = 4f };
        var rig = new Rig(rod, StiffTestLine(), terminal, 64);
        rig.Line.SegmentLength = 0.05f;
        rig.GuideLineForces = guides;
        var grip = Vec2.Zero;
        rig.Reset(grip, 0f, 0.3f);
        var env = DryEnv();
        int ticks = (int)(seconds * c.TickHz);
        for (int t = 0; t < ticks; t++) rig.Step(c, c.Dt, env, grip, 0f, 0f);

        var r = rig.Rod;
        float handleAngle = MathF.Atan2(r.Y[1] - r.Y[0], r.X[1] - r.X[0]);
        float tipSegAngle = MathF.Atan2(r.Y[r.Tip] - r.Y[r.Tip - 1], r.X[r.Tip] - r.X[r.Tip - 1]);
        float total = Angles.Wrap(tipSegAngle - handleAngle);
        // Angle of the blank where s crosses 0.7.
        float at70 = 0f;
        for (int i = 2; i < r.Count; i++)
        {
            if (r.S[i] >= 0.7f)
            {
                at70 = Angles.Wrap(MathF.Atan2(r.Y[i] - r.Y[i - 1], r.X[i] - r.X[i - 1]) - handleAngle);
                break;
            }
        }
        return new StaticResult
        {
            TipDropFraction = (grip.Y - r.Y[r.Tip]) / rod.LengthM,
            TopThirtyBendFraction = MathF.Abs(total) > 1e-4f ? (total - at70) / total : 0f,
            TipTension = rig.Line.TipTension,
            PeakLoadRatio = r.PeakLoadRatio,
            TipAngleDeg = total * 180f / MathF.PI,
        };
    }

    /// <summary>Heavy, low-stretch braid so static rod tests measure the rod, not the line.</summary>
    public static LineSpec StiffTestLine() => new()
    {
        Id = "test_braid", Material = "braid", RatedLb = 80f, DiameterMm = 0.4f, Density = 970f, StretchAtBreak = 0.03f,
    };
}
