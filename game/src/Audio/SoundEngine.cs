using Godot;
using TheLedger.Controls;
using TheLedger.Sim.Core;
using TheLedger.Sim.Tackle;
using TheLedger.SimGlue;

namespace TheLedger.Audio;

/// <summary>
/// Audio pass 1 (docs/08 M6.5), fully synthesised (no sample files, nothing to license): pond ambience that follows
/// the wind and the time of day (birds by day; crickets and frogs at night), reel clicks per handle turn, the bail,
/// the drag's ratchet buzz rising with slip speed, line hiss off the spool, rod swish, splashes and plops sized by the
/// impact, popper pops and a line snap. Everything is driven by sim state and events; nothing feeds back into the sim.
/// One AudioStreamGenerator, filled each frame.
/// </summary>
public partial class SoundEngine : Node
{
    public SimDriver Driver { get; set; } = null!;
    public InputRouter Router { get; set; } = null!;

    private const int Rate = 44100;
    private AudioStreamPlayer _player = null!;
    private AudioStreamGeneratorPlayback _pb = null!;
    private readonly Dictionary<int, Vector2[]> _buffers = new();
    private readonly Random _rng = new(7);

    private enum Kind : byte { Noise, Plop, Click, Chirp, Snap, Croak, Bark }

    private struct Voice
    {
        public Kind Kind;
        public float T, Dur, Amp, F0, F1, Pan, Cut;
        public float Phase, Lp, Lp2;
    }

    private readonly List<Voice> _voices = new(64);

    // Continuous state.
    private float _ambLp, _ambLp2, _hissHp, _hissPrev, _swishLp, _swishBp, _dragPhase, _dragLp;
    private float _lapPhase, _whinePhase;
    private double _time;
    private bool _lastBail;
    private float _crankClickCarry;
    private float _nextCritter = 2f;
    private float _warnT;

    public override void _Ready()
    {
        TheLedger.Render.TestView.Barked += OnBark;
        var gen = new AudioStreamGenerator { MixRate = Rate, BufferLength = 0.12f };
        _player = new AudioStreamPlayer { Stream = gen, VolumeDb = -2f };
        AddChild(_player);
        _player.Play();
        _pb = (AudioStreamGeneratorPlayback)_player.GetStreamPlayback();
        Driver.EventRaised += OnEvent;
        _lastBail = Driver.World.Anglers[0].Reel.BailOpen;
    }

    public override void _ExitTree()
    {
        Driver.EventRaised -= OnEvent;
        TheLedger.Render.TestView.Barked -= OnBark;
    }

    private void OnBark() => Add(Kind.Bark, 0.14f, 0.35f, 520f, 300f, -0.35f, 0.4f);

    private float Rnd() => (float)_rng.NextDouble();
    private float Noise() => Rnd() * 2f - 1f;

    private void Add(Kind k, float dur, float amp, float f0, float f1, float pan = 0f, float cut = 0.3f)
    {
        if (_voices.Count >= 60) return;
        _voices.Add(new Voice { Kind = k, Dur = dur, Amp = amp, F0 = f0, F1 = f1, Pan = Math.Clamp(pan, -1f, 1f), Cut = cut });
    }

    /// <summary>Distance falloff and stereo pan from the angler (sounds out on the water are quieter and to the right).</summary>
    private (float gain, float pan) Place(Vec2 p)
    {
        var a = Driver.World.Anglers[0].Position;
        float dx = p.X - a.X;
        return (1f / (1f + MathF.Abs(dx) / 10f), Math.Clamp(dx / 25f, -0.8f, 0.8f));
    }

    private void OnEvent(SimEvent e)
    {
        switch (e.Type)
        {
            case SimEventType.Splash:
            {
                var (g, pan) = Place(e.Position);
                float energy = MathF.Max(e.Value, 0f);
                float size = MathF.Min(1f, MathF.Sqrt(energy) * 1.2f);
                if (e.Id == 2) // popper pop: a hollow bloop
                {
                    Add(Kind.Plop, 0.09f, 0.5f * g, 520f, 260f, pan);
                    Add(Kind.Noise, 0.08f, 0.25f * g, 0f, 0f, pan, 0.25f);
                }
                else if (e.Id == 1) // skip: a slap
                    Add(Kind.Noise, 0.07f, (0.25f + 0.4f * size) * g, 0f, 0f, pan, 0.5f);
                else
                {
                    // Small entries plop (a bubble's ring-down); bigger ones add a crash of spray.
                    Add(Kind.Plop, 0.12f + 0.1f * size, (0.25f + 0.35f * size) * g, 900f - 500f * size, 300f - 150f * size, pan);
                    Add(Kind.Noise, 0.15f + 0.5f * size, (0.1f + 0.5f * size) * g, 0f, 0f, pan, 0.12f + 0.3f * size);
                }
                break;
            }
            case SimEventType.Backlash:
                Add(Kind.Noise, 0.25f, 0.4f, 0f, 0f, -0.3f, 0.5f);
                Add(Kind.Snap, 0.06f, 0.4f, 1200f, 500f, -0.3f);
                break;
            case SimEventType.LineBroke:
                Add(Kind.Snap, 0.05f, 0.7f, 2400f, 900f);
                Add(Kind.Chirp, 0.18f, 0.25f, 3200f, 2600f, 0.2f);
                break;
            case SimEventType.RodBroke:
                Add(Kind.Snap, 0.12f, 0.9f, 900f, 300f);
                Add(Kind.Noise, 0.2f, 0.5f, 0f, 0f, 0f, 0.4f);
                break;
            case SimEventType.FishLanded:
            {
                var (g, pan) = Place(e.Position);
                Add(Kind.Noise, 0.35f, 0.45f * g, 0f, 0f, pan, 0.3f);
                break;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_pb == null) return;
        int n = _pb.GetFramesAvailable();
        if (n <= 0) return;
        if (!_buffers.TryGetValue(n, out var buf)) { buf = new Vector2[n]; if (_buffers.Count < 32) _buffers[n] = buf; }

        var w = Driver.World;
        var a = w.Anglers[0];
        var reel = a.Reel;
        float dt = 1f / Rate;

        // Discrete cues from state changes this frame.
        if (reel.BailOpen != _lastBail)
        {
            Add(Kind.Click, 0.02f, 0.5f, 2200f, 1400f, -0.3f);
            Add(Kind.Click, 0.015f, 0.3f, 3500f, 3000f, -0.3f);
            _lastBail = reel.BailOpen;
        }
        float crank = a.LastInput.CrankRate;
        if (crank > 0f && !reel.BailOpen)
        {
            // The anti-reverse and gears tick ~10 times per handle turn.
            _crankClickCarry += crank * (float)delta * 10f;
            while (_crankClickCarry >= 1f)
            {
                _crankClickCarry -= 1f;
                Add(Kind.Click, 0.008f, 0.12f + 0.04f * Rnd(), 3000f + 800f * Rnd(), 2500f, -0.3f);
            }
        }

        // Warnings while fighting: a fast tick while the hook is tearing out, a low creak as the line nears its limit.
        _warnT -= (float)delta;
        if (_warnT <= 0f)
        {
            var wl = Driver.World.Anglers[0].Rig.Line;
            float lim = wl.Spec.BreakingForceN * wl.Spec.KnotFactor;
            bool tearing = false;
            foreach (var f in Driver.World.Fish) if (f.State == TheLedger.Sim.Fish.FishState.Hooked && f.LureAngler == 0) { tearing = f.HookTearing; break; }
            if (tearing) { Add(Kind.Click, 0.05f, 0.5f, 2600f, 2200f, 0.2f, 0.6f); _warnT = 0.18f; }
            else if (wl.BreakLoad > 0.75f * lim) { Add(Kind.Croak, 0.12f, 0.25f, 70f, 55f, 0.1f, 0.2f); _warnT = 0.3f; }
        }

        // Critters: birds by day, crickets and frogs at night.
        float hour = w.Hour;
        float day = hour < 5.5f || hour > 20.5f ? 0f : hour < 7.5f ? (hour - 5.5f) / 2f : hour > 18.5f ? (20.5f - hour) / 2f : 1f;
        _nextCritter -= (float)delta;
        if (_nextCritter <= 0f)
        {
            float pan = Noise() * 0.8f;
            if (Rnd() < day)
            {
                int notes = 2 + _rng.Next(4);
                float f = 2500f + 2500f * Rnd();
                for (int i = 0; i < notes; i++) Add(Kind.Chirp, 0.06f + 0.06f * Rnd(), 0.05f * (0.5f + Rnd()), f * (0.9f + 0.2f * Rnd()), f * (1.1f + 0.3f * Rnd()), pan);
                _nextCritter = 1.5f + 5f * Rnd();
            }
            else
            {
                if (Rnd() < 0.6f) for (int i = 0; i < 3; i++) Add(Kind.Chirp, 0.025f, 0.035f, 4400f, 4500f, pan);
                else Add(Kind.Croak, 0.22f, 0.12f, 180f + 60f * Rnd(), 120f, pan);
                _nextCritter = 0.3f + 1.2f * Rnd();
            }
        }

        // Continuous layers.
        float wind = MathF.Abs(w.Wind.X);
        float lineSpeed = reel.LineSpeed;
        bool hiss = reel.BailOpen && lineSpeed > 0.5f;
        float hissAmp = hiss ? MathF.Min(0.12f, lineSpeed * 0.006f) : 0f;
        float dragAmp = reel.DragSlipping ? MathF.Min(0.22f, 0.06f + lineSpeed * 0.08f) : 0f;
        float dragRate = MathF.Min(900f, MathF.Max(40f, lineSpeed / 0.004f)); // detent clicks per second
        float omega = MathF.Abs(a.Hands.AngularVelocity);
        float swishAmp = omega > 4f ? MathF.Min(0.25f, (omega - 4f) * 0.02f) : 0f;
        float ambAmp = 0.035f + 0.006f * wind;
        // A baitcaster spool singing in free spool: pitch from its speed.
        float whineAmp = reel.Spec.IsBaitcaster && reel.BailOpen && reel.SpoolOmega > 30f ? MathF.Min(0.08f, reel.SpoolOmega * 0.00015f) : 0f;
        float whineHz = 120f + reel.SpoolOmega * 1.6f;

        for (int i = 0; i < n; i++)
        {
            _time += dt;
            float l = 0f, r = 0f;

            // Lapping water: brown-ish noise with a slow swell.
            float nz = Noise();
            _ambLp += (nz - _ambLp) * 0.04f;
            _ambLp2 += (_ambLp - _ambLp2) * 0.08f;
            _lapPhase += dt * 0.23f;
            float lap = ambAmp * (0.6f + 0.4f * MathF.Sin(_lapPhase * MathF.Tau)) * _ambLp2 * 6f;
            l += lap; r += lap;

            if (hissAmp > 0f)
            {
                float hp = nz - _hissPrev; _hissPrev = nz;
                _hissHp += (hp - _hissHp) * 0.5f;
                float s = _hissHp * hissAmp;
                l += s * 1.1f; r += s * 0.7f;
            }
            if (whineAmp > 0f)
            {
                _whinePhase += whineHz * dt;
                float s = (MathF.Sin(_whinePhase * MathF.Tau) + 0.3f * MathF.Sin(_whinePhase * MathF.Tau * 2.7f)) * whineAmp;
                l += s * 1.1f; r += s * 0.7f;
            }
            if (dragAmp > 0f)
            {
                // Ratchet: a fast train of clicks through a ringing filter.
                _dragPhase += dragRate * dt;
                float click = 0f;
                if (_dragPhase >= 1f) { _dragPhase -= 1f; click = 1f; }
                _dragLp += (click + 0.15f * nz - _dragLp) * 0.35f;
                float s = _dragLp * dragAmp * 2f;
                l += s * 1.1f; r += s * 0.6f;
            }
            if (swishAmp > 0f)
            {
                _swishLp += (nz - _swishLp) * MathF.Min(0.6f, 0.05f + omega * 0.02f);
                _swishBp += (_swishLp - _swishBp) * 0.1f;
                float s = (_swishLp - _swishBp) * swishAmp * 3f;
                l += s; r += s;
            }

            // One-shot voices.
            for (int v = _voices.Count - 1; v >= 0; v--)
            {
                var vo = _voices[v];
                vo.T += dt;
                if (vo.T >= vo.Dur) { _voices.RemoveAt(v); continue; }
                float u = vo.T / vo.Dur;
                float env = vo.Kind == Kind.Croak ? MathF.Sin(MathF.PI * u) * (0.6f + 0.4f * MathF.Sin(vo.T * MathF.Tau * 28f)) : (1f - u) * (1f - u);
                if (vo.T < 0.002f) env *= vo.T / 0.002f;
                float f = vo.F0 + (vo.F1 - vo.F0) * u;
                float s;
                switch (vo.Kind)
                {
                    case Kind.Noise:
                        vo.Lp += (nz - vo.Lp) * vo.Cut;
                        vo.Lp2 += (vo.Lp - vo.Lp2) * vo.Cut;
                        s = vo.Lp2 * 2.2f;
                        break;
                    case Kind.Snap:
                        vo.Phase += f * dt;
                        s = 0.6f * nz + 0.6f * MathF.Sin(vo.Phase * MathF.Tau);
                        break;
                    case Kind.Bark:
                        // A short rough woof: a falling buzzy tone with breath noise.
                        vo.Phase += f * dt;
                        s = 0.6f * MathF.Sign(MathF.Sin(vo.Phase * MathF.Tau)) * MathF.Abs(MathF.Sin(vo.Phase * MathF.Tau * 0.5f)) + 0.35f * Noise();
                        break;
                    case Kind.Croak:
                        vo.Phase += f * dt;
                        s = MathF.Sign(MathF.Sin(vo.Phase * MathF.Tau)) * 0.5f + 0.3f * MathF.Sin(vo.Phase * MathF.Tau * 2f);
                        break;
                    default:
                        vo.Phase += f * dt;
                        s = MathF.Sin(vo.Phase * MathF.Tau);
                        break;
                }
                s *= env * vo.Amp;
                l += s * (1f - MathF.Max(0f, vo.Pan));
                r += s * (1f + MathF.Min(0f, vo.Pan));
                _voices[v] = vo;
            }

            buf[i] = new Vector2(MathF.Tanh(l), MathF.Tanh(r));
        }
        _pb.PushBuffer(buf);
    }
}
