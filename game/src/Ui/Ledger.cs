using System.Text.Json;
using Godot;

namespace TheLedger.Ui;

/// <summary>One fish in the Ledger (the angler's lifetime catch log; the full Ledger with research notes is M7+).</summary>
public sealed class LedgerEntry
{
    public string Species { get; set; } = "";
    public string CommonName { get; set; } = "";
    public float LengthCm { get; set; }
    public float WeightKg { get; set; }
    public bool Kept { get; set; }
    public string Lure { get; set; } = "";
    public string Spot { get; set; } = "station_pond";
    /// <summary>The legendary fish's name, if this was one.</summary>
    public string Legend { get; set; } = "";
    public float GameHour { get; set; }
    public string When { get; set; } = "";
}

/// <summary>
/// The Ledger on disk: user://ledger.json, appended after every keep or release so a crash never loses a catch.
/// Godot puts user:// in %APPDATA%\Godot\app_userdata\The Ledger.
/// </summary>
public static class LedgerStore
{
    private const string Path = "user://ledger.json";
    private static List<LedgerEntry>? _entries;

    public static IReadOnlyList<LedgerEntry> Entries => _entries ??= Load();

    private static List<LedgerEntry> Load()
    {
        try
        {
            if (!Godot.FileAccess.FileExists(Path)) return new List<LedgerEntry>();
            return JsonSerializer.Deserialize<List<LedgerEntry>>(Godot.FileAccess.GetFileAsString(Path)) ?? new List<LedgerEntry>();
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Ledger: couldn't read {Path} ({ex.Message}); starting a new one");
            return new List<LedgerEntry>();
        }
    }

    public static void Add(LedgerEntry e)
    {
        _entries ??= Load();
        _entries.Add(e);
        using var f = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Write);
        if (f == null) { GD.PushWarning($"Ledger: couldn't write {Path}"); return; }
        f.StoreString(JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true }));
    }
}

/// <summary>The Ledger screen (L / Start): records per species and the latest catches.</summary>
public partial class LedgerPanel : PanelContainer
{
    private RichTextLabel _text = null!;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorLeft = 0.5f; AnchorRight = 0.5f; AnchorTop = 0.5f; AnchorBottom = 0.5f;
        OffsetLeft = -460f; OffsetRight = 460f; OffsetTop = -330f; OffsetBottom = 330f;
        var sb = new StyleBoxFlat { BgColor = new Color(0.07f, 0.06f, 0.05f, 0.92f) };
        sb.SetCornerRadiusAll(12);
        sb.SetContentMarginAll(24);
        sb.BorderColor = new Color(0.85f, 0.75f, 0.55f, 0.5f);
        sb.SetBorderWidthAll(2);
        AddThemeStyleboxOverride("panel", sb);
        _text = new RichTextLabel { BbcodeEnabled = true, ScrollActive = true, MouseFilter = MouseFilterEnum.Ignore };
        _text.AddThemeFontSizeOverride("normal_font_size", 20);
        _text.AddThemeFontSizeOverride("bold_font_size", 20);
        _text.AddThemeColorOverride("default_color", new Color(0.95f, 0.9f, 0.8f));
        AddChild(_text);
        Visible = false;
    }

    public void Toggle()
    {
        Visible = !Visible;
        if (Visible) Refresh();
    }

    public void Refresh()
    {
        var all = LedgerStore.Entries;
        var sb = new System.Text.StringBuilder();
        sb.Append("[font_size=32][b]The Ledger[/b][/font_size]\n");
        sb.Append($"[color=#b8a888]{all.Count} fish logged, {all.Count(e => e.Kept)} kept.   L / Start to close[/color]\n\n");
        if (all.Count == 0) sb.Append("No fish yet. Every fish you land is written here, kept or released.\n");
        else
        {
            sb.Append("[b]Species                      caught   longest       heaviest[/b]\n");
            foreach (var g in all.GroupBy(e => e.CommonName).OrderByDescending(g => g.Count()))
            {
                var longest = g.Max(e => e.LengthCm);
                var heaviest = g.Max(e => e.WeightKg);
                sb.Append($"{g.Key,-28} {g.Count(),6}   {longest,6:0.0} cm   {Weight(heaviest),12}\n");
            }
            sb.Append("\n[b]Latest[/b]\n");
            foreach (var e in all.Reverse().Take(10))
                sb.Append($"{e.When,-17} {e.CommonName,-18} {e.LengthCm,5:0.0} cm  {Weight(e.WeightKg),10}  {(e.Kept ? "kept" : "released"),-8} {e.Lure}\n");
        }
        _text.Text = sb.ToString();
    }

    public static string Weight(float kg) => kg < 1f ? $"{kg * 1000f:0} g" : $"{kg:0.00} kg";
}
