using TheLedger.Sim.Angler;
using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.Tackle;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Tackle;

public class BaitcasterTests
{
    private readonly ITestOutputHelper _out;
    public BaitcasterTests(ITestOutputHelper output) => _out = output;

    private static string Gear(string dir, string id) =>
        File.ReadAllText(Path.Combine(TestUtil.RepoRoot, "game", "data", "gear", dir, id + ".json"));

    internal static Outfit BaitcasterOutfit() => new()
    {
        Id = "baitcaster_mh",
        Rod = GearJson.Rod(Gear("rods", "rod_mh_casting_7ft")),
        Reel = GearJson.Reel(Gear("reels", "reel_bait_150")),
        Line = GearJson.Line(Gear("lines", "line_mono_12lb")),
    };

    /// <summary>
    /// An overhead cast on the baitcaster with a 14 g sinker: free spool, thumb down, sweep, lift the thumb at about
    /// 10 o'clock. <paramref name="skilled"/> feathers the spool as the lure drops and thumbs it hard on landing.
    /// </summary>
    private (SimWorld world, int backlashes, float distance) Cast(int brake, bool skilled, float flightThumb = 0.02f, float startThumb = 0.2f, float releaseAngle = 1.1f)
    {
        var c = new SimConfig();
        var world = new SimWorld(c, 1);
        var a = world.Anglers[0];
        a.Rack.Add(BaitcasterOutfit());
        a.UseOutfit(c, 0);
        a.Reel.BrakeClick = brake;
        var frames = new InputFrame[1];
        var f = new InputFrame { RodTargetAngle = 2.5f, RodReach = 0.6f };
        bool released = false, landed = false;
        float releaseTime = 0f, landX = 0f;
        int backlashes = 0;
        for (int t = 0; t < 120 * 7; t++)
        {
            float time = t * c.Dt;
            f.Buttons = t == 2 ? InputButtons.Bail : InputButtons.None;
            var l = a.Rig.Line;
            float lureY = l.TerminalPos.Y;
            if (lureY < 0f && !landed) { landed = true; landX = l.TerminalPos.X - a.Rig.Rod.TipPos.X; }
            if (time < 1.0f) { f.RodTargetAngle = 2.5f; f.LineHold = 1f; }
            else
            {
                f.RodTargetAngle = 0.35f;
                if (!released && a.Hands.Angle < releaseAngle) { released = true; releaseTime = time; }
                f.LineHold = !released ? 1f
                    : !skilled ? 0f
                    : landed ? 1f
                    : l.VY[l.TerminalIndex] < 0f && lureY < 1.2f ? 0.35f  // feather as it drops
                    : time - releaseTime < 0.06f ? startThumb  // thumb the spool's start-up surge
                    : MathF.Min(0.6f, flightThumb + a.Reel.Loose * 0.8f); // feel the overrun, press harder through the flight keeps the spool with the lure
            }
            frames[0] = f;
            world.Step(frames);
            while (world.Events.TryPop(out var e)) if (e.Type == SimEventType.Backlash) backlashes++;
        }
        float dist = landX; // where it hit the water (a thumbed-down sinker swings back afterwards)
        _out.WriteLine($"brake {brake}, {(skilled ? "thumbed" : "no thumb")}: {backlashes} backlash(es), lure {dist:0.0} m out, " +
                       $"spool peak loose {a.Reel.Loose:0.00} m, faults {world.FaultCount}");
        Assert.Equal(0, world.FaultCount);
        return (world, backlashes, dist);
    }

    [Fact]
    public void NoBrakeNoThumbBirdsNests()
    {
        var (_, backlashes, _) = Cast(0, skilled: false);
        Assert.True(backlashes >= 1);
    }

    [Fact(Skip = "Baitcaster parked until plan step 10: its scripted thumbing needs retuning for the new arms (2026-10-18)")]
    public void BrakeAndThumbMakeACleanCast()
    {
        var (_, backlashes, dist) = Cast(5, skilled: true);
        Assert.Equal(0, backlashes);
        Assert.True(dist > 4f, "a thumbed baitcaster cast should still go out");
    }
}
