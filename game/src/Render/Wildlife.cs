using Godot;
using TheLedger.Boot;
using TheLedger.SimGlue;

namespace TheLedger.Render;

/// <summary>
/// Life around the water (docs/07 §3): flocks of small birds crossing the sky, a great egret flapping low over the
/// water, and dragonflies darting over the shallows on warm days. Cosmetic only (frame time, its own RNG): nothing
/// here touches the sim, and the fish don't see any of it.
/// </summary>
public partial class Wildlife : Node2D
{
    public SimDriver Driver { get; set; } = null!;

    private readonly RandomNumberGenerator _rng = new() { Seed = 4242 };

    private struct Bird
    {
        public Vector2 Pos, Vel;
        public float Phase, FlapHz, Size;
        public bool Egret;
    }

    private struct Fly
    {
        public Vector2 Pos, Target;
        public float Retarget, Phase;
    }

    private readonly List<Bird> _birds = new();
    private readonly Fly[] _flies = new Fly[3];
    private int _flyCount;
    private float _nextFlock = 4f, _nextEgret = 25f;

    private static readonly Color BirdCol = new(0.13f, 0.15f, 0.19f);
    private static readonly Color EgretCol = new(0.96f, 0.96f, 0.93f);
    private static readonly Color EgretBill = new(0.95f, 0.75f, 0.2f);
    private static readonly Color Legs = new(0.12f, 0.12f, 0.12f);
    private static readonly Color FlyBody = new(0.15f, 0.5f, 0.55f);
    private static readonly Color Wing = new(0.85f, 0.95f, 1f, 0.45f);

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        var s = Atmosphere.Now;
        bool day = s.Elevation > 0.04f;
        float calm = 1f - s.Rain;

        // The visible world, m.
        var vp = GetViewportRect();
        var inv = GetCanvasTransform().AffineInverse();
        float left = (inv * vp.Position).X / SimSpace.PixelsPerMetre;
        float right = (inv * vp.End).X / SimSpace.PixelsPerMetre;

        _nextFlock -= dt;
        if (_nextFlock <= 0f)
        {
            _nextFlock = _rng.RandfRange(12f, 35f);
            if (day && calm > 0.5f) SpawnFlock(left, right);
        }
        _nextEgret -= dt;
        if (_nextEgret <= 0f)
        {
            _nextEgret = _rng.RandfRange(40f, 110f);
            if ((day || s.Elevation > -0.05f) && calm > 0.6f) SpawnEgret(left, right);
        }
        for (int i = _birds.Count - 1; i >= 0; i--)
        {
            var b = _birds[i];
            b.Pos += b.Vel * dt;
            b.Pos.Y += MathF.Sin(b.Phase * 0.3f) * 0.05f * dt;
            b.Phase += b.FlapHz * Mathf.Tau * dt;
            _birds[i] = b;
            if (b.Pos.X < left - 12f || b.Pos.X > right + 12f) _birds.RemoveAt(i);
        }

        // Dragonflies on warm days.
        int want = day && calm > 0.7f && Driver.World.WaterTempC > 18f && s.Cloud < 0.7f ? (s.Elevation > 0.25f ? 3 : 1) : 0;
        while (_flyCount < want)
        {
            var p = new Vector2(_rng.RandfRange(0.2f, 4f), _rng.RandfRange(0.2f, 1.1f));
            _flies[_flyCount++] = new Fly { Pos = p + new Vector2(-6f, 1.5f), Target = p, Phase = _rng.Randf() * 10f };
        }
        if (_flyCount > want) _flyCount = want;
        for (int i = 0; i < _flyCount; i++)
        {
            ref var f = ref _flies[i];
            f.Retarget -= dt;
            if (f.Retarget <= 0f)
            {
                // Dart to a new hover point over the shallows, sometimes a long way.
                f.Target = new Vector2(Mathf.Clamp(f.Pos.X + _rng.RandfRange(-1.6f, 1.6f), -0.5f, 6f), _rng.RandfRange(0.12f, 1.2f));
                f.Retarget = _rng.RandfRange(0.4f, 2.2f);
            }
            float k = 1f - MathF.Exp(-9f * dt);
            f.Pos = f.Pos.Lerp(f.Target, k);
            f.Phase += dt;
        }
        QueueRedraw();
    }

    private void SpawnFlock(float left, float right)
    {
        bool fromLeft = _rng.Randf() < 0.5f;
        int n = _rng.RandiRange(3, 8);
        float speed = _rng.RandfRange(4f, 7f) * (fromLeft ? 1f : -1f);
        float y = _rng.RandfRange(3.2f, 6.5f);
        float x = fromLeft ? left - 3f : right + 3f;
        for (int i = 0; i < n; i++)
        {
            // A loose V: each bird a little behind and to one side of the one ahead.
            int rank = (i + 1) / 2;
            float side = i % 2 == 0 ? 1f : -1f;
            _birds.Add(new Bird
            {
                Pos = new Vector2(x - rank * 0.55f * MathF.Sign(speed) + _rng.RandfRange(-0.1f, 0.1f), y + side * rank * 0.28f),
                Vel = new Vector2(speed * _rng.RandfRange(0.97f, 1.03f), _rng.RandfRange(-0.1f, 0.1f)),
                Phase = _rng.Randf() * 10f, FlapHz = _rng.RandfRange(3.5f, 5f), Size = _rng.RandfRange(0.16f, 0.22f),
            });
        }
    }

    private void SpawnEgret(float left, float right)
    {
        bool fromLeft = _rng.Randf() < 0.5f;
        _birds.Add(new Bird
        {
            Pos = new Vector2(fromLeft ? left - 3f : right + 3f, _rng.RandfRange(1.3f, 2.6f)),
            Vel = new Vector2((fromLeft ? 1f : -1f) * _rng.RandfRange(2.4f, 3.2f), _rng.RandfRange(-0.05f, 0.08f)),
            Phase = 0f, FlapHz = 1.6f, Size = 0.5f, Egret = true,
        });
    }

    public override void _Draw()
    {
        foreach (var b in _birds)
        {
            if (b.Egret) DrawEgret(b);
            else
            {
                // A chevron that flaps: two wings from the body.
                float flap = MathF.Sin(b.Phase);
                var c = b.Pos;
                float dir = MathF.Sign(b.Vel.X);
                var tipL = c + new Vector2(-b.Size * 0.9f * dir * 0.3f - b.Size * 0.5f, b.Size * 0.55f * flap);
                var tipR = c + new Vector2(b.Size * 0.5f - b.Size * 0.9f * dir * 0.3f, b.Size * 0.55f * flap);
                Lp.Limb(this, BirdCol, c, tipL, 0.05f, 0.012f);
                Lp.Limb(this, BirdCol, c, tipR, 0.05f, 0.012f);
                Lp.Limb(this, BirdCol, c - new Vector2(dir * 0.06f, 0f), c + new Vector2(dir * 0.08f, 0f), 0.05f, 0.03f);
            }
        }
        for (int i = 0; i < _flyCount; i++)
        {
            var f = _flies[i];
            float dir = f.Target.X >= f.Pos.X ? 1f : -1f;
            var tail = f.Pos - new Vector2(dir * 0.09f, 0f);
            var head = f.Pos + new Vector2(dir * 0.03f, 0f);
            float flick = MathF.Sin(f.Phase * 90f) * 0.025f;
            Lp.Poly(this, Wing, f.Pos, f.Pos + new Vector2(-0.05f, 0.05f + flick), f.Pos + new Vector2(0.01f, 0.05f + flick));
            Lp.Poly(this, Wing, f.Pos, f.Pos + new Vector2(0.05f, 0.045f - flick), f.Pos + new Vector2(0.0f, 0.045f - flick));
            Lp.Limb(this, FlyBody, tail, head, 0.008f, 0.014f);
        }
    }

    /// <summary>A great egret in flight: neck folded back, legs trailing, slow deep wingbeats.</summary>
    private void DrawEgret(Bird b)
    {
        float d = MathF.Sign(b.Vel.X);
        var c = b.Pos;
        float s = b.Size;
        float flap = MathF.Sin(b.Phase);
        var fwd = new Vector2(d, 0f);
        // Legs trailing behind.
        Lp.Limb(this, Legs, c - fwd * s * 0.3f, c - fwd * s * 1.15f + new Vector2(0f, -0.03f), 0.015f, 0.01f);
        // Far wing (shaded) then body, folded neck, head and bill, near wing.
        var shade = EgretCol.Darkened(0.18f);
        Lp.Poly(this, shade, c + fwd * s * 0.15f, c - fwd * s * 0.2f, c + new Vector2(-d * s * 0.1f, s * 0.9f * flap + 0.05f));
        Lp.Poly(this, EgretCol, c + fwd * s * 0.3f + new Vector2(0f, 0.02f), c + fwd * s * 0.3f + new Vector2(0f, -0.05f),
                c - fwd * s * 0.35f + new Vector2(0f, -0.02f), c - fwd * s * 0.4f + new Vector2(0f, 0.02f));
        var neck = c + fwd * s * 0.42f + new Vector2(0f, 0.03f);
        Lp.Limb(this, EgretCol, c + fwd * s * 0.25f, neck, 0.06f, 0.05f);
        Lp.Ngon(this, EgretCol, neck + fwd * 0.03f, 0.035f, 6);
        Lp.Poly(this, EgretBill, neck + fwd * 0.05f + new Vector2(0f, 0.012f), neck + fwd * 0.05f + new Vector2(0f, -0.012f), neck + fwd * 0.2f);
        Lp.Poly(this, EgretCol, c + fwd * s * 0.12f, c - fwd * s * 0.18f, c + new Vector2(d * s * 0.05f, s * 1.1f * flap));
    }
}

/// <summary>
/// Fireflies over the bank on warm nights: soft blinking glows on their own canvas layer so the night tint doesn't
/// dim them (added light, not lit objects).
/// </summary>
public partial class Fireflies : Node2D
{
    public SimDriver Driver { get; set; } = null!;

    private readonly RandomNumberGenerator _rng = new() { Seed = 99 };
    private readonly Vector2[] _pos = new Vector2[22], _vel = new Vector2[22];
    private readonly float[] _phase = new float[22], _rate = new float[22];
    private float _show;

    public override void _Ready()
    {
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        for (int i = 0; i < _pos.Length; i++)
        {
            _pos[i] = new Vector2(_rng.RandfRange(-12f, 2.5f), _rng.RandfRange(0.7f, 2.6f));
            _phase[i] = _rng.Randf() * 10f;
            _rate[i] = _rng.RandfRange(0.25f, 0.6f);
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        var s = Atmosphere.Now;
        string season = Driver.World.Season;
        bool warm = season is "spring" or "summer" || Driver.World.WaterTempC > 20f;
        float target = warm && s.Elevation < -0.03f ? (1f - s.Rain) : 0f;
        _show += (target - _show) * MathF.Min(1f, dt * 0.5f);
        Visible = _show > 0.01f;
        if (!Visible) return;
        for (int i = 0; i < _pos.Length; i++)
        {
            _vel[i] += new Vector2(_rng.RandfRange(-1f, 1f), _rng.RandfRange(-1f, 1f)) * dt * 0.8f;
            _vel[i] *= 1f - MathF.Min(1f, dt * 0.8f);
            _pos[i] += _vel[i] * dt;
            _pos[i] = new Vector2(Mathf.Clamp(_pos[i].X, -14f, 3f), Mathf.Clamp(_pos[i].Y, 0.6f, 2.8f));
            _phase[i] += _rate[i] * dt;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (int i = 0; i < _pos.Length; i++)
        {
            // Each one glows for a moment in its own rhythm, then goes dark.
            float p = _phase[i] % 1f;
            float glow = p < 0.25f ? MathF.Sin(p / 0.25f * MathF.PI) : 0f;
            if (glow <= 0.01f) continue;
            glow *= _show;
            var c = SimSpace.ToPx(_pos[i].X, _pos[i].Y);
            DrawCircle(c, 24f, new Color(0.55f, 0.75f, 0.12f, 0.07f * glow));
            DrawCircle(c, 10f, new Color(0.75f, 0.95f, 0.25f, 0.22f * glow));
            DrawCircle(c, 3f, new Color(0.95f, 1f, 0.6f, 1f * glow));
        }
    }
}
