using TheLedger.Sim.Core;

namespace TheLedger.Sim.Tests.Core;

public class SimConfigTests
{
    [Fact]
    public void EveryTunableHasAValidRangeAndDefaultInside()
    {
        var c = new SimConfig();
        Assert.NotEmpty(SimConfig.Tunables);
        foreach (var t in SimConfig.Tunables)
        {
            Assert.True(t.Info.Min < t.Info.Max, $"{t.Name}: min must be below max");
            Assert.True(t.Type == typeof(float) || t.Type == typeof(int) || t.Type == typeof(bool),
                $"{t.Name}: tunables must be float, int or bool");
            float v = c.GetAsFloat(t);
            Assert.InRange(v, t.Info.Min, t.Info.Max);
        }
    }

    [Fact]
    public void JsonRoundTripPreservesValues()
    {
        var c = new SimConfig { Gravity = 9.5f, TackleSubsteps = 12, HandStiffness = 123.25f };
        var back = SimConfig.FromJson(c.ToJson());
        Assert.Equal(9.5f, back.Gravity);
        Assert.Equal(12, back.TackleSubsteps);
        Assert.Equal(123.25f, back.HandStiffness);
    }

    [Fact]
    public void UnknownKeysWarnAndValuesClamp()
    {
        var warnings = new List<string>();
        var c = SimConfig.FromJson("{ \"Gravity\": 500, \"NotAField\": 1 }", warnings);
        Assert.Equal(20f, c.Gravity); // clamped to the slider max
        Assert.Single(warnings);
    }

    [Fact]
    public void CommittedTuningFileLoadsCleanly()
    {
        string path = Path.Combine(TestUtil.RepoRoot, "game", "data", "tuning", "sim.json");
        Assert.True(File.Exists(path), "game/data/tuning/sim.json must exist");
        var warnings = new List<string>();
        SimConfig.FromJson(File.ReadAllText(path), warnings);
        Assert.Empty(warnings);
    }
}
