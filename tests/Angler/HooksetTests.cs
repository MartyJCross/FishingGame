using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Angler;

/// <summary>A hard hookset on a fish holding the bait must not break the rod or the line (owner, 2026-10-18).</summary>
public class HooksetTests
{
    private readonly ITestOutputHelper _out;
    public HooksetTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// Fish holding the bait 8 m out (the line end pinned to its mouth, as in the game), drag at about 2 kg. The rod
    /// sweeps from low to <paramref name="sweepTo"/> as fast as the mouse allows (target up to <paramref name="lead"/>
    /// rad ahead of the rod, like the input router).
    /// </summary>
    [Theory]
    [InlineData(1.6f, 0.6f)]
    [InlineData(2.2f, 0.6f)]
    [InlineData(3.1f, 0.6f)]
    [InlineData(1.6f, 1.5f)]
    [InlineData(3.1f, 1.5f)]
    public void HardHooksetBreaksNothing(float sweepTo, float lead)
    {
        var c = new SimConfig();
        var world = new SimWorld(c, 3);
        var a = world.Anglers[0];
        var tip = a.Rig.Rod.TipPos;
        var mouth = new Vec2(8f, -1.2f);
        var d = mouth - tip;
        a.Rig.Line.Reset(tip, d.Normalized(), d.Length);
        a.Reel.DragClick = 8; // 20 N, about 2 kg
        var frames = new InputFrame[1];
        float peakLine = 0f, peakRod = 0f, target = 0.4f;
        int lineBreaks = 0, rodBreaks = 0;
        float firstBreak = -1f, slipped = 0f;
        for (int t = 0; t < 120 * 3 && lineBreaks + rodBreaks == 0; t++)
        {
            float time = t * c.Dt;
            if (time > 1f) target = MathF.Min(sweepTo, a.Hands.Angle + lead);
            frames[0] = new InputFrame { RodTargetAngle = target, RodReach = 0.5f };
            a.Rig.Line.Pin(mouth);
            world.Step(frames);
            peakLine = MathF.Max(peakLine, a.Rig.Line.MaxTension);
            peakRod = MathF.Max(peakRod, a.Rig.Rod.PeakLoadRatio);
            slipped += a.Rig.SlippedThisTick;
            while (world.Events.TryPop(out var e))
            {
                if (e.Type == SimEventType.LineBroke) lineBreaks++;
                if (e.Type == SimEventType.RodBroke) rodBreaks++;
                if (firstBreak < 0f && e.Type is SimEventType.LineBroke or SimEventType.RodBroke) firstBreak = time;
            }
        }
        float limit = a.Rig.Line.Spec.BreakingForceN * a.Rig.Line.Spec.KnotFactor;
        _out.WriteLine($"sweep to {sweepTo:0.0} rad, lead {lead}: line peak {peakLine:0.0} N of {limit:0.0}, rod peak load {peakRod:0.00}, " +
                       $"drag {a.Reel.DragForce:0.0} N, line breaks {lineBreaks}, rod breaks {rodBreaks} at {firstBreak:0.00} s, rod angle {a.Hands.Angle:0.00}, drag slipped {slipped:0.00} m");
        Assert.Equal(0, rodBreaks);
        Assert.Equal(0, lineBreaks);
    }
}
