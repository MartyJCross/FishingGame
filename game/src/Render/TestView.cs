using Godot;
using TheLedger.Boot;
using TheLedger.Sim.Core;
using TheLedger.SimGlue;

namespace TheLedger.Render;

/// <summary>
/// Placeholder pond pieces until the M8 art pass: the sky (behind the water) and the bank, pond bottom and a
/// stick-figure angler (in front of the water).
/// </summary>
public static class TestPond
{
    /// <summary>The pond's terrain cross-section (bank on the left, sloping into a 3 m deep bottom).</summary>
    public static readonly Vec2[] Terrain =
    {
        new(-40f, 0.6f), new(-1.2f, 0.6f), new(0.4f, -0.4f), new(2.0f, -2.5f), new(4.5f, -3.0f),
        new(20f, -3.3f), new(40f, -3.0f), new(60f, -3.2f), new(60f, -12f), new(-40f, -12f),
    };

    /// <summary>Where the bank meets still water.</summary>
    public const float WaterlineX = -0.24f;
}

/// <summary>Sky behind everything.</summary>
public partial class TestSky : Node2D
{
    private static readonly Color Top = new(0.36f, 0.55f, 0.76f);
    private static readonly Color Horizon = new(0.70f, 0.82f, 0.90f);

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        var pts = new[]
        {
            SimSpace.ToPx(-40f, 25f), SimSpace.ToPx(60f, 25f), SimSpace.ToPx(60f, -1f), SimSpace.ToPx(-40f, -1f),
        };
        var cols = new[] { Top, Top, Horizon, Horizon };
        DrawPolygon(pts, cols);
        // Far shore silhouette so the near band reads as water in front of land.
        var shore = new[]
        {
            SimSpace.ToPx(-40f, 0.4f), SimSpace.ToPx(-40f, 1.6f), SimSpace.ToPx(8f, 1.3f), SimSpace.ToPx(14f, 1.9f),
            SimSpace.ToPx(22f, 1.5f), SimSpace.ToPx(30f, 2.4f), SimSpace.ToPx(40f, 1.7f), SimSpace.ToPx(60f, 2.0f),
            SimSpace.ToPx(60f, 0.4f),
        };
        DrawColoredPolygon(shore, new Color(0.42f, 0.52f, 0.46f));
    }
}

/// <summary>Bank, pond bottom and the angler, in front of the water.</summary>
public partial class TestView : Node2D
{
    public SimDriver Driver { get; set; } = null!;

    private static readonly Color Bank = new(0.30f, 0.36f, 0.20f);
    private static readonly Color Bottom = new(0.20f, 0.19f, 0.13f);
    private static readonly Color Figure = new(0.14f, 0.15f, 0.17f);

    private Vector2[]? _terrainPx;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        _terrainPx ??= TestPond.Terrain.Select(SimSpace.ToPx).ToArray();
        DrawColoredPolygon(_terrainPx, Bottom);
        // Grass on the dry bank.
        DrawColoredPolygon(new[]
        {
            SimSpace.ToPx(-40f, 0.6f), SimSpace.ToPx(-1.2f, 0.6f), SimSpace.ToPx(-0.24f, 0f),
            SimSpace.ToPx(-1.4f, 0.0f), SimSpace.ToPx(-40f, 0.0f),
        }, Bank);

        float alpha = Driver.Alpha;
        foreach (var a in Driver.World.Anglers)
        {
            Vec2 feet = Vec2.Lerp(a.PrevPosition, a.Position, alpha);
            Vec2 shoulder = feet + new Vec2(0f, a.ShoulderHeight);
            Vec2 hip = feet + new Vec2(0f, 0.9f);
            Vec2 head = shoulder + new Vec2(0f, 0.28f);

            DrawLine(SimSpace.ToPx(hip), SimSpace.ToPx(feet + new Vec2(-0.15f, 0f)), Figure, 7f);
            DrawLine(SimSpace.ToPx(hip), SimSpace.ToPx(feet + new Vec2(0.15f, 0f)), Figure, 7f);
            DrawLine(SimSpace.ToPx(hip), SimSpace.ToPx(shoulder), Figure, 10f);
            DrawCircle(SimSpace.ToPx(head), 0.13f * SimSpace.PixelsPerMetre, Figure);

            // The arm reaches to the rod's grip node (the rod and line are drawn by TackleRenderer).
            Vec2 grip = a.Rig.Rod.Node(1);
            DrawLine(SimSpace.ToPx(shoulder), SimSpace.ToPx(grip), Figure, 6f);
        }
    }
}
