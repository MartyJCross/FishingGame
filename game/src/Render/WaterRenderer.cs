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

    public Color Shallow { get; set; } = new(0.20f, 0.45f, 0.47f, 0.96f);
    public Color Deep { get; set; } = new(0.03f, 0.10f, 0.13f, 1f);
    public Color BandNear { get; set; } = new(0.36f, 0.58f, 0.66f);
    public Color BandFar { get; set; } = new(0.62f, 0.77f, 0.84f);
    public Color Highlight { get; set; } = new(0.92f, 0.98f, 1f, 0.9f);
    public Color FoamColor { get; set; } = new(1f, 1f, 1f, 1f);

    private const int MaxSamples = 1400;
    private readonly Vector2[] _surface = new Vector2[MaxSamples];
    private readonly Color[] _surfaceColors = new Color[MaxSamples];
    private readonly Vector2[] _stripPoints = new Vector2[MaxSamples * 2];
    private readonly Color[] _stripColors = new Color[MaxSamples * 2];
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

    public override void _Process(double delta)
    {
        CollectRings();
        QueueRedraw();
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

    public override void _Draw()
    {
        var world = Driver.World;
        var water = world.Water;
        // Render time between the last two ticks.
        double t = world.Time - (1.0 - Driver.Alpha) * Driver.Config.Dt;

        // Visible x range from the camera.
        Rect2 view = GetViewportRect();
        Transform2D inv = GetCanvasTransform().AffineInverse();
        float left = (inv * view.Position).X / SimSpace.PixelsPerMetre;
        float right = (inv * view.End).X / SimSpace.PixelsPerMetre;
        float x0 = MathF.Max(WaterStartX, left - 0.5f);
        float x1 = MathF.Min(WaterEndX, right + 0.5f);
        if (x1 <= x0) return;

        float spacing = Driver.Config.PatchSpacing;
        int n = Math.Clamp((int)((x1 - x0) / spacing) + 1, 2, MaxSamples);
        float step = (x1 - x0) / (n - 1);
        float bottomY = -GradientDepth;

        for (int i = 0; i < n; i++)
        {
            float x = x0 + i * step;
            float h = water.Height(x, t);
            float foam = water.FoamAt(x);
            float slope = water.Slope(x, t);
            _surface[i] = SimSpace.ToPx(x, h);
            // Brighter where the surface tilts toward the (placeholder) sun on the upper left.
            float glint = Mathf.Clamp(0.55f + 2.5f * slope, 0.25f, 1f);
            _surfaceColors[i] = Highlight.Lerp(FoamColor, foam) * new Color(1, 1, 1, glint);

            _stripPoints[2 * i] = _surface[i];
            _stripPoints[2 * i + 1] = SimSpace.ToPx(x, bottomY);
            _stripColors[2 * i] = Shallow;
            _stripColors[2 * i + 1] = Deep;

            // Near band: from the waterline up, fading toward the "far" colour (sky reflection).
            _bandPoints[2 * i] = _surface[i];
            _bandPoints[2 * i + 1] = SimSpace.ToPx(x, h + BandHeight);
            _bandColors[2 * i] = BandNear.Lerp(FoamColor, foam * 0.6f);
            _bandColors[2 * i + 1] = BandFar;
        }

        EnsureIndices(n);
        int indexCount = (n - 1) * 6;
        var indices = _stripIndices.AsSpan(0, indexCount).ToArray();
        var rid = GetCanvasItem();
        RenderingServer.CanvasItemAddTriangleArray(rid, indices, _bandPoints.AsSpan(0, 2 * n).ToArray(), _bandColors.AsSpan(0, 2 * n).ToArray());
        DrawRings(t, x0, x1, water);
        RenderingServer.CanvasItemAddTriangleArray(rid, indices, _stripPoints.AsSpan(0, 2 * n).ToArray(), _stripColors.AsSpan(0, 2 * n).ToArray());
        DrawPolylineColors(_surface.AsSpan(0, n).ToArray(), _surfaceColors.AsSpan(0, n).ToArray(), 2.5f, true);

        DrawDroplets(water);
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
