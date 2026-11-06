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

    /// <summary>
    /// A low-poly boulder: a jittered, slightly flattened outline cut into facets from an off-centre point, each facet
    /// shaded by how much it faces the light (upper left).
    /// </summary>
    public static void Boulder(CanvasItem c, Vector2 centre, float r, RandomNumberGenerator rng, Color col)
    {
        int n = rng.RandiRange(7, 10);
        var pts = new Vector2[n];
        float rot = rng.Randf() * Mathf.Tau;
        for (int i = 0; i < n; i++)
        {
            float a = rot + i * Mathf.Tau / n + rng.RandfRange(-0.2f, 0.2f);
            float rr = r * rng.RandfRange(0.78f, 1.12f);
            pts[i] = centre + new Vector2(MathF.Cos(a) * rr * 1.15f, MathF.Sin(a) * rr * 0.8f);
        }
        var hub = centre + new Vector2(-0.18f * r, 0.22f * r);
        var light = new Vector2(-0.55f, 0.83f);
        for (int i = 0; i < n; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % n];
            var mid = (a + b) * 0.5f - centre;
            float shade = 0.72f + 0.38f * mid.Normalized().Dot(light);
            Poly(c, new Color(col.R * shade, col.G * shade, col.B * shade, col.A), hub, a, b);
        }
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

/// <summary>The bank, the pond bottom and the shade trees: none of it moves, so it's drawn once.</summary>
public partial class PondBank : Node2D
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

    private Vector2[]? _terrainPx, _bedPx;
    private Texture2D _soil = null!, _bed = null!, _grass = null!;
    private Texture2D[] _tufts = null!, _tall = null!, _flowers = null!, _water = null!;

    /// <summary>Texture tile size on screen, px (2 m of ground per tile).</summary>
    private const float TilePx = 2f * SimSpace.PixelsPerMetre;

    public override void _Ready()
    {
        // CC0 art (game/assets/CREDITS.md): ambientCG ground textures, Kenney foliage sprites (white, tinted here).
        TextureRepeat = TextureRepeatEnum.Enabled;
        _soil = GD.Load<Texture2D>("res://assets/ground/soil.jpg");
        _bed = GD.Load<Texture2D>("res://assets/ground/lakebed.jpg");
        _grass = GD.Load<Texture2D>("res://assets/ground/grass.jpg");
        Texture2D F(string n) => GD.Load<Texture2D>($"res://assets/foliage/foliage_{n}.png");
        _tufts = new[] { F("0001"), F("0002"), F("0003"), F("0011"), F("0012"), F("0016"), F("0018") };
        _tall = new[] { F("0005"), F("0006"), F("0017"), F("0021"), F("0022") };
        _flowers = new[] { F("0010"), F("0020") };
        _water = new[] { F("0009"), F("0024") };
    }

    /// <summary>A textured polygon: UVs from screen position so the texture tiles seamlessly across shapes.</summary>
    private void Textured(Vector2[] px, Texture2D tex, Color tint)
    {
        var uv = new Vector2[px.Length];
        var col = new Color[px.Length];
        for (int i = 0; i < px.Length; i++) { uv[i] = px[i] / TilePx; col[i] = tint; }
        DrawPolygon(px, col, uv, tex);
    }

    /// <summary>A foliage sprite standing on (x, y) world metres, <paramref name="h"/> m tall, tinted.</summary>
    private void Plant(Texture2D tex, float x, float y, float h, Color tint, bool flip = false)
    {
        float hp = h * SimSpace.PixelsPerMetre;
        float wp = hp * tex.GetWidth() / MathF.Max(tex.GetHeight(), 1f);
        var basePx = SimSpace.ToPx(x, y);
        var rect = new Rect2(basePx.X - wp * 0.5f, basePx.Y - hp, flip ? -wp : wp, hp);
        if (flip) rect.Position = new Vector2(basePx.X + wp * 0.5f, rect.Position.Y);
        DrawTextureRect(tex, rect, false, tint);
    }

    public override void _Draw()
    {
        var spot = Driver.World.Spot;
        float wl = spot.WaterlineX;
        if (_terrainPx == null)
        {
            // The spot's cross-section closed off below, and the lake-bed band: the bottom surface out from the
            // waterline, 0.7 m thick (mossy mud over the soil underneath).
            var pts = new List<Vector2>();
            var top = new List<Vector2>();
            for (int i = 0; i < spot.PointCount; i++)
            {
                var p = spot.Point(i);
                pts.Add(SimSpace.ToPx(p.X, p.Y));
                if (p.X > wl) top.Add(new Vector2(p.X, p.Y));
            }
            var last = spot.Point(spot.PointCount - 1);
            pts.Add(SimSpace.ToPx(last.X, -14f));
            pts.Add(SimSpace.ToPx(spot.Point(0).X, -14f));
            top.Insert(0, new Vector2(wl, spot.GroundHeight(wl)));
            var bed = new List<Vector2>();
            foreach (var q in top) bed.Add(SimSpace.ToPx(q.X, q.Y));
            for (int i = top.Count - 1; i >= 0; i--) bed.Add(SimSpace.ToPx(top[i].X + (i == 0 ? 0.35f : 0f), top[i].Y - 0.7f));
            _terrainPx = pts.ToArray();
            _bedPx = bed.ToArray();
        }
        // Earth: soil through the whole cross-section, the mossy lake bed as a band along the bottom.
        Textured(_terrainPx, _soil, new Color(0.62f, 0.55f, 0.48f));
        Textured(_bedPx!, _bed, new Color(0.62f, 0.66f, 0.6f));

        // Bank top: a sandy lip down to the water, then a grass strip with clumps and the odd flower.
        Lp.Poly(this, Sand, new Vector2(-1.35f, 0.5f), new Vector2(-1.15f, 0.6f), new Vector2(0.4f, -0.4f), new Vector2(0.1f, -0.45f), new Vector2(-0.3f, -0.05f));
        Textured(new[] { SimSpace.ToPx(-40f, 0.6f), SimSpace.ToPx(-1.2f, 0.6f), SimSpace.ToPx(-1.4f, 0.4f), SimSpace.ToPx(-40f, 0.38f) }, _grass, new Color(0.85f, 0.95f, 0.75f));
        var rng = new RandomNumberGenerator { Seed = 1234 };
        for (float x = -30f; x < -1.35f; x += rng.RandfRange(0.25f, 0.55f))
        {
            var tex = _tufts[rng.RandiRange(0, _tufts.Length - 1)];
            var tint = new Color(0.32f, 0.48f, 0.2f).Lerp(new Color(0.5f, 0.6f, 0.28f), rng.Randf());
            Plant(tex, x, 0.58f, rng.RandfRange(0.18f, 0.34f), tint, rng.Randf() < 0.5f);
            if (rng.Randf() < 0.08f)
                Plant(_flowers[rng.RandiRange(0, 1)], x + 0.1f, 0.58f, rng.RandfRange(0.2f, 0.3f),
                      rng.Randf() < 0.5f ? new Color(0.95f, 0.9f, 0.55f) : new Color(0.85f, 0.7f, 0.9f));
        }
        // Shoreline: marsh grass and seed-headed reeds down the sandy slope.
        for (float x = -1.3f; x < 0.35f; x += rng.RandfRange(0.12f, 0.22f))
        {
            float y = spot.GroundHeight(x) < 0.6f ? MathF.Max(spot.GroundHeight(x), -0.3f) : 0.58f;
            if (x < -1.15f) y = 0.58f;
            var tint = new Color(0.42f, 0.5f, 0.24f).Lerp(new Color(0.6f, 0.58f, 0.32f), rng.Randf());
            Plant(_tall[rng.RandiRange(0, _tall.Length - 1)], x, y, rng.RandfRange(0.45f, 0.85f), tint, rng.Randf() < 0.5f);
        }
        // Submerged plants scattered over the lake bed (tinted toward the water colour).
        for (float x = 1.5f; x < 18f; x += rng.RandfRange(0.9f, 2.4f))
            Plant(_water[rng.RandiRange(0, 1)], x, spot.GroundHeight(x), rng.RandfRange(0.35f, 0.8f), new Color(0.2f, 0.38f, 0.3f, 0.85f), rng.Randf() < 0.5f);

        // Shade trees behind the angler.
        Lp.Oak(this, new Vector2(-8.5f, 0.55f), 3.6f, Trunk, OakLeaf, Moss);
        Lp.Palm(this, new Vector2(-4.2f, 0.55f), 3.8f, 0.4f, Trunk, Palm, 0f);

    }
}

/// <summary>The moving things in front of the water: cover (pads, weeds), cattails, the angler and the dog.</summary>
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

    // Skiff, the scruffy station dog (docs/01 section 3): brindle-brown with a pale muzzle.
    private static readonly Color Dog = new(0.55f, 0.45f, 0.34f);
    private static readonly Color DogShade = new(0.42f, 0.34f, 0.26f);
    private static readonly Color Muzzle = new(0.78f, 0.72f, 0.62f);
    private static readonly Color Nose = new(0.15f, 0.11f, 0.09f);

    /// <summary>Whether Skiff comes fishing (Tab menu; saved in settings).</summary>
    public static bool DogOn = true;

    private enum DogMood { Sit, Alert, Sleep }
    private DogMood _mood = DogMood.Sit;
    private double _moodUntil, _quietSince, _barkUntil, _nextBark;
    private int _barksLeft;

    public override void _Ready()
    {
        Driver.EventRaised += OnDogEvent;
        var cfg = new ConfigFile();
        if (cfg.Load("user://settings.cfg") == Error.Ok) DogOn = (bool)cfg.GetValue("game", "dog", true);
        _quietSince = Now;
    }

    public override void _ExitTree() => Driver.EventRaised -= OnDogEvent;

    private static double Now => Time.GetTicksMsec() * 0.001;

    /// <summary>Skiff reacts to the fishing: barks at jumps, perks up at hooksets and night bites, wakes for any of it.</summary>
    private void OnDogEvent(SimEvent e)
    {
        bool night = Driver.World.Hour < 6f || Driver.World.Hour > 20f;
        switch (e.Type)
        {
            case SimEventType.FishJumped: Wake(4); Bark(2); break;
            case SimEventType.HookSet: Wake(6); Bark(1); break;
            case SimEventType.FishLanded: Wake(5); break;
            case SimEventType.Bite when night: Wake(3); break;
            case SimEventType.LureEntered:
                _quietSince = Now;
                if (_mood == DogMood.Sleep) Wake(2);
                break;
        }
    }

    private void Wake(double seconds)
    {
        _mood = DogMood.Alert;
        _moodUntil = Now + seconds;
        _quietSince = Now;
    }

    private void Bark(int n)
    {
        _barksLeft = n;
        _nextBark = Now + 0.15;
    }

    /// <summary>A bark happened (the sound engine listens for this).</summary>
    public static event Action? Barked;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        float t = (float)Time.GetTicksMsec() * 0.001f;
        foreach (var z in Driver.World.Spot.Structures) DrawStructure(z, t);

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
        foreach (var f in Driver.World.Fish)
        {
            if (f.State != FishState.Hooked) continue;
            fishOn = true;
            // A soft pulsing ring round the fish you're fighting, so it's never lost among the others.
            var c = SimSpace.ToPx(f.HeadX, f.HeadY);
            float r = (f.LengthM * 0.7f + 0.15f) * SimSpace.PixelsPerMetre * (1f + 0.12f * MathF.Sin(t * 6f));
            DrawArc(c, r, 0f, Mathf.Tau, 32, new Color(1f, 0.85f, 0.45f, 0.75f), 3f);
        }
        foreach (var a in Driver.World.Anglers)
        {
            Vec2 feetV = Vec2.Lerp(a.PrevPosition, a.Position, alpha);
            var feet = new Vector2(feetV.X, feetV.Y);
            if (DogOn) DrawDog(feet + new Vector2(-1.0f, 0f), t, fishOn);
            DrawAngler(a, feet);
        }
    }

    private static readonly Color Wood = new(0.24f, 0.21f, 0.17f, 0.97f);
    private static readonly Color WoodLight = new(0.33f, 0.29f, 0.23f, 0.97f);
    private static readonly Color Pad = new(0.27f, 0.50f, 0.25f);
    private static readonly Color PadDark = new(0.20f, 0.40f, 0.19f);
    private static readonly Color Weed = new(0.26f, 0.45f, 0.27f, 0.85f);
    private static readonly Color Rock = new(0.46f, 0.45f, 0.40f);
    private static readonly Color RockDark = new(0.36f, 0.35f, 0.31f);

    /// <summary>Cover in the water, drawn from the spot's structure zones (a stable layout per zone).</summary>
    private void DrawStructure(TheLedger.Sim.World.StructureZone z, float t)
    {
        var w = Driver.World;
        var rng = new RandomNumberGenerator { Seed = (ulong)(z.X0 * 1000f + z.Y0 * 37f + 11f) };
        switch (z.Kind)
        {
            case "wood":
            {
                // The fallen tree the sim lays out (hooks snag on exactly these pieces).
                for (int i = 0; i < z.Parts.Length; i++)
                {
                    var part = z.Parts[i];
                    Lp.Limb(this, i % 4 == 1 ? WoodLight : Wood, new Vector2(part.A.X, part.A.Y), new Vector2(part.B.X, part.B.Y), part.RA * 2f, part.RB * 2f);
                }
                if (z.Parts.Length > 0) Lp.Ngon(this, WoodLight, new Vector2(z.Parts[0].A.X, z.Parts[0].A.Y), 0.23f, 7, 0.4f); // root end
                break;
            }
            case "lilyPads":
            {
                for (float x = z.X0; x < z.X1; x += rng.RandfRange(0.35f, 0.6f))
                {
                    float sY = w.Water.Height(x, w.Time);
                    float ground = w.Spot.GroundHeight(x);
                    Lp.Limb(this, new Color(0.3f, 0.45f, 0.25f, 0.5f), new Vector2(x, ground), new Vector2(x + 0.05f * MathF.Sin(t + x), sY - 0.02f), 0.025f, 0.02f);
                    float r = rng.RandfRange(0.14f, 0.22f);
                    Lp.Poly(this, rng.Randf() < 0.3f ? PadDark : Pad, new Vector2(x - r, sY + 0.005f), new Vector2(x - r * 0.6f, sY + 0.035f),
                            new Vector2(x + r * 0.6f, sY + 0.035f), new Vector2(x + r, sY + 0.005f), new Vector2(x, sY - 0.01f));
                    if (rng.Randf() < 0.18f)
                        Lp.Poly(this, new Color(0.97f, 0.95f, 0.92f), new Vector2(x - 0.05f, sY + 0.03f), new Vector2(x, sY + 0.12f), new Vector2(x + 0.05f, sY + 0.03f));
                }
                break;
            }
            case "weeds":
            {
                for (float x = z.X0; x < z.X1; x += rng.RandfRange(0.12f, 0.3f))
                {
                    float ground = w.Spot.GroundHeight(x);
                    float h = rng.RandfRange(0.4f, 1f) * (z.Y1 - ground);
                    float sway = 0.12f * MathF.Sin(t * 0.8f + x * 1.7f);
                    var b0 = new Vector2(x, ground);
                    var m = new Vector2(x + sway * 0.4f, ground + h * 0.5f);
                    var top = new Vector2(x + sway, ground + h);
                    Lp.Limb(this, Weed, b0, m, 0.07f, 0.05f);
                    Lp.Limb(this, Weed, m, top, 0.05f, 0.015f);
                }
                break;
            }
            case "rock":
            {
                for (float x = z.X0; x < z.X1; x += rng.RandfRange(0.4f, 0.9f))
                {
                    float ground = w.Spot.GroundHeight(x);
                    float r = rng.RandfRange(0.25f, 0.6f);
                    Lp.Boulder(this, new Vector2(x, ground + r * 0.35f), r, rng, rng.Randf() < 0.5f ? Rock : RockDark);
                }
                break;
            }
        }
    }

    /// <summary>Skiff asleep: lying flat, head on paws, slow breathing.</summary>
    private void DrawDogAsleep(Vector2 at, float t)
    {
        float breathe = 0.012f * MathF.Sin(t * 1.6f);
        Lp.Poly(this, DogShade, at + new Vector2(-0.5f, 0.02f), at + new Vector2(-0.62f, 0.12f), at + new Vector2(-0.45f, 0.1f)); // tail curled
        Lp.Poly(this, Dog, at + new Vector2(-0.48f, 0f), at + new Vector2(0.2f, 0f), at + new Vector2(0.16f, 0.2f + breathe),
                at + new Vector2(-0.1f, 0.26f + breathe), at + new Vector2(-0.42f, 0.2f + breathe));
        var head = at + new Vector2(0.3f, 0.1f);
        Lp.Ngon(this, Dog, head, 0.1f, 6, 0.3f);
        Lp.Poly(this, Muzzle, head + new Vector2(0.04f, -0.08f), head + new Vector2(0.24f, -0.08f), head + new Vector2(0.22f, -0.01f), head + new Vector2(0.04f, 0.02f));
        Lp.Poly(this, DogShade, head + new Vector2(-0.07f, 0.05f), head + new Vector2(0.03f, 0.07f), head + new Vector2(0.02f, -0.1f), head + new Vector2(-0.08f, -0.08f));
        Lp.Ngon(this, Nose, head + new Vector2(0.24f, -0.045f), 0.025f, 5);
    }

    // Per-angler pose state, eased from frame to frame (index = angler).
    private readonly float[] _lean = new float[4], _look = new float[4], _crankBlend = new float[4], _crankPhase = new float[4];
    private float _poseT = -1f;

    /// <summary>
    /// The angler, posed from the sim: the front hand on the rod's grip, the other hand turning the reel handle while
    /// they reel (at the real handle speed), the body leaning into the cast and back against a fish's pull, the head
    /// following the lure, and slow breathing when nothing's happening.
    /// </summary>
    private void DrawAngler(TheLedger.Sim.Angler.AnglerState a, Vector2 feet)
    {
        int i = 0;
        while (i < 3 && i < Driver.World.Anglers.Count && Driver.World.Anglers[i] != a) i++;
        float t = (float)Time.GetTicksMsec() * 0.001f;
        float dt = _poseT < 0f ? 0f : Math.Clamp(t - _poseT, 0f, 0.1f);
        if (i == 0) _poseT = t;
        float ease(float rate) => 1f - MathF.Exp(-dt * rate);

        var g = a.Rig.Rod.Node(1);
        var bt = a.Rig.Rod.Node(0);
        var grip = new Vector2(g.X, g.Y);
        var butt = new Vector2(bt.X, bt.Y);
        var hip = feet + new Vector2(-0.02f, 0.92f);
        var shoulder0 = feet + new Vector2(0f, a.ShoulderHeight);

        // Lean: forward as the hands go out over the water, back against a fish's pull (the tip tension).
        var line = a.Rig.Line;
        float pull = Math.Clamp(line.TipTension / 25f, 0f, 1f);
        float reach = Math.Clamp((grip.X - shoulder0.X - 0.35f) * 0.5f, -0.12f, 0.16f);
        float leanTarget = reach - 0.22f * pull;
        _lean[i] += (leanTarget - _lean[i]) * ease(6f);
        float breathe = 0.007f * MathF.Sin(t * 1.5f + i);
        Vector2 Body(Vector2 p) => hip + (p - hip).Rotated(-_lean[i]);
        var shoulder = Body(shoulder0) + new Vector2(0f, breathe);
        var head0 = shoulder + new Vector2(0.02f, 0.27f).Rotated(-_lean[i]);

        // The head follows the lure (or the fish on it), within what a neck allows.
        int last = line.Count - 1;
        var lure = new Vector2(line.X[last], line.Y[last]);
        var toLure = lure - head0;
        float lookTarget = toLure.LengthSquared() > 1f ? Math.Clamp(MathF.Atan2(toLure.Y, MathF.Max(toLure.X, 0.3f)) * 0.45f, -0.35f, 0.18f) : -0.2f;
        _look[i] += (lookTarget - _look[i]) * ease(4f);
        float look = _look[i];
        Vector2 H(float x, float y) => head0 + new Vector2(x, y).Rotated(look);

        // The reel: under the seat on a spinning rod, on top of a casting rod. Its handle turns as the angler cranks.
        var hdir = (grip - butt).LengthSquared() > 1e-6f ? (grip - butt).Normalized() : Vector2.Right;
        var below = new Vector2(hdir.Y, -hdir.X); // under the rod in y-up space
        bool casting = a.Rig.Rod.Spec.Type == "casting";
        var seat = grip.Lerp(butt, 0.18f);
        var reelC = casting ? seat - below * 0.03f : seat + below * 0.07f;
        float crank = a.LastInput.CrankRate;
        bool cranking = crank > 0.05f && !a.Reel.BailOpen;
        _crankBlend[i] += ((cranking ? 1f : 0f) - _crankBlend[i]) * ease(cranking ? 10f : 2f);
        _crankPhase[i] += crank * MathF.Tau * dt;
        float ph = _crankPhase[i];
        var handle = reelC + (hdir * MathF.Cos(ph) + below * MathF.Sin(ph)) * 0.055f;
        var rest = grip + new Vector2(-0.08f, -0.03f);
        var backHand = rest.Lerp(handle, _crankBlend[i]);

        // Back arm (darker), legs, boots, torso, head, cap, front arm.
        DrawArm(shoulder + new Vector2(-0.05f, -0.02f), backHand, ShirtShade, true);
        Lp.Limb(this, JeansShade, hip + new Vector2(-0.06f, 0f), feet + new Vector2(-0.14f, 0.08f), 0.17f, 0.13f);
        Lp.Limb(this, Jeans, hip + new Vector2(0.06f, 0f), feet + new Vector2(0.14f, 0.08f), 0.17f, 0.13f);
        Lp.Poly(this, Boot, feet + new Vector2(-0.24f, 0f), feet + new Vector2(-0.07f, 0f), feet + new Vector2(-0.08f, 0.1f), feet + new Vector2(-0.2f, 0.1f));
        Lp.Poly(this, Boot, feet + new Vector2(0.07f, 0f), feet + new Vector2(0.27f, 0f), feet + new Vector2(0.2f, 0.1f), feet + new Vector2(0.08f, 0.1f));
        Lp.Poly(this, Shirt, Body(hip + new Vector2(-0.17f, -0.04f)), Body(hip + new Vector2(0.17f, -0.04f)), shoulder + new Vector2(0.18f, -0.04f).Rotated(-_lean[i]),
                shoulder + new Vector2(0.1f, 0.05f).Rotated(-_lean[i]), shoulder + new Vector2(-0.12f, 0.05f).Rotated(-_lean[i]), shoulder + new Vector2(-0.2f, -0.06f).Rotated(-_lean[i]));
        // A fishing vest over the shirt: pockets and a darker panel.
        Lp.Poly(this, Vest, Body(hip + new Vector2(-0.15f, 0.05f)), Body(hip + new Vector2(0.15f, 0.05f)), shoulder + new Vector2(0.12f, -0.06f).Rotated(-_lean[i]), shoulder + new Vector2(-0.14f, -0.06f).Rotated(-_lean[i]));
        Lp.Poly(this, VestPocket, Body(hip + new Vector2(0.02f, 0.12f)), Body(hip + new Vector2(0.13f, 0.12f)), Body(hip + new Vector2(0.13f, 0.24f)), Body(hip + new Vector2(0.02f, 0.24f)));
        Lp.Limb(this, Skin, shoulder + new Vector2(0f, 0.03f), head0 + new Vector2(0f, -0.08f), 0.09f, 0.09f);
        Lp.Ngon(this, Skin, head0, 0.13f, 7, 0.3f + look);
        Lp.Poly(this, Hair, H(-0.13f, -0.02f), H(-0.06f, 0.03f), H(-0.08f, -0.1f));
        Lp.Poly(this, Shades, H(0.05f, 0.02f), H(0.135f, 0.02f), H(0.13f, -0.015f), H(0.06f, -0.01f)); // sunglasses
        Lp.Poly(this, Cap, H(-0.14f, 0.03f), H(0.12f, 0.03f), H(0.27f, 0.0f), H(0.27f, 0.04f), H(0.11f, 0.13f), H(-0.03f, 0.16f), H(-0.12f, 0.11f));
        DrawArm(shoulder, grip, Shirt, false);
    }

    private static readonly Color Vest = new(0.36f, 0.38f, 0.27f);
    private static readonly Color VestPocket = new(0.30f, 0.32f, 0.22f);
    private static readonly Color Shades = new(0.08f, 0.09f, 0.1f);

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

    /// <summary>Skiff sits by the angler: alert (ears up, tail going) when something happens, asleep when nothing does.</summary>
    private void DrawDog(Vector2 at, float t, bool excited)
    {
        double now = Now;
        if (_barksLeft > 0 && now >= _nextBark)
        {
            _barksLeft--;
            _barkUntil = now + 0.16;
            _nextBark = now + 0.32;
            Barked?.Invoke();
        }
        if (excited) { _mood = DogMood.Alert; _moodUntil = now + 2; _quietSince = now; }
        if (_mood == DogMood.Alert && now > _moodUntil) _mood = DogMood.Sit;
        bool night = Driver.World.Hour < 6f || Driver.World.Hour > 20f;
        if (_mood == DogMood.Sit && now - _quietSince > (night ? 25 : 60)) _mood = DogMood.Sleep;
        if (_mood == DogMood.Sleep) { DrawDogAsleep(at, t); return; }
        bool alert = _mood == DogMood.Alert;
        bool barking = now < _barkUntil;
        excited |= alert;
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
        var head = at + new Vector2(0.24f, 0.66f + bob + (alert ? 0.04f : 0f) + (barking ? 0.03f : 0f));
        Lp.Ngon(this, Dog, head, 0.12f, 6, 0.2f);
        // Scruffy tufts on the head and chest.
        Lp.Poly(this, DogShade, head + new Vector2(-0.06f, 0.1f), head + new Vector2(-0.02f, 0.16f), head + new Vector2(0.02f, 0.1f));
        Lp.Poly(this, DogShade, hip + new Vector2(0.24f, 0.1f), hip + new Vector2(0.3f, 0.02f), hip + new Vector2(0.2f, 0.04f));
        Lp.Poly(this, Muzzle, head + new Vector2(0.05f, -0.02f), head + new Vector2(0.27f, -0.05f), head + new Vector2(0.27f, -0.12f), head + new Vector2(0.05f, -0.12f));
        if (barking) Lp.Poly(this, Nose, head + new Vector2(0.12f, -0.11f), head + new Vector2(0.27f, -0.12f), head + new Vector2(0.24f, -0.19f));
        Lp.Ngon(this, Nose, head + new Vector2(0.27f, -0.07f), 0.03f, 5);
        if (alert)
            Lp.Poly(this, DogShade, head + new Vector2(-0.08f, 0.06f), head + new Vector2(0.0f, 0.08f), head + new Vector2(-0.05f, 0.24f)); // ears pricked
        else
            Lp.Poly(this, DogShade, head + new Vector2(-0.08f, 0.06f), head + new Vector2(0.02f, 0.08f), head + new Vector2(-0.04f, -0.14f), head + new Vector2(-0.1f, -0.1f));
        Lp.Ngon(this, Nose, head + new Vector2(0.08f, 0.03f), 0.018f, 5);
    }
}
