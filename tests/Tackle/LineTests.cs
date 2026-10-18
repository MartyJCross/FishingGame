using TheLedger.Sim.Core;
using TheLedger.Sim.Tackle;
using TheLedger.Sim.Water;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Tackle;

public class LineTests
{
    private readonly ITestOutputHelper _out;
    public LineTests(ITestOutputHelper output) => _out = output;

    private static LineSpec LoadLine(string id) =>
        GearJson.Line(File.ReadAllText(Path.Combine(TestUtil.RepoRoot, "game", "data", "gear", "lines", id + ".json")));

    /// <summary>A rod so stiff it doesn't bend, so line tests measure the line.</summary>
    private static RodSpec Broomstick() => new()
    {
        EIButt = 1e5f, EITip = 5e4f, BlankMassKg = 0.5f, MaxMomentButt = 1e6f, MaxMomentTip = 1e6f,
    };

    [Theory]
    [InlineData("line_mono_10lb")]
    [InlineData("line_fluoro_10lb")]
    [InlineData("line_braid_20lb")]
    public void StretchAtHalfBreakingLoadMatchesTheMaterial(string id)
    {
        var line = LoadLine(id);
        float load = 0.5f * line.BreakingForceN;
        var c = new SimConfig { TackleSettleDamping = 8f };
        var rig = new Rig(Broomstick(), line, new TerminalSpec { MassKg = load / c.Gravity, VolumeM3 = 0f }, 64);
        rig.Reset(Vec2.Zero, 0f, 2.0f);
        var env = TackleTestUtil.DryEnv();
        for (int t = 0; t < 120 * 6; t++) rig.Step(c, c.Dt, env, Vec2.Zero, 0f, 0f);

        float measured = rig.Line.CurrentLength(rig.Rod.TipPos) / rig.Line.LineOut - 1f;
        float expected = 0.5f * line.StretchAtBreak;
        _out.WriteLine($"{id}: {load:0.0} N (half of {line.RatedLb} lb): stretch {measured:P2}, material says {expected:P2}, tip tension {rig.Line.TipTension:0.0} N");
        Assert.InRange(measured, expected * 0.9f, expected * 1.1f);
    }

    [Fact]
    public void SlackLineSagsAndNeverPushes()
    {
        // Tip at (0,0); the sinker rests on a ledge 2 m away at the same height; 2.6 m of line between them.
        var spot = new Spot("ledge", new[]
        {
            new Vec2(-50f, -50f), new Vec2(1.9f, -50f), new Vec2(1.95f, -0.01f), new Vec2(3f, -0.01f), new Vec2(3.05f, -50f), new Vec2(50f, -50f),
        });
        var water = new WaterSurface(1) { TideLevel = -200f };
        var env = new TackleEnvironment(water, spot, Vec2.Zero, 0);
        var c = new SimConfig { TackleSettleDamping = 4f };
        var rig = new Rig(Broomstick(), LoadLine("line_mono_10lb"), TerminalSpec.HalfOunceSinker(), 64);
        var grip = new Vec2(-2.13f + 0.4f, 0f);
        rig.Reset(grip, 0f, 2.6f);
        // Lay the line from the tip to the ledge, then let it hang.
        var tip = rig.Rod.TipPos;
        rig.Line.Reset(tip, new Vec2(1f, 0f), 2.6f);
        int term = rig.Line.TerminalIndex;
        rig.Line.X[term] = 2.2f; rig.Line.Y[term] = 0f;
        float startX = rig.Line.X[term];
        for (int t = 0; t < 120 * 5; t++) rig.Step(c, c.Dt, env, grip, 0f, 0f);

        float lowest = 0f;
        for (int k = 0; k < rig.Line.Count - 1; k++) lowest = MathF.Min(lowest, rig.Line.Y[k]);
        float moved = MathF.Abs(rig.Line.X[term] - startX);
        _out.WriteLine($"Slack 2.6 m over a 2.2 m gap: lowest point {lowest:0.000} m, sinker moved {moved * 1000:0.0} mm, max tension {rig.Line.MaxTension:0.000} N");
        Assert.True(lowest < -0.3f, "slack line should hang well below the straight line");
        Assert.True(moved < 0.02f, "slack line must not push the sinker");
        Assert.True(rig.Line.MaxTension < 0.05f, "slack line carries almost no tension");
    }

    [Fact]
    public void PayingOutAndRetrievingTracksLineOut()
    {
        var c = new SimConfig();
        var rig = new Rig(RodSpec.MediumSpinning(), LoadLine("line_mono_10lb"), TerminalSpec.HalfOunceSinker(), c.MaxLineNodes);
        rig.Reset(Vec2.Zero, 0.5f, 1.0f);
        var env = TackleTestUtil.DryEnv();
        int startNodes = rig.Line.Count;
        // Pay out 6 m over 6 s, then retrieve 5.5 m over 5.5 s.
        for (int t = 0; t < 720; t++) rig.Step(c, c.Dt, env, Vec2.Zero, 0.5f, 1f * c.Dt);
        float outAfterPay = rig.Line.LineOut;
        int nodesAfterPay = rig.Line.Count;
        for (int t = 0; t < 660; t++) rig.Step(c, c.Dt, env, Vec2.Zero, 0.5f, -1f * c.Dt);
        float outAfterRetrieve = rig.Line.LineOut;
        _out.WriteLine($"Line out 1.0 → {outAfterPay:0.000} m ({startNodes} → {nodesAfterPay} nodes) → {outAfterRetrieve:0.000} m ({rig.Line.Count} nodes)");
        Assert.InRange(outAfterPay, 6.99f, 7.01f);
        Assert.InRange(outAfterRetrieve, 1.49f, 1.51f);
        Assert.True(nodesAfterPay > startNodes && rig.Line.Count < nodesAfterPay);
        Assert.True(rig.IsFinite());
    }

    [Fact]
    public void RetrieveStopsAtTheMinimum()
    {
        var c = new SimConfig();
        var rig = new Rig(RodSpec.MediumSpinning(), LoadLine("line_mono_10lb"), TerminalSpec.HalfOunceSinker(), c.MaxLineNodes);
        rig.Reset(Vec2.Zero, 0.5f, 1.0f);
        var env = TackleTestUtil.DryEnv();
        for (int t = 0; t < 240; t++) rig.Step(c, c.Dt, env, Vec2.Zero, 0.5f, -1f * c.Dt);
        Assert.InRange(rig.Line.LineOut, c.MinLineOut - 0.001f, c.MinLineOut + 0.001f);
    }

    [Fact]
    public void BraidStaysUpAndFluorocarbonSinks()
    {
        // 3 m laid on calm water from a low rod tip, a feather on the end. Braid (970 kg/m³) is barely lighter than water
        // and stays at the surface; fluorocarbon (1780 kg/m³) sinks (a few cm/s for 0.28 mm line).
        float Depth(string id)
        {
            var water = new WaterSurface(1);
            water.Conditions = new WaterConditions { Density = 1000f, Depth = 5f, Fetch = 1f };
            var spot = new Spot("deep", new[] { new Vec2(-50f, -50f), new Vec2(50f, -50f) });
            var c = new SimConfig();
            var rig = new Rig(Broomstick(), LoadLine(id), new TerminalSpec { MassKg = 0.0001f, VolumeM3 = 1e-7f }, 64);
            var grip = new Vec2(-1.8f, 0.05f);
            rig.Reset(grip, 0f, 3f);
            rig.Line.Reset(rig.Rod.TipPos + new Vec2(0f, -0.05f), new Vec2(1f, 0f), 3f);
            for (int t = 0; t < 120 * 8; t++)
            {
                water.Step(c, c.Dt, t * (double)c.Dt);
                rig.Step(c, c.Dt, new TackleEnvironment(water, spot, Vec2.Zero, t * (double)c.Dt), grip, 0f, 0f);
            }
            float deepest = 0f;
            for (int k = 0; k < rig.Line.Count; k++) deepest = MathF.Max(deepest, -rig.Line.Y[k]);
            return deepest;
        }
        float braid = Depth("line_braid_20lb");
        float fluoro = Depth("line_fluoro_10lb");
        _out.WriteLine($"After 8 s on the water: braid's deepest point {braid * 100:0.0} cm, fluorocarbon's {fluoro * 100:0.0} cm");
        Assert.True(braid < 0.03f, "braid stays at the surface");
        Assert.True(fluoro > 0.15f, "fluorocarbon sinks");
    }
}
