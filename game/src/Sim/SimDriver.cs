using Godot;
using TheLedger.Boot;
using TheLedger.Controls;
using TheLedger.Sim.Core;
using TheLedger.Sim.Input;
using TheLedger.Sim.World;

namespace TheLedger.SimGlue;

/// <summary>
/// Owns the SimWorld and steps it at the fixed tick (docs/02_TECH_STACK.md §3.2). Normally ticks come from
/// _PhysicsProcess (120 Hz) and renderers interpolate with <see cref="Alpha"/>. In lockstep mode (screenshots,
/// smoke runs) ticks advance a fixed amount per rendered frame, so results don't depend on machine speed.
/// </summary>
public partial class SimDriver : Node
{
    private InputFrame[] _frames = Array.Empty<InputFrame>();
    private InputRecording? _replay;
    private InputRecording? _recording;
    private string? _recordPath;
    private bool _lockstep;
    private int _ticksPerFrame = 2;

    public SimWorld World { get; private set; } = null!;
    public SimConfig Config { get; private set; } = null!;

    /// <summary>Local input routers, one per local player. Remote/neutral anglers get empty frames.</summary>
    public List<InputRouter> Routers { get; } = new();

    /// <summary>Supplies the mouse position in world pixels (the view knows the camera).</summary>
    public Func<Vector2> MouseWorldPx { get; set; } = () => Vector2.Zero;

    /// <summary>Events seen since start, by type (for the overlay and smoke checks).</summary>
    public long[] EventCounts { get; } = new long[Enum.GetValues<SimEventType>().Length];

    /// <summary>Raised for every sim event as it's drained (on the main thread, inside the physics step).</summary>
    public event Action<SimEvent>? EventRaised;

    /// <summary>Unhandled exceptions caught while stepping (smoke runs fail on any).</summary>
    public int ExceptionCount { get; private set; }

    /// <summary>Interpolation factor between the previous and current tick for rendering.</summary>
    public float Alpha => _lockstep ? 1f : (float)Engine.GetPhysicsInterpolationFraction();

    private struct PendingSplash
    {
        public long Tick;
        public float X, Speed, Mass, Radius;
    }

    private readonly List<PendingSplash> _splashes = new();

    /// <summary>
    /// Debug/test splash applied at the start of <paramref name="atTick"/> (or the next tick if it's in the past).
    /// Not recorded in .rec files: it's a debug tool, not player input.
    /// </summary>
    public void QueueSplash(float x, float speed, float mass, float radius, long atTick = -1) =>
        _splashes.Add(new PendingSplash { Tick = atTick, X = x, Speed = speed, Mass = mass, Radius = radius });

    /// <summary>Longest single Step, in milliseconds (perf budget check).</summary>
    public double WorstStepMs { get; private set; }
    /// <summary>Mean Step time, in milliseconds.</summary>
    public double MeanStepMs => _steps > 0 ? _totalStepMs / _steps : 0;
    private double _totalStepMs;
    private long _steps;

    public void Setup(SimConfig config, CommandLine args)
    {
        Config = config;
        int anglers = args.Anglers;
        if (args.ReplayPath != null)
        {
            _replay = InputRecording.Load(ProjectSettings.GlobalizePath(args.ReplayPath));
            anglers = _replay.AnglerCount;
            GD.Print($"Replaying {args.ReplayPath}: {_replay.TickCount} ticks, {anglers} angler(s), seed {_replay.Seed}");
        }
        ulong seed = _replay?.Seed ?? args.Seed;
        World = new SimWorld(config, seed, anglers);
        PopulateFish(args);
        AllOutfits = LoadOutfits();
        AllLures = LoadTerminals();
        // Only what the angler owns (profile; the shop sells the rest). --outfit / --lure still pick anything.
        var profile = TheLedger.Ui.ProfileStore.Current;
        var outfits = AllOutfits.Where(o => o.Id == args.Outfit || profile.Outfits.Contains(o.Id)).ToList();
        if (outfits.Count == 0) outfits.Add(AllOutfits[0]);
        var lures = AllLures.Where(l => l.Id == args.Lure || profile.Lures.Contains(l.Id)).ToList();
        if (lures.Count == 0) lures.Add(AllLures[0]);
        foreach (var a in World.Anglers)
        {
            a.TackleBox.Clear();
            a.TackleBox.AddRange(lures);
            a.Rack.Clear();
            a.Rack.AddRange(outfits);
            if (outfits.Count > 0)
            {
                int o = Math.Max(0, outfits.FindIndex(x => x.Id == args.Outfit));
                if (args.Outfit != null && outfits[o].Id != args.Outfit) GD.PushWarning($"--outfit {args.Outfit}: no such outfit");
                a.UseOutfit(config, o);
            }
            int pick = Math.Max(0, lures.FindIndex(l => l.Id == args.Lure));
            if (args.Lure != null && lures[pick].Id != args.Lure) GD.PushWarning($"--lure {args.Lure}: no such lure");
            a.TackleIndex = pick;
            a.Rig.Line.SetTerminal(a.TackleBox[pick]);
        }
        _frames = new InputFrame[anglers];
        _lockstep = args.Lockstep;
        _ticksPerFrame = Math.Max(1, config.TickHz / 60);
        if (args.RecordPath != null)
        {
            _recordPath = args.RecordPath;
            _recording = new InputRecording { Seed = seed, AnglerCount = anglers, TickHz = config.TickHz, Setup = $"scene={args.Scene}" };
        }
    }

    /// <summary>Stocks the spot from data/spots/station_pond.json with the species in data/species (--no-fish skips).</summary>
    private void PopulateFish(CommandLine args)
    {
        if (args.NoFish) return;
        const string dir = "res://data/species";
        var files = DirAccess.GetFilesAt(dir).Where(f => f.EndsWith(".json")).OrderBy(f => f, StringComparer.Ordinal);
        var errors = new List<string>();
        var species = TheLedger.Sim.Fish.SpeciesLibrary.Load(
            files.Select(f => Godot.FileAccess.GetFileAsString($"{dir}/{f}")),
            r => Godot.FileAccess.FileExists($"{dir}/{r}") ? Godot.FileAccess.GetFileAsString($"{dir}/{r}") : null,
            errors);
        foreach (string e in errors) GD.Print($"Species: {e}");
        var stock = TheLedger.Sim.Fish.SpotStock.Load(Godot.FileAccess.GetFileAsString("res://data/spots/station_pond.json"));
        World.Populate(stock, species);
        GD.Print($"Station Pond: {World.Fish.Count} fish of {stock.Stock.Count} species");
    }

    /// <summary>Rod/reel/line outfits from data/gear/combos (in file-name order), resolved against rods, reels, lines.</summary>
    /// <summary>Every outfit and lure in the game data (the shop's catalogue).</summary>
    public List<TheLedger.Sim.Angler.Outfit> AllOutfits { get; private set; } = new();
    public List<TheLedger.Sim.Tackle.TerminalSpec> AllLures { get; private set; } = new();

    private static List<TheLedger.Sim.Angler.Outfit> LoadOutfits()
    {
        var list = new List<TheLedger.Sim.Angler.Outfit>();
        string Read(string dir, string id) => Godot.FileAccess.GetFileAsString($"res://data/gear/{dir}/{id}.json");
        const string combos = "res://data/gear/combos";
        foreach (string f in DirAccess.GetFilesAt(combos).Where(f => f.EndsWith(".json")).OrderBy(f => f, StringComparer.Ordinal))
        {
            try
            {
                var d = System.Text.Json.JsonDocument.Parse(Godot.FileAccess.GetFileAsString($"{combos}/{f}")).RootElement;
                list.Add(new TheLedger.Sim.Angler.Outfit
                {
                    Id = d.GetProperty("id").GetString() ?? f,
                    Name = d.GetProperty("name").GetString() ?? f,
                    Rod = TheLedger.Sim.Tackle.GearJson.Rod(Read("rods", d.GetProperty("rod").GetString()!)),
                    Reel = TheLedger.Sim.Tackle.GearJson.Reel(Read("reels", d.GetProperty("reel").GetString()!)),
                    Line = TheLedger.Sim.Tackle.GearJson.Line(Read("lines", d.GetProperty("line").GetString()!)),
                });
            }
            catch (Exception ex) { GD.PushError($"Outfit {f}: {ex.Message}"); }
        }
        return list;
    }

    /// <summary>Everything in data/lures, in file-name order (replays rely on the same order).</summary>
    private static List<TheLedger.Sim.Tackle.TerminalSpec> LoadTerminals()
    {
        var list = new List<TheLedger.Sim.Tackle.TerminalSpec>();
        const string dir = "res://data/lures";
        var files = DirAccess.GetFilesAt(dir).Where(f => f.EndsWith(".json")).OrderBy(f => f, StringComparer.Ordinal);
        foreach (string f in files)
        {
            try { list.Add(TheLedger.Sim.Tackle.GearJson.Terminal(Godot.FileAccess.GetFileAsString($"{dir}/{f}"))); }
            catch (Exception ex) { GD.PushError($"Lure {f}: {ex.Message}"); }
        }
        if (list.Count == 0) list.Add(TheLedger.Sim.Tackle.TerminalSpec.HalfOunceSinker());
        return list;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_lockstep) StepOnce();
    }

    /// <summary>Lockstep: advance a fixed number of ticks per rendered frame.</summary>
    public void StepFrame(int ticks)
    {
        for (int i = 0; i < ticks; i++) StepOnce();
    }

    public int TicksPerFrame => _ticksPerFrame;

    private void StepOnce()
    {
        float dt = Config.Dt;
        Vector2 mouse = MouseWorldPx();
        for (int a = 0; a < _frames.Length; a++)
        {
            if (_replay != null)
            {
                int tick = (int)World.Tick;
                _frames[a] = tick < _replay.TickCount ? _replay.TickFrames(tick)[a] : default;
            }
            else if (a < Routers.Count)
            {
                _frames[a] = Routers[a].BuildFrame(dt, mouse);
            }
            else
            {
                _frames[a] = default;
            }
        }

        for (int i = _splashes.Count - 1; i >= 0; i--)
        {
            var s = _splashes[i];
            if (s.Tick > World.Tick) continue;
            World.Splash(s.X, s.Speed, s.Mass, s.Radius);
            _splashes.RemoveAt(i);
        }

        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            World.Step(_frames);
        }
        catch (Exception ex)
        {
            ExceptionCount++;
            GD.PushError($"Sim step failed at tick {World.Tick}: {ex}");
        }
        double ms = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        if (ms > WorstStepMs && _steps > 120) WorstStepMs = ms; // skip JIT warm-up
        _totalStepMs += ms;
        _steps++;

        _recording?.AddTick(_frames);

        while (World.Events.TryPop(out var e))
        {
            EventCounts[(int)e.Type]++;
            EventRaised?.Invoke(e);
            if (e.Type == SimEventType.SimFault) GD.PushWarning($"SimFault: {e}");
        }
    }

    public override void _ExitTree()
    {
        if (_recording != null && _recordPath != null)
        {
            string path = ProjectSettings.GlobalizePath(_recordPath);
            _recording.Save(path);
            GD.Print($"Recorded {_recording.TickCount} ticks to {path}");
        }
    }
}
