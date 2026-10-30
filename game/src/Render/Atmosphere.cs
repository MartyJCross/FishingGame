using Godot;
using TheLedger.Boot;
using TheLedger.SimGlue;

namespace TheLedger.Render;

/// <summary>
/// The light of the moment (docs/07_ART_DIRECTION.md §4.2, §7): palette colours from the sun's elevation (night, blue
/// hour, sunrise, golden hour, day), greyed and dimmed by cloud and rain. Every renderer reads <see cref="Now"/>.
/// Cosmetic only: nothing here touches the sim.
/// </summary>
public struct SkyState
{
    public Color SkyTop, SkyHorizon, Sun, Ambient, Haze, CloudLit, CloudShade;
    /// <summary>Sun elevation, -1 (midnight) .. 1 (noon); 0 at sunrise and sunset.</summary>
    public float Elevation;
    /// <summary>0 at sunrise .. 1 at sunset (outside that range at night).</summary>
    public float DayFrac, NightFrac;
    /// <summary>Direct sunlight strength 0..1 (god rays, caustics, glints): low sun, cloud and night take it away.</summary>
    public float Direct;
    public float Stars, Moon, Cloud, Rain, Mist;
    /// <summary>Where the sun and moon are on screen (0..1), set by the director.</summary>
    public Vector2 SunUv, MoonUv;
}

public static class Atmosphere
{
    public static SkyState Now;

    /// <summary>A lightning flash happened; the value is how far off the strike was (0 = close, 1 = distant).</summary>
    public static event Action<float>? Lightning;

    internal static void RaiseLightning(float distance) => Lightning?.Invoke(distance);

    private readonly struct Key
    {
        public readonly float E;
        public readonly Color Top, Horizon, Sun, Ambient;
        public Key(float e, string top, string horizon, string sun, Color ambient)
        {
            E = e; Top = Color.FromHtml(top); Horizon = Color.FromHtml(horizon); Sun = Color.FromHtml(sun); Ambient = ambient;
        }
    }

    // Sun elevation keyframes (a Nature Coast palette: warm horizons, blue-black nights, never grey).
    private static readonly Key[] Keys =
    {
        new(-1.0f,  "#060a18", "#111a33", "#8aa0d0", new Color(0.19f, 0.23f, 0.39f)),
        new(-0.14f, "#0a1228", "#1c2848", "#90a6d8", new Color(0.23f, 0.27f, 0.44f)),
        new(-0.05f, "#25305c", "#7a6386", "#e08a70", new Color(0.42f, 0.40f, 0.56f)),
        new(0.0f,   "#3b5286", "#f2925a", "#ffa866", new Color(0.82f, 0.64f, 0.56f)),
        new(0.09f,  "#4a70aa", "#f6bf88", "#ffcf94", new Color(1.0f, 0.85f, 0.71f)),
        new(0.28f,  "#3f7cc2", "#c4dbe8", "#fff1d6", new Color(1.0f, 0.97f, 0.92f)),
        new(1.0f,   "#2c6cb8", "#b2d2e8", "#fffbea", new Color(1f, 1f, 1f)),
    };

    /// <summary>Sunrise and sunset hours on the Nature Coast by season (local clock, simplified).</summary>
    public static (float rise, float set) SunTimes(string season) => season switch
    {
        "winter" => (7.2f, 17.9f),
        "summer" => (6.6f, 20.4f),
        "autumn" => (7.3f, 19.1f),
        _ => (6.9f, 19.6f),
    };

    public static SkyState Compute(float hour, string season, float cloud, float rain)
    {
        var (rise, set) = SunTimes(season);
        var s = new SkyState { Cloud = Mathf.Clamp(cloud, 0f, 1f), Rain = Mathf.Clamp(rain / 25f, 0f, 1f) };
        float dayLen = set - rise;
        s.DayFrac = (hour - rise) / dayLen;
        float nightLen = 24f - dayLen;
        s.NightFrac = Mathf.PosMod(hour - set, 24f) / nightLen;
        s.Elevation = s.DayFrac is >= 0f and <= 1f ? MathF.Sin(MathF.PI * s.DayFrac) : -MathF.Sin(MathF.PI * Math.Clamp(s.NightFrac, 0f, 1f));

        int i = 0;
        while (i < Keys.Length - 2 && s.Elevation > Keys[i + 1].E) i++;
        var a = Keys[i];
        var b = Keys[i + 1];
        float t = Mathf.Clamp((s.Elevation - a.E) / (b.E - a.E), 0f, 1f);
        s.SkyTop = a.Top.Lerp(b.Top, t);
        s.SkyHorizon = a.Horizon.Lerp(b.Horizon, t);
        s.Sun = a.Sun.Lerp(b.Sun, t);
        s.Ambient = a.Ambient.Lerp(b.Ambient, t);

        // Cloud and rain: flatten the sky toward a grey of the same brightness, dim and cool the light.
        float overcast = s.Cloud * 0.85f + s.Rain * 0.15f;
        Color Grey(Color c, float dim) { float l = c.R * 0.3f + c.G * 0.59f + c.B * 0.11f; return new Color(l * 0.95f, l * 0.98f, l * 1.04f) * dim; }
        s.SkyTop = s.SkyTop.Lerp(Grey(s.SkyTop, 1.15f), overcast);
        s.SkyHorizon = s.SkyHorizon.Lerp(Grey(s.SkyHorizon, 0.92f), overcast);
        s.Ambient = s.Ambient.Lerp(s.Ambient * new Color(0.74f, 0.77f, 0.82f), overcast);
        s.Haze = s.SkyHorizon.Lerp(s.SkyTop, 0.2f);

        float day = Mathf.Clamp((s.Elevation + 0.06f) / 0.16f, 0f, 1f);
        s.Direct = Mathf.Clamp(s.Elevation * 3f, 0f, 1f) * (1f - 0.85f * s.Cloud);
        s.Stars = Mathf.Clamp((-s.Elevation - 0.03f) / 0.12f, 0f, 1f) * (1f - s.Cloud);
        s.Moon = Mathf.Clamp((-s.Elevation + 0.02f) / 0.1f, 0f, 1f) * (1f - 0.8f * s.Cloud);

        // Clouds: lit by the sun's colour by day, blue-grey at night; storms are dark underneath.
        var lit = Colors.White.Lerp(s.Sun, 0.35f) * Mathf.Lerp(0.3f, 1f, day);
        lit = lit.Lerp(new Color(0.42f, 0.48f, 0.62f), (1f - day) * 0.6f);
        s.CloudLit = lit.Lerp(Grey(lit, 0.9f), overcast) * (1f - 0.35f * s.Rain);
        s.CloudShade = s.SkyTop.Lerp(new Color(0.55f, 0.58f, 0.66f) * Mathf.Lerp(0.25f, 1f, day), 0.55f).Lerp(new Color(0.3f, 0.32f, 0.37f), s.Rain * 0.8f);
        s.CloudLit.A = s.CloudShade.A = 1f;

        // Mist on the water at first light, and in rain.
        float dawn = Mathf.Clamp(1f - MathF.Abs(s.DayFrac - 0.02f) / 0.08f, 0f, 1f);
        s.Mist = Mathf.Clamp(dawn * 0.55f + s.Rain * 0.35f, 0f, 0.7f);
        return s;
    }
}

/// <summary>
/// Updates <see cref="Atmosphere.Now"/> each frame and drives the sky shader, the far-land layers (haze, light,
/// parallax), the day/night tint on the near world and the vignette.
/// </summary>
public partial class AtmosphereDirector : Node
{
    public SimDriver Driver { get; set; } = null!;
    public CanvasModulate Daylight { get; set; } = null!;

    private ShaderMaterial _sky = null!, _rain = null!;
    private ColorRect _rainRect = null!;
    private readonly List<(FarShore node, ShaderMaterial mat)> _layers = new();
    private float _drift;
    private readonly RandomNumberGenerator _rng = new() { Seed = 31337 };
    private float _nextStrike = 8f, _flashT = 99f;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // the sky keeps moving behind the title and pause menus

        var skyLayer = new CanvasLayer { Name = "SkyLayer", Layer = -10 };
        AddChild(skyLayer);
        _sky = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/sky.gdshader") };
        var rect = new ColorRect { Material = _sky, MouseFilter = Control.MouseFilterEnum.Ignore };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        skyLayer.AddChild(rect);

        // Far land: three silhouette bands, farthest first, each hazier and moving less with the camera.
        var landLayer = new CanvasLayer { Name = "FarLand", Layer = -5, FollowViewportEnabled = true };
        AddChild(landLayer);
        var shader = GD.Load<Shader>("res://shaders/far_land.gdshader");
        string scenery = Driver.Info.Scenery;
        for (int band = 0; band < 3; band++)
        {
            var mat = new ShaderMaterial { Shader = shader };
            var node = new FarShore { Band = band, Scenery = scenery, Material = mat, Name = $"Band{band}" };
            landLayer.AddChild(node);
            _layers.Add((node, mat));
        }

        // Fireflies glow on their own layer above the world (added light: the night tint mustn't dim them).
        var glowLayer = new CanvasLayer { Name = "Glow", Layer = 1, FollowViewportEnabled = true };
        AddChild(glowLayer);
        glowLayer.AddChild(new Fireflies { Name = "Fireflies", Driver = Driver });

        var rainLayer = new CanvasLayer { Name = "Rain", Layer = 3 };
        AddChild(rainLayer);
        _rain = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/rain.gdshader") };
        _rainRect = new ColorRect { Material = _rain, MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        _rainRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        rainLayer.AddChild(_rainRect);

        var post = new CanvasLayer { Name = "Vignette", Layer = 4 };
        AddChild(post);
        var vig = new ColorRect { Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/vignette.gdshader") }, MouseFilter = Control.MouseFilterEnum.Ignore };
        vig.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        post.AddChild(vig);
    }

    private static readonly float[] Haze = { 0.62f, 0.4f, 0.2f };
    private static readonly float[] Parallax = { 0.45f, 0.3f, 0.15f };

    public override void _Process(double delta)
    {
        var w = Driver.World;
        var c = Driver.Config;
        var s = Atmosphere.Now = Atmosphere.Compute(w.Hour, w.Season, c.CloudCover, c.DebugRainRate);

        // Thunderstorms: every so often a flash (a bright pulse and a flicker), then the thunder (SoundEngine).
        _flashT += (float)delta;
        if (s.Rain > 0.55f)
        {
            _nextStrike -= (float)delta;
            if (_nextStrike <= 0f)
            {
                _nextStrike = _rng.RandfRange(6f, 22f);
                _flashT = 0f;
                Atmosphere.RaiseLightning(_rng.Randf());
            }
        }
        float flash = _flashT < 0.6f ? MathF.Exp(-_flashT * 16f) + (_flashT > 0.16f ? 0.75f * MathF.Exp(-(_flashT - 0.16f) * 12f) : 0f) : 0f;
        if (flash > 0.001f)
        {
            var white = new Color(0.92f, 0.94f, 1f);
            s.Ambient = s.Ambient.Lerp(white, Mathf.Clamp(flash * 0.65f, 0f, 1f));
            s.SkyTop = s.SkyTop.Lerp(white * 0.8f, Mathf.Clamp(flash * 0.5f, 0f, 1f));
            s.SkyHorizon = s.SkyHorizon.Lerp(white, Mathf.Clamp(flash * 0.5f, 0f, 1f));
            s.CloudShade = s.CloudShade.Lerp(white * 0.85f, Mathf.Clamp(flash * 0.6f, 0f, 1f));
            s.Haze = s.Haze.Lerp(white * 0.9f, Mathf.Clamp(flash * 0.4f, 0f, 1f));
        }
        Daylight.Color = s.Ambient;

        _rainRect.Visible = s.Rain > 0.01f;
        if (_rainRect.Visible)
        {
            _rain.SetShaderParameter("amount", Mathf.Clamp(c.DebugRainRate / 20f, 0.1f, 1f));
            _rain.SetShaderParameter("slant", Mathf.Clamp(c.DebugWindSpeed * 0.025f, 0.02f, 0.4f));
            _rain.SetShaderParameter("aspect", GetViewport().GetVisibleRect().Size.Aspect());
        }

        var vp = GetViewport();
        var size = vp.GetVisibleRect().Size;
        var ct = vp.GetCanvasTransform();
        float zoom = ct.X.X;
        float horizonUv = (ct * SimSpace.ToPx(0f, 0.35f)).Y / size.Y;
        float camX = (ct.AffineInverse() * (size * 0.5f)).X / SimSpace.PixelsPerMetre;
        _drift += (float)delta * (0.15f + 0.08f * c.DebugWindSpeed);

        _sky.SetShaderParameter("sky_top", s.SkyTop);
        _sky.SetShaderParameter("sky_horizon", s.SkyHorizon);
        _sky.SetShaderParameter("sun_color", s.Sun);
        _sky.SetShaderParameter("cloud_lit", s.CloudLit);
        _sky.SetShaderParameter("cloud_shade", s.CloudShade);
        _sky.SetShaderParameter("horizon_uv", horizonUv);
        _sky.SetShaderParameter("aspect", size.X / size.Y);
        _sky.SetShaderParameter("metres_per_uv", size.Y / (SimSpace.PixelsPerMetre * zoom));
        _sky.SetShaderParameter("cam_x", camX);
        _sky.SetShaderParameter("cloud_cover", Mathf.Max(s.Cloud, 0.12f));
        _sky.SetShaderParameter("cloud_drift", _drift);
        // The sun crosses from the left (morning) to set over the far shore on the right; the moon follows at night.
        float top = 0.07f;
        s.SunUv = new Vector2(Mathf.Lerp(0.1f, 0.9f, Mathf.Clamp(s.DayFrac, -0.1f, 1.1f)), horizonUv - MathF.Max(s.Elevation, -0.2f) * (horizonUv - top));
        _sky.SetShaderParameter("sun_uv", s.SunUv);
        _sky.SetShaderParameter("sun_visible", Mathf.Clamp((s.Elevation + 0.08f) / 0.08f, 0f, 1f) * MathF.Pow(1f - s.Cloud, 1.6f));
        float moonE = MathF.Sin(MathF.PI * Math.Clamp(s.NightFrac, 0f, 1f));
        s.MoonUv = new Vector2(Mathf.Lerp(0.15f, 0.85f, Math.Clamp(s.NightFrac, 0f, 1f)), horizonUv - moonE * 0.75f * (horizonUv - top));
        _sky.SetShaderParameter("moon_uv", s.MoonUv);
        Atmosphere.Now = s;
        _sky.SetShaderParameter("moon_visible", s.Moon);
        _sky.SetShaderParameter("stars", s.Stars);

        float camPx = camX * SimSpace.PixelsPerMetre;
        for (int i = 0; i < _layers.Count; i++)
        {
            var (node, mat) = _layers[i];
            node.Position = new Vector2(camPx * Parallax[i], 0f);
            mat.SetShaderParameter("light_col", s.Ambient);
            mat.SetShaderParameter("haze_col", s.Haze);
            mat.SetShaderParameter("haze", Mathf.Clamp(Haze[i] + 0.25f * s.Cloud + 0.15f * s.Rain, 0f, 0.92f));
            mat.SetShaderParameter("mist", s.Mist * (1f - 0.25f * i));
        }
    }
}

/// <summary>
/// One band of the far shore, generated once from a seed: rolling hills (band 0), a hammock tree line of oaks, cabbage
/// palms and cypress (band 1, with waterfront houses at the springs), and low shoreline scrub (band 2). Drawn in flat
/// silhouette colours; the far-land shader lights and hazes them.
/// </summary>
public partial class FarShore : Node2D
{
    public int Band { get; set; }
    public string Scenery { get; set; } = "pond";

    private static readonly Color[] Base =
    {
        new(0.46f, 0.56f, 0.6f), new(0.3f, 0.42f, 0.36f), new(0.2f, 0.3f, 0.22f),
    };

    private const float X0 = -70f, X1 = 120f;

    public override void _Draw()
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)(17 + Band * 101 + (Scenery == "springs" ? 7 : 0)) };
        var col = Base[Band];
        switch (Band)
        {
            case 0: Hills(rng, col); break;
            case 1: TreeLine(rng, col); break;
            default: Scrub(rng, col); break;
        }
    }

    private void Ridge(Func<float, float> height, float step, float baseY, Color col)
    {
        var pts = new List<Vector2> { Lp.P(X0, baseY) };
        for (float x = X0; x <= X1; x += step) pts.Add(Lp.P(x, height(x)));
        pts.Add(Lp.P(X1, baseY));
        DrawColoredPolygon(pts.ToArray(), col);
    }

    private void Hills(RandomNumberGenerator rng, Color col)
    {
        float p1 = rng.Randf() * 10f, p2 = rng.Randf() * 10f;
        Ridge(x => 1.6f + 0.35f * MathF.Sin(x * 0.07f + p1) + 0.2f * MathF.Sin(x * 0.19f + p2) + 0.06f * MathF.Sin(x * 1.3f), 0.9f, 0.2f, col);
        // Tiny distant tree bumps along the ridge.
        for (float x = X0; x < X1; x += rng.RandfRange(0.5f, 1.4f))
        {
            float y = 1.6f + 0.35f * MathF.Sin(x * 0.07f + p1) + 0.2f * MathF.Sin(x * 0.19f + p2) + 0.06f * MathF.Sin(x * 1.3f) - 0.05f;
            float r = rng.RandfRange(0.12f, 0.24f);
            Lp.Ngon(this, col, new Vector2(x, y + r * 0.3f), r, 9, rng.Randf());
        }
    }

    private void TreeLine(RandomNumberGenerator rng, Color col)
    {
        bool springs = Scenery == "springs";
        // A canopy edge from overlapping round crowns (sampled coarsely: faceted, low-poly).
        var crowns = new List<(float cx, float r, float h)>();
        for (float x = X0; x < X1; x += rng.RandfRange(0.7f, 1.6f))
            crowns.Add((x, rng.RandfRange(0.6f, 1.2f), rng.RandfRange(0.7f, 1.3f)));
        float Canopy(float x)
        {
            float best = 0.7f;
            foreach (var (cx, r, h) in crowns)
            {
                float dx = x - cx;
                if (MathF.Abs(dx) >= r) continue;
                best = MathF.Max(best, h + 0.55f * MathF.Sqrt(r * r - dx * dx));
            }
            return best;
        }
        Ridge(Canopy, 0.22f, 0.2f, col);
        // Emergent trees above the canopy: cabbage palms, and bald cypress spires at the springs.
        for (float x = X0; x < X1; x += rng.RandfRange(1.5f, 4.5f))
        {
            float baseY = Canopy(x) - 0.4f;
            if (springs && rng.Randf() < 0.45f) Lp.Cypress(this, new Vector2(x, baseY - 0.2f), rng.RandfRange(1.0f, 1.5f), col);
            else Lp.Palm(this, new Vector2(x, baseY), rng.RandfRange(0.8f, 1.2f), rng.RandfRange(-0.15f, 0.15f), col, col, 0f);
        }
        if (springs)
        {
            // Waterfront houses among the trees (pale walls, darker roofs, a dock below).
            for (float x = X0 + 5f; x < X1; x += rng.RandfRange(9f, 16f))
            {
                float w = rng.RandfRange(1.0f, 1.6f), h = rng.RandfRange(0.45f, 0.65f);
                var wall = col.Lerp(new Color(0.85f, 0.84f, 0.8f), 0.55f);
                var roof = col.Lerp(new Color(0.35f, 0.3f, 0.3f), 0.5f);
                Lp.Poly(this, wall, new Vector2(x, 0.35f), new Vector2(x + w, 0.35f), new Vector2(x + w, 0.35f + h), new Vector2(x, 0.35f + h));
                Lp.Poly(this, roof, new Vector2(x - 0.1f, 0.35f + h), new Vector2(x + w + 0.1f, 0.35f + h), new Vector2(x + w * 0.5f, 0.35f + h + 0.35f));
                Lp.Poly(this, roof, new Vector2(x + w * 0.3f, 0.3f), new Vector2(x + w * 1.4f, 0.3f), new Vector2(x + w * 1.4f, 0.36f), new Vector2(x + w * 0.3f, 0.36f));
            }
        }
    }

    private void Scrub(RandomNumberGenerator rng, Color col)
    {
        float p = rng.Randf() * 10f;
        Ridge(x => 0.82f + 0.18f * MathF.Sin(x * 0.5f + p) + 0.12f * MathF.Sin(x * 1.7f + 2f * p) + 0.05f * MathF.Sin(x * 4.1f), 0.3f, 0.2f, col);
        for (float x = X0; x < X1; x += rng.RandfRange(4f, 10f))
            Lp.Palm(this, new Vector2(x, 0.75f), rng.RandfRange(1.1f, 1.6f), rng.RandfRange(-0.2f, 0.2f), col, col, 0f);
    }
}
