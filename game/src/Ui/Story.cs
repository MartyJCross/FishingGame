using System.Text.Json;
using Godot;

namespace TheLedger.Ui;

public sealed class StoryGoal
{
    public string Type { get; set; } = "none";
    public string Spot { get; set; } = "";
    public int Count { get; set; }
    public List<string> Species { get; set; } = new();
    public float Amount { get; set; }
    public string Button { get; set; } = "";
}

public sealed class StoryPage
{
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class StoryStep
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Giver { get; set; } = "";
    public string Intro { get; set; } = "";
    public StoryGoal Goal { get; set; } = new();
    public string Done { get; set; } = "";
    public List<string> Unlocks { get; set; } = new();
    public StoryPage? Page { get; set; }
}

/// <summary>
/// The story (game/data/story.json): steps in order, each with a goal. Progress is read from the Ledger (catches are
/// already saved there) and the profile (story step, unlocked spots, money), so nothing extra can get out of sync.
/// </summary>
public static class Story
{
    private sealed class File { public List<StoryStep> Steps { get; set; } = new(); }

    private static List<StoryStep>? _steps;
    public static List<StoryStep> Steps => _steps ??= JsonSerializer.Deserialize<File>(
        Godot.FileAccess.GetFileAsString("res://data/story.json"), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.Steps;

    public static StoryStep Current => Steps[Math.Clamp(ProfileStore.Current.StoryStep, 0, Steps.Count - 1)];

    /// <summary>Progress on the current goal: (done so far, needed, lines for the checklist).</summary>
    public static (int have, int need, List<(string text, bool done)> items) Progress()
    {
        var g = Current.Goal;
        var items = new List<(string, bool)>();
        var ledger = LedgerStore.Entries;
        switch (g.Type)
        {
            case "releaseSpecies":
            {
                var names = ledger.Where(e => !e.Kept && e.Spot == g.Spot).GroupBy(e => e.Species).Select(x => x.First().CommonName).ToList();
                foreach (var n in names) items.Add(($"{n} released", true));
                return (Math.Min(names.Count, g.Count), g.Count, items);
            }
            case "catchSpecies":
            {
                int have = 0;
                foreach (var s in g.Species)
                {
                    var hit = ledger.FirstOrDefault(e => e.Species == s && e.Spot == g.Spot);
                    string name = hit?.CommonName ?? s.Replace('_', ' ');
                    items.Add((name, hit != null));
                    if (hit != null) have++;
                }
                return (have, g.Species.Count, items);
            }
            case "pay":
                return (0, 1, items);
            default:
                return (0, 0, items);
        }
    }

    public static bool CurrentComplete
    {
        get
        {
            var (have, need, _) = Progress();
            return Current.Goal.Type is "releaseSpecies" or "catchSpecies" && have >= need;
        }
    }

    /// <summary>Finishes the current step (unlocks, Wren's page) and moves on. Returns the finished step.</summary>
    public static StoryStep Advance()
    {
        var p = ProfileStore.Current;
        var step = Current;
        foreach (var u in step.Unlocks) if (!p.Unlocked.Contains(u)) p.Unlocked.Add(u);
        if (step.Page != null && !p.Pages.Contains(step.Id)) p.Pages.Add(step.Id);
        if (p.StoryStep < Steps.Count - 1) p.StoryStep++;
        ProfileStore.Save();
        return step;
    }

    /// <summary>Spots open from the start; the rest come from story unlocks.</summary>
    public static bool SpotOpen(string id) => id == "station_pond" || ProfileStore.Current.Unlocked.Contains(id);
}

/// <summary>Field notes (Tab menu): the current task, its checklist, and Wren's pages found so far.</summary>
public partial class StoryPanel : PanelContainer
{
    public TheLedger.SimGlue.SimDriver Driver { get; set; } = null!;

    /// <summary>Called when a step finishes from inside the panel (paying), so Main can show the moment.</summary>
    public Action<StoryStep> Finished { get; set; } = _ => { };

    private VBoxContainer _box = null!;

    public override void _Ready()
    {
        var sb = new StyleBoxFlat { BgColor = new Color(0.93f, 0.89f, 0.78f, 0.97f) };
        sb.SetCornerRadiusAll(6);
        sb.SetContentMarginAll(22);
        AddThemeStyleboxOverride("panel", sb);
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(640, 520), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        AddChild(scroll);
        _box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _box.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_box);
        Visible = false;
    }

    private static readonly Color Ink = new(0.18f, 0.14f, 0.1f);

    private Label Text(string text, int size, Color? col = null, bool italic = false)
    {
        var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(600, 0) };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", col ?? Ink);
        if (italic) l.Modulate = new Color(1f, 1f, 1f, 0.92f);
        _box.AddChild(l);
        return l;
    }

    public void Open()
    {
        Visible = true;
        foreach (var c in _box.GetChildren()) c.QueueFree();
        var step = Story.Current;
        Text("Field notes", 28);
        Text(step.Title, 22, new Color(0.45f, 0.2f, 0.1f));
        Text($"{step.Giver}: \"{step.Intro}\"", 17);
        var (have, need, _) = Story.Progress();
        if (need > 0 && step.Goal.Type != "pay") Text($"Progress: {have} / {need}", 18, new Color(0.15f, 0.35f, 0.2f));
        Checklist(step.Goal);
        if (step.Goal.Type == "pay")
        {
            var p = ProfileStore.Current;
            var b = new Button { Text = step.Goal.Button, Disabled = p.Money < step.Goal.Amount, CustomMinimumSize = new Vector2(0, 40), FocusMode = FocusModeEnum.None };
            b.Pressed += () =>
            {
                p.Money -= step.Goal.Amount;
                ProfileStore.Save();
                Finished(Story.Advance());
                Open();
            };
            _box.AddChild(b);
            if (p.Money < step.Goal.Amount) Text($"You have ${p.Money:0.00}. Sell some fish at the shop.", 15);
        }

        var pages = ProfileStore.Current.Pages;
        if (pages.Count > 0)
        {
            Text("", 8);
            Text("Wren's pages", 22, new Color(0.45f, 0.2f, 0.1f));
            foreach (var s in Story.Steps.Where(s => s.Page != null && pages.Contains(s.Id)))
            {
                Text(s.Page!.Title, 17, new Color(0.3f, 0.22f, 0.15f));
                Text(s.Page.Text, 16, new Color(0.25f, 0.2f, 0.32f), italic: true);
            }
        }
        var close = new Button { Text = "Close", CustomMinimumSize = new Vector2(0, 36), FocusMode = FocusModeEnum.None };
        close.Pressed += () => Visible = false;
        _box.AddChild(close);
    }

    /// <summary>The fish this goal is about, each with its picture: in colour with a tick once done, grey until then.</summary>
    private void Checklist(StoryGoal g)
    {
        if (g.Type is not ("releaseSpecies" or "catchSpecies")) return;
        var ids = g.Type == "catchSpecies" ? g.Species
            : TheLedger.Sim.Fish.SpotStock.Load(TheLedger.SimGlue.SpotInfo.Load(g.Spot).Json).Stock.Select(e => e.Species).Distinct().ToList();
        string verb = g.Type == "releaseSpecies" ? "released" : "caught";
        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 18);
        grid.AddThemeConstantOverride("v_separation", 6);
        _box.AddChild(grid);
        foreach (var id in ids)
        {
            var sp = Driver.Species.FirstOrDefault(s => s.Id == id);
            if (sp == null) continue;
            bool done = LedgerStore.Entries.Any(e => e.Species == id && e.Spot == g.Spot && (g.Type == "catchSpecies" || !e.Kept));
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(new FishPortrait { Species = sp, Faded = !done, CustomMinimumSize = new Vector2(130, 56) });
            var col = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
            var name = new Label { Text = sp.CommonName };
            name.AddThemeFontSizeOverride("font_size", 17);
            name.AddThemeColorOverride("font_color", Ink);
            var state = new Label { Text = done ? $"✓ {verb}" : "not yet" };
            state.AddThemeFontSizeOverride("font_size", 14);
            state.AddThemeColorOverride("font_color", done ? new Color(0.15f, 0.45f, 0.2f) : new Color(0.45f, 0.4f, 0.35f));
            col.AddChild(name);
            col.AddChild(state);
            row.AddChild(col);
            grid.AddChild(row);
        }
        if (g.Type == "releaseSpecies") Text($"Any {g.Count} of these, measured and released at {TheLedger.SimGlue.SpotInfo.Load(g.Spot).Name}. The species guide (Tab) says how to catch each.", 15);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (Visible && e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape or Key.Tab })
        {
            Visible = false;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (Visible) Position = ((GetViewportRect().Size - Size) * 0.5f).Floor();
    }
}
