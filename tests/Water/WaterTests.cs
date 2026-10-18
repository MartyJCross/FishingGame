using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.Water;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.Water;

public class AmbientWaveTests
{
    private readonly ITestOutputHelper _out;
    public AmbientWaveTests(ITestOutputHelper output) => _out = output;

    /// <summary>Measures significant wave height as 4 × the standard deviation of elevation at one point.</summary>
    private static float MeasuredHs(AmbientWaves w, float seconds)
    {
        double sum = 0, sumSq = 0;
        int n = 0;
        for (double t = 0; t < seconds; t += 0.01)
        {
            double h = w.Height(3.3f, t);
            sum += h;
            sumSq += h * h;
            n++;
        }
        double mean = sum / n;
        return (float)(4.0 * Math.Sqrt(sumSq / n - mean * mean));
    }

    [Fact]
    public void PondBreezeMakesCentimetreRipples()
    {
        var w = new AmbientWaves();
        w.Configure(windSpeed: 5f, windDir: 1f, fetch: 60f, depth: 3f, 0f, 8f, 1f, seed: 1);
        float measured = MeasuredHs(w, 120f);
        _out.WriteLine($"Pond, 5 m/s, 60 m fetch: Hs {w.WindSeaHs * 100:0.0} cm (measured {measured * 100:0.0} cm), Tp {w.WindSeaTp:0.00} s");
        Assert.InRange(w.WindSeaHs, 0.012f, 0.03f);
        Assert.InRange(w.WindSeaTp, 0.3f, 0.6f);
        Assert.InRange(measured, w.WindSeaHs * 0.8f, w.WindSeaHs * 1.2f);
    }

    [Fact]
    public void OpenSeaWindMakesMetreWaves()
    {
        var w = new AmbientWaves();
        w.Configure(windSpeed: 10f, windDir: 1f, fetch: 100_000f, depth: 200f, 0f, 8f, 1f, seed: 2);
        _out.WriteLine($"Open sea, 10 m/s, 100 km fetch: Hs {w.WindSeaHs:0.00} m, Tp {w.WindSeaTp:0.0} s");
        // JONSWAP fetch-limited gives ~1.6 m; fully developed PM at 10 m/s is ~2.1 m.
        Assert.InRange(w.WindSeaHs, 1.3f, 2.2f);
        Assert.InRange(w.WindSeaTp, 4.5f, 8f);
    }

    [Fact]
    public void NoWindNoSwellIsFlat()
    {
        var w = new AmbientWaves();
        w.Configure(0f, 1f, 60f, 3f, 0f, 8f, 1f, seed: 3);
        Assert.Equal(0, w.TrainCount);
        Assert.Equal(0f, w.Height(1f, 5.0));
    }

    [Fact]
    public void DispersionMatchesDeepAndShallowLimits()
    {
        const float g = 9.81f;
        // Deep water: k = ω²/g.
        float omega = 2f * MathF.PI / 2f;
        Assert.Equal(omega * omega / g, AmbientWaves.WaveNumber(omega, 500f), 3);
        // Shallow water: phase speed ≈ sqrt(g·h).
        float depth = 0.5f;
        float omegaLong = 2f * MathF.PI / 30f;
        float k = AmbientWaves.WaveNumber(omegaLong, depth);
        float c = omegaLong / k;
        Assert.InRange(c, MathF.Sqrt(g * depth) * 0.97f, MathF.Sqrt(g * depth) * 1.01f);
    }

    [Fact]
    public void OrbitalMotionFadesWithDepth()
    {
        var w = new AmbientWaves();
        w.Configure(0f, 1f, 1f, 50f, swellHeight: 1f, swellPeriod: 8f, swellDir: 1f, seed: 4);
        float surface = 0f, deep = 0f;
        for (double t = 0; t < 16; t += 0.05)
        {
            surface = MathF.Max(surface, w.OrbitalVelocity(0f, 0f, t).Length);
            deep = MathF.Max(deep, w.OrbitalVelocity(0f, 30f, t).Length);
        }
        _out.WriteLine($"Swell 1 m / 8 s: orbital speed {surface:0.00} m/s at surface, {deep:0.000} m/s at 30 m");
        Assert.True(deep < surface * 0.35f);
    }
}

public class SurfacePatchTests
{
    private readonly ITestOutputHelper _out;
    public SurfacePatchTests(ITestOutputHelper output) => _out = output;

    private const float Dt = 1f / 120f;

    [Fact]
    public void SplashEnergyDecaysAndSettlesFlat()
    {
        var p = new SurfacePatch(512, 0.05f, 0f);
        p.AddImpulse(12.8f, 1.5f, 0.08f);
        double initial = 0;
        double last = double.MaxValue;
        int increases = 0;
        for (int tick = 0; tick < 120 * 20; tick++)
        {
            p.Step(Dt, 0.7f, 0.6f, 48, 4f, 1.2f);
            double e = p.Energy(0.7f);
            if (tick == 0) initial = e;
            if (tick % 12 == 0)
            {
                if (e > last * 1.0001) increases++;
                last = e;
            }
        }
        _out.WriteLine($"Splash energy: start {initial:E3}, after 20 s {last:E3} ({100 * last / initial:0.000}%)");
        Assert.Equal(0, increases);
        Assert.True(last < initial * 0.01);
    }

    [Fact]
    public void SpongeEdgesAbsorbInsteadOfReflecting()
    {
        // No interior damping: only the sponges can remove energy.
        double Remaining(int sponge)
        {
            var p = new SurfacePatch(512, 0.05f, 0f);
            p.AddImpulse(12.8f, 1.0f, 0.08f);
            p.Step(Dt, 0.7f, 0f, sponge, 0f, 1f);
            double start = p.Energy(0.7f);
            for (int t = 0; t < 120 * 40; t++) p.Step(Dt, 0.7f, 0f, sponge, 0f, 1f);
            return p.Energy(0.7f) / start;
        }
        double withSponge = Remaining(48);
        double noSponge = Remaining(1);
        _out.WriteLine($"Energy left after 40 s with no damping: sponge {100 * withSponge:0.0}%, no sponge {100 * noSponge:0.0}%");
        Assert.True(withSponge < 0.10, "sponges should absorb at least 90%");
        Assert.True(noSponge > 0.5, "without sponges, waves bounce around");
    }

    [Fact]
    public void ImpulsesConserveWaterVolume()
    {
        var p = new SurfacePatch(512, 0.05f, 0f);
        var rng = new SimRng(8);
        for (int i = 0; i < 50; i++) p.AddImpulse(rng.Range(8f, 17f), rng.Range(-1f, 1f), rng.Range(0.02f, 0.2f));
        for (int t = 0; t < 120; t++) p.Step(Dt, 0.7f, 0.6f, 48, 4f, 1.2f);
        double volume = 0, maxAbs = 0;
        foreach (float h in p.Heights)
        {
            volume += h * p.Spacing;
            maxAbs = Math.Max(maxAbs, Math.Abs(h));
        }
        _out.WriteLine($"Net volume after 50 impulses: {volume:E2} m² (peak height {maxAbs * 100:0.0} cm)");
        Assert.InRange(volume, -1e-4, 1e-4);
    }

    [Fact]
    public void FastWavesOnFineGridStayStable()
    {
        // c·dt/dx = 3 · (1/120) / 0.02 = 1.25: the step must substep to stay stable.
        var p = new SurfacePatch(256, 0.02f, 0f);
        p.AddImpulse(2.5f, 2f, 0.05f);
        for (int t = 0; t < 1200; t++) p.Step(Dt, 3f, 0.3f, 32, 4f, 1.2f);
        Assert.True(p.IsFinite());
        foreach (float h in p.Heights) Assert.InRange(h, -0.5f, 0.5f);
    }

    [Fact]
    public void FollowShiftsByWholeColumnsAndKeepsRipples()
    {
        var p = new SurfacePatch(256, 0.05f, 0f);
        p.AddImpulse(6.4f, 1f, 0.08f);
        p.Step(Dt, 0.7f, 0.6f, 32, 4f, 1.2f);
        float before = p.HeightAt(6.4f);
        p.Follow(12f);
        Assert.NotEqual(0f, p.OriginX);
        Assert.Equal(before, p.HeightAt(6.4f), 5);
    }
}

public class WorldWaterTests
{
    private readonly ITestOutputHelper _out;
    public WorldWaterTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void TenMinuteStormStaysFiniteAndBounded()
    {
        var c = new SimConfig { DebugWindSpeed = 18f, DebugFetch = 3000f, DebugRainRate = 60f, DebugDepth = 10f };
        var world = new SimWorld(c, 21, 1);
        var frames = new InputFrame[1];
        var rng = new SimRng(1);
        float maxPatch = 0f;
        for (long t = 0; t < 120L * 600; t++)
        {
            frames[0] = TestUtil.ScriptedInput(ref rng, t, 0);
            if (t % 30 == 0) world.Splash(rng.Range(2f, 20f), rng.Range(1f, 8f), rng.Range(0.005f, 0.5f), 0.06f);
            world.Step(frames);
            if (t % 120 == 0)
                foreach (float h in world.Water.Patch(0)!.Heights) maxPatch = MathF.Max(maxPatch, MathF.Abs(h));
        }
        _out.WriteLine($"10 min storm (18 m/s wind, 60 mm/h rain, splash every 0.25 s): faults {world.FaultCount}, " +
                       $"peak patch height {maxPatch * 100:0.0} cm, ambient Hs {world.Water.Ambient.WindSeaHs:0.00} m, droplets {world.Water.Droplets.Count}");
        Assert.Equal(0, world.FaultCount);
        Assert.True(world.Water.IsFinite());
        Assert.True(maxPatch < 0.5f);
    }

    [Fact]
    public void SplashEnergyOrdersPitchBelowSlam()
    {
        var world = new SimWorld(new SimConfig(), 1, 1);
        float pitch = world.Splash(5f, 2f, 0.007f, 0.02f);
        float overhead = world.Splash(8f, 9f, 0.014f, 0.03f);
        float slam = world.Splash(11f, 15f, 0.06f, 0.05f);
        _out.WriteLine($"Splash energy: pitch {pitch:0.000} J, overhead {overhead:0.000} J, slam {slam:0.000} J");
        Assert.True(pitch < overhead && overhead < slam);
    }

    [Fact]
    public void DropletsFallBackIntoTheWater()
    {
        var world = new SimWorld(new SimConfig { DebugWindSpeed = 0f }, 1, 1);
        world.Splash(6f, 8f, 0.05f, 0.05f);
        int spawned = world.Water.Droplets.Count;
        var frames = new InputFrame[1];
        for (int t = 0; t < 240; t++) world.Step(frames);
        _out.WriteLine($"Droplets: {spawned} thrown, {world.Water.Droplets.Count} still airborne after 2 s");
        Assert.True(spawned > 10);
        Assert.Equal(0, world.Water.Droplets.Count);
    }

    [Fact]
    public void RainingWorldStepAllocatesNothing()
    {
        var world = new SimWorld(new SimConfig { DebugRainRate = 20f, DebugWindSpeed = 6f }, 3, 2);
        var frames = new InputFrame[2];
        for (int t = 0; t < 240; t++) world.Step(frames);
        world.Splash(5f, 5f, 0.02f, 0.03f);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int t = 0; t < 1000; t++)
        {
            if (t % 50 == 0) world.Splash(4f + t * 0.01f, 5f, 0.02f, 0.03f);
            world.Step(frames);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        _out.WriteLine($"Raining world: {allocated} bytes allocated over 1000 ticks");
        Assert.Equal(0, allocated);
    }
}
