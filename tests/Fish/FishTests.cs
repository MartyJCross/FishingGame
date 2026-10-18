using TheLedger.Sim.Core;
using TheLedger.Sim.Fish;
using TheLedger.Sim.Input;
using TheLedger.Sim.Tackle;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Fish;

public class FishTests
{
    private readonly ITestOutputHelper _out;
    public FishTests(ITestOutputHelper output) => _out = output;

    private static readonly string SpeciesDir = Path.Combine(TestUtil.RepoRoot, "game", "data", "species");

    internal static List<SpeciesSpec> LoadSpecies(List<string>? errors = null)
    {
        var files = Directory.GetFiles(SpeciesDir, "*.json").OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal);
        return SpeciesLibrary.Load(files.Select(File.ReadAllText),
            r => File.Exists(Path.Combine(SpeciesDir, r)) ? File.ReadAllText(Path.Combine(SpeciesDir, r)) : null,
            errors ?? new List<string>());
    }

    internal static SpotStock PondStock() =>
        SpotStock.Load(File.ReadAllText(Path.Combine(TestUtil.RepoRoot, "game", "data", "spots", "station_pond.json")));

    [Fact]
    public void EverySpeciesLoadsAndPondSpeciesHaveShapes()
    {
        var errors = new List<string>();
        var all = LoadSpecies(errors);
        foreach (string e in errors) _out.WriteLine(e);
        Assert.Equal(Directory.GetFiles(SpeciesDir, "*.json").Length, all.Count);
        foreach (var entry in PondStock().Stock)
        {
            var sp = all.Single(s => s.Id == entry.Species);
            Assert.NotEqual("default", sp.Shape.Id);
            float w = sp.WeightKg(sp.Size.LengthCm.Max);
            Assert.InRange(w, sp.Size.RecordWeightKg * 0.5f, sp.Size.RecordWeightKg * 1.6f);
        }
    }

    [Fact]
    public void PondFishSwimAMinuteAndStayInTheWater()
    {
        var c = new SimConfig();
        var world = new SimWorld(c, 3);
        world.Populate(PondStock(), LoadSpecies());
        var start = world.Fish.Select(f => new Vec2(f.HeadX, f.HeadY)).ToArray();
        var frames = new InputFrame[1];
        float worstOut = 0f;
        for (int t = 0; t < 120 * 60; t++)
        {
            world.Step(frames);
            if (t % 60 != 0) continue;
            foreach (var f in world.Fish)
            {
                float surface = world.Water.Height(f.HeadX, world.Time), ground = world.Spot.GroundHeight(f.HeadX);
                worstOut = MathF.Max(worstOut, MathF.Max(f.HeadY - surface, ground - f.HeadY));
            }
        }
        float moved = world.Fish.Select((f, i) => (new Vec2(f.HeadX, f.HeadY) - start[i]).Length).Average();
        _out.WriteLine($"{world.Fish.Count} fish, mean displacement {moved:0.0} m after 60 s, worst outside water {worstOut * 100:0.0} cm, faults {world.FaultCount}");
        Assert.Equal(0, world.FaultCount);
        Assert.True(world.Fish.All(f => f.IsFinite()));
        Assert.True(worstOut < 0.01f);
        Assert.True(moved > 1f, "fish should cruise around");
    }

    [Fact]
    public void TurningFishStayWhereTheyAre()
    {
        // A fish reversing direction turns about its middle: its body centre never jumps (owner bug, v0.4.2).
        var world = new SimWorld(new SimConfig(), 9);
        world.Populate(PondStock(), LoadSpecies());
        var frames = new InputFrame[1];
        var last = world.Fish.Select(Centre).ToArray();
        float worst = 0f;
        int turns = 0;
        var facing = world.Fish.Select(f => f.FacingSign).ToArray();
        for (int t = 0; t < 120 * 60; t++)
        {
            world.Step(frames);
            for (int i = 0; i < world.Fish.Count; i++)
            {
                var f = world.Fish[i];
                float c = Centre(f);
                if (f.FacingSign != facing[i]) { turns++; facing[i] = f.FacingSign; }
                if (!f.Holding) worst = MathF.Max(worst, MathF.Abs(c - last[i]) / f.LengthM);
                last[i] = c;
            }
        }
        _out.WriteLine($"{turns} turns in 60 s; largest centre move in one tick {worst:0.000} body lengths");
        Assert.True(turns > 10);
        Assert.True(worst < 0.15f);

        static float Centre(TheLedger.Sim.Fish.Fish f) => f.X.Average();
    }

    [Fact]
    public void BluegillTakeAWormUnderABobber()
    {
        var c = new SimConfig { StartHour = 9f };
        var world = new SimWorld(c, 5);
        var stock = PondStock();
        stock.Stock = new List<StockEntry> { new() { Species = "bluegill", Count = 6 } };
        stock.RangeX = new[] { 5f, 12f };
        world.Populate(stock, LoadSpecies());
        var a = world.Anglers[0];
        var worm = Tackle.LureTests.LoadLure("01_bait_worm_bobber");
        a.Rig.Line.SetTerminal(worm);
        // As if cast 8 m out and left to settle.
        var tip = a.Rig.Rod.TipPos;
        a.Rig.Line.Reset(tip, new Vec2(1f, -tip.Y / 9f).Normalized(), 9f);
        var frames = new InputFrame[1];
        frames[0] = new InputFrame { RodTargetAngle = 0.6f, RodReach = 0.5f };
        int strikes = 0, bites = 0, spits = 0;
        double firstBite = -1;
        for (int t = 0; t < 120 * 120; t++)
        {
            world.Step(frames);
            while (world.Events.TryPop(out var e))
            {
                if (e.Type == SimEventType.FishStrike) strikes++;
                if (e.Type == SimEventType.Bite) { bites++; if (firstBite < 0) firstBite = world.Time; }
                if (e.Type == SimEventType.FishSpit) spits++;
            }
        }
        _out.WriteLine($"2 min with a worm under a bobber: {strikes} strikes, {bites} bites (first at {firstBite:0.0} s), {spits} spits, faults {world.FaultCount}");
        Assert.Equal(0, world.FaultCount);
        Assert.True(bites >= 1, "hungry bluegill around a worm should take it");
    }
}
