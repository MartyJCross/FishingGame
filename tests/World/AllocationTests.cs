using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.World;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests.World;

/// <summary>No heap allocation inside SimWorld.Step (docs/02_TECH_STACK.md §7).</summary>
public class AllocationTests
{
    private readonly ITestOutputHelper _out;
    public AllocationTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void StepAllocatesNothing()
    {
        var config = new SimConfig();
        var world = new SimWorld(config, 3, anglerCount: 2);
        var frames = new InputFrame[2];
        var rng = new SimRng(4);

        // Warm up (JIT, static init).
        for (long t = 0; t < 240; t++)
        {
            for (int a = 0; a < 2; a++) frames[a] = TestUtil.ScriptedInput(ref rng, t, a);
            world.Step(frames);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (long t = 0; t < 1000; t++)
        {
            frames[0].RodTargetAngle = 0.5f + 0.001f * t;
            frames[1].RodTargetAngle = 1.0f - 0.001f * t;
            world.Step(frames);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        _out.WriteLine($"Allocated over 1000 ticks: {allocated} bytes");
        Assert.Equal(0, allocated);
    }
}
