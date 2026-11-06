using Godot;
using TheLedger.Sim.Core;
using TheLedger.SimGlue;

namespace TheLedger.Audio;

/// <summary>
/// Audio pass 2 (2026-11-06): real CC0 field recordings (game/assets/audio, sources in SOURCES.md) under the
/// synthesised tackle sounds. Looping beds crossfade with the hour, the weather and the spot: day birds, the dawn
/// chorus, night insects and frogs, water lapping, wind, light and heavy rain, a river's flow. One-shots: splashes sized
/// by the sim's impact energy, fish jumps, thunder after lightning, the odd heron or owl, bullfrogs at night, Skiff's
/// bark. Files are found by name prefix (ambience/day_*.mp3 etc.), so adding a recording needs no code. Whatever is
/// covered here, <see cref="SoundEngine"/> stops synthesising (its fallbacks stay for anything missing).
/// </summary>
public partial class Ambience : Node
{
    public SimDriver Driver { get; set; } = null!;

    private const string Root = "res://assets/audio";

    /// <summary>Which sounds the recordings cover this scene (SoundEngine skips its synthesised versions).</summary>
    public static bool HasBeds, HasRain, HasSplash, HasThunder, HasBark, HasCritters;

    /// <summary>A looping recording that crossfades into itself at the end, so the loop point never clicks.</summary>
    private sealed class Bed
    {
        public string Name = "";
        public List<AudioStream> Streams = new();
        public AudioStreamPlayer A = null!, B = null!;
        public bool OnA = true;
        public float Gain, Target;
        public double Length, OldLength;
        public int Index;
        public bool FadingIn;
    }

    private readonly Dictionary<string, Bed> _beds = new();
    private readonly Dictionary<string, List<AudioStream>> _shots = new();
    private readonly List<AudioStreamPlayer> _pool = new();
    private readonly Random _rng = new(23);
    private float _nextCall = 20f, _nextFrog = 6f, _thunderIn = -1f, _thunderDist;
    private const float Fade = 3f;

    public override void _Ready()
    {
        foreach (var name in new[] { "day", "dawn", "night", "lap", "wind", "rain_light", "rain_heavy", "river" })
        {
            var list = Load("ambience", name);
            if (list.Count == 0) continue;
            var bed = new Bed { Name = name, Streams = list, Index = _rng.Next(list.Count) };
            bed.A = MakePlayer(); bed.B = MakePlayer();
            _beds[name] = bed;
        }
        foreach (var name in new[] { "splash_small", "splash_big", "fish_jump", "thunder", "bark", "bird", "owl", "bullfrog" })
        {
            var list = Load("sfx", name);
            if (list.Count > 0) _shots[name] = list;
        }
        for (int i = 0; i < 10; i++) _pool.Add(MakePlayer());

        HasBeds = _beds.ContainsKey("lap") || _beds.ContainsKey("day") || _beds.ContainsKey("night");
        HasRain = _beds.ContainsKey("rain_light") || _beds.ContainsKey("rain_heavy");
        HasSplash = _shots.ContainsKey("splash_small");
        HasThunder = _shots.ContainsKey("thunder");
        HasBark = _shots.ContainsKey("bark");
        HasCritters = _beds.ContainsKey("day") && _beds.ContainsKey("night");

        Driver.EventRaised += OnEvent;
        TheLedger.Render.Atmosphere.Lightning += OnLightning;
        TheLedger.Render.TestView.Barked += OnBark;
    }

    public override void _ExitTree()
    {
        Driver.EventRaised -= OnEvent;
        TheLedger.Render.Atmosphere.Lightning -= OnLightning;
        TheLedger.Render.TestView.Barked -= OnBark;
        HasBeds = HasRain = HasSplash = HasThunder = HasBark = HasCritters = false;
    }

    private AudioStreamPlayer MakePlayer()
    {
        var p = new AudioStreamPlayer { VolumeDb = -80f };
        AddChild(p);
        return p;
    }

    /// <summary>
    /// Every recording in a folder whose name starts with the prefix. Exported builds only list the .import stubs,
    /// so those are mapped back to the source name ResourceLoader expects.
    /// </summary>
    public static List<AudioStream> Load(string dir, string prefix)
    {
        var list = new List<AudioStream>();
        string path = $"{Root}/{dir}";
        if (!DirAccess.DirExistsAbsolute(path)) return list;
        var names = DirAccess.GetFilesAt(path)
            .Select(f => f.EndsWith(".import") ? f[..^7] : f.EndsWith(".remap") ? f[..^6] : f)
            .Where(f => (f.EndsWith(".ogg") || f.EndsWith(".mp3") || f.EndsWith(".wav")) && (prefix == "" || f.StartsWith(prefix + "_")))
            .Distinct().OrderBy(f => f, StringComparer.Ordinal);
        foreach (var f in names)
            if (ResourceLoader.Load<AudioStream>($"{path}/{f}") is { } s)
            {
                switch (s)
                {
                    case AudioStreamMP3 m: m.Loop = false; break;
                    case AudioStreamOggVorbis o: o.Loop = false; break;
                    case AudioStreamWav w: w.LoopMode = AudioStreamWav.LoopModeEnum.Disabled; break;
                }
                list.Add(s);
            }
        return list;
    }

    private float Rnd() => (float)_rng.NextDouble();

    private void Shot(string name, float gain, float pitch = 1f)
    {
        if (!_shots.TryGetValue(name, out var list) || gain < 0.01f) return;
        var p = _pool.FirstOrDefault(x => !x.Playing) ?? _pool[0];
        p.Stream = list[_rng.Next(list.Count)];
        p.VolumeDb = Mathf.LinearToDb(gain);
        p.PitchScale = pitch;
        p.Play();
    }

    private float Gain(Vec2 at)
    {
        float dx = at.X - Driver.World.Anglers[0].Position.X;
        return 1f / (1f + MathF.Abs(dx) / 10f);
    }

    private void OnEvent(SimEvent e)
    {
        if (e.Type == SimEventType.Splash)
        {
            float size = MathF.Min(1f, MathF.Sqrt(MathF.Max(e.Value, 0f)) * 1.2f);
            float g = Gain(e.Position);
            if (e.Id == 3) Shot(_shots.ContainsKey("fish_jump") ? "fish_jump" : "splash_big", (0.5f + 0.5f * size) * g, 0.9f + 0.2f * Rnd());
            else if (e.Id == 0)
            {
                if (size > 0.45f && _shots.ContainsKey("splash_big")) Shot("splash_big", (0.3f + 0.6f * size) * g, 0.95f + 0.15f * Rnd());
                // Smaller things plop higher: a sinker sounds lighter than a crankbait.
                else Shot("splash_small", (0.25f + 0.6f * size) * g, 1.25f - 0.4f * size + 0.1f * Rnd());
            }
        }
        else if (e.Type == SimEventType.FishLanded) Shot("splash_small", 0.5f, 0.75f);
    }

    private void OnLightning(float distance)
    {
        _thunderDist = distance;
        _thunderIn = 0.15f + distance * 3.5f;
    }

    private void OnBark() => Shot("bark", 0.45f, 0.95f + 0.1f * Rnd());

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        var w = Driver.World;
        float hour = w.Hour;
        float day = hour < 5.5f || hour > 20.5f ? 0f : hour < 7.5f ? (hour - 5.5f) / 2f : hour > 18.5f ? (20.5f - hour) / 2f : 1f;
        float dawn = MathF.Max(0f, 1f - MathF.Abs(hour - 6.8f) / 1.4f);
        float wind = MathF.Abs(w.Wind.X);
        float rain = Driver.Config.DebugRainRate;
        float wet = MathF.Min(1f, rain / 6f);
        string scenery = Driver.Info.Scenery;

        SetTarget("day", 0.55f * day * (1f - 0.7f * wet) * (1f - 0.6f * dawn));
        SetTarget("dawn", 0.6f * dawn * (1f - 0.7f * wet));
        SetTarget("night", 0.5f * (1f - day) * (1f - 0.5f * wet));
        SetTarget("lap", 0.35f + 0.04f * MathF.Min(wind, 10f));
        SetTarget("wind", MathF.Min(0.5f, MathF.Max(0f, wind - 2f) * 0.06f));
        SetTarget("rain_light", 0.6f * MathF.Min(1f, rain / 4f) * (1f - MathF.Min(1f, MathF.Max(0f, rain - 10f) / 15f)));
        SetTarget("rain_heavy", 0.7f * MathF.Min(1f, MathF.Max(0f, rain - 8f) / 15f));
        SetTarget("river", scenery == "river" ? 0.45f : scenery == "springs" ? 0.25f : 0f);
        foreach (var b in _beds.Values) StepBed(b, dt);

        // Now and then a call out of the trees: heron or kingfisher by day, an owl at night; bullfrogs after dark.
        _nextCall -= dt;
        if (_nextCall <= 0f)
        {
            if (day > 0.5f && wet < 0.5f) Shot("bird", 0.25f + 0.2f * Rnd(), 0.95f + 0.1f * Rnd());
            else if (day < 0.3f) Shot("owl", 0.2f + 0.15f * Rnd());
            _nextCall = 25f + 50f * Rnd();
        }
        _nextFrog -= dt;
        if (_nextFrog <= 0f)
        {
            if (day < 0.4f && scenery != "springs") Shot("bullfrog", 0.15f + 0.2f * Rnd(), 0.9f + 0.2f * Rnd());
            _nextFrog = 5f + 14f * Rnd();
        }
        if (_thunderIn >= 0f)
        {
            _thunderIn -= dt;
            if (_thunderIn < 0f) Shot("thunder", 1f - 0.55f * _thunderDist, 1.05f - 0.25f * _thunderDist);
        }
    }

    private void SetTarget(string name, float target)
    {
        if (_beds.TryGetValue(name, out var b)) b.Target = target;
    }

    /// <summary>Eases the bed's level to its target, starts it when it becomes audible, and loops by crossfading.</summary>
    private void StepBed(Bed b, float dt)
    {
        b.Gain += (b.Target - b.Gain) * (1f - MathF.Exp(-dt / 2.5f));
        var cur = b.OnA ? b.A : b.B;
        var old = b.OnA ? b.B : b.A;
        if (b.Gain < 0.005f && b.Target < 0.005f)
        {
            cur.Stop();
            old.Stop();
            return;
        }
        if (!cur.Playing)
        {
            // First start: somewhere in the middle, so every session doesn't open on the same bird. The bed's own
            // gain eases in from silence, so no extra fade.
            var s0 = b.Streams[b.Index];
            cur.Stream = s0;
            b.Length = s0.GetLength();
            cur.Play((float)(b.Length * 0.6 * Rnd()));
            b.FadingIn = false;
        }
        else if (b.Length > Fade * 2.5 && cur.GetPlaybackPosition() > b.Length - Fade)
        {
            // Near the end: the next recording (or the same one) starts on the other player and they crossfade.
            b.Index = (b.Index + 1) % b.Streams.Count;
            var next = b.Streams[b.Index];
            old.Stream = next;
            old.Play(0f);
            b.OldLength = b.Length;
            b.Length = next.GetLength();
            b.OnA = !b.OnA;
            (cur, old) = (old, cur);
            b.FadingIn = true;
        }
        float inF = b.FadingIn ? Math.Clamp(cur.GetPlaybackPosition() / Fade, 0f, 1f) : 1f;
        if (inF >= 1f) b.FadingIn = false;
        cur.VolumeDb = Mathf.LinearToDb(MathF.Max(1e-4f, b.Gain * inF));
        if (old.Playing)
            old.VolumeDb = Mathf.LinearToDb(MathF.Max(1e-4f, b.Gain * Math.Clamp((float)(b.OldLength - old.GetPlaybackPosition()) / Fade, 0f, 1f)));
    }
}
