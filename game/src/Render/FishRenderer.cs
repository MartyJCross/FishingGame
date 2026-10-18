using Godot;
using TheLedger.Boot;
using TheLedger.Sim.Fish;
using TheLedger.Sim.Core;
using TheLedger.SimGlue;

namespace TheLedger.Render;

/// <summary>
/// Draws every fish from its simulated spine (docs/07_ART_DIRECTION.md): a flat low-poly body built from the
/// species' shape (dorsal and ventral profile around the spine), dorsal-flank-belly colours, markings, fins, eye
/// and a tail fin that beats. The facing yaw foreshortens the body when the fish turns toward the camera; the
/// lateral tail wave shades the flank. Deeper and murkier fish fade toward the water colour but stay visible.
/// All bodies go into one triangle array per frame (one draw call).
/// </summary>
public partial class FishRenderer : Node2D
{
    public SimDriver Driver { get; set; } = null!;

    private static readonly Color WaterTint = new(0.17f, 0.36f, 0.38f);
    private const int Stations = 9;

    private readonly List<Vector2> _pts = new();
    private readonly List<Color> _cols = new();
    private readonly List<int> _idx = new();
    private readonly Vector2[] _spine = new Vector2[24];
    private readonly Vector2[] _top = new Vector2[Stations], _mid = new Vector2[Stations], _bot = new Vector2[Stations];
    private readonly Vector2[] _tan = new Vector2[Stations];
    private readonly Dictionary<SpeciesSpec, (Color d, Color f, Color b, Color eye, bool bars, bool stripe, bool ear, bool spots)> _palette = new();

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        var world = Driver.World;
        if (world.Fish.Count == 0) return;
        _pts.Clear(); _cols.Clear(); _idx.Clear();
        float alpha = Driver.Alpha;
        float light = 0.35f + 0.65f * Daylight(world.Hour);
        foreach (var fish in world.Fish) AddFish(fish, alpha, light, world.Stock.VisibilityM);
        if (_idx.Count > 0)
            RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), _idx.ToArray(), _pts.ToArray(), _cols.ToArray());
        // Eyes on top (small circles).
        foreach (var fish in world.Fish) DrawEye(fish, alpha, light, world.Stock.VisibilityM);
    }

    private static float Daylight(float h) =>
        h < 5.5f || h > 20.5f ? 0.04f : h < 7.5f ? 0.04f + 0.96f * (h - 5.5f) / 2f : h > 18.5f ? 0.04f + 0.96f * (20.5f - h) / 2f : 1f;

    private (Color d, Color f, Color b, Color eye, bool bars, bool stripe, bool ear, bool spots) Palette(SpeciesSpec sp)
    {
        if (_palette.TryGetValue(sp, out var p)) return p;
        var pat = sp.Body.Pattern;
        bool Has(string t) => pat.Markings.Any(m => m.Type.Contains(t));
        p = (Color.FromHtml(pat.Dorsal), Color.FromHtml(pat.Flank), Color.FromHtml(pat.Belly), Color.FromHtml(pat.Eye),
             Has("bar"), Has("stripe") || Has("band"), Has("opercular"), Has("spot"));
        _palette[sp] = p;
        return p;
    }

    /// <summary>Interpolated spine, foreshortened about its middle by the facing yaw.</summary>
    private int BuildSpine(Fish f, float alpha)
    {
        int n = Math.Min(f.Count, _spine.Length);
        // The body mirrors on the tick the fish's facing flips: don't blend across that (it smears the fish).
        bool flipped = MathF.Cos(f.PrevYaw) * MathF.Cos(f.Yaw) < 0f;
        float a = flipped ? 1f : alpha;
        for (int i = 0; i < n; i++)
            _spine[i] = SimSpace.ToPx(Mathf.Lerp(f.PX[i], f.X[i], a), Mathf.Lerp(f.PY[i], f.Y[i], a));
        float yaw = flipped ? f.Yaw : Mathf.Lerp(f.PrevYaw, f.Yaw, alpha);
        // Facing the camera mid-turn: a short, full-bodied silhouette, never a sliver.
        float squash = 0.38f + 0.62f * MathF.Abs(MathF.Cos(yaw));
        Vector2 c = _spine[n / 2];
        for (int i = 0; i < n; i++) _spine[i] = c + (_spine[i] - c) * squash;
        return n;
    }

    /// <summary>Point and tangent (head → tail) at fraction s of the spine's length.</summary>
    private (Vector2 p, Vector2 t) Along(int n, float s)
    {
        float u = Math.Clamp(s, 0f, 1f) * (n - 1);
        int i = Math.Min((int)u, n - 2);
        float k = u - i;
        Vector2 t = _spine[i + 1] - _spine[i];
        float len = t.Length();
        return (_spine[i].Lerp(_spine[i + 1], k), len > 1e-4f ? t / len : Vector2.Left);
    }

    private static Color Mul(Color c, float k) => new(c.R * k, c.G * k, c.B * k, c.A);

    private Color Fade(Color c, float fade, float light) => c.Lerp(WaterTint, fade) * new Color(light, light, light, 1f);

    private int V(Vector2 p, Color c)
    {
        _pts.Add(p);
        _cols.Add(c);
        return _pts.Count - 1;
    }

    private void Tri(int a, int b, int c) { _idx.Add(a); _idx.Add(b); _idx.Add(c); }

    private void AddFish(Fish f, float alpha, float light, float visibility)
    {
        var sp = f.Species;
        var shape = sp.Shape;
        var pal = Palette(sp);
        int n = BuildSpine(f, alpha);
        float L = f.LengthM * SimSpace.PixelsPerMetre;
        float facing = f.FacingSign;
        var world = Driver.World;
        float depth = world.Water.Height(f.HeadX, world.Time) - f.HeadY;
        float fade = Math.Clamp(depth / MathF.Max(visibility * 2.5f, 0.5f), 0f, 0.55f);

        float bodyEnd = 1f - shape.TailLength;
        float beat = f.TailBeat;
        for (int j = 0; j < Stations; j++)
        {
            float s = bodyEnd * j / (Stations - 1);
            var (p, t) = Along(n, s);
            // Screen "up" for the dorsal side whichever way the fish faces (screen y is down).
            Vector2 up = new Vector2(t.Y, -t.X) * -facing;
            float hd = Sample(shape.Dorsal, j) * L, hv = Sample(shape.Ventral, j) * L;
            _top[j] = p + up * hd;
            _mid[j] = p + up * ((hd - hv) * 0.15f);
            _bot[j] = p - up * hv;
            _tan[j] = t;
        }

        // Body: two strips (dorsal→flank, flank→belly). The lateral wave lightens the side turning toward us.
        int baseIdx = _pts.Count;
        for (int j = 0; j < Stations; j++)
        {
            float s = (float)j / (Stations - 1);
            float wave = MathF.Sin(f.TailPhase - s * 5.5f) * beat * s * s;
            float shade = 1f + 0.18f * wave;
            Color flank = Mul(pal.f, shade);
            if (pal.stripe && j > 1) flank = flank.Lerp(pal.d, 0.35f);
            V(_top[j], Fade(Mul(pal.d, shade), fade, light));
            V(_mid[j], Fade(flank, fade, light));
            V(_bot[j], Fade(pal.b, fade, light));
        }
        for (int j = 0; j < Stations - 1; j++)
        {
            int a = baseIdx + j * 3, b = a + 3;
            Tri(a, a + 1, b); Tri(a + 1, b + 1, b);
            Tri(a + 1, a + 2, b + 1); Tri(a + 2, b + 2, b + 1);
        }

        // Bars across the flank (sunfish, juveniles) and the bluegill/redear "ear".
        if (pal.bars)
        {
            Color bar = Fade(pal.d.Darkened(0.25f), fade, light) with { A = 0.45f };
            for (int j = 2; j < Stations - 2; j += 1)
            {
                if (j % 2 == 1) continue;
                Vector2 w = (_top[j + 1] - _top[j]) * 0.35f;
                int a = V(_top[j] + w * 0.4f, bar), b = V(_top[j] + w * 1.4f, bar);
                int c = V(_bot[j] + w * 1.4f * 0.8f, bar with { A = 0.1f }), d = V(_bot[j] + w * 0.4f * 0.8f, bar with { A = 0.1f });
                Tri(a, b, c); Tri(a, c, d);
            }
        }
        if (pal.ear)
        {
            Color ear = Fade(new Color(0.07f, 0.08f, 0.09f), fade * 0.5f, light);
            Vector2 p = _mid[2].Lerp(_top[2], 0.25f);
            Vector2 back = _tan[2] * (0.07f * L);
            Vector2 up = (_top[2] - _mid[2]).Normalized() * (0.035f * L);
            Tri(V(p - back * 0.3f + up, ear), V(p + back, ear), V(p - back * 0.3f - up, ear));
        }
        if (pal.spots)
        {
            Color spot = Fade(pal.d.Darkened(0.35f), fade, light) with { A = 0.55f };
            for (int j = 2; j < Stations - 1; j++)
            {
                Vector2 p = _mid[j].Lerp(_top[j], 0.3f + 0.25f * ((j * 37 + f.Id) % 3));
                float r = 0.012f * L + 0.6f;
                Tri(V(p + new Vector2(0, -r), spot), V(p + new Vector2(r, r), spot), V(p + new Vector2(-r, r), spot));
            }
        }

        // Fins (translucent darker body colour). Dorsal along the back, anal along the belly.
        Color fin = Fade(pal.d.Lerp(pal.f, 0.3f), fade, light) with { A = 0.8f };
        AddEdgeFin(shape.DorsalFin, _top, bodyEnd, L, fin, true);
        AddEdgeFin(shape.AnalFin, _bot, bodyEnd, L, fin with { A = 0.7f }, false);

        // Pectoral fin, sculling slowly.
        {
            Vector2 root = _mid[2].Lerp(_bot[2], 0.3f);
            Vector2 back = _tan[2];
            Vector2 down = (_bot[2] - _mid[2]).Normalized();
            float flap = 0.5f + 0.5f * MathF.Sin(f.TailPhase * 0.5f + f.Id);
            Tri(V(root, fin), V(root + back * (0.14f * L) + down * (0.03f * L * flap), fin with { A = 0.5f }), V(root + back * (0.06f * L) + down * (0.07f * L), fin with { A = 0.5f }));
        }

        // Tail fin: seen face-on mid-stroke, edge-on at the ends of the beat.
        {
            var (p, t) = Along(n, 1f);
            Vector2 baseP = (_top[Stations - 1] + _bot[Stations - 1]) * 0.5f;
            Vector2 up = (_top[Stations - 1] - _bot[Stations - 1]).Normalized();
            float sweep = MathF.Cos(f.TailPhase) * beat;
            float spread = shape.TailSpread * L * (0.55f + 0.45f * MathF.Abs(MathF.Cos(f.TailPhase * 0.5f + 0.4f)) * (1f - 0.5f * beat) + 0.2f);
            float tl = shape.TailLength * L * (0.85f + 0.15f * MathF.Abs(sweep));
            Vector2 dir = t;
            Vector2 tipTop = baseP + dir * tl + up * spread, tipBot = baseP + dir * tl - up * spread;
            Vector2 notch = baseP + dir * (tl * (1f - 0.6f * shape.TailFork));
            Color tail = Fade(pal.d.Lerp(pal.f, 0.5f), fade, light) with { A = 0.9f };
            float bh = (_top[Stations - 1] - _bot[Stations - 1]).Length() * 0.5f;
            int b0 = V(baseP + up * bh, tail), b1 = V(baseP - up * bh, tail);
            int tt = V(tipTop, tail with { A = 0.75f }), tb = V(tipBot, tail with { A = 0.75f }), nn = V(notch, tail);
            Tri(b0, tt, nn); Tri(b0, nn, b1); Tri(b1, nn, tb);
        }

        // Barbels (catfish).
        if (shape.Barbels)
        {
            Color barbel = Fade(pal.d.Darkened(0.3f), fade, light);
            Vector2 snout = _mid[0];
            Vector2 fwd = -_tan[0];
            Vector2 down = (_bot[1] - _mid[1]).Normalized();
            for (int k = 0; k < 2; k++)
            {
                Vector2 tip = snout + fwd * (0.06f * L) + down * ((0.05f + 0.05f * k) * L);
                Vector2 side = down * 0.6f;
                Tri(V(snout, barbel), V(tip, barbel), V(snout + side, barbel));
            }
        }
    }

    /// <summary>A fin standing off the top or bottom edge between two stations.</summary>
    private void AddEdgeFin(float[] spec, Vector2[] edge, float bodyEnd, float L, Color c, bool dorsal)
    {
        if (spec.Length < 3 || spec[2] <= 0f) return;
        int j0 = Math.Clamp((int)MathF.Round(spec[0] * (Stations - 1)), 0, Stations - 1);
        int j1 = Math.Clamp((int)MathF.Round(spec[1] * (Stations - 1)), j0 + 1, Stations - 1);
        Vector2 outward = (edge[(j0 + j1) / 2] - _mid[(j0 + j1) / 2]).Normalized();
        float h = spec[2] * L;
        int prev = V(edge[j0], c);
        int prevTop = V(edge[j0] + outward * (h * 0.4f), c with { A = c.A * 0.7f });
        for (int j = j0 + 1; j <= j1; j++)
        {
            float k = (float)(j - j0) / (j1 - j0);
            float hh = h * (0.4f + 0.6f * MathF.Sin(MathF.PI * MathF.Min(1f, k * 1.1f)));
            int e = V(edge[j], c);
            int top = V(edge[j] + outward * hh, c with { A = c.A * 0.7f });
            Tri(prev, prevTop, top); Tri(prev, top, e);
            prev = e; prevTop = top;
        }
    }

    private static float Sample(float[] arr, int j)
    {
        if (arr.Length == 0) return 0.1f;
        if (arr.Length == Stations) return arr[j];
        float u = (float)j / (Stations - 1) * (arr.Length - 1);
        int i = Math.Min((int)u, arr.Length - 2);
        return Mathf.Lerp(arr[i], arr[i + 1], u - i);
    }

    private void DrawEye(Fish f, float alpha, float light, float visibility)
    {
        int n = BuildSpine(f, alpha);
        float L = f.LengthM * SimSpace.PixelsPerMetre;
        var (p, t) = Along(n, 0.07f);
        Vector2 up = new Vector2(t.Y, -t.X) * -f.FacingSign;
        float yaw = MathF.Cos(f.PrevYaw) * MathF.Cos(f.Yaw) < 0f ? f.Yaw : Mathf.Lerp(f.PrevYaw, f.Yaw, alpha);
        float r = MathF.Max(0.032f * L, 1.2f) * (0.6f + 0.4f * MathF.Abs(MathF.Cos(yaw)));
        var world = Driver.World;
        float fade = Math.Clamp((world.Water.Height(f.HeadX, world.Time) - f.HeadY) / MathF.Max(visibility * 2.5f, 0.5f), 0f, 0.55f);
        Vector2 c = p + up * (0.045f * L);
        DrawCircle(c, r, Fade(new Color(0.9f, 0.85f, 0.6f), fade, light));
        DrawCircle(c, r * 0.62f, Fade(Palette(f.Species).eye.Darkened(0.4f), fade * 0.6f, light));
    }
}
