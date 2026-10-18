using Godot;
using TheLedger.Sim.Fish;
using TheLedger.SimGlue;

namespace TheLedger.Audio;

/// <summary>
/// Ambient music, synthesised like the rest of the audio (nothing to license): slow pad chords with sparse plucked
/// notes (Karplus-Strong strings) through a soft stereo echo. Major and warm by day, minor at night. It fades out
/// while a fish is on (the fight is reel and water) and sits low under storms. Volume is a Tab-menu slider.
/// </summary>
public partial class MusicEngine : Node
{
    public SimDriver Driver { get; set; } = null!;

    private const int Rate = 44100;
    private const string SettingsPath = "user://settings.cfg";
    private AudioStreamPlayer _player = null!;
    private AudioStreamGeneratorPlayback _pb = null!;
    private readonly Random _rng = new(11);

    /// <summary>Music volume 0..1 (saved).</summary>
    public static float Volume { get; private set; } = 0.6f;

    public static void LoadVolume()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SettingsPath) == Error.Ok) Volume = (float)cfg.GetValue("audio", "music", 0.6f);
    }

    public static void SetVolume(float v)
    {
        Volume = Mathf.Clamp(v, 0f, 1f);
        var cfg = new ConfigFile();
        cfg.Load(SettingsPath);
        cfg.SetValue("audio", "music", Volume);
        cfg.Save(SettingsPath);
    }

    // Chords as MIDI notes: day (D major: Dmaj9, Bm7, Gmaj7, Asus) and night (D minor: Dm9, Bbmaj7, Fmaj7, Csus).
    private static readonly int[][] Day = { new[] { 50, 57, 61, 64 }, new[] { 47, 54, 57, 62 }, new[] { 43, 50, 54, 59 }, new[] { 45, 52, 55, 62 } };
    private static readonly int[][] Night = { new[] { 50, 57, 60, 64 }, new[] { 46, 53, 57, 62 }, new[] { 41, 48, 52, 57 }, new[] { 48, 55, 58, 62 } };
    private static readonly int[] DayScale = { 62, 64, 66, 69, 71, 74, 76, 78, 81 };
    private static readonly int[] NightScale = { 62, 65, 67, 69, 72, 74, 77, 79, 81 };

    private const float ChordSeconds = 9f;

    // Pad: two chords crossfading, 4 notes x 2 detuned oscillators.
    private int _chord, _prevChord;
    private bool _night, _prevNight;
    private float _chordT = ChordSeconds; // start with a fade-in
    private readonly float[] _padPhase = new float[16], _prevPadPhase = new float[16];
    private float _padLpL, _padLpR, _lfo;

    // Plucks.
    private struct Pluck { public float[] Buf; public int Len, Pos; public float Amp, Pan, Last; public bool On; }
    private readonly Pluck[] _plucks = new Pluck[6];
    private float _nextPluck = 3f;
    private readonly float[] _echoL = new float[Rate], _echoR = new float[Rate];
    private int _echoPos;

    private float _gain, _gainTarget = 1f;
    private readonly Dictionary<int, Vector2[]> _bufs = new();
    private readonly List<(float at, int midi)> _pending = new(8);

    public override void _Ready()
    {
        LoadVolume();
        for (int i = 0; i < _plucks.Length; i++) _plucks[i].Buf = new float[Rate / 60];
        var gen = new AudioStreamGenerator { MixRate = Rate, BufferLength = 0.15f };
        _player = new AudioStreamPlayer { Stream = gen, VolumeDb = -6f };
        AddChild(_player);
        _player.Play();
        _pb = (AudioStreamGeneratorPlayback)_player.GetStreamPlayback();
    }

    private static float Hz(int midi) => 440f * MathF.Pow(2f, (midi - 69) / 12f);

    public override void _Process(double delta)
    {
        var w = Driver.World;
        float h = w.Hour;
        bool night = h < 6f || h > 20f;
        bool fishOn = false;
        foreach (var f in w.Fish) if (f.State == FishState.Hooked) { fishOn = true; break; }
        float storm = Math.Clamp(Driver.Config.DebugRainRate / 25f, 0f, 1f);
        _gainTarget = fishOn ? 0f : 1f - 0.7f * storm;

        int frames = _pb.GetFramesAvailable();
        if (frames <= 0) return;
        if (!_bufs.TryGetValue(frames, out var buf)) _bufs[frames] = buf = new Vector2[frames];
        float dt = 1f / Rate;
        for (int n = 0; n < frames; n++)
        {
            _gain += (_gainTarget * Volume - _gain) * dt / 2.5f; // slow fades
            _chordT += dt;
            if (_chordT >= ChordSeconds)
            {
                _chordT = 0f;
                _prevChord = _chord;
                _prevNight = _night;
                Array.Copy(_padPhase, _prevPadPhase, 16);
                _night = night;
                _chord = (_chord + 1) % 4;
            }
            float fadeIn = Math.Clamp(_chordT / 3f, 0f, 1f);
            float fadeOut = 1f - fadeIn;
            _lfo += dt * 0.15f;
            float trem = 0.85f + 0.15f * MathF.Sin(_lfo * MathF.Tau);
            var (cl, cr) = Pad(_night ? Night[_chord] : Day[_chord], _padPhase);
            var (pl, pr) = fadeOut > 0f ? Pad(_prevNight ? Night[_prevChord] : Day[_prevChord], _prevPadPhase) : (0f, 0f);
            float padL = (cl * fadeIn + pl * fadeOut) * trem * 0.05f;
            float padR = (cr * fadeIn + pr * fadeOut) * trem * 0.05f;
            _padLpL += (padL - _padLpL) * 0.08f;
            _padLpR += (padR - _padLpR) * 0.08f;

            // Plucks (Karplus-Strong), into a stereo echo.
            float sl = 0f, sr = 0f;
            for (int i = 0; i < _plucks.Length; i++)
            {
                ref var p = ref _plucks[i];
                if (!p.On) continue;
                float y = p.Buf[p.Pos];
                int next = (p.Pos + 1) % p.Len;
                p.Buf[p.Pos] = 0.4985f * (y + p.Buf[next]); // average = string damping
                p.Pos = next;
                p.Amp *= 0.99997f;
                if (p.Amp < 1e-3f) p.On = false;
                float s = y * p.Amp;
                sl += s * (1f - p.Pan) * 0.5f;
                sr += s * (1f + p.Pan) * 0.5f;
            }
            int ep = _echoPos;
            int tapL = (ep + _echoL.Length - (int)(0.37f * Rate)) % _echoL.Length;
            int tapR = (ep + _echoR.Length - (int)(0.49f * Rate)) % _echoR.Length;
            float el = _echoL[tapL], er = _echoR[tapR];
            _echoL[ep] = sl + er * 0.38f;
            _echoR[ep] = sr + el * 0.38f;
            _echoPos = (ep + 1) % _echoL.Length;

            float outL = (_padLpL + (sl + el * 0.5f) * 0.12f) * _gain;
            float outR = (_padLpR + (sr + er * 0.5f) * 0.12f) * _gain;
            buf[n] = new Vector2(MathF.Tanh(outL), MathF.Tanh(outR));
        }
        _pb.PushBuffer(buf);

        _nextPluck -= (float)delta;
        if (_nextPluck <= 0f)
        {
            _nextPluck = 1.8f + Rnd() * 3.5f;
            var scale = night ? NightScale : DayScale;
            int start = (int)(Rnd() * (scale.Length - 2));
            int count = Rnd() < 0.35f ? 2 + (int)(Rnd() * 2f) : 1; // sometimes a little run of notes
            for (int k = 0; k < count; k++) _pending.Add((k * 0.22f, scale[Math.Min(scale.Length - 1, start + k)]));
        }
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var (at, midi) = _pending[i];
            at -= (float)delta;
            if (at <= 0f) { Strike(midi); _pending.RemoveAt(i); }
            else _pending[i] = (at, midi);
        }
    }

    private float Rnd() => (float)_rng.NextDouble();

    /// <summary>One pad chord sample (stereo: each note's two oscillators detuned left and right).</summary>
    private static (float l, float r) Pad(int[] chord, float[] phase)
    {
        float l = 0f, r = 0f;
        for (int i = 0; i < chord.Length; i++)
        {
            float f = Hz(chord[i]);
            phase[2 * i] = (phase[2 * i] + f * 0.998f / Rate) % 1f;
            phase[2 * i + 1] = (phase[2 * i + 1] + f * 1.002f / Rate) % 1f;
            // Sine plus a little 2nd harmonic: soft, slightly warm.
            float a = MathF.Sin(phase[2 * i] * MathF.Tau), b = MathF.Sin(phase[2 * i + 1] * MathF.Tau);
            l += a + 0.2f * MathF.Sin(phase[2 * i] * 2f * MathF.Tau);
            r += b + 0.2f * MathF.Sin(phase[2 * i + 1] * 2f * MathF.Tau);
        }
        return (l, r);
    }

    /// <summary>Plucks a free string at <paramref name="midi"/>.</summary>
    private void Strike(int midi)
    {
        for (int i = 0; i < _plucks.Length; i++)
        {
            ref var p = ref _plucks[i];
            if (p.On) continue;
            p.Len = Math.Clamp((int)(Rate / Hz(midi)), 8, p.Buf.Length);
            // A softened noise burst (a fingertip, not a pick).
            float lp = 0f;
            for (int k = 0; k < p.Len; k++) { lp += ((Rnd() * 2f - 1f) - lp) * 0.5f; p.Buf[k] = lp; }
            p.Pos = 0;
            p.Amp = 0.9f;
            p.Pan = Rnd() * 1.2f - 0.6f;
            p.On = true;
            return;
        }
    }
}
