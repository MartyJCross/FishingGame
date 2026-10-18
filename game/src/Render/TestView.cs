using Godot;
using TheLedger.Boot;
using TheLedger.Sim.Core;
using TheLedger.SimGlue;

namespace TheLedger.Render;

/// <summary>
/// M0 placeholder scene: sky, bank, water and a stick-figure angler holding a rigid rod at the sim's hand angle.
/// Exists to prove the pipeline (input → sim → interpolated render → screenshot). Replaced by real renderers from
/// M1 (water) and M2 (rod and line).
/// </summary>
public partial class TestView : Node2D
{
    public SimDriver Driver { get; set; } = null!;

    private static readonly Color Sky = new(0.48f, 0.66f, 0.82f);
    private static readonly Color Bank = new(0.30f, 0.36f, 0.20f);
    private static readonly Color WaterTop = new(0.22f, 0.46f, 0.50f);
    private static readonly Color WaterDeep = new(0.06f, 0.16f, 0.20f);
    private static readonly Color Figure = new(0.14f, 0.15f, 0.17f);
    private static readonly Color RodColor = new(0.55f, 0.32f, 0.16f);

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        // Sky and water as big rectangles in world space (y up in sim = negative pixels).
        DrawRect(new Rect2(SimSpace.ToPx(-40f, 20f), new Vector2(80f, 20f) * SimSpace.PixelsPerMetre), Sky);
        DrawRect(new Rect2(SimSpace.ToPx(-40f, 0f), new Vector2(80f, 3f) * SimSpace.PixelsPerMetre), WaterTop);
        DrawRect(new Rect2(SimSpace.ToPx(-40f, -3f), new Vector2(80f, 10f) * SimSpace.PixelsPerMetre), WaterDeep);

        // The bank the angler stands on: a low-poly slope into the water.
        var bank = new[]
        {
            SimSpace.ToPx(-40f, 0.6f), SimSpace.ToPx(-1.2f, 0.6f), SimSpace.ToPx(0.4f, -0.4f),
            SimSpace.ToPx(2.0f, -2.5f), SimSpace.ToPx(2.0f, -10f), SimSpace.ToPx(-40f, -10f),
        };
        DrawColoredPolygon(bank, Bank);
        DrawLine(SimSpace.ToPx(-0.24f, 0f), SimSpace.ToPx(40f, 0f), new Color(0.85f, 0.95f, 1f, 0.8f), 2f);

        float alpha = Driver.Alpha;
        foreach (var a in Driver.World.Anglers)
        {
            Vec2 feet = Vec2.Lerp(a.PrevPosition, a.Position, alpha);
            Vec2 shoulder = feet + new Vec2(0f, a.ShoulderHeight);
            Vec2 hip = feet + new Vec2(0f, 0.9f);
            Vec2 head = shoulder + new Vec2(0f, 0.28f);

            // Legs, body, head.
            DrawLine(SimSpace.ToPx(hip), SimSpace.ToPx(feet + new Vec2(-0.15f, 0f)), Figure, 7f);
            DrawLine(SimSpace.ToPx(hip), SimSpace.ToPx(feet + new Vec2(0.15f, 0f)), Figure, 7f);
            DrawLine(SimSpace.ToPx(hip), SimSpace.ToPx(shoulder), Figure, 10f);
            DrawCircle(SimSpace.ToPx(head), 0.13f * SimSpace.PixelsPerMetre, Figure);

            // Hands and a rigid 2.1 m rod at the interpolated hand angle (the flexing rod comes in M2).
            float angle = Mathf.LerpAngle(a.Hands.PrevAngle, a.Hands.Angle, alpha);
            float reach = Mathf.Lerp(a.Hands.PrevReach, a.Hands.Reach, alpha);
            Vec2 grip = shoulder + new Vec2(0.25f + 0.35f * reach, -0.15f + 0.25f * reach);
            Vec2 dir = Vec2.FromAngle(angle);
            Vec2 butt = grip - dir * 0.35f;
            Vec2 tip = grip + dir * 1.75f;
            DrawLine(SimSpace.ToPx(shoulder), SimSpace.ToPx(grip), Figure, 6f);
            DrawLine(SimSpace.ToPx(butt), SimSpace.ToPx(tip), RodColor, 4f);
            DrawLine(SimSpace.ToPx(butt), SimSpace.ToPx(grip), new Color(0.75f, 0.62f, 0.42f), 7f);
        }
    }
}
