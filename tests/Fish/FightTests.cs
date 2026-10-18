using TheLedger.Sim.Core;
using TheLedger.Sim.Fish;
using TheLedger.Sim.Input;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Fish;

public class FightTests
{
    private readonly ITestOutputHelper _out;
    public FightTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void SweepingOnABiteSetsTheHook()
    {
        var c = new SimConfig { StartHour = 9f };
        var world = new SimWorld(c, 5);
        var stock = FishTests.PondStock();
        stock.Stock = new List<StockEntry> { new() { Species = "bluegill", Count = 6 } };
        stock.RangeX = new[] { 5f, 12f };
        world.Populate(stock, FishTests.LoadSpecies());
        var a = world.Anglers[0];
        a.Rig.Line.SetTerminal(Tackle.LureTests.LoadLure("01_bait_worm_bobber"));
        var tip = a.Rig.Rod.TipPos;
        a.Rig.Line.Reset(tip, new Vec2(1f, -tip.Y / 9f).Normalized(), 9f);
        var frames = new InputFrame[1];
        int bites = 0, sets = 0;
        float sweepUntil = -1f;
        for (int t = 0; t < 120 * 180 && sets == 0; t++)
        {
            float time = t * c.Dt;
            // Rod low and still; on a bite, sweep it up and reel.
            bool sweeping = time < sweepUntil;
            frames[0] = new InputFrame { RodTargetAngle = sweeping ? 1.6f : 0.6f, RodReach = 0.5f, CrankRate = sweeping ? 3f : 0f };
            world.Step(frames);
            // Bait thrown out of the water by a missed hookset: cast it back out.
            if (!a.Rig.Lure.Wet && time > sweepUntil + 3f && t % 360 == 0)
            {
                var tipNow = a.Rig.Rod.TipPos;
                a.Reel.LineOnSpool += a.Rig.Line.LineOut - 9f;
                a.Rig.Line.Reset(tipNow, new Vec2(1f, -tipNow.Y / 9f).Normalized(), 9f);
            }
            while (world.Events.TryPop(out var e))
            {
                // Strike on a real take, not on a nibble (the bobber goes under rather than twitching).
                if (e.Type == SimEventType.Bite) { bites++; if (world.Fish[e.Id].Style != BiteStyle.Nibble) sweepUntil = time + 0.5f; }
                if (e.Type == SimEventType.HookSet) sets++;
            }
        }
        _out.WriteLine($"{bites} bites before the hook was set ({sets} hooksets)");
        Assert.Equal(0, world.FaultCount);
        Assert.True(sets >= 1);
    }

    [Fact]
    public void TwoKiloBassOnTenPoundMonoAtQuarterDragIsLandable()
    {
        var c = new SimConfig { StartHour = 9f };
        var world = new SimWorld(c, 11);
        var species = FishTests.LoadSpecies();
        var bass = species.Single(s => s.Id == "florida_bass");
        world.Populate(new SpotStock { RangeX = new[] { 1f, 40f } }, species);
        float lenCm = MathF.Pow(2000f / bass.Size.LengthWeight.A, 1f / bass.Size.LengthWeight.B);
        var fish = new TheLedger.Sim.Fish.Fish(0, bass, lenCm * 0.01f, new Vec2(12f, -1.5f), true, 11);
        world.AddFish(fish);
        var a = world.Anglers[0];
        var lure = Tackle.LureTests.LoadLure("03_texas_worm_7in");
        a.Rig.Line.SetTerminal(lure);
        var tip = a.Rig.Rod.TipPos;
        var toFish = fish.Mouth - tip;
        a.Rig.Line.Reset(tip, toFish.Normalized(), toFish.Length);
        a.Reel.DragClick = 4; // 10 N, about a quarter of 10 lb
        fish.SetHooked(0, lure);

        var frames = new InputFrame[1];
        bool landed = false, broke = false;
        float drag = a.Reel.DragForce;
        int t;
        for (t = 0; t < 120 * 300 && !landed && !broke; t++)
        {
            // A patient angler: rod up at about 60°, reel when the fish isn't taking drag, try to land when it's close.
            float tension = a.Rig.Line.TipTension;
            float dist = MathF.Abs(fish.HeadX - a.Position.X);
            frames[0] = new InputFrame
            {
                RodTargetAngle = 1.05f, RodReach = 0.5f,
                CrankRate = tension < 0.7f * drag ? 2f : 0f,
                Buttons = dist < c.LandReach && t % 30 == 0 ? InputButtons.Land : InputButtons.None,
            };
            world.Step(frames);
            while (world.Events.TryPop(out var e))
            {
                if (e.Type == SimEventType.FishLanded) landed = true;
                if (e.Type is SimEventType.LineBroke or SimEventType.RodBroke or SimEventType.HookPulled)
                {
                    broke = true;
                    _out.WriteLine($"{e.Type} at {t / 120f:0.0} s: value {e.Value:0.00}, tip tension {a.Rig.Line.TipTension:0.0} N, line max {a.Rig.Line.MaxTension:0.0} N, hold {fish.HookHold:0.00}, move {fish.Move}, line out {a.Rig.Line.LineOut:0.0}");
                }
            }
        }
        _out.WriteLine($"{fish.MassKg:0.00} kg bass ({lenCm:0} cm), drag {drag:0.0} N: landed {landed} after {t / 120f:0} s, " +
                       $"failure {broke}, stamina {fish.StaminaA:0.00}/{fish.StaminaB:0.00}, faults {world.FaultCount}");
        Assert.Equal(0, world.FaultCount);
        Assert.False(broke);
        Assert.True(landed);
        Assert.InRange(t / 120f, 15f, 300f); // a real fight, not an instant winch
    }
}
