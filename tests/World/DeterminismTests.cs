using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.World;

/// <summary>Same seed + same inputs = same state, tick for tick (docs/02_TECH_STACK.md §3.4).</summary>
public class DeterminismTests
{
    private readonly ITestOutputHelper _out;
    public DeterminismTests(ITestOutputHelper output) => _out = output;

    private static List<ulong> RunAndHashEverySecond(ulong seed, int anglers, int seconds)
    {
        var config = new SimConfig();
        var world = new SimWorld(config, seed, anglers);
        var inputRng = new SimRng(seed, 77);
        var frames = new InputFrame[anglers];
        var hashes = new List<ulong>();
        for (long t = 0; t < seconds * (long)config.TickHz; t++)
        {
            for (int a = 0; a < anglers; a++) frames[a] = TestUtil.ScriptedInput(ref inputRng, t, a);
            world.Step(frames);
            if (world.Tick % config.TickHz == 0) hashes.Add(world.ComputeHash());
        }
        Assert.Equal(0, world.FaultCount);
        return hashes;
    }

    [Fact]
    public void SixtySecondsTwiceGivesIdenticalHashes()
    {
        var first = RunAndHashEverySecond(1234, anglers: 2, seconds: 60);
        var second = RunAndHashEverySecond(1234, anglers: 2, seconds: 60);
        Assert.Equal(60, first.Count);
        Assert.Equal(first, second);
        _out.WriteLine($"60 s, 2 anglers: final hash {first[^1]:X16}");
    }

    [Fact]
    public void ReplayingARecordingReproducesTheRun()
    {
        var config = new SimConfig();
        var rec = new InputRecording { Seed = 55, AnglerCount = 2 };
        var live = new SimWorld(config, rec.Seed, rec.AnglerCount);
        var rng = new SimRng(9);
        var frames = new InputFrame[2];
        for (long t = 0; t < 1200; t++)
        {
            for (int a = 0; a < 2; a++) frames[a] = TestUtil.ScriptedInput(ref rng, t, a);
            rec.AddTick(frames);
            live.Step(frames);
        }

        using var ms = new MemoryStream();
        rec.Save(ms);
        ms.Position = 0;
        var loaded = InputRecording.Load(ms);

        var replay = new SimWorld(config, loaded.Seed, loaded.AnglerCount);
        for (int t = 0; t < loaded.TickCount; t++) replay.Step(loaded.TickFrames(t));

        Assert.Equal(live.ComputeHash(), replay.ComputeHash());
    }

    [Fact]
    public void DifferentInputsGiveDifferentState()
    {
        var a = RunAndHashEverySecond(1, anglers: 1, seconds: 5);
        var b = RunAndHashEverySecond(2, anglers: 1, seconds: 5);
        Assert.NotEqual(a[^1], b[^1]);
    }
}
