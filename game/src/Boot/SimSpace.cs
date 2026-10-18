using Godot;
using TheLedger.Sim.Core;

namespace TheLedger.Boot;

/// <summary>
/// Converts between sim space (metres, y up) and Godot 2D space (pixels, y down). Only renderers and input use it
/// (docs/02_TECH_STACK.md §3.3).
/// </summary>
public static class SimSpace
{
    /// <summary>Pixels per metre at camera zoom 1 (1920-wide viewport shows 16 m).</summary>
    public const float PixelsPerMetre = 120f;

    public static Vector2 ToPx(Vec2 v) => new(v.X * PixelsPerMetre, -v.Y * PixelsPerMetre);

    public static Vec2 ToSim(Vector2 p) => new(p.X / PixelsPerMetre, -p.Y / PixelsPerMetre);

    public static Vector2 ToPx(float x, float y) => new(x * PixelsPerMetre, -y * PixelsPerMetre);
}
