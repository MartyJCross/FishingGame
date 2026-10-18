using TheLedger.Sim.Core;
using TheLedger.Sim.World;

namespace TheLedger.Sim.Tests.World;

public class SeasonTests
{
    private static SimWorld At(float day, float springMix)
    {
        var c = new SimConfig { StartDayOfYear = day, StartHour = 12f };
        var pond = Spot.TestPond();
        var spot = new Spot("t", Enumerable.Range(0, pond.PointCount).Select(pond.Point).ToList()) { SpringMix = springMix, SpringC = 22.8f };
        return new SimWorld(c, 1, 1, spot);
    }

    [Fact]
    public void PondsSwingWithTheSeasonsAndSpringsHoldSteady()
    {
        float pondJan = At(15, 0f).WaterTempC, pondJul = At(196, 0f).WaterTempC;
        float springJan = At(15, 0.75f).WaterTempC, springJul = At(196, 0.75f).WaterTempC;
        Assert.InRange(pondJan, 13f, 18f);
        Assert.InRange(pondJul, 28f, 32f);
        Assert.True(springJan > pondJan + 4f, "spring water keeps a bay warmer than a pond in January");
        Assert.True(springJul < pondJul - 3f, "and cooler in July");
        Assert.Equal("winter", At(15, 0f).Season);
        Assert.Equal("summer", At(196, 0f).Season);
    }
}
