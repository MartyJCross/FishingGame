using Godot;
using TheLedger.Sim.Fish;
using TheLedger.SimGlue;

namespace TheLedger.Ui;

/// <summary>
/// The species guide (Tab menu): a card with a picture for every species at this spot; pick one for how to catch it.
/// Everything comes from the species data: time of day, season, depth, cover, what it takes (your tackle and the
/// shop's), size, rules and price, plus your own record from the Ledger.
/// </summary>
public partial class SpeciesGuide : PanelContainer
{
    public SimDriver Driver { get; set; } = null!;

    private VBoxContainer _box = null!;
    private static readonly Color Ink = new(0.92f, 0.94f, 0.92f);
    private static readonly Color Soft = new(0.7f, 0.78f, 0.8f);
    private static readonly Color Gold = new(1f, 0.85f, 0.45f);

    public override void _Ready()
    {
        var sb = new StyleBoxFlat { BgColor = new Color(0.05f, 0.07f, 0.08f, 0.95f) };
        sb.SetCornerRadiusAll(10);
        sb.SetContentMarginAll(18);
        AddThemeStyleboxOverride("panel", sb);
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(760, 700), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        AddChild(scroll);
        _box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _box.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_box);
        Visible = false;
    }

    /// <summary>Species living at the current spot, in the spot's stocking order.</summary>
    private List<SpeciesSpec> Here() =>
        Driver.World.Stock.Stock.Select(e => Driver.Species.FirstOrDefault(s => s.Id == e.Species)).Where(s => s != null).Distinct().ToList()!;

    public void Open()
    {
        Visible = true;
        ShowGrid();
    }

    /// <summary>Opens straight to one species' page (by id).</summary>
    public void OpenSpecies(string id)
    {
        Visible = true;
        var sp = Driver.Species.FirstOrDefault(s => s.Id == id);
        if (sp != null) ShowSpecies(sp); else ShowGrid();
    }

    private void Clear() { foreach (var c in _box.GetChildren()) c.QueueFree(); }

    private Label Text(Control parent, string text, int size, Color col, bool wrap = false)
    {
        var l = new Label { Text = text };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", col);
        if (wrap) { l.AutowrapMode = TextServer.AutowrapMode.WordSmart; l.CustomMinimumSize = new Vector2(700, 0); }
        parent.AddChild(l);
        return l;
    }

    private void ShowGrid()
    {
        Clear();
        Text(_box, $"Species guide: {Driver.Info.Name}", 26, Ink);
        Text(_box, "Pick a fish to see how to catch it.", 15, Soft);
        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        _box.AddChild(grid);
        foreach (var sp in Here())
        {
            int caught = LedgerStore.Entries.Count(e => e.Species == sp.Id);
            var card = new Button { CustomMinimumSize = new Vector2(236, 132), FocusMode = FocusModeEnum.None };
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.SetAnchorsPreset(LayoutPreset.FullRect);
            card.AddChild(col);
            var pic = new FishPortrait { Species = sp, Faded = caught == 0, CustomMinimumSize = new Vector2(226, 84) };
            col.AddChild(pic);
            var name = Text(col, sp.CommonName, 16, Ink);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.MouseFilter = MouseFilterEnum.Ignore;
            var got = Text(col, caught > 0 ? $"caught {caught}" : "not caught yet", 12, caught > 0 ? Gold : Soft);
            got.HorizontalAlignment = HorizontalAlignment.Center;
            got.MouseFilter = MouseFilterEnum.Ignore;
            card.Pressed += () => ShowSpecies(sp);
            grid.AddChild(card);
        }
        Close("Close");
    }

    private void Close(string label)
    {
        var b = new Button { Text = label, CustomMinimumSize = new Vector2(0, 38), FocusMode = FocusModeEnum.None };
        b.Pressed += () => Visible = false;
        _box.AddChild(b);
    }

    private static string Join(IEnumerable<string> parts)
    {
        var l = parts.ToList();
        return l.Count switch { 0 => "", 1 => l[0], _ => string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1] };
    }

    private void ShowSpecies(SpeciesSpec sp)
    {
        Clear();
        Text(_box, sp.CommonName, 28, Ink);
        Text(_box, sp.ScientificName, 15, Soft);
        var pic = new FishPortrait { Species = sp, CustomMinimumSize = new Vector2(700, 200) };
        _box.AddChild(pic);

        // When.
        var diel = sp.Activity.Diel;
        string when = "any time";
        if (diel.Count > 0)
        {
            float best = diel.Values.Max();
            when = Join(diel.Where(kv => kv.Value >= best * 0.85f).Select(kv => kv.Key switch
            {
                "dawn" => "dawn", "day" => "the middle of the day", "dusk" => "dusk", "night" => "at night", _ => kv.Key,
            }));
        }
        string season = "";
        var seas = sp.Activity.Seasonal;
        if (seas.Count > 0)
        {
            float best = seas.Values.Max();
            season = $"; most active in {Join(seas.Where(kv => kv.Value >= best * 0.9f).Select(kv => kv.Key))}";
        }
        Line("When", $"Bites best at {when}{season}.");

        // Where.
        var d = sp.Habitat.DepthM;
        // Only the cover this spot actually has, best liked first.
        var here = Driver.World.Spot.Structures.Select(z => z.Kind).ToHashSet();
        var cover = sp.Habitat.Structure.Where(kv => kv.Value >= 0.5f && here.Contains(kv.Key)).OrderByDescending(kv => kv.Value).Select(kv => kv.Key switch
        {
            "lilyPads" => "the lily pads", "weeds" => "the weed bed", "wood" => "fallen wood", "rock" => "the rocks", _ => kv.Key,
        });
        string coverText = Join(cover);
        bool bottom = sp.Feeding.Mode.StartsWith("bottom");
        Line("Where", $"{d[0]:0.#}–{d[Math.Min(1, d.Length - 1)]:0.#} m deep{(bottom ? ", feeding on the bottom" : "")}" +
                      (coverText != "" ? $". Here: around {coverText}." : ". Here: out in open water."));

        // What it takes: your own tackle first (usable right now), then what's worth buying.
        var owned = ProfileStore.Current.Lures;
        var ranked = Driver.AllLures
            .Select(l => (l, score: l.Natural ? sp.Feeding.BaitAttraction.GetValueOrDefault(l.AttractKey) : sp.Feeding.LureAttraction.GetValueOrDefault(l.AttractKey)))
            .Where(x => x.score >= 0.3f).OrderByDescending(x => x.score).ToList();
        static string Grade(float s) => s >= 0.75f ? "great" : s >= 0.5f ? "good" : "fair";
        var mineL = ranked.Where(x => owned.Contains(x.l.Id)).ToList();
        var shopL = ranked.Where(x => !owned.Contains(x.l.Id)).Take(3).ToList();
        string takes = ranked.Count == 0 ? "None of the tackle in the game yet suits it."
            : (mineL.Count > 0 ? "Yours: " + string.Join(";  ", mineL.Select(x => $"{x.l.Name} ({Grade(x.score)})")) : "Nothing you own suits it yet.")
              + (shopL.Count > 0 ? "\nIn the shop: " + string.Join(";  ", shopL.Select(x => $"{x.l.Name} ({Grade(x.score)})")) : "");
        var sp0 = sp.Feeding.SpeedPreferenceMps;
        float slow = sp0[0], fast = sp0[Math.Min(1, sp0.Length - 1)];
        string speed = fast <= 0.35f ? "Fish lures slowly, with pauses." : slow >= 0.4f ? "Retrieve lures quickly." : "A steady retrieve with the odd twitch works.";
        Line("Use", takes + "\n" + speed);

        // Size, rules, money.
        var len = sp.Size.LengthCm;
        Line("Size", $"Usually {len.Typical[0]:0}–{len.Typical[Math.Min(1, len.Typical.Length - 1)]:0} cm; up to {len.Max:0} cm" +
                     (sp.Size.RecordWeightKg > 0f ? $" (world record {sp.Size.RecordWeightKg:0.##} kg)." : "."));
        var reg = sp.Regulations;
        string rules = reg.BagLimit == 0 ? "Release only."
            : $"{(reg.MinLengthCm is float m ? $"Keep if at least {m:0} cm" : "No size limit")}{(reg.BagLimit is int b ? $", {b} a day." : ".")}";
        float price = ProfileStore.Shop.PricePerKg(sp.Id);
        Line("Rules", rules + (price > 0f ? $"  The bait shop pays ${price:0.00}/kg." : "  A game fish: it can't be sold."));

        // Your record.
        var mine = LedgerStore.Entries.Where(e => e.Species == sp.Id).ToList();
        Line("Ledger", mine.Count == 0 ? "Not caught yet."
            : $"Caught {mine.Count}; biggest {mine.Max(e => e.LengthCm):0} cm, {LedgerPanel.Weight(mine.Max(e => e.WeightKg))}.");

        var back = new Button { Text = "Back to the guide", CustomMinimumSize = new Vector2(0, 38), FocusMode = FocusModeEnum.None };
        back.Pressed += ShowGrid;
        _box.AddChild(back);
        Close("Close");
    }

    private void Line(string head, string body)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 14);
        var h = Text(row, head, 17, Gold);
        h.CustomMinimumSize = new Vector2(80, 0);
        var b = Text(row, body, 17, Ink);
        b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        b.CustomMinimumSize = new Vector2(610, 0);
        _box.AddChild(row);
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
