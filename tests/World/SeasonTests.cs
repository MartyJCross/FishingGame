using TheLedger.Sim.Core;
using TheLedger.Sim.World;

namespace TheLedger.Sim.Tests.World;

public class SeasonTests
{
    private static SimWorld At(float day, float springMix)
    {
        var c = new SimConfig { StartDay = day, StartHour = 12f };
        var pond = Spot.TestPond();
        var spot = new Spot("t", Enumerable.Range(0, pond.PointCount).Select(pond.Point).ToList()) { SpringMix = springMix, SpringC = 22.8f };
        return new SimWorld(c, 1, 1, spot);
    }

    [Fact]
    public void PondsSwingWithTheSeasonsAndSpringsHoldSteady()
    {
        // Game year: 28 days from 1 December. Day 3 is early January, day 17 early July.
        float pondJan = At(3, 0f).WaterTempC, pondJul = At(17, 0f).WaterTempC;
        float springJan = At(3, 0.75f).WaterTempC, springJul = At(17, 0.75f).WaterTempC;
        Assert.InRange(pondJan, 13f, 18f);
        Assert.InRange(pondJul, 28f, 32f);
        Assert.True(springJan > pondJan + 4f, "spring water keeps a bay warmer than a pond in January");
        Assert.True(springJul < pondJul - 3f, "and cooler in July");
        Assert.Equal("winter", At(3, 0f).Season);
        Assert.Equal("summer", At(17, 0f).Season);
        Assert.Equal("winter", At(28 + 3, 0f).Season); // the next year
    }
}
