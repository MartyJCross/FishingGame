using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.Tackle;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Tackle;

public class ReelTests
{
    private readonly ITestOutputHelper _out;
    public ReelTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// A scripted overhead cast with the half-ounce sinker: open the bail, finger on the line, rod back, sweep forward,
    /// let go at about 10 o'clock. Returns the world after it has landed and settled.
    /// </summary>
    internal static SimWorld Cast(float releaseAngle, out float lineBefore, InputRecording? rec = null, float lip = -1f)
    {
        var c = new SimConfig();
        var world = new SimWorld(c, 1);
        if (lip >= 0f) world.Anglers[0].Reel.Spec.LipFrictionN = lip;
        var frame = new InputFrame { RodTargetAngle = 2.5f, RodReach = 0.6f };
        var frames = new InputFrame[1];
        lineBefore = world.Anglers[0].Reel.LineOnSpool + world.Anglers[0].Rig.Line.LineOut;
        bool released = false;
        for (int t = 0; t < 120 * 7; t++)
        {
            float time = t * c.Dt;
            frame.Buttons = t == 2 ? InputButtons.Bail : InputButtons.None;
            if (time < 1.0f) { frame.RodTargetAngle = 2.5f; frame.LineHold = 1f; }      // load up behind
            else if (time < 1.6f)
            {
                frame.RodTargetAngle = 0.35f;                                             // sweep forward
                if (!released && world.Anglers[0].Hands.Angle < releaseAngle) released = true;
                frame.LineHold = released ? 0f : 1f;
            }
            else frame.LineHold = 0f;
            frames[0] = frame;
            rec?.AddTick(frames);
            world.Step(frames);
        }
        return world;
    }

    [Fact]
    public void OverheadCastFliesAndConservesLine()
    {
        var rec = new InputRecording { Seed = 1, AnglerCount = 1, TickHz = 120, Setup = "scene=test" };
        var world = Cast(1.1f, out float lineBefore, rec);
        // Kept in the repo so the game can replay it: --replay res://data/replays/cast_overhead.rec
        rec.Save(Path.Combine(TestUtil.RepoRoot, "game", "data", "replays", "cast_overhead.rec"));
        var a = world.Anglers[0];
        float dist = a.Rig.Line.TerminalPos.X - a.Rig.Rod.TipPos.X;
        float total = a.Reel.LineOnSpool + a.Rig.Line.LineOut;
        int splashes = 0;
        while (world.Events.TryPop(out var e)) if (e.Type == SimEventType.Splash) splashes++;
        _out.WriteLine($"Cast: sinker {dist:0.0} m out, line out {a.Rig.Line.LineOut:0.0} m ({a.Rig.Line.Count} nodes), " +
                       $"spool+out {total:0.000} vs {lineBefore:0.000} m, splashes {splashes}, faults {world.FaultCount}");
        Assert.Equal(0, world.FaultCount);
        Assert.True(dist > 12f, "a half-ounce sinker on a 7 ft rod should fly well past 12 m");
        Assert.InRange(total, lineBefore - 0.05f, lineBefore + 0.05f);
        Assert.True(splashes >= 1, "landing should splash");
    }

    [Fact]
    public void DragSlipsNearItsSettingAndCrankingStallsUnderLoad()
    {
        var c = new SimConfig();
        var rig = new Rig(RodSpec.MediumSpinning(), LineSpec.Mono10(), new TerminalSpec { MassKg = 2f, VolumeM3 = 0f }, 64);
        var reel = new Reel(ReelSpec.Spinning2500(), 100f) { DragClick = 4 };  // 10 N drag, 2 kg hanging = 19.6 N
        rig.Reset(new Vec2(0f, 2f), 0f, 1.0f);
        var env = TackleTestUtil.DryEnv();
        var input = new InputFrame { CrankRate = 2f };
        float tensionSum = 0f;
        int n = 0;
        for (int t = 0; t < 240; t++)
        {
            reel.BeginTick(c, c.Dt, input, rig.Line.Spec, rig.Line.TipTension);
            rig.Step(c, c.Dt, env, new Vec2(0f, 2f), 0f, reel.Retrieve, reel.SlipForce);
            reel.EndTick(c.Dt, rig.SlippedThisTick, rig.TakenThisTick);
            if (t > 120) { tensionSum += rig.Line.TipTension; n++; }
        }
        float mean = tensionSum / n;
        _out.WriteLine($"Drag {reel.DragForce:0.0} N: mean tip tension while slipping {mean:0.0} N, line out {rig.Line.LineOut:0.00} m, slipping {reel.DragSlipping}");
        Assert.True(reel.DragSlipping);
        Assert.InRange(mean, reel.DragForce / reel.Spec.DragStickRatio * 0.85f, reel.DragForce * 1.15f);
        Assert.True(rig.Line.LineOut > 1.5f, "a weight heavier than the drag pulls line off despite cranking");
    }
}
