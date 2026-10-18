using System.Globalization;

namespace TheLedger.Boot;

/// <summary>
/// Arguments after "--" on the Godot command line (CLAUDE.md §5). Example:
/// godot --path game -- --light --screenshot out.png --frames 120 --debug-overlay
/// </summary>
public sealed class CommandLine
{
    /// <summary>Quiet and light for automated runs: audio muted, 30 FPS cap.</summary>
    public bool Light;
    /// <summary>Save a PNG to this path after <see cref="Frames"/> sim-locked frames, then quit.</summary>
    public string? ScreenshotPath;
    public int Frames = 60;
    public string Scene = "test";
    public float? TimeOfDay;
    public string? Weather;
    public string? Season;
    public ulong Seed = 1;
    public string? ReplayPath;
    public string? RecordPath;
    /// <summary>Headless smoke run: simulate this many seconds as fast as possible, then exit 0 (ok) or 1.</summary>
    public float? SmokeSeconds;
    public float? BenchSeconds;
    public bool DebugOverlay;
    public int Anglers = 1;
    /// <summary>Frame-rate cap (0 = uncapped). Default: uncapped with mailbox vsync (project.godot).</summary>
    public int? MaxFps;
    /// <summary>Start with this lure/rig tied on (id from data/lures).</summary>
    public string? Lure;
    /// <summary>Start with an empty pond (tackle and water work).</summary>
    public bool NoFish;
    /// <summary>Debug camera: centre x, y (m) and zoom, e.g. --look 6,-1,4 (screenshots of small things).</summary>
    public float[]? Look;

    public List<string> Errors { get; } = new();

    /// <summary>Sim ticks advance in lockstep with rendered frames (screenshots, smoke) instead of real time.</summary>
    public bool Lockstep => ScreenshotPath != null || SmokeSeconds != null;

    public static CommandLine Parse(IReadOnlyList<string> args)
    {
        var c = new CommandLine();
        for (int i = 0; i < args.Count; i++)
        {
            string a = args[i];
            string? Next()
            {
                if (i + 1 < args.Count) return args[++i];
                c.Errors.Add($"{a} needs a value");
                return null;
            }
            float? NextFloat()
            {
                string? s = Next();
                if (s == null) return null;
                if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return f;
                c.Errors.Add($"{a}: '{s}' is not a number");
                return null;
            }

            switch (a)
            {
                case "--light": c.Light = true; break;
                case "--screenshot": c.ScreenshotPath = Next(); break;
                case "--frames": c.Frames = (int)(NextFloat() ?? c.Frames); break;
                case "--scene": c.Scene = Next() ?? c.Scene; break;
                case "--time": c.TimeOfDay = NextFloat(); break;
                case "--weather": c.Weather = Next(); break;
                case "--season": c.Season = Next(); break;
                case "--seed": c.Seed = (ulong)(NextFloat() ?? 1); break;
                case "--replay": c.ReplayPath = Next(); break;
                case "--record": c.RecordPath = Next(); break;
                case "--smoke": c.SmokeSeconds = NextFloat(); break;
                case "--bench": c.BenchSeconds = NextFloat(); break;
                case "--debug-overlay": c.DebugOverlay = true; break;
                case "--lure": c.Lure = Next(); break;
                case "--no-fish": c.NoFish = true; break;
                case "--look":
                {
                    var parts = (Next() ?? "").Split(',');
                    var vals = parts.Select(p => float.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : float.NaN).ToArray();
                    if (vals.Length == 3 && vals.All(float.IsFinite)) c.Look = vals;
                    else c.Errors.Add("--look needs x,y,zoom");
                    break;
                }
                case "--max-fps": c.MaxFps = Math.Max(0, (int)(NextFloat() ?? 0)); break;
                case "--anglers": c.Anglers = Math.Clamp((int)(NextFloat() ?? 1), 1, 4); break;
                default: c.Errors.Add($"Unknown argument '{a}'"); break;
            }
        }
        return c;
    }
}
