using Godot;
using TheLedger.Boot;
using TheLedger.Sim.Water;
using TheLedger.SimGlue;

namespace TheLedger.Render;

/// <summary>
/// Draws the water from the sim (docs/07_ART_DIRECTION.md §6), M1 version:
/// the water volume with a depth gradient, the surface line (meniscus highlight, foam where the sim says so),
/// the near surface band with ripple rings spawned exactly where the sim logged impacts, and splash droplets.
/// The M8 art pass adds reflections, god rays and caustics on top of this.
/// </summary>
public partial class WaterRenderer : Node2D
{
    public SimDriver Driver { get; set; } = null!;

    /// <summary>World x where the water starts (the bank's waterline); nothing is drawn left of it.</summary>
    public float WaterStartX { get; set; } = -0.24f;
    public float WaterEndX { get; set; } = 60f;
    /// <summary>How deep the gradient runs before it's fully the deep colour, m.</summary>
    public float GradientDepth { get; set; } = 5f;
    /// <summary>Height of the near surface band above the waterline, m.</summary>
    public float BandHeight { get; set; } = 0.42f;

    public Color Shallow { get; set; } = new(0.26f, 0.60f, 0.64f, 0.96f);
    public Color Deep { get; set; } = new(0.07f, 0.27f, 0.36f, 1f);
    public Color BandNear { get; set; } = new(0.36f, 0.58f, 0.66f);
    public Color BandFar { get; set; } = new(0.62f, 0.77f, 0.84f);
    public Color Highlight { get; set; } = new(0.92f, 0.98f, 1f, 0.9f);
    public Color FoamColor { get; set; } = new(1f, 1f, 1f, 1f);

    private const int MaxSamples = 2600;
    private readonly Vector2[] _surface = new Vector2[MaxSamples];
    private readonly Color[] _surfaceColors = new Color[MaxSamples];
    private readonly Vector2[] _stripPoints = new Vector2[MaxSamples * 2];
    private int[] _stripIndices = Array.Empty<int>();
    private readonly Vector2[] _bandPoints = new Vector2[MaxSamples * 2];
    private readonly Color[] _bandColors = new Color[MaxSamples * 2];

    private struct Ring
    {
        public float X;
        public double Born;
        public float Strength;
        /// <summary>How far across the surface plane the ring sits, 0 = at the cross-section, 1 = far edge of the band.</summary>
        public float Z;
    }

    private readonly Ring[] _rings = new Ring[256];
    private int _ringCount;
    private long _lastImpact;

    private DrawLayer _band = null!, _volume = null!;
    private ShaderMaterial _bandMat = null!, _volumeMat = null!;
    private int _n;
    private readonly Vector2[] _bandUvs = new Vector2[MaxSamples * 2];
    private readonly Color[] _white = new Color[MaxSamples * 2];

    /// <summary>The bank, plants and cover below the surface use this (fog toward the water colour, caustics).</summary>
    public ShaderMaterial Underwater { get; private set; } = null!;

    /// <summary>How deep the volume is drawn, m (deeper than any spot; the bank is drawn in front).</summary>
    private const float VolumeBottom = -16f;

    public override void _EnterTree()
    {
        // Built before _Ready so Main can hand it to the bank and cover as they're created.
        Underwater = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/underwater.gdshader") };
    }

    public override void _Ready()
    {
        Array.Fill(_white, Colors.White);
        _bandMat = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/water_band.gdshader") };
        _volumeMat = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/water_volume.gdshader") };
        _band = new DrawLayer { Name = "Band", ShowBehindParent = true, Material = _bandMat, Paint = PaintBand };
        _volume = new DrawLayer { Name = "Volume", ShowBehindParent = true, Material = _volumeMat, Paint = PaintVolume };
        AddChild(_band);
        AddChild(_volume);

        var spot = Driver.World.Spot;
        var profile = new Vector2[16];
        int count = Math.Min(spot.PointCount, 16);
        for (int i = 0; i < count; i++) profile[i] = new Vector2(spot.Point(i).X, spot.Point(i).Y);
        float vis = Driver.World.Stock.VisibilityM;
        Underwater.SetShaderParameter("profile", profile);
        Underwater.SetShaderParameter("profile_count", count);
        Underwater.SetShaderParameter("visibility", vis);
        Underwater.SetShaderParameter("water_col", Shallow.Lerp(Deep, 0.45f) with { A = 1f });
        _volumeMat.SetShaderParameter("visibility", vis);
        _volumeMat.SetShaderParameter("shallow", Shallow);
        _volumeMat.SetShaderParameter("deep", Deep);
        _volumeMat.SetShaderParameter("grad_depth", GradientDepth);
    }

    /// <summary>Undoes the scene's light tint (CanvasModulate) for colours that should read as the sky's own.</summary>
    private static Color Unlit(Color c, Color ambient) =>
        new(MathF.Min(c.R / MathF.Max(ambient.R, 0.12f), 1f), MathF.Min(c.G / MathF.Max(ambient.G, 0.12f), 1f), MathF.Min(c.B / MathF.Max(ambient.B, 0.12f), 1f), c.A);

    public override void _Process(double delta)
    {
        CollectRings();
        Sample();
        UpdateLight();
        _band.QueueRedraw();
        _volume.QueueRedraw();
        QueueRedraw();
    }

    private void UpdateLight()
    {
        var s = Atmosphere.Now;
        var amb = s.Ambient;
        bool moon = s.Elevation < -0.02f;
        // Where the light sits on the water: under the sun, or the moon at night.
        Vector2 uv = moon ? s.MoonUv : s.SunUv;
        Rect2 view = GetViewportRect();
        Transform2D inv = GetCanvasTransform().AffineInverse();
        float lightX = (inv * new Vector2(view.Size.X * uv.X, 0f)).X / SimSpace.PixelsPerMetre;
        float rough = Mathf.Clamp(Driver.Config.DebugWindSpeed / 10f, 0f, 1f);

        _bandMat.SetShaderParameter("near_col", Unlit((Shallow with { A = 1f }).Lerp(BandNear, 0.35f).Lerp(s.SkyTop, 0.35f), amb));
        _bandMat.SetShaderParameter("far_col", Unlit(s.SkyHorizon.Lerp(s.Haze, 0.3f), amb));
        _bandMat.SetShaderParameter("shore_col", Unlit(new Color(0.2f, 0.3f, 0.22f) * amb, amb).Lerp(Unlit(s.Haze, amb), 0.35f));
        _bandMat.SetShaderParameter("glint_col", moon ? new Color(0.75f, 0.82f, 1f) : Unlit(s.Sun, amb));
        _bandMat.SetShaderParameter("glint", moon ? s.Moon * 0.45f : Mathf.Clamp(s.Elevation * 4f + 0.3f, 0f, 1f) * (1f - 0.5f * Mathf.SmoothStep(0.3f, 0.9f, s.Elevation)) * MathF.Pow(1f - s.Cloud, 1.5f));
        _bandMat.SetShaderParameter("sun_x_m", lightX);
        _bandMat.SetShaderParameter("rough", rough);

        float slope = Mathf.Clamp(-(s.DayFrac - 0.5f) * 1.3f, -0.65f, 0.65f);
        _volumeMat.SetShaderParameter("direct", s.Direct);
        _volumeMat.SetShaderParameter("sun_tint", Unlit(s.Sun, amb));
        _volumeMat.SetShaderParameter("sun_slope", slope);
        Underwater.SetShaderParameter("direct", s.Direct);
        Underwater.SetShaderParameter("sun_tint", Unlit(s.Sun, amb));
    }

    private void CollectRings()
    {
        var log = Driver.World.Water.Impacts;
        long latest = log.Latest;
        long from = Math.Max(_lastImpact + 1, latest - log.Capacity + 1);
        for (long s = from; s <= latest; s++)
        {
            if (!log.TryGet(s, out var imp)) continue;
            if (_ringCount == _rings.Length) RemoveRing(0);
            float strength = MathF.Abs(imp.Strength);
            // Lures, fish and stones hit at the cross-section (z = 0). Small impacts (rain, droplets) land anywhere on the
            // surface plane, so spread them across the band using a hash of the sequence (deterministic, no RNG state).
            float z = strength < 0.3f ? Hash01(s) : 0f;
            _rings[_ringCount++] = new Ring { X = imp.X, Born = imp.Time, Strength = strength, Z = z };
        }
        _lastImpact = latest;
    }

    private static float Hash01(long s)
    {
        ulong z = (ulong)s * 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 29)) * 0xBF58476D1CE4E5B9UL;
        return (z >> 40) / (float)(1UL << 24);
    }

    private void RemoveRing(int i)
    {
        _rings[i] = _rings[--_ringCount];
    }

    private float _x0, _x1;
    private double _t;

    /// <summary>Samples the surface across the visible range (before any layer draws this frame).</summary>
    private void Sample()
    {
        var world = Driver.World;
        var water = world.Water;
        // Render time between the last two ticks.
        double t = _t = world.Time - (1.0 - Driver.Alpha) * Driver.Config.Dt;

        // Visible x range from the camera.
        Rect2 view = GetViewportRect();
        Transform2D inv = GetCanvasTransform().AffineInverse();
        float left = (inv * view.Position).X / SimSpace.PixelsPerMetre;
        float right = (inv * view.End).X / SimSpace.PixelsPerMetre;
        // Start a few metres under the bank (drawn in front) so no sky shows between the bank slope and the far-water band.
        float x0 = _x0 = MathF.Max(WaterStartX - 4f, left - 0.5f);
        float x1 = _x1 = MathF.Min(WaterEndX, right + 0.5f);
        _n = 0;
        if (x1 <= x0) return;

        float spacing = Driver.Config.PatchSpacing;
        int n = Math.Clamp((int)((x1 - x0) / spacing) + 1, 2, MaxSamples);
        float step = (x1 - x0) / (n - 1);

        for (int i = 0; i < n; i++)
        {
            float x = x0 + i * step;
            float h = water.Height(x, t);
            float foam = water.FoamAt(x);
            float slope = water.Slope(x, t);
            _surface[i] = SimSpace.ToPx(x, h);
            // Brighter where the surface tilts toward the light.
            float glint = Mathf.Clamp(0.55f + 2.5f * slope, 0.25f, 1f);
            _surfaceColors[i] = Highlight.Lerp(FoamColor, foam) * new Color(1, 1, 1, glint);

            _stripPoints[2 * i] = _surface[i];
            _stripPoints[2 * i + 1] = SimSpace.ToPx(x, VolumeBottom);

            // Near band: from the waterline up to the band's back edge (the shader colours it; foam whitens it).
            _bandPoints[2 * i] = _surface[i];
            _bandPoints[2 * i + 1] = SimSpace.ToPx(x, h + BandHeight);
            _bandColors[2 * i] = Colors.White.Lerp(new Color(1.6f, 1.6f, 1.6f), foam * 0.6f);
            _bandColors[2 * i + 1] = Colors.White;
            _bandUvs[2 * i] = new Vector2(0f, 0f);
            _bandUvs[2 * i + 1] = new Vector2(0f, 1f);
        }
        EnsureIndices(n);
        _n = n;
    }

    // Exact-length copies for Godot's array APIs, reused while the sample count stays the same (no per-frame garbage).
    private int[] _xIdx = Array.Empty<int>();
    private Vector2[] _xBandPts = Array.Empty<Vector2>(), _xBandUvs = Array.Empty<Vector2>(), _xStrip = Array.Empty<Vector2>(), _xSurf = Array.Empty<Vector2>();
    private Color[] _xBandCols = Array.Empty<Color>(), _xWhite = Array.Empty<Color>(), _xSurfCols = Array.Empty<Color>();

    private static T[] Exact<T>(ref T[] cache, T[] src, int count)
    {
        if (cache.Length != count) cache = new T[count];
        Array.Copy(src, cache, count);
        return cache;
    }

    private void PaintBand(DrawLayer layer)
    {
        if (_n < 2) return;
        RenderingServer.CanvasItemAddTriangleArray(layer.GetCanvasItem(), Exact(ref _xIdx, _stripIndices, (_n - 1) * 6),
            Exact(ref _xBandPts, _bandPoints, 2 * _n), Exact(ref _xBandCols, _bandColors, 2 * _n), Exact(ref _xBandUvs, _bandUvs, 2 * _n));
    }

    private void PaintVolume(DrawLayer layer)
    {
        if (_n < 2) return;
        RenderingServer.CanvasItemAddTriangleArray(layer.GetCanvasItem(), Exact(ref _xIdx, _stripIndices, (_n - 1) * 6),
            Exact(ref _xStrip, _stripPoints, 2 * _n), Exact(ref _xWhite, _white, 2 * _n));
    }

    public override void _Draw()
    {
        if (_n < 2) return;
        DrawRings(_t, _x0, _x1, Driver.World.Water);
        DrawPolylineColors(Exact(ref _xSurf, _surface, _n), Exact(ref _xSurfCols, _surfaceColors, _n), 2.5f, true);
        DrawDroplets(Driver.World.Water);
    }

    private void EnsureIndices(int n)
    {
        int needed = (n - 1) * 6;
        if (_stripIndices.Length >= needed) return;
        _stripIndices = new int[(MaxSamples - 1) * 6];
        for (int i = 0; i < MaxSamples - 1; i++)
        {
            int a = 2 * i, b = 2 * i + 1, c = 2 * i + 2, d = 2 * i + 3;
            int k = i * 6;
            _stripIndices[k] = a; _stripIndices[k + 1] = b; _stripIndices[k + 2] = c;
            _stripIndices[k + 3] = c; _stripIndices[k + 4] = b; _stripIndices[k + 5] = d;
        }
    }

    /// <summary>
    /// Ripple rings on the near band: ellipses growing at the sim's ripple speed from each logged impact, flattened
    /// because we see the surface at a low angle. Older and weaker rings fade out.
    /// </summary>
    private void DrawRings(double t, float x0, float x1, WaterSurface water)
    {
        float speed = Driver.Config.RippleSpeed;
        int i = 0;
        while (i < _ringCount)
        {
            ref var r = ref _rings[i];
            float age = (float)(t - r.Born);
            float life = Mathf.Clamp(0.8f + 2.5f * r.Strength, 0.6f, 3.5f);
            if (age > life)
            {
                RemoveRing(i);
                continue;
            }
            if (age >= 0f && r.X >= x0 && r.X <= x1)
            {
                // Farther rings (higher on the band) look smaller: a cheap perspective.
                float scale = 1f - 0.55f * r.Z;
                float radius = (0.02f + speed * age) * scale;
                float fade = 1f - age / life;
                float alpha = Mathf.Clamp(fade * (0.25f + r.Strength), 0f, 0.8f) * (1f - 0.35f * r.Z);
                // The ring sits on the surface plane seen at a low angle: just above the waterline for z = 0,
                // up toward the top of the band for distant ones.
                float y = water.Height(r.X, t) + 0.05f + r.Z * (BandHeight - 0.1f) + radius * 0.10f;
                Vector2 c = SimSpace.ToPx(r.X, y);
                float rx = radius * SimSpace.PixelsPerMetre;
                float ry = rx * 0.16f;
                DrawEllipse(c, rx, ry, new Color(0.95f, 1f, 1f, alpha), 1.5f);
                if (r.Strength > 0.4f && radius > 0.12f)
                    DrawEllipse(c, rx * 0.62f, ry * 0.62f, new Color(0.95f, 1f, 1f, alpha * 0.6f), 1.2f);
            }
            i++;
        }
    }

    private readonly Vector2[] _ellipse = new Vector2[33];

    private void DrawEllipse(Vector2 centre, float rx, float ry, Color color, float width)
    {
        int segs = rx < 10f ? 12 : rx < 40f ? 20 : 32;
        for (int k = 0; k <= segs; k++)
        {
            float a = k * MathF.Tau / segs;
            _ellipse[k] = centre + new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry);
        }
        DrawPolyline(_ellipse.AsSpan(0, segs + 1).ToArray(), color, width, true);
    }

    private void DrawDroplets(WaterSurface water)
    {
        var d = water.Droplets;
        var xs = d.X;
        var ys = d.Y;
        var sz = d.Size;
        for (int i = 0; i < d.Count; i++)
        {
            float r = 1.5f + 2.5f * sz[i];
            DrawCircle(SimSpace.ToPx(xs[i], ys[i]), r, new Color(0.9f, 0.97f, 1f, 0.85f));
        }
    }
}

/// <summary>A child canvas item that draws through a callback, so one renderer can paint layers with their own materials.</summary>
public partial class DrawLayer : Node2D
{
    public Action<DrawLayer>? Paint { get; set; }

    public override void _Draw() => Paint?.Invoke(this);
}
