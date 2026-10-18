using Godot;
using TheLedger.Boot;
using TheLedger.Sim.Core;
using TheLedger.Sim.Fish;
using TheLedger.SimGlue;

namespace TheLedger.Render;

/// <summary>
/// The station pond's look (first art pass, flat low-poly): sky, far Nature Coast tree line, the bank with a cabbage
/// palm and a live oak, cattails at the waterline, the angler and the dog. Decoration only: none of it touches the sim.
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

/// <summary>Low-poly drawing helpers in world metres.</summary>
internal static class Lp
{
    public static Vector2 P(float x, float y) => SimSpace.ToPx(x, y);

    public static void Poly(CanvasItem c, Color col, params Vector2[] world)
    {
        var px = new Vector2[world.Length];
        for (int i = 0; i < world.Length; i++) px[i] = SimSpace.ToPx(world[i].X, world[i].Y);
        c.DrawColoredPolygon(px, col);
    }

    /// <summary>A quad of width <paramref name="w"/> along a→b (limbs, trunks).</summary>
    public static void Limb(CanvasItem c, Color col, Vector2 a, Vector2 b, float wa, float wb)
    {
        var d = b - a;
        if (d.LengthSquared() < 1e-8f) return;
        var n = new Vector2(-d.Y, d.X).Normalized();
        Poly(c, col, a + n * wa * 0.5f, b + n * wb * 0.5f, b - n * wb * 0.5f, a - n * wa * 0.5f);
    }

    /// <summary>A regular n-gon (low-poly circle).</summary>
    public static void Ngon(CanvasItem c, Color col, Vector2 centre, float r, int n, float rot = 0f)
    {
        var pts = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float a = rot + i * Mathf.Tau / n;
            pts[i] = centre + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
        }
        Poly(c, col, pts);
    }

    /// <summary>Cabbage palm: a gently leaning trunk and a crown of drooping fronds.</summary>
    public static void Palm(CanvasItem c, Vector2 baseAt, float h, float lean, Color trunk, Color leaf, float sway)
    {
        var top = baseAt + new Vector2(lean + sway * 0.3f, h);
        Limb(c, trunk, baseAt, top, h * 0.06f, h * 0.045f);
        for (int i = 0; i < 9; i++)
        {
            float a = Mathf.Pi * (0.08f + 0.84f * i / 8f) + sway * 0.15f;
            float len = h * (0.32f + 0.06f * MathF.Sin(i * 2.3f));
            var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
            var mid = top + dir * len * 0.55f + new Vector2(0f, len * 0.08f);
            var tip = top + dir * len + new Vector2(0f, -len * 0.45f * MathF.Abs(dir.X));
            var n = new Vector2(-dir.Y, dir.X) * h * 0.035f;
            Poly(c, leaf, top, mid + n, tip, mid - n);
        }
    }

    /// <summary>Bald cypress: a tall narrow flat-topped cone with a flared base.</summary>
    public static void Cypress(CanvasItem c, Vector2 b, float h, Color col)
    {
        float w = h * 0.16f;
        Poly(c, col, b + new Vector2(-w * 0.9f, 0f), b + new Vector2(-w * 0.35f, h * 0.25f), b + new Vector2(-w, h * 0.55f),
             b + new Vector2(-w * 0.45f, h), b + new Vector2(w * 0.5f, h * 0.97f), b + new Vector2(w * 1.05f, h * 0.6f),
             b + new Vector2(w * 0.4f, h * 0.25f), b + new Vector2(w * 0.9f, 0f));
    }

    /// <summary>Live oak: a broad, low, lumpy crown on a short trunk.</summary>
    public static void Oak(CanvasItem c, Vector2 b, float h, Color trunk, Color leaf, Color moss)
    {
        Limb(c, trunk, b, b + new Vector2(0.1f * h, 0.55f * h), 0.12f * h, 0.08f * h);
        Limb(c, trunk, b + new Vector2(0.05f * h, 0.4f * h), b + new Vector2(0.55f * h, 0.7f * h), 0.06f * h, 0.03f * h);
        Limb(c, trunk, b + new Vector2(0.05f * h, 0.45f * h), b + new Vector2(-0.5f * h, 0.72f * h), 0.06f * h, 0.03f * h);
        var cx = b + new Vector2(0.05f * h, 0.78f * h);
        Poly(c, leaf, cx + new Vector2(-0.95f * h, -0.1f * h), cx + new Vector2(-0.7f * h, 0.22f * h), cx + new Vector2(-0.2f * h, 0.33f * h),
             cx + new Vector2(0.35f * h, 0.3f * h), cx + new Vector2(0.85f * h, 0.12f * h), cx + new Vector2(1.0f * h, -0.12f * h),
             cx + new Vector2(0.5f * h, -0.24f * h), cx + new Vector2(-0.45f * h, -0.24f * h));
        // Spanish moss hanging from the crown.
        for (int i = 0; i < 7; i++)
        {
            float x = -0.75f + 1.5f * i / 6f;
            float len = (0.18f + 0.08f * MathF.Sin(i * 1.7f)) * h;
            var top = cx + new Vector2(x * h, -0.2f * h);
            Poly(c, moss, top + new Vector2(-0.018f * h, 0f), top + new Vector2(0.018f * h, 0f), top + new Vector2(0.01f * h, -len));
        }
    }
}

/// <summary>Sky, sun, clouds and the far shore (behind the water).</summary>
public partial class TestSky : Node2D
{
    private static readonly Color Top = new(0.33f, 0.56f, 0.80f);
    private static readonly Color Horizon = new(0.78f, 0.86f, 0.90f);
    private static readonly Color Cloud = new(0.97f, 0.98f, 1f, 0.92f);
    private static readonly Color CloudShade = new(0.85f, 0.89f, 0.94f, 0.92f);
    private static readonly Color HillFar = new(0.56f, 0.66f, 0.70f);
    private static readonly Color TreesFar = new(0.38f, 0.50f, 0.47f);
    private static readonly Color TreesNear = new(0.27f, 0.39f, 0.31f);

    private (float x, float h, int kind)[] _far = null!;

    public override void _Ready()
    {
        var rng = new RandomNumberGenerator { Seed = 7 };
        var list = new List<(float, float, int)>();
        for (float x = -40f; x < 62f; x += rng.RandfRange(0.6f, 1.6f))
            list.Add((x, rng.RandfRange(0.7f, 1.5f), rng.RandiRange(0, 2)));
        _far = list.ToArray();
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        float t = (float)Time.GetTicksMsec() * 0.001f;
        DrawPolygon(new[] { Lp.P(-40f, 25f), Lp.P(60f, 25f), Lp.P(60f, -1f), Lp.P(-40f, -1f) },
                    new[] { Top, Top, Horizon, Horizon });
        Lp.Ngon(this, new Color(1f, 0.97f, 0.85f, 0.35f), new Vector2(-14f, 13f), 1.6f, 12);
        Lp.Ngon(this, new Color(1f, 0.98f, 0.9f), new Vector2(-14f, 13f), 1.0f, 12);

        // Slow low-poly clouds.
        for (int i = 0; i < 6; i++)
        {
            float x = Mathf.PosMod(-40f + i * 19f + t * 0.15f * (1f + 0.3f * i), 104f) - 42f;
            float y = 8f + 3.5f * MathF.Sin(i * 2.1f) + i % 2 * 2f;
            float s = 1.2f + 0.5f * (i % 3);
            Lp.Poly(this, CloudShade, new Vector2(x - 2.2f * s, y), new Vector2(x + 2.4f * s, y), new Vector2(x + 1.6f * s, y + 0.5f * s), new Vector2(x - 1.6f * s, y + 0.5f * s));
            Lp.Poly(this, Cloud, new Vector2(x - 1.6f * s, y + 0.3f * s), new Vector2(x - 0.9f * s, y + 0.9f * s), new Vector2(x - 0.1f * s, y + 1.3f * s),
                    new Vector2(x + 0.9f * s, y + 1.0f * s), new Vector2(x + 1.6f * s, y + 0.4f * s));
        }

        // Distant hammock: a soft hill line, then trees (palms, cypress, oaks) standing on the far shore.
        Lp.Poly(this, HillFar, new Vector2(-40f, 0.3f), new Vector2(-40f, 1.7f), new Vector2(-10f, 2.0f), new Vector2(15f, 1.7f),
                new Vector2(35f, 2.1f), new Vector2(60f, 1.8f), new Vector2(60f, 0.3f));
        foreach (var (x, h, kind) in _far)
        {
            var b = new Vector2(x, 0.9f);
            switch (kind)
            {
                case 0: Lp.Palm(this, b, h * 1.1f, 0.15f * h, TreesFar, TreesFar, MathF.Sin(t * 0.6f + x) * 0.3f); break;
                case 1: Lp.Cypress(this, b, h * 1.2f, TreesFar); break;
                default: Lp.Ngon(this, TreesFar, b + new Vector2(0f, h * 0.4f), h * 0.42f, 7, x); break;
            }
        }
        Lp.Poly(this, TreesNear, new Vector2(-40f, 0.3f), new Vector2(-40f, 1.3f), new Vector2(-20f, 1.15f), new Vector2(0f, 1.25f),
                new Vector2(20f, 1.1f), new Vector2(40f, 1.3f), new Vector2(60f, 1.15f), new Vector2(60f, 0.3f));
    }
}

/// <summary>Bank, pond bottom, near trees, cattails, the angler and the dog (in front of the water).</summary>
public partial class TestView : Node2D
{
    public SimDriver Driver { get; set; } = null!;

    private static readonly Color Bottom = new(0.24f, 0.22f, 0.15f);
    private static readonly Color BottomDeep = new(0.16f, 0.15f, 0.11f);
    private static readonly Color Soil = new(0.36f, 0.27f, 0.17f);
    private static readonly Color Sand = new(0.72f, 0.64f, 0.46f);
    private static readonly Color Grass = new(0.36f, 0.50f, 0.22f);
    private static readonly Color GrassDark = new(0.27f, 0.40f, 0.17f);
    private static readonly Color Trunk = new(0.33f, 0.26f, 0.19f);
    private static readonly Color Palm = new(0.30f, 0.46f, 0.24f);
    private static readonly Color OakLeaf = new(0.24f, 0.36f, 0.20f);
    private static readonly Color Moss = new(0.62f, 0.66f, 0.56f, 0.9f);
    private static readonly Color Reed = new(0.42f, 0.52f, 0.25f);
    private static readonly Color ReedHead = new(0.40f, 0.26f, 0.14f);

    // Angler.
    private static readonly Color Skin = new(0.86f, 0.66f, 0.52f);
    private static readonly Color Shirt = new(0.56f, 0.70f, 0.72f);
    private static readonly Color ShirtShade = new(0.44f, 0.57f, 0.60f);
    private static readonly Color Jeans = new(0.24f, 0.30f, 0.42f);
    private static readonly Color JeansShade = new(0.19f, 0.24f, 0.34f);
    private static readonly Color Boot = new(0.30f, 0.22f, 0.15f);
    private static readonly Color Cap = new(0.78f, 0.36f, 0.22f);
    private static readonly Color Hair = new(0.25f, 0.18f, 0.12f);

    // Dog (a yellow lab).
    private static readonly Color Dog = new(0.80f, 0.62f, 0.36f);
    private static readonly Color DogShade = new(0.66f, 0.49f, 0.27f);
    private static readonly Color Nose = new(0.15f, 0.11f, 0.09f);

    private Vector2[]? _terrainPx;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        float t = (float)Time.GetTicksMsec() * 0.001f;
        _terrainPx ??= TestPond.Terrain.Select(SimSpace.ToPx).ToArray();
        DrawColoredPolygon(_terrainPx, Bottom);
        Lp.Poly(this, BottomDeep, new Vector2(4.5f, -3.6f), new Vector2(20f, -3.9f), new Vector2(60f, -3.8f), new Vector2(60f, -12f), new Vector2(-40f, -12f), new Vector2(-40f, -1.5f));
        // Bank: soil, a sandy lip down to the water, grass on top with tufts.
        Lp.Poly(this, Soil, new Vector2(-40f, 0.45f), new Vector2(-1.25f, 0.45f), new Vector2(-0.24f, -0.1f), new Vector2(0.2f, -0.45f), new Vector2(-40f, -0.6f));
        Lp.Poly(this, Sand, new Vector2(-1.35f, 0.5f), new Vector2(-1.15f, 0.6f), new Vector2(0.4f, -0.4f), new Vector2(0.1f, -0.45f), new Vector2(-0.3f, -0.05f));
        Lp.Poly(this, Grass, new Vector2(-40f, 0.6f), new Vector2(-1.2f, 0.6f), new Vector2(-1.4f, 0.45f), new Vector2(-40f, 0.42f));
        for (int i = 0; i < 40; i++)
        {
            float x = -30f + i * 0.73f + 0.2f * MathF.Sin(i * 3.1f);
            if (x > -1.3f) break;
            float h = 0.12f + 0.07f * MathF.Abs(MathF.Sin(i * 1.9f));
            float s = 0.03f * MathF.Sin(t * 1.3f + i);
            Lp.Poly(this, i % 3 == 0 ? GrassDark : Grass, new Vector2(x - 0.07f, 0.58f), new Vector2(x + 0.07f, 0.58f), new Vector2(x + 0.02f + s, 0.6f + h));
        }

        // Shade trees behind the angler.
        Lp.Oak(this, new Vector2(-8.5f, 0.55f), 3.6f, Trunk, OakLeaf, Moss);
        Lp.Palm(this, new Vector2(-4.2f, 0.55f), 3.8f, 0.4f, Trunk, Palm, MathF.Sin(t * 0.7f) * 0.5f);

        // Cattails in the shallows at the foot of the bank.
        for (int i = 0; i < 9; i++)
        {
            float x = -0.9f + i * 0.17f + 0.04f * MathF.Sin(i * 4.1f);
            float baseY = x < -0.24f ? 0.1f : -0.35f;
            float h = 0.9f + 0.35f * MathF.Abs(MathF.Sin(i * 2.7f));
            float sway = 0.06f * MathF.Sin(t * 1.1f + i * 0.8f);
            var b = new Vector2(x, baseY);
            var top = new Vector2(x + sway, baseY + h);
            Lp.Limb(this, Reed, b, top, 0.035f, 0.02f);
            if (i % 2 == 0) Lp.Limb(this, ReedHead, top + new Vector2(-sway * 0.1f, -0.28f), top + new Vector2(0f, -0.05f), 0.07f, 0.07f);
            else Lp.Poly(this, Reed, b + new Vector2(0.02f, 0f), b + new Vector2(0.05f, 0f), top + new Vector2(0.18f + sway, -0.3f));
        }

        float alpha = Driver.Alpha;
        bool fishOn = false;
        foreach (var f in Driver.World.Fish) if (f.State == FishState.Hooked) { fishOn = true; break; }
        foreach (var a in Driver.World.Anglers)
        {
            Vec2 feetV = Vec2.Lerp(a.PrevPosition, a.Position, alpha);
            var feet = new Vector2(feetV.X, feetV.Y);
            DrawDog(feet + new Vector2(-1.0f, 0f), t, fishOn);
            DrawAngler(a, feet);
        }
    }

    private void DrawAngler(TheLedger.Sim.Angler.AnglerState a, Vector2 feet)
    {
        var shoulder = feet + new Vector2(0f, a.ShoulderHeight);
        var hip = feet + new Vector2(-0.02f, 0.92f);
        var head = shoulder + new Vector2(0.02f, 0.27f);
        var g = a.Rig.Rod.Node(1);
        var grip = new Vector2(g.X, g.Y);

        // Back arm (darker), legs, boots, torso, head, cap, front arm.
        DrawArm(shoulder + new Vector2(-0.05f, -0.02f), grip + new Vector2(-0.08f, -0.03f), ShirtShade, true);
        Lp.Limb(this, JeansShade, hip + new Vector2(-0.06f, 0f), feet + new Vector2(-0.14f, 0.08f), 0.17f, 0.13f);
        Lp.Limb(this, Jeans, hip + new Vector2(0.06f, 0f), feet + new Vector2(0.14f, 0.08f), 0.17f, 0.13f);
        Lp.Poly(this, Boot, feet + new Vector2(-0.24f, 0f), feet + new Vector2(-0.07f, 0f), feet + new Vector2(-0.08f, 0.1f), feet + new Vector2(-0.2f, 0.1f));
        Lp.Poly(this, Boot, feet + new Vector2(0.07f, 0f), feet + new Vector2(0.27f, 0f), feet + new Vector2(0.2f, 0.1f), feet + new Vector2(0.08f, 0.1f));
        Lp.Poly(this, Shirt, hip + new Vector2(-0.17f, -0.04f), hip + new Vector2(0.17f, -0.04f), shoulder + new Vector2(0.18f, -0.04f),
                shoulder + new Vector2(0.1f, 0.05f), shoulder + new Vector2(-0.12f, 0.05f), shoulder + new Vector2(-0.2f, -0.06f));
        Lp.Limb(this, Skin, shoulder + new Vector2(0f, 0.03f), head + new Vector2(0f, -0.08f), 0.09f, 0.09f);
        Lp.Ngon(this, Skin, head, 0.13f, 7, 0.3f);
        Lp.Poly(this, Hair, head + new Vector2(-0.13f, -0.02f), head + new Vector2(-0.06f, 0.03f), head + new Vector2(-0.08f, -0.1f));
        Lp.Poly(this, Cap, head + new Vector2(-0.14f, 0.03f), head + new Vector2(0.12f, 0.03f), head + new Vector2(0.27f, 0.0f),
                head + new Vector2(0.27f, 0.04f), head + new Vector2(0.11f, 0.13f), head + new Vector2(-0.03f, 0.16f), head + new Vector2(-0.12f, 0.11f));
        DrawArm(shoulder, grip, Shirt, false);
    }

    /// <summary>Two-bone arm from shoulder to hand, elbow bent downward, short sleeve then skin.</summary>
    private void DrawArm(Vector2 shoulder, Vector2 hand, Color sleeve, bool back)
    {
        const float l1 = 0.31f, l2 = 0.31f;
        var d = hand - shoulder;
        float dist = Mathf.Clamp(d.Length(), 0.05f, l1 + l2 - 1e-3f);
        var dir = d.Normalized();
        float along = (l1 * l1 - l2 * l2 + dist * dist) / (2f * dist);
        float off = MathF.Sqrt(MathF.Max(0f, l1 * l1 - along * along));
        var n = new Vector2(dir.Y, -dir.X); // clockwise normal: elbow hangs below the shoulder→hand line
        if (n.Y > 0f) n = -n;
        var elbow = shoulder + dir * along + n * off;
        var sleeveEnd = shoulder.Lerp(elbow, 0.6f);
        Lp.Limb(this, sleeve, shoulder, sleeveEnd, 0.12f, 0.11f);
        var skin = back ? Skin.Darkened(0.15f) : Skin;
        Lp.Limb(this, skin, sleeveEnd, elbow, 0.085f, 0.08f);
        Lp.Limb(this, skin, elbow, shoulder + d.Normalized() * dist, 0.08f, 0.07f);
        Lp.Ngon(this, skin, shoulder + d.Normalized() * dist, 0.05f, 6);
    }

    /// <summary>The dog sits by the angler; its tail goes when a fish is on.</summary>
    private void DrawDog(Vector2 at, float t, bool excited)
    {
        float wag = MathF.Sin(t * (excited ? 14f : 3f)) * (excited ? 0.5f : 0.2f);
        float bob = excited ? 0.03f * MathF.Abs(MathF.Sin(t * 7f)) : 0.01f * MathF.Sin(t * 1.3f);
        var hip = at + new Vector2(-0.22f, 0.18f + bob);
        // Tail, back leg, body, front legs, chest, head, ear, snout, nose.
        var tailBase = hip + new Vector2(-0.12f, 0.02f);
        Lp.Limb(this, DogShade, tailBase, tailBase + new Vector2(-0.22f, 0.12f + wag * 0.3f).Rotated(wag * 0.4f), 0.06f, 0.025f);
        Lp.Poly(this, DogShade, at + new Vector2(-0.4f, 0f), at + new Vector2(-0.05f, 0f), hip + new Vector2(0.1f, 0.05f), hip + new Vector2(-0.12f, 0.12f), hip + new Vector2(-0.18f, -0.05f));
        Lp.Poly(this, Dog, hip + new Vector2(-0.14f, 0.08f), hip + new Vector2(0.05f, 0.22f), at + new Vector2(0.14f, 0.58f + bob),
                at + new Vector2(0.24f, 0.42f + bob), at + new Vector2(0.12f, 0.12f), hip + new Vector2(0.05f, -0.12f));
        Lp.Limb(this, Dog, at + new Vector2(0.12f, 0.3f), at + new Vector2(0.12f, 0f), 0.07f, 0.06f);
        Lp.Limb(this, DogShade, at + new Vector2(0.03f, 0.3f), at + new Vector2(0.04f, 0f), 0.07f, 0.06f);
        var head = at + new Vector2(0.24f, 0.66f + bob);
        Lp.Ngon(this, Dog, head, 0.12f, 6, 0.2f);
        Lp.Poly(this, Dog, head + new Vector2(0.05f, -0.02f), head + new Vector2(0.27f, -0.05f), head + new Vector2(0.27f, -0.12f), head + new Vector2(0.05f, -0.12f));
        Lp.Ngon(this, Nose, head + new Vector2(0.27f, -0.07f), 0.03f, 5);
        Lp.Poly(this, DogShade, head + new Vector2(-0.08f, 0.06f), head + new Vector2(0.02f, 0.08f), head + new Vector2(-0.04f, -0.14f), head + new Vector2(-0.1f, -0.1f));
        Lp.Ngon(this, Nose, head + new Vector2(0.08f, 0.03f), 0.018f, 5);
    }
}
