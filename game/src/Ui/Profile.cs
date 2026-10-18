using System.Text.Json;
using Godot;

namespace TheLedger.Ui;

/// <summary>A kept fish waiting in the cooler to be sold (or not: game fish can't be).</summary>
public sealed class CoolerFish
{
    public string Species { get; set; } = "";
    public string CommonName { get; set; } = "";
    public float WeightKg { get; set; }
}

/// <summary>The angler's money, owned gear and cooler: user://profile.json, saved on every change.</summary>
public sealed class Profile
{
    public float Money { get; set; }
    public List<string> Lures { get; set; } = new();
    public List<string> Outfits { get; set; } = new();
    public List<CoolerFish> Cooler { get; set; } = new();
    /// <summary>Where the angler is fishing (data/spots id).</summary>
    public string Spot { get; set; } = "station_pond";
    /// <summary>The in-game calendar: game day (since the start; 7 per season, winter first) and hour, carried between sessions.</summary>
    public float Day { get; set; } = 8f;
    /// <summary>Story progress: current step, spots it has opened, and Wren's pages found (step ids).</summary>
    public int StoryStep { get; set; }
    public List<string> Unlocked { get; set; } = new();
    public List<string> Pages { get; set; } = new();
    /// <summary>Legendary fish landed (legend ids).</summary>
    public List<string> Legends { get; set; } = new();
    public float Hour { get; set; } = 7f;
}

/// <summary>Shop prices and the fish market (game/data/shop.json).</summary>
public sealed class ShopData
{
    public float StartMoney { get; set; }
    public List<string> StartLures { get; set; } = new();
    public List<string> StartOutfits { get; set; } = new();
    public Dictionary<string, float> Lures { get; set; } = new();
    public Dictionary<string, float> Outfits { get; set; } = new();
    public Dictionary<string, JsonElement> Market { get; set; } = new();

    /// <summary>US$ per kg, 0 when the species can't be sold.</summary>
    public float PricePerKg(string species) =>
        Market.TryGetValue(species, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : 0f;
}

public static class ProfileStore
{
    private const string Path = "user://profile.json";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static Profile? _p;
    private static ShopData? _shop;

    public static ShopData Shop => _shop ??=
        JsonSerializer.Deserialize<ShopData>(Godot.FileAccess.GetFileAsString("res://data/shop.json"), Json) ?? new ShopData();

    public static Profile Current => _p ??= Load();

    private static Profile Load()
    {
        try
        {
            if (Godot.FileAccess.FileExists(Path))
                return JsonSerializer.Deserialize<Profile>(Godot.FileAccess.GetFileAsString(Path), Json) ?? Fresh();
        }
        catch (Exception ex) { GD.PushWarning($"Profile: couldn't read {Path} ({ex.Message}); starting fresh"); }
        return Fresh();
    }

    private static Profile Fresh() => new()
    {
        Money = Shop.StartMoney, Lures = new List<string>(Shop.StartLures), Outfits = new List<string>(Shop.StartOutfits),
    };

    /// <summary>New game: forget the saved profile (the next read starts fresh).</summary>
    public static void Reset()
    {
        if (Godot.FileAccess.FileExists(Path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(Path));
        _p = null;
    }

    public static void Save()
    {
        using var f = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Write);
        if (f == null) { GD.PushWarning($"Profile: couldn't write {Path}"); return; }
        f.StoreString(JsonSerializer.Serialize(Current, Json));
    }

    /// <summary>What the cooler would fetch now, and how many fish in it can be sold.</summary>
    public static (float value, int sellable) CoolerValue()
    {
        float v = 0f; int n = 0;
        foreach (var c in Current.Cooler)
        {
            float p = Shop.PricePerKg(c.Species);
            if (p > 0f) { v += p * c.WeightKg; n++; }
        }
        return (MathF.Round(v, 2), n);
    }

    /// <summary>Sells every sellable fish; game fish stay out of it (they're eaten at home).</summary>
    public static float SellCooler()
    {
        var (v, _) = CoolerValue();
        Current.Money += v;
        Current.Cooler.Clear();
        Save();
        return v;
    }
}
