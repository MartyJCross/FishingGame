using TheLedger.Sim.Core;
using TheLedger.Sim.World;

namespace TheLedger.Sim.Tests.World;

public class StructureTests
{
    [Fact]
    public void HooksSnagOnTheTreeNotTheWaterAroundIt()
    {
        var pond = Spot.TestPond();
        var log = new StructureZone { Kind = "wood", X0 = 2.6f, X1 = 7.8f, Y0 = -3.1f, Y1 = -0.5f, Snag = 0.6f };
        _ = new Spot("t", Enumerable.Range(0, pond.PointCount).Select(pond.Point).ToList()) { Structures = new[] { log } };
        var trunk = log.Parts[0];
        Assert.True(log.Touches(Vec2.Lerp(trunk.A, trunk.B, 0.5f)), "the trunk itself");
        Assert.False(log.Touches(new Vec2(5f, -0.05f)), "just under the surface, above the log");
        Assert.False(log.Touches(new Vec2(12f, -1.5f)), "out past the tree");
    }
}
