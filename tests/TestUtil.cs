using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using Xunit.Abstractions;

namespace TheLedger.Sim.Tests;

public static class TestUtil
{
    /// <summary>The repo root (the folder holding CLAUDE.md), found by walking up from the test binary.</summary>
    public static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Repo root (CLAUDE.md) not found");
        }
    }

    /// <summary>Scripted but varied input: sweeps, flicks, holds and cranks, deterministic from a seed.</summary>
    public static InputFrame ScriptedInput(ref SimRng rng, long tick, int angler)
    {
        float t = tick / 120f;
        float sweep = 0.6f + 0.9f * MathF.Sin(t * (0.7f + 0.3f * angler));
        // Occasional flicks: jump the target to a random angle.
        if (rng.Chance(0.01f)) sweep = rng.Range(-1f, 2.4f);
        return new InputFrame
        {
            RodTargetAngle = sweep,
            RodReach = 0.5f + 0.5f * MathF.Sin(t * 0.31f),
            LineHold = rng.Chance(0.5f) ? 1f : 0f,
            CrankRate = MathF.Max(0f, MathF.Sin(t * 1.3f)) * 2f,
            DragDelta = (sbyte)(rng.Chance(0.002f) ? 1 : 0),
            Move = MathF.Sin(t * 0.2f),
            Buttons = rng.Chance(0.003f) ? InputButtons.Bail : InputButtons.None,
        };
    }
}

/// <summary>Prints measurements so regressions are visible, not just pass/fail (CLAUDE.md rule 7).</summary>
public static class Report
{
    public static void Line(ITestOutputHelper output, string text) => output.WriteLine(text);
}
