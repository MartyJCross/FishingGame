using Godot;
using TheLedger.Sim.Core;

namespace TheLedger.SimGlue;

/// <summary>
/// Loads and saves SimConfig tuning (CLAUDE.md rule 11). Running from source, saves go straight to
/// game/data/tuning/sim.json so they can be committed. In an exported build res:// is read-only, so saves go to
/// user://tuning/sim.json, which then overrides the shipped file.
/// </summary>
public static class TuningStore
{
    public const string ShippedPath = "res://data/tuning/sim.json";
    public const string UserPath = "user://tuning/sim.json";

    public static bool RunningFromSource => !OS.HasFeature("template");

    public static string SavePath => RunningFromSource ? ShippedPath : UserPath;

    public static SimConfig Load(List<string> warnings)
    {
        var config = new SimConfig();
        Apply(config, ShippedPath, warnings);
        if (!RunningFromSource) Apply(config, UserPath, warnings);
        return config;
    }

    public static void Reload(SimConfig config, List<string> warnings)
    {
        var defaults = new SimConfig();
        foreach (var t in SimConfig.Tunables) config.SetFromFloat(t, defaults.GetAsFloat(t));
        Apply(config, ShippedPath, warnings);
        if (!RunningFromSource) Apply(config, UserPath, warnings);
    }

    public static string Save(SimConfig config)
    {
        string path = SavePath;
        if (!RunningFromSource) DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("user://tuning"));
        using var f = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        if (f == null) return $"Could not write {path}: {Godot.FileAccess.GetOpenError()}";
        f.StoreString(config.ToJson());
        return $"Saved {ProjectSettings.GlobalizePath(path)}";
    }

    private static void Apply(SimConfig config, string path, List<string> warnings)
    {
        if (!Godot.FileAccess.FileExists(path)) return;
        string json = Godot.FileAccess.GetFileAsString(path);
        try
        {
            config.ApplyJson(json, warnings);
        }
        catch (System.Text.Json.JsonException ex)
        {
            warnings.Add($"{path}: {ex.Message}");
        }
    }
}
