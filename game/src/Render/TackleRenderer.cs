using Godot;
using TheLedger.Boot;
using TheLedger.Sim.Angler;
using TheLedger.Sim.Tackle;
using TheLedger.SimGlue;

namespace TheLedger.Render;

/// <summary>
/// Draws each angler's simulated rod and line (docs/07_ART_PLAN.md: flat low-poly, no outlines). The rod is a
/// tapered triangle strip along the sim nodes with cork handle, reel and guides; the line is a polyline from the tip
/// through every line node to the terminal. Positions are interpolated between the last two ticks the renderer saw,
/// so motion stays smooth at any frame rate. With <see cref="ShowLoads"/> the blank is coloured by moment/limit and
/// the line by tension/breaking force (green → yellow → red).
/// </summary>
public partial class TackleRenderer : Node2D
{
    public SimDriver Driver { get; set; } = null!;
    public Func<bool> ShowLoads { get; set; } = () => false;

    private static readonly Color Blank = new(0.16f, 0.20f, 0.26f);
    private static readonly Color BlankShade = new(0.10f, 0.13f, 0.17f);
    private static readonly Color Cork = new(0.78f, 0.64f, 0.44f);
    private static readonly Color CorkShade = new(0.64f, 0.50f, 0.33f);
    private static readonly Color ReelBody = new(0.30f, 0.32f, 0.35f);
    private static readonly Color ReelSpool = new(0.62f, 0.64f, 0.66f);
    private static readonly Color Guide = new(0.75f, 0.78f, 0.80f);
    private static readonly Color Lead = new(0.36f, 0.37f, 0.40f);

    /// <summary>Per-angler snapshots of the previous and current tick, so drawing can interpolate.</summary>
    private sealed class Snap
    {
        public long Tick = -1;
        public float[] RodX = Array.Empty<float>(), RodY = Array.Empty<float>();
        public float[] RodPX = Array.Empty<float>(), RodPY = Array.Empty<float>();
        public float[] LineX = Array.Empty<float>(), LineY = Array.Empty<float>();
        public float[] LinePX = Array.Empty<float>(), LinePY = Array.Empty<float>();
        public int LineCount, LinePrevCount;
    }

    private readonly List<Snap> _snaps = new();
    private Vector2[] _rodPts = Array.Empty<Vector2>();
    private Vector2[] _linePts = Array.Empty<Vector2>();
    private Color[] _lineCols = Array.Empty<Color>();
    private Vector2[] _strip = Array.Empty<Vector2>();
    private Color[] _stripCols = Array.Empty<Color>();
    private int[] _stripIdx = Array.Empty<int>();

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        var anglers = Driver.World.Anglers;
        while (_snaps.Count < anglers.Count) _snaps.Add(new Snap());
        float alpha = Driver.Alpha;
        bool loads = ShowLoads();
        for (int i = 0; i < anglers.Count; i++)
        {
            var a = anglers[i];
            var snap = _snaps[i];
            Capture(snap, a.Rig, Driver.World.Tick);
            DrawRig(a, a.Rig, snap, alpha, loads);
        }
    }

    private static void Ensure(ref float[] arr, int n)
    {
        if (arr.Length < n) Array.Resize(ref arr, n);
    }

    /// <summary>On a new tick, current → previous and copy the sim state in (no allocation once sized).</summary>
    private static void Capture(Snap s, Rig rig, long tick)
    {
        if (s.Tick == tick) return;
        var rod = rig.Rod;
        var line = rig.Line;
        bool first = s.Tick < 0;
        Ensure(ref s.RodX, rod.Count); Ensure(ref s.RodY, rod.Count);
        Ensure(ref s.RodPX, rod.Count); Ensure(ref s.RodPY, rod.Count);
        Ensure(ref s.LineX, line.Capacity); Ensure(ref s.LineY, line.Capacity);
        Ensure(ref s.LinePX, line.Capacity); Ensure(ref s.LinePY, line.Capacity);
        (s.RodPX, s.RodX) = (s.RodX, s.RodPX);
        (s.RodPY, s.RodY) = (s.RodY, s.RodPY);
        (s.LinePX, s.LineX) = (s.LineX, s.LinePX);
        (s.LinePY, s.LineY) = (s.LineY, s.LinePY);
        s.LinePrevCount = s.LineCount;
        Array.Copy(rod.X, s.RodX, rod.Count);
        Array.Copy(rod.Y, s.RodY, rod.Count);
        Array.Copy(line.X, s.LineX, line.Count);
        Array.Copy(line.Y, s.LineY, line.Count);
        s.LineCount = line.Count;
        if (first)
        {
            Array.Copy(rod.X, s.RodPX, rod.Count);
            Array.Copy(rod.Y, s.RodPY, rod.Count);
            Array.Copy(line.X, s.LinePX, line.Count);
            Array.Copy(line.Y, s.LinePY, line.Count);
            s.LinePrevCount = line.Count;
        }
        s.Tick = tick;
    }

    private static Color LoadColor(float ratio)
    {
        ratio = Math.Clamp(ratio, 0f, 1.2f);
        if (ratio < 0.5f) return new Color(0.2f, 0.85f, 0.3f).Lerp(new Color(1f, 0.9f, 0.2f), ratio * 2f);
        return new Color(1f, 0.9f, 0.2f).Lerp(new Color(1f, 0.15f, 0.1f), Math.Min((ratio - 0.5f) * 2f, 1f));
    }

    private void DrawRig(AnglerState a, Rig rig, Snap s, float alpha, bool loads)
    {
        var rod = rig.Rod;
        int n = rod.Count;
        if (_rodPts.Length < n) Array.Resize(ref _rodPts, n);
        for (int k = 0; k < n; k++)
            _rodPts[k] = SimSpace.ToPx(Mathf.Lerp(s.RodPX[k], s.RodX[k], alpha), Mathf.Lerp(s.RodPY[k], s.RodY[k], alpha));

        // Line first so the rod and reel sit over it. Tip → nodes → terminal.
        var line = rig.Line;
        int m = s.LineCount;
        bool interp = s.LinePrevCount == m;
        if (_linePts.Length < m + 1) { Array.Resize(ref _linePts, m + 1); Array.Resize(ref _lineCols, m + 1); }
        _linePts[0] = _rodPts[n - 1];
        for (int k = 0; k < m; k++)
        {
            float x = interp ? Mathf.Lerp(s.LinePX[k], s.LineX[k], alpha) : s.LineX[k];
            float y = interp ? Mathf.Lerp(s.LinePY[k], s.LineY[k], alpha) : s.LineY[k];
            _linePts[k + 1] = SimSpace.ToPx(x, y);
        }
        var lineColor = Color.FromHtml(line.Spec.Color);
        float breakN = line.Spec.BreakingForceN;
        for (int k = 0; k <= m; k++)
        {
            float tension = k < m ? line.Tension[Math.Min(k, m - 1)] : 0f;
            _lineCols[k] = loads ? LoadColor(tension / breakN) : lineColor;
        }
        // Constant on-screen width whatever the camera zoom (a sub-pixel line breaks up into dashes).
        float px = 1f / MathF.Max(GetViewport().GetCanvasTransform().X.Length(), 0.05f);
        if (m >= 1) DrawPolylineColors(_linePts.AsSpan(0, m + 1).ToArray(), _lineCols.AsSpan(0, m + 1).ToArray(), (loads ? 2.5f : 1.6f) * px, true);

        // Terminal: a lead sinker (teardrop as a low-poly diamond) until lures exist.
        Vector2 term = _linePts[m];
        float r = Math.Max(rig.Line.Terminal.DiameterM * 0.5f * SimSpace.PixelsPerMetre, 3f);
        DrawColoredPolygon(new[] { term + new Vector2(0, -r * 0.6f), term + new Vector2(r, r * 0.6f), term + new Vector2(0, r * 1.8f), term + new Vector2(-r, r * 0.6f) }, Lead);

        BuildBlankStrip(rod, n, loads);
        RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), _stripIdx, _strip, _stripCols);

        // Guides: small rings standing off the blank on the line side.
        for (int k = 2; k < n - 1; k++)
        {
            if (!rod.IsGuide[k]) continue;
            Vector2 dir = (_rodPts[k + 1] - _rodPts[k - 1]).Normalized();
            Vector2 nrm = new(dir.Y, -dir.X);
            float d = rod.Diameter[k] * SimSpace.PixelsPerMetre;
            float stand = 3f + d * 1.2f;
            DrawLine(_rodPts[k], _rodPts[k] + nrm * stand, Guide, 1.2f);
            DrawArc(_rodPts[k] + nrm * (stand + 1.5f), 1.6f + d * 0.3f, 0f, Mathf.Tau, 6, Guide, 1f);
        }

        // Cork handle (butt → grip) and the reel hanging under the reel seat just behind the grip.
        Vector2 butt = _rodPts[0], grip = _rodPts[1];
        Vector2 hdir = (grip - butt).Normalized();
        Vector2 hn = new(hdir.Y, -hdir.X);
        float hw = 0.03f * SimSpace.PixelsPerMetre;
        DrawColoredPolygon(new[] { butt + hn * hw, grip + hn * hw * 0.85f, grip - hn * hw * 0.85f, butt - hn * hw }, Cork);
        DrawColoredPolygon(new[] { butt - hn * hw * 0.2f, grip - hn * hw * 0.2f, grip - hn * hw * 0.85f, butt - hn * hw }, CorkShade);
        Vector2 seat = grip.Lerp(butt, 0.18f);
        Vector2 foot = seat - hn * 0.045f * SimSpace.PixelsPerMetre;
        DrawLine(seat, foot, ReelBody, 3f);
        float rr = 0.028f * SimSpace.PixelsPerMetre;
        Vector2 c = foot - hn * rr * 0.8f;
        DrawColoredPolygon(Hex(c, rr), ReelBody);
        DrawColoredPolygon(Hex(c + hdir * rr * 0.6f, rr * 0.7f), ReelSpool);
    }

    /// <summary>Two-tone tapered strip: the upper half lighter, the lower half shaded (flat low-poly look).</summary>
    private void BuildBlankStrip(Rod rod, int n, bool loads)
    {
        int segs = n - 2; // grip (1) → tip (n-1)
        int verts = (segs + 1) * 3;
        if (_strip.Length != verts)
        {
            _strip = new Vector2[verts];
            _stripCols = new Color[verts];
            _stripIdx = new int[segs * 12];
            for (int j = 0; j < segs; j++)
            {
                int a = j * 3, b = (j + 1) * 3, o = j * 12;
                // Top half: a0 a1 b0, a1 b1 b0. Bottom half: a1 a2 b1, a2 b2 b1.
                _stripIdx[o + 0] = a; _stripIdx[o + 1] = a + 1; _stripIdx[o + 2] = b;
                _stripIdx[o + 3] = a + 1; _stripIdx[o + 4] = b + 1; _stripIdx[o + 5] = b;
                _stripIdx[o + 6] = a + 1; _stripIdx[o + 7] = a + 2; _stripIdx[o + 8] = b + 1;
                _stripIdx[o + 9] = a + 2; _stripIdx[o + 10] = b + 2; _stripIdx[o + 11] = b + 1;
            }
        }
        for (int j = 0; j <= segs; j++)
        {
            int k = j + 1;
            Vector2 prev = _rodPts[Math.Max(k - 1, 1)], next = _rodPts[Math.Min(k + 1, n - 1)];
            Vector2 dir = (next - prev).Normalized();
            Vector2 nrm = new(dir.Y, -dir.X);
            // Drawn ~4x real width so a 2 mm tip still reads at gameplay zoom.
            float half = Math.Max(rod.Diameter[k] * 0.5f * SimSpace.PixelsPerMetre * 4f, 1.1f);
            int v = j * 3;
            _strip[v] = _rodPts[k] + nrm * half;
            _strip[v + 1] = _rodPts[k];
            _strip[v + 2] = _rodPts[k] - nrm * half;
            Color top = Blank, bottom = BlankShade;
            if (loads)
            {
                float lim = rod.MomentLimit[Math.Min(k, n - 2)];
                top = LoadColor(lim > 0f ? MathF.Abs(rod.Moment[Math.Min(k, n - 2)]) / lim : 0f);
                bottom = top.Darkened(0.25f);
            }
            _stripCols[v] = top; _stripCols[v + 1] = top; _stripCols[v + 2] = bottom;
        }
    }

    private static Vector2[] Hex(Vector2 c, float r)
    {
        var p = new Vector2[6];
        for (int i = 0; i < 6; i++) p[i] = c + Vector2.FromAngle(i * Mathf.Tau / 6f + 0.3f) * r;
        return p;
    }
}
