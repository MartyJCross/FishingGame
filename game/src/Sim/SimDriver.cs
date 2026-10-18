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
        Info = SpotInfo.Load(SpotId);
        World = new SimWorld(config, seed, anglers, Info.Spot);
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

    /// <summary>Legendary fish in the water this session, by fish id (name, and the note about them).</summary>
    public Dictionary<int, (string id, string name, string story)> Legends { get; } = new();

    /// <summary>
    /// Named giants from the spot data (docs/01 §7): one fish of a fixed size, present only in its seasons, starting in
    /// the cover it's known for. It's an ordinary fish of its species in every other way.
    /// </summary>
    private void AddLegends(IReadOnlyList<TheLedger.Sim.Fish.SpeciesSpec> species)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(Info.Json);
        if (!doc.RootElement.TryGetProperty("legends", out var list)) return;
        int id = 1000;
        foreach (var l in list.EnumerateArray())
        {
            var seasons = l.GetProperty("seasons").EnumerateArray().Select(s => s.GetString()).ToList();
            if (!seasons.Contains(World.Season)) continue;
            var sp = species.FirstOrDefault(s => s.Id == l.GetProperty("species").GetString());
            if (sp == null) continue;
            string near = l.GetProperty("near").GetString()!;
            var zone = World.Spot.Structures.FirstOrDefault(z => z.Kind == near);
            float x = zone != null ? 0.5f * (zone.X0 + zone.X1) : 10f;
            float y = zone != null ? 0.5f * (zone.Y0 + zone.Y1) : World.Spot.GroundHeight(x) + 0.5f;
            World.AddFish(new TheLedger.Sim.Fish.Fish(id, sp, l.GetProperty("lengthCm").GetSingle() * 0.01f, new Vec2(x, y), false, World.Seed + 4242UL));
            Legends[id] = (l.GetProperty("id").GetString()!, l.GetProperty("name").GetString()!, l.GetProperty("story").GetString()!);
            id++;
        }
    }

    /// <summary>Which spot to load (data/spots/{id}.json); set before Setup. Automated runs keep the station pond.</summary>
    public string SpotId { get; set; } = "station_pond";

    /// <summary>The loaded spot: its sim cross-section and its look.</summary>
    public SpotInfo Info { get; private set; } = null!;

    /// <summary>Stocks the spot from its data file with the species in data/species (--no-fish skips).</summary>
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
        Species = species;
        var stock = TheLedger.Sim.Fish.SpotStock.Load(Info.Json);
        World.Populate(stock, species);
        AddLegends(species);
        GD.Print($"{Info.Name}: {World.Fish.Count} fish of {stock.Stock.Count} species");
    }

    /// <summary>Test scenes: a 2 kg bass already hooked 8 m out (for screenshots of the fight panel).</summary>
    public void DebugHookBass()
    {
        var sp = Species.FirstOrDefault(s => s.Id == "florida_bass");
        if (sp == null) return;
        float lenCm = MathF.Pow(2000f / sp.Size.LengthWeight.A, 1f / sp.Size.LengthWeight.B);
        var fish = new TheLedger.Sim.Fish.Fish(999, sp, lenCm * 0.01f, new Vec2(8f, -1.5f), true, 99);
        World.AddFish(fish);
        var a = World.Anglers[0];
        var tip = a.Rig.Rod.TipPos;
        var to = fish.Mouth - tip;
        a.Rig.Line.Reset(tip, to.Normalized(), to.Length);
        fish.SetHooked(0, a.Rig.Line.Terminal);
    }

    /// <summary>Every species in the game data (for the guide and checklists).</summary>
    public IReadOnlyList<TheLedger.Sim.Fish.SpeciesSpec> Species { get; private set; } = Array.Empty<TheLedger.Sim.Fish.SpeciesSpec>();

    /// <summary>Every outfit and lure in the game data (the shop's catalogue).</summary>
    public List<TheLedger.Sim.Angler.Outfit> AllOutfits { get; private set; } = new();
    public List<TheLedger.Sim.Tackle.TerminalSpec> AllLures { get; private set; } = new();

    /// <summary>Rod/reel/line outfits from data/gear/combos (in file-name order), resolved against rods, reels, lines.</summary>

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

/// <summary>A fishing spot from game/data/spots/{id}.json: name, travel cost, its cross-section for the sim and its look.</summary>
public sealed class SpotInfo
{
    public string Id = "", Name = "", Place = "", Scenery = "pond", Json = "";
    public float TravelCost;
    public Godot.Color WaterShallow = new(0.26f, 0.60f, 0.64f), WaterDeep = new(0.07f, 0.27f, 0.36f);
    public TheLedger.Sim.World.Spot Spot = null!;

    public static SpotInfo Load(string id)
    {
        string path = $"res://data/spots/{id}.json";
        if (!Godot.FileAccess.FileExists(path)) { GD.PushWarning($"No spot {id}; using the station pond"); path = "res://data/spots/station_pond.json"; }
        string json = Godot.FileAccess.GetFileAsString(path);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var r = doc.RootElement;
        static Vec2 V(System.Text.Json.JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle());
        static Godot.Color C(System.Text.Json.JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle(), e[2].GetSingle());
        var info = new SpotInfo
        {
            Id = r.GetProperty("id").GetString()!, Name = r.GetProperty("name").GetString()!, Place = r.GetProperty("place").GetString()!,
            Scenery = r.GetProperty("scenery").GetString()!, TravelCost = r.GetProperty("travelCost").GetSingle(), Json = json,
            WaterShallow = C(r.GetProperty("waterShallow")), WaterDeep = C(r.GetProperty("waterDeep")),
        };
        var profile = r.GetProperty("profile").EnumerateArray().Select(V).ToList();
        info.Spot = new TheLedger.Sim.World.Spot(info.Id, profile)
        {
            AnglerStart = V(r.GetProperty("anglerStart")), WaterlineX = r.GetProperty("waterlineX").GetSingle(),
            SpringC = r.TryGetProperty("springC", out var sc) ? sc.GetSingle() : 22.5f,
            SpringMix = r.TryGetProperty("springMix", out var sm) ? sm.GetSingle() : 0f,
            Structures = r.TryGetProperty("structures", out var st)
                ? st.EnumerateArray().Select(z => new TheLedger.Sim.World.StructureZone
                {
                    Kind = z.GetProperty("kind").GetString()!, X0 = z.GetProperty("x")[0].GetSingle(), X1 = z.GetProperty("x")[1].GetSingle(),
                    Y0 = z.GetProperty("y")[0].GetSingle(), Y1 = z.GetProperty("y")[1].GetSingle(), Snag = z.GetProperty("snag").GetSingle(),
                    HoldMinN = z.GetProperty("holdN")[0].GetSingle(), HoldMaxN = z.GetProperty("holdN")[1].GetSingle(),
                }).ToList()
                : new List<TheLedger.Sim.World.StructureZone>(),
        };
        return info;
    }

    /// <summary>All spots in data/spots, home first.</summary>
    public static List<SpotInfo> All() => DirAccess.GetFilesAt("res://data/spots").Where(f => f.EndsWith(".json"))
        .Select(f => Load(f[..^5])).OrderBy(s => s.TravelCost).ToList();
}
