using TheLedger.Sim.Core;

namespace TheLedger.Sim.Tests.Core;

public class SimRngTests
{
    [Fact]
    public void SameSeedGivesSameSequence()
    {
        var a = new SimRng(42);
        var b = new SimRng(42);
        for (int i = 0; i < 1000; i++) Assert.Equal(a.NextUInt(), b.NextUInt());
    }

    [Fact]
    public void DifferentStreamsDiffer()
    {
        var a = new SimRng(42, 1);
        var b = new SimRng(42, 2);
        int same = 0;
        for (int i = 0; i < 100; i++) if (a.NextUInt() == b.NextUInt()) same++;
        Assert.True(same < 3);
    }

    [Fact]
    public void FloatsAreInRangeAndRoughlyUniform()
    {
        var r = new SimRng(7);
        var buckets = new int[10];
        for (int i = 0; i < 100_000; i++)
        {
            float f = r.NextFloat();
            Assert.InRange(f, 0f, 0.99999994f);
            buckets[(int)(f * 10)]++;
        }
        foreach (int b in buckets) Assert.InRange(b, 9_500, 10_500);
    }

    [Fact]
    public void RangeIntStaysInBounds()
    {
        var r = new SimRng(3);
        for (int i = 0; i < 10_000; i++) Assert.InRange(r.RangeInt(-3, 4), -3, 3);
    }

    [Fact]
    public void GaussianHasUnitVariance()
    {
        var r = new SimRng(11);
        double sum = 0, sumSq = 0;
        const int n = 50_000;
        for (int i = 0; i < n; i++)
        {
            double g = r.NextGaussian();
            sum += g;
            sumSq += g * g;
        }
        double mean = sum / n;
        double variance = sumSq / n - mean * mean;
        Assert.InRange(mean, -0.03, 0.03);
        Assert.InRange(variance, 0.95, 1.05);
    }
}
