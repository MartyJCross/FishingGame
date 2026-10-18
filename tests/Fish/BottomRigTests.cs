using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Fish;

/// <summary>The starting bottom rig (worm on a sinker) must actually catch fish in the Station Pond (owner, 2026-10-18).</summary>
public class BottomRigTests
{
    private readonly ITestOutputHelper _out;
    public BottomRigTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void WormOnTheBottomGetsBites()
    {
        var c = new SimConfig { StartHour = 7f };
        var world = new SimWorld(c, 5, 1, LandingTests.StationPond());
        world.Populate(FishTests.PondStock(), FishTests.LoadSpecies());
        var a = world.Anglers[0];
        a.Rig.Line.SetTerminal(Tackle.LureTests.LoadLure("00_sinker_half_oz"));
        var tip = a.Rig.Rod.TipPos;
        var spot = new Vec2(8f, world.Spot.GroundHeight(8f) + 0.05f);
        a.Rig.Line.Reset(tip, (spot - tip).Normalized(), (spot - tip).Length);
        var frames = new InputFrame[1];
        int strikes = 0, bites = 0;
        for (int t = 0; t < 120 * 180; t++)
        {
            frames[0] = new InputFrame { RodTargetAngle = 0.8f, RodReach = 0.5f };
            world.Step(frames);
            while (world.Events.TryPop(out var e))
            {
                if (e.Type == SimEventType.FishStrike) strikes++;
                if (e.Type == SimEventType.Bite) bites++;
            }
        }
        _out.WriteLine($"bottom rig, 3 min at dawn: {strikes} strikes, {bites} bites");
        Assert.True(bites >= 1, "fish should take a worm lying on the bottom");
    }
}
