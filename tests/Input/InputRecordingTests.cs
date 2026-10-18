using TheLedger.Sim.Core;
using TheLedger.Sim.Input;

namespace TheLedger.Sim.Tests.Input;

public class InputRecordingTests
{
    [Fact]
    public void SaveAndLoadRoundTripsEveryField()
    {
        var rec = new InputRecording { Seed = 99, AnglerCount = 2, Setup = "scene=pond_test" };
        var rng = new SimRng(5);
        var frames = new InputFrame[2];
        for (int t = 0; t < 300; t++)
        {
            for (int a = 0; a < 2; a++) frames[a] = TestUtil.ScriptedInput(ref rng, t, a);
            rec.AddTick(frames);
        }

        using var ms = new MemoryStream();
        rec.Save(ms);
        ms.Position = 0;
        var back = InputRecording.Load(ms);

        Assert.Equal(99UL, back.Seed);
        Assert.Equal(2, back.AnglerCount);
        Assert.Equal("scene=pond_test", back.Setup);
        Assert.Equal(300, back.TickCount);
        Assert.Equal(rec.Frames, back.Frames);
    }

    [Fact]
    public void RejectsOtherFiles()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        Assert.Throws<InvalidDataException>(() => InputRecording.Load(ms));
    }
}
