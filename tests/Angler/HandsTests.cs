using TheLedger.Sim.Angler;
using TheLedger.Sim.Core;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Angler;

public class HandsTests
{
    private readonly ITestOutputHelper _out;
    public HandsTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void HandsSettleOnTarget()
    {
        var c = new SimConfig();
        var h = new Hands(0f);
        for (int i = 0; i < 240; i++) h.Step(c, c.Dt, 1.2f, 0.5f);
        Assert.InRange(h.Angle, 1.19f, 1.21f);
        Assert.InRange(MathF.Abs(h.AngularVelocity), 0f, 0.05f);
    }

    [Fact]
    public void AFlickIsFastButNotInstant()
    {
        var c = new SimConfig();
        var h = new Hands(-0.8f);
        int ticks = 0;
        float peak = 0f;
        while (h.Angle < 1.9f && ticks < 600)
        {
            h.Step(c, c.Dt, 2.0f, 0.5f);
            peak = MathF.Max(peak, MathF.Abs(h.AngularVelocity));
            ticks++;
        }
        float seconds = ticks * c.Dt;
        _out.WriteLine($"Flick from -0.8 to 1.9 rad took {seconds:0.000} s, peak {peak:0.0} rad/s");
        Assert.InRange(seconds, 0.15f, 0.6f);
        Assert.True(peak <= c.HandMaxAngularSpeed + 1e-4f);
    }

    [Fact]
    public void AngleLimitsHold()
    {
        var c = new SimConfig();
        var h = new Hands(0f);
        for (int i = 0; i < 600; i++) h.Step(c, c.Dt, 3.1f, 1f);
        Assert.True(h.Angle <= c.HandMaxAngle + 1e-5f);
        for (int i = 0; i < 600; i++) h.Step(c, c.Dt, -3f, 1f);
        Assert.True(h.Angle >= c.HandMinAngle - 1e-5f);
    }
}
