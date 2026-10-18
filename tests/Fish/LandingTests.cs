using System.Text.Json;
using TheLedger.Sim.Core;
using TheLedger.Sim.Fish;
using TheLedger.Sim.Input;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Fish;

/// <summary>
/// Landing a fish the way the owner plays (2026-10-18): in the real Station Pond, rod up, holding RMB to reel at the
/// steady rate, pressing F once the fish is near the bank.
/// </summary>
public class LandingTests
{
    private readonly ITestOutputHelper _out;
    public LandingTests(ITestOutputHelper output) => _out = output;

    internal static Spot StationPond()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestUtil.RepoRoot, "game", "data", "spots", "station_pond.json")));
        var r = doc.RootElement;
        var pts = r.GetProperty("profile").EnumerateArray().Select(p => new Vec2(p[0].GetSingle(), p[1].GetSingle())).ToList();
        return new Spot("station_pond", pts)
        {
            AnglerStart = new Vec2(r.GetProperty("anglerStart")[0].GetSingle(), r.GetProperty("anglerStart")[1].GetSingle()),
            WaterlineX = r.GetProperty("waterlineX").GetSingle(),
        };
    }

    private float RodAngle = 1.05f;

    [Theory]
    [InlineData("florida_bass", 2.0f, 1.05f)]
    [InlineData("florida_bass", 0.6f, 1.05f)]
    [InlineData("bluegill", 0.2f, 1.05f)]
    [InlineData("channel_catfish", 1.5f, 1.05f)]
    [InlineData("florida_bass", 2.0f, 0.5f)]  // rod held low
    [InlineData("bluegill", 0.2f, 1.6f)]      // rod straight up
    public void ReelItInAndLandIt(string speciesId, float kg, float rodAngle)
    {
        RodAngle = rodAngle;
        var c = new SimConfig { StartHour = 9f };
        var world = new SimWorld(c, 11, 1, StationPond());
        var species = FishTests.LoadSpecies();
        var sp = species.Single(s => s.Id == speciesId);
        world.Populate(new SpotStock { RangeX = new[] { 1f, 40f } }, species);
        float lenCm = MathF.Pow(kg * 1000f / sp.Size.LengthWeight.A, 1f / sp.Size.LengthWeight.B);
        var fish = new TheLedger.Sim.Fish.Fish(0, sp, lenCm * 0.01f, new Vec2(12f, -1.5f), true, 11);
        world.AddFish(fish);
        var a = world.Anglers[0];
        var lure = Tackle.LureTests.LoadLure("03_texas_worm_7in");
        a.Rig.Line.SetTerminal(lure);
        var tip = a.Rig.Rod.TipPos;
        var toFish = fish.Mouth - tip;
        a.Rig.Line.Reset(tip, toFish.Normalized(), toFish.Length);
        a.Reel.DragClick = 8; // ~2 kg, what the owner had
        fish.SetHooked(0, lure);
        var frames = new InputFrame[1];
        bool landed = false, lost = false;
        int fails = 0, jumps = 0;
        float closest = 99f, closestDepth = 0f, staminaThen = 0f;
        string lostHow = "";
        int t;
        for (t = 0; t < 120 * 240 && !landed && !lost; t++)
        {
            float dist = MathF.Abs(fish.HeadX - a.Position.X);
            float depth = world.Water.Height(fish.HeadX, world.Time) - fish.HeadY;
            if (dist < closest) { closest = dist; closestDepth = depth; staminaThen = fish.StaminaA; }
            frames[0] = new InputFrame
            {
                RodTargetAngle = RodAngle, RodReach = 0.5f, CrankRate = 1.5f, // RMB held: steady reel
                Buttons = dist < 4f && t % 60 == 0 ? InputButtons.Land : InputButtons.None,
            };
            world.Step(frames);
            while (world.Events.TryPop(out var e))
            {
                if (e.Type == SimEventType.FishLanded) landed = true;
                if (e.Type == SimEventType.LandFailed) fails++;
                if (e.Type == SimEventType.FishJumped) jumps++;
                if (e.Type is SimEventType.LineBroke or SimEventType.RodBroke or SimEventType.HookPulled) { lost = true; lostHow = e.Type.ToString(); }
            }
        }
        _out.WriteLine($"{speciesId} {fish.MassKg:0.00} kg: landed {landed} after {t / 120f:0} s, lost {lostHow}, F refused {fails}x, {jumps} jumps; " +
                       $"closest {closest:0.00} m from the angler at depth {closestDepth:0.00} m, stamina then {staminaThen:0.00}; " +
                       $"now at x {fish.HeadX:0.00}, depth {world.Water.Height(fish.HeadX, world.Time) - fish.HeadY:0.00}, line out {a.Rig.Line.LineOut:0.0} m");
        Assert.True(landed);
        Assert.True(fails <= 2, "F should work once the fish is at the bank");
    }
}
