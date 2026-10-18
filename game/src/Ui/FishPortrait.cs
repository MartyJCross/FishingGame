using Godot;
using TheLedger.Sim.Fish;

namespace TheLedger.Ui;

/// <summary>
/// A side-on picture of a species for the guide and checklists, drawn from the same data as the fish in the water
/// (body profile, colours, markings, fins, tail), head to the left. Set <see cref="Species"/>; it fills its rect.
/// </summary>
public partial class FishPortrait : Control
{
    private SpeciesSpec? _species;
    public SpeciesSpec? Species { get => _species; set { _species = value; QueueRedraw(); } }

    /// <summary>Greyed out (not caught yet).</summary>
    public bool Faded { get; set; }

    private const int Stations = 9;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    private static float Sample(float[] a, int j) => a.Length == 0 ? 0.1f : a.Length == Stations ? a[j] : a[Math.Min(a.Length - 1, j * a.Length / Stations)];

    public override void _Draw()
    {
        if (_species == null) return;
        var sp = _species;
        var shape = sp.Shape;
        var pat = sp.Body.Pattern;
        Color dorsal = Color.FromHtml(pat.Dorsal), flank = Color.FromHtml(pat.Flank), belly = Color.FromHtml(pat.Belly), eye = Color.FromHtml(pat.Eye);
        bool Has(string t) => pat.Markings.Any(m => m.Type.Contains(t));
        bool bars = Has("bar"), stripe = Has("stripe") || Has("band"), ear = Has("opercular"), spots = Has("spot");
        if (Faded)
        {
            Color G(Color c) => new Color(c.Luminance, c.Luminance, c.Luminance).Lerp(new Color(0.5f, 0.55f, 0.58f), 0.5f);
            dorsal = G(dorsal); flank = G(flank); belly = G(belly); eye = G(eye);
        }

        // Fit the whole fish (body + tail + fins) into the rect, head left.
        float maxH = 0f;
        for (int j = 0; j < Stations; j++) maxH = MathF.Max(maxH, Sample(shape.Dorsal, j) + Sample(shape.Ventral, j));
        float finH = MathF.Max(shape.DorsalFin.Length > 2 ? shape.DorsalFin[2] : 0f, 0f) + MathF.Max(shape.AnalFin.Length > 2 ? shape.AnalFin[2] : 0f, 0f);
        float tallest = MathF.Max(maxH + finH, 2f * shape.TailSpread) + 0.02f;
        var r = GetRect();
        float L = MathF.Min(r.Size.X * 0.92f, r.Size.Y * 0.9f / tallest);
        float x0 = (r.Size.X - L) * 0.5f;
        float spineY = r.Size.Y * 0.5f + (Sample(shape.Dorsal, 4) - Sample(shape.Ventral, 4)) * L * 0.5f;
        float bodyEnd = 1f - shape.TailLength;
        var top = new Vector2[Stations];
        var mid = new Vector2[Stations];
        var bot = new Vector2[Stations];
        for (int j = 0; j < Stations; j++)
        {
            float x = x0 + bodyEnd * L * j / (Stations - 1);
            float hd = Sample(shape.Dorsal, j) * L, hv = Sample(shape.Ventral, j) * L;
            top[j] = new Vector2(x, spineY - hd);
            mid[j] = new Vector2(x, spineY - (hd - hv) * 0.15f);
            bot[j] = new Vector2(x, spineY + hv);
        }

        // Tail.
        {
            var baseP = (top[^1] + bot[^1]) * 0.5f;
            float tl = shape.TailLength * L, spread = shape.TailSpread * L;
            var tipTop = baseP + new Vector2(tl, -spread);
            var tipBot = baseP + new Vector2(tl, spread);
            var notch = baseP + new Vector2(tl * (1f - 0.6f * shape.TailFork), 0f);
            var tc = dorsal.Lerp(flank, 0.5f);
            DrawColoredPolygon(new[] { top[^1], tipTop, notch, tipBot, bot[^1] }, tc);
        }
        // Fins.
        Fin(shape.DorsalFin, top, bodyEnd, L, -1f, dorsal.Lerp(flank, 0.3f));
        Fin(shape.AnalFin, bot, bodyEnd, L, 1f, dorsal.Lerp(flank, 0.3f) with { A = 0.85f });

        // Body: dorsal→flank and flank→belly strips with per-vertex colour.
        for (int j = 0; j < Stations - 1; j++)
        {
            var fc = stripe && j > 1 ? flank.Lerp(dorsal, 0.35f) : flank;
            DrawPolygon(new[] { top[j], top[j + 1], mid[j + 1], mid[j] }, new[] { dorsal, dorsal, fc, fc });
            DrawPolygon(new[] { mid[j], mid[j + 1], bot[j + 1], bot[j] }, new[] { fc, fc, belly, belly });
        }
        if (bars)
            for (int j = 2; j < Stations - 2; j += 2)
            {
                var w = (top[j + 1] - top[j]) * 0.35f;
                DrawColoredPolygon(new[] { top[j] + w * 0.4f, top[j] + w * 1.4f, bot[j] + w * 1.1f, bot[j] + w * 0.3f }, dorsal.Darkened(0.25f) with { A = 0.4f });
            }
        if (spots)
            for (int j = 2; j < Stations - 1; j++)
            {
                var p = mid[j].Lerp(top[j], 0.3f + 0.25f * ((j * 37) % 3));
                DrawCircle(p, MathF.Max(1.2f, 0.012f * L), dorsal.Darkened(0.35f) with { A = 0.6f });
            }
        // Gill line, ear, eye, barbels.
        DrawLine(top[1].Lerp(bot[1], 0.15f) + new Vector2(L * 0.02f, 0f), bot[1] + new Vector2(L * 0.015f, -L * 0.01f), dorsal.Darkened(0.3f) with { A = 0.6f }, MathF.Max(1f, L * 0.006f));
        if (ear) DrawColoredPolygon(new[] { mid[1].Lerp(top[1], 0.3f) + new Vector2(L * 0.03f, -L * 0.02f), mid[1].Lerp(top[1], 0.3f) + new Vector2(L * 0.1f, 0f), mid[1].Lerp(top[1], 0.3f) + new Vector2(L * 0.03f, L * 0.02f) }, new Color(0.08f, 0.09f, 0.1f));
        var eyeP = mid[0].Lerp(top[0], 0.45f) + new Vector2(L * 0.045f, 0f);
        float er = MathF.Max(2f, 0.025f * L);
        DrawCircle(eyeP, er, new Color(0.92f, 0.88f, 0.65f));
        DrawCircle(eyeP, er * 0.6f, eye.Darkened(0.5f));
        if (shape.Barbels)
            for (int k = 0; k < 2; k++)
                DrawLine(mid[0], mid[0] + new Vector2(-L * 0.06f, L * (0.05f + 0.05f * k)), dorsal.Darkened(0.3f), MathF.Max(1f, L * 0.006f));
    }

    /// <summary>A fin standing off the back (dir −1) or belly (dir +1) between fractions spec[0]..spec[1], height spec[2].</summary>
    private void Fin(float[] spec, Vector2[] edge, float bodyEnd, float L, float dir, Color c)
    {
        if (spec.Length < 3 || spec[2] <= 0f) return;
        Vector2 At(float s)
        {
            float u = Math.Clamp(s / MathF.Max(bodyEnd, 0.01f), 0f, 1f) * (Stations - 1);
            int i = Math.Min((int)u, Stations - 2);
            return edge[i].Lerp(edge[i + 1], u - i);
        }
        var a = At(spec[0]);
        var b = At(spec[1]);
        float h = spec[2] * L;
        DrawColoredPolygon(new[] { a, a.Lerp(b, 0.25f) + new Vector2(0f, dir * h), b + new Vector2(-L * 0.02f, dir * h * 0.6f), b }, c);
    }
}
