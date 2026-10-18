using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Angler;

/// <summary>Swinging the rod around with the lure hanging at the tip must never break the line (owner, 2026-10-18).</summary>
public class RodSwingTests
{
    private readonly ITestOutputHelper _out;
    public RodSwingTests(ITestOutputHelper output) => _out = output;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BigBackswingFromRestKeepsTheLine(bool bailOpen)
    {
        var c = new SimConfig();
        var world = new SimWorld(c, 1);
        var a = world.Anglers[0];
        var frames = new InputFrame[1];
        int breaks = 0;
        float peak = 0f;
        for (int t = 0; t < 120 * 4; t++)
        {
            float time = t * c.Dt;
            // Rest, then rip it back, then rip it forward, then back again.
            float target = time < 1f ? 0.6f : time < 2f ? 3.1f : time < 3f ? -1.0f : 3.1f;
            frames[0] = new InputFrame
            {
                RodTargetAngle = target, RodReach = 0.5f, LineHold = bailOpen ? 1f : 0f,
                Buttons = bailOpen && t == 2 ? InputButtons.Bail : InputButtons.None,
            };
            world.Step(frames);
            peak = MathF.Max(peak, a.Rig.Line.MaxTension);
            while (world.Events.TryPop(out var e)) if (e.Type == SimEventType.LineBroke) breaks++;
        }
        float limit = a.Rig.Line.Spec.BreakingForceN * a.Rig.Line.Spec.KnotFactor;
        _out.WriteLine($"bail {(bailOpen ? "open" : "closed")}: peak tension {peak:0.0} N of {limit:0.0}, breaks {breaks}");
        Assert.Equal(0, breaks);
    }
}
