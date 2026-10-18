using TheLedger.Sim.Core;
using TheLedger.Sim.Tackle;
using TheLedger.Sim.Water;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Tackle;

public class LureTests
{
    private readonly ITestOutputHelper _out;
    public LureTests(ITestOutputHelper output) => _out = output;

    internal static TerminalSpec LoadLure(string file) =>
        GearJson.Terminal(File.ReadAllText(Path.Combine(TestUtil.RepoRoot, "game", "data", "lures", file + ".json")));

    private static (Rig rig, WaterSurface water, Spot spot, SimConfig c, Vec2 grip) DeepWater(TerminalSpec lure, float lineOut)
    {
        var water = new WaterSurface(1) { Conditions = new WaterConditions { Density = 1000f, Depth = 6f, Fetch = 1f } };
        var spot = new Spot("deep", new[] { new Vec2(-50f, -6f), new Vec2(80f, -6f) });
        var c = new SimConfig();
        var rig = new Rig(RodSpec.MediumSpinning(), LineSpec.Mono10(), lure, c.MaxLineNodes);
        var grip = new Vec2(-1.8f, 1.2f);
        rig.Reset(grip, 0.3f, lineOut);
        // The cast has landed: line laid out across the surface from the tip.
        var tip = rig.Rod.TipPos;
        rig.Line.Reset(tip, new Vec2(1f, -tip.Y / lineOut).Normalized(), lineOut);
        return (rig, water, spot, c, grip);
    }

    /// <summary>Retrieves at a steady speed and returns the deepest the lure ran.</summary>
    private static float RunDepth(TerminalSpec lure, float speed, float lineOut = 20f, float seconds = 10f)
    {
        var (rig, water, spot, c, grip) = DeepWater(lure, lineOut);
        float deepest = 0f;
        for (int t = 0; t < (int)(seconds * 120); t++)
        {
            double time = t * (double)c.Dt;
            water.Step(c, c.Dt, time);
            rig.Step(c, c.Dt, new TackleEnvironment(water, spot, Vec2.Zero, time), grip, 0.3f, -speed * c.Dt);
            deepest = MathF.Max(deepest, rig.Lure.Depth);
        }
        Assert.True(rig.IsFinite());
        return deepest;
    }

    [Fact]
    public void CrankbaitDivesDeeperWhenRetrievedFaster()
    {
        var crank = LoadLure("04_crankbait_square_2in");
        float slow = RunDepth(crank, 0.4f);
        float fast = RunDepth(crank, 1.0f);
        _out.WriteLine($"Squarebill: deepest {slow:0.00} m at 0.4 m/s, {fast:0.00} m at 1.0 m/s");
        Assert.True(fast > slow + 0.15f, "faster retrieve should dive deeper");
        Assert.InRange(fast, 0.5f, 2.0f); // a squarebill runs shallow: roughly 0.5-1.5 m
    }

    [Fact]
    public void BobberHoldsTheBaitNearItsDepth()
    {
        var bait = LoadLure("01_bait_worm_bobber");
        var (rig, water, spot, c, grip) = DeepWater(bait, 6f);
        for (int t = 0; t < 120 * 8; t++)
        {
            double time = t * (double)c.Dt;
            water.Step(c, c.Dt, time);
            rig.Step(c, c.Dt, new TackleEnvironment(water, spot, Vec2.Zero, time), grip, 0.3f, 0f);
        }
        var l = rig.Line;
        float bobberY = l.Y[l.FloatIndex];
        float baitDepth = rig.Lure.Depth;
        _out.WriteLine($"Bobber at node {l.FloatIndex}: {bobberY * 100:0.0} cm from the surface; bait {baitDepth:0.00} m deep (set 0.9 m)");
        Assert.InRange(bobberY, -0.03f, 0.02f);
        Assert.InRange(baitDepth, 0.7f, 0.95f);
    }
}
