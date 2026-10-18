using TheLedger.Sim.Core;
using TheLedger.Sim.Tackle;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Tackle;

public class RodTests
{
    private readonly ITestOutputHelper _out;
    public RodTests(ITestOutputHelper output) => _out = output;

    public static IEnumerable<RodSpec> DataRods() =>
        Directory.GetFiles(Path.Combine(TestUtil.RepoRoot, "game", "data", "gear", "rods"), "*.json")
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => GearJson.Rod(File.ReadAllText(f)));

    [Fact]
    public void EveryRodInDataLoadsAndMatchesItsFileName()
    {
        var files = Directory.GetFiles(Path.Combine(TestUtil.RepoRoot, "game", "data", "gear", "rods"), "*.json");
        Assert.NotEmpty(files);
        foreach (var f in files)
        {
            var rod = GearJson.Rod(File.ReadAllText(f));
            Assert.Equal(Path.GetFileNameWithoutExtension(f), rod.Id);
            Assert.True(rod.LengthM > rod.HandleLengthM);
            Assert.True(rod.EIButt > rod.EITip && rod.EITip > 0f, $"{rod.Id}: EI must taper from butt to tip");
            Assert.True(rod.MaxMomentButt > rod.MaxMomentTip, $"{rod.Id}: strength must taper");
            Assert.All(rod.GuideNodes, g => Assert.InRange(g, 1, rod.BlankNodes));
        }
    }

    /// <summary>
    /// Butt clamped horizontal, a weight hung from the tip. Prints the table for every rod in the data; the bands are
    /// starting calibration targets (a 7 ft medium spinning rod with 1 kg hanging should bend deeply but not fold).
    /// </summary>
    [Fact]
    public void DataRodsDeflectUnderLoadInTheirBands()
    {
        var bands = new Dictionary<string, (float lo, float hi)>
        {
            ["rod_ul_spinning_6ft"] = (0.40f, 0.75f),
            ["rod_m_spinning_7ft"] = (0.28f, 0.50f),
            ["rod_mh_casting_7ft"] = (0.15f, 0.35f),
        };
        _out.WriteLine("rod                    |  50 g drop | 250 g drop top30 | 1 kg drop  tip angle  load/limit");
        foreach (var rod in DataRods())
        {
            var r50 = TackleTestUtil.HangLoad(rod, 0.05f);
            var r250 = TackleTestUtil.HangLoad(rod, 0.25f);
            var r1k = TackleTestUtil.HangLoad(rod, 1.0f);
            _out.WriteLine($"{rod.Id,-22} |   {r50.TipDropFraction,6:P1}  |  {r250.TipDropFraction,6:P1}  {r250.TopThirtyBendFraction,5:P0} | {r1k.TipDropFraction,6:P1}  {r1k.TipAngleDeg,6:0}°   {r1k.PeakLoadRatio,5:0.00}");
            Assert.True(r50.TipDropFraction < r250.TipDropFraction && r250.TipDropFraction < r1k.TipDropFraction,
                $"{rod.Id}: more load must bend more");
            if (bands.TryGetValue(rod.Id, out var band))
                Assert.InRange(r1k.TipDropFraction, band.lo, band.hi);
        }
    }

    [Fact]
    public void FasterActionBendsNearerTheTip()
    {
        // Same stiffness range; only the action exponent changes (docs/04 §4.2: larger exponent = faster action).
        var actions = new[] { ("slow", 0.7f), ("moderate", 1.2f), ("fast", 2.2f), ("extra-fast", 3.0f) };
        float last = -1f;
        foreach (var (label, a) in actions)
        {
            var spec = new RodSpec { EIButt = 55f, EITip = 0.25f, ActionExponent = a };
            var r = TackleTestUtil.HangLoad(spec, 0.25f);
            _out.WriteLine($"{label,-11} a={a:0.0}: {r.TopThirtyBendFraction:P0} of the bend in the top 30%, tip drop {r.TipDropFraction:P1}");
            Assert.True(r.TopThirtyBendFraction > last, $"{label} should bend nearer the tip than the previous action");
            last = r.TopThirtyBendFraction;
            if (label == "slow") Assert.True(r.TopThirtyBendFraction < 0.30f);
            if (label == "fast") Assert.True(r.TopThirtyBendFraction > 0.50f);
        }
    }

    [Fact]
    public void BendDoesNotDependOnSolverSettings()
    {
        var rod = RodSpec.MediumSpinning();
        var baseline = TackleTestUtil.HangLoad(rod, 0.5f, new SimConfig { TackleSettleDamping = 6f });
        var fine = TackleTestUtil.HangLoad(rod, 0.5f, new SimConfig { TackleSettleDamping = 6f, TackleSubsteps = 24, TackleIterations = 3 });
        _out.WriteLine($"0.5 kg drop: default solver {baseline.TipDropFraction:P2}, 24 substeps x 3 iterations {fine.TipDropFraction:P2}");
        Assert.InRange(fine.TipDropFraction, baseline.TipDropFraction * 0.92f, baseline.TipDropFraction * 1.08f);
    }

    [Fact]
    public void UnloadedRodBarelyDroops()
    {
        foreach (var rod in DataRods())
        {
            var r = TackleTestUtil.HangLoad(rod, 0.0005f);
            _out.WriteLine($"{rod.Id}: droop under its own weight {r.TipDropFraction:P2}");
            Assert.InRange(r.TipDropFraction, 0f, 0.03f);
        }
    }

    [Fact]
    public void ReleasedRodVibratesAndSettles()
    {
        // Bend the rod with 0.5 kg, cut the load, and watch the tip ring down (no settle damping: real material damping only).
        var c = new SimConfig();
        var rod = RodSpec.MediumSpinning();
        var rig = new Rig(rod, TackleTestUtil.StiffTestLine(), new TerminalSpec { MassKg = 0.5f, VolumeM3 = 0f }, 64);
        rig.Reset(Vec2.Zero, 0f, 0.3f);
        var env = TackleTestUtil.DryEnv();
        var settle = new SimConfig { TackleSettleDamping = 6f };
        for (int t = 0; t < 600; t++) rig.Step(settle, c.Dt, env, Vec2.Zero, 0f, 0f);

        // "Cut" the line: a fresh rig with a feather on the end, starting from the bent rod's shape.
        var free = new Rig(rod, TackleTestUtil.StiffTestLine(), new TerminalSpec { MassKg = 0.001f, VolumeM3 = 0f }, 64);
        free.Reset(Vec2.Zero, 0f, 0.3f);
        Array.Copy(rig.Rod.X, free.Rod.X, rod.BlankNodes + 2);
        Array.Copy(rig.Rod.Y, free.Rod.Y, rod.BlankNodes + 2);
        free.Line.Reset(free.Rod.TipPos, new Vec2(0f, -1f), 0.3f);

        float startY = free.Rod.Y[free.Rod.Tip];
        int crossings = 0;
        float prev = startY, restY = 0f;
        float lastBig = 0f;
        for (int t = 0; t < 120 * 4; t++)
        {
            free.Step(c, c.Dt, env, Vec2.Zero, 0f, 0f);
            float y = free.Rod.Y[free.Rod.Tip];
            if (t < 120 && (prev - restY) * (y - restY) < 0f) crossings++;
            prev = y;
            if (t >= 120 * 3) lastBig = MathF.Max(lastBig, MathF.Abs(y - restY));
            if (t == 120 * 3 - 1) restY = y;
        }
        _out.WriteLine($"Released from {startY:0.000} m: {crossings} rest crossings in the first second (~{crossings / 2f:0.0} Hz), amplitude after 3 s {lastBig * 1000:0.0} mm");
        Assert.InRange(crossings / 2f, 1.5f, 15f);
        Assert.True(lastBig < 0.01f, "the blank should stop quivering within a few seconds");
    }
}
