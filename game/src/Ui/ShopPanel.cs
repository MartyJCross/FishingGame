using Godot;
using TheLedger.SimGlue;

namespace TheLedger.Ui;

/// <summary>
/// The bait and tackle shop (from the Tab menu): sell the cooler, buy lures and outfits. Bought gear goes straight
/// into the tackle box / rod rack.
/// </summary>
public partial class ShopPanel : PanelContainer
{
    public SimDriver Driver { get; set; } = null!;

    private VBoxContainer _list = null!;
    private Label _money = null!;

    public override void _Ready()
    {
        var sb = new StyleBoxFlat { BgColor = new Color(0.05f, 0.07f, 0.08f, 0.94f) };
        sb.SetCornerRadiusAll(10);
        sb.SetContentMarginAll(18);
        AddThemeStyleboxOverride("panel", sb);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(560, 0) };
        box.AddThemeConstantOverride("separation", 8);
        AddChild(box);
        var title = new Label { Text = "Bait & Tackle", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 26);
        box.AddChild(title);
        _money = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 0.85f, 0.45f) };
        _money.AddThemeFontSizeOverride("font_size", 20);
        box.AddChild(_money);
        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 6);
        box.AddChild(_list);
        var close = new Button { Text = "Close", CustomMinimumSize = new Vector2(0, 38), FocusMode = FocusModeEnum.None };
        close.Pressed += () => Visible = false;
        box.AddChild(close);
        Visible = false;
    }

    public void Open()
    {
        Visible = true;
        Rebuild();
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

    private void Rebuild()
    {
        foreach (var c in _list.GetChildren()) c.QueueFree();
        var p = ProfileStore.Current;
        var shop = ProfileStore.Shop;
        _money.Text = $"${p.Money:0.00}";

        var (value, sellable) = ProfileStore.CoolerValue();
        Header("Your cooler");
        int gameFish = p.Cooler.Count - sellable;
        string coolerText = p.Cooler.Count == 0 ? "Empty"
            : $"{p.Cooler.Count} fish" + (gameFish > 0 ? $" ({gameFish} game fish: can't be sold, they go home with you)" : "");
        Row(coolerText, p.Cooler.Count == 0 ? "" : $"Sell  ${value:0.00}", p.Cooler.Count > 0, () =>
        {
            ProfileStore.SellCooler();
            Rebuild();
        });

        Header("Lures and bait");
        foreach (var l in Driver.AllLures)
        {
            if (!shop.Lures.TryGetValue(l.Id, out float price)) continue;
            bool owned = p.Lures.Contains(l.Id);
            var (open, after) = shop.Stocked(l.Id);
            if (!open && !owned) { Locked(l.Name, after); continue; }
            Row(l.Name, owned ? "Owned" : $"Buy  ${price:0.00}", !owned && p.Money >= price, () =>
            {
                p.Money -= price;
                p.Lures.Add(l.Id);
                ProfileStore.Save();
                foreach (var a in Driver.World.Anglers) a.TackleBox.Add(l);
                Rebuild();
            }, shop.Notes.GetValueOrDefault(l.Id, ""));
        }

        Header("Rods and reels");
        foreach (var o in Driver.AllOutfits)
        {
            if (!shop.Outfits.TryGetValue(o.Id, out float price)) continue;
            bool owned = p.Outfits.Contains(o.Id);
            var (open, after) = shop.Stocked(o.Id);
            if (!open && !owned) { Locked(o.Name, after); continue; }
            Row(o.Name, owned ? "Owned" : $"Buy  ${price:0.00}", !owned && p.Money >= price, () =>
            {
                p.Money -= price;
                p.Outfits.Add(o.Id);
                ProfileStore.Save();
                foreach (var a in Driver.World.Anglers) a.Rack.Add(o);
                Rebuild();
            }, shop.Notes.GetValueOrDefault(o.Id, ""));
        }
        var lost = new Label { Text = "Lures lost to a broken line are gone; the sinker and worm rig you always have spares of.", Modulate = new Color(0.6f, 0.66f, 0.68f), AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(520, 0) };
        lost.AddThemeFontSizeOverride("font_size", 13);
        _list.AddChild(lost);
    }

    private void Header(string text)
    {
        var h = new Label { Text = text, Modulate = new Color(0.7f, 0.8f, 0.82f) };
        h.AddThemeFontSizeOverride("font_size", 15);
        _list.AddChild(h);
    }

    /// <summary>Gear Junie doesn't stock yet: greyed out, with the story step that brings it in.</summary>
    private void Locked(string name, string after)
    {
        var row = new HBoxContainer { Modulate = new Color(1f, 1f, 1f, 0.45f) };
        var label = new Label { Text = name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", 17);
        row.AddChild(label);
        var when = new Label { Text = $"In stock after \"{after}\"", CustomMinimumSize = new Vector2(150, 0), HorizontalAlignment = HorizontalAlignment.Right };
        when.AddThemeFontSizeOverride("font_size", 14);
        row.AddChild(when);
        _list.AddChild(row);
    }

    private void Row(string name, string action, bool enabled, Action onPress, string note = "")
    {
        var row = new HBoxContainer();
        var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 0);
        var label = new Label { Text = name };
        label.AddThemeFontSizeOverride("font_size", 17);
        text.AddChild(label);
        if (note != "")
        {
            var n = new Label { Text = note, Modulate = new Color(0.62f, 0.7f, 0.72f) };
            n.AddThemeFontSizeOverride("font_size", 13);
            text.AddChild(n);
        }
        row.AddChild(text);
        if (action != "")
        {
            var b = new Button { Text = action, CustomMinimumSize = new Vector2(150, 32), Disabled = !enabled, FocusMode = FocusModeEnum.None };
            b.Pressed += onPress;
            row.AddChild(b);
        }
        _list.AddChild(row);
    }
}
