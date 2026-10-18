using Godot;
using TheLedger.SimGlue;

namespace TheLedger.Ui;

/// <summary>
/// Travel between real Nature Coast spots (from the Tab menu). Driving costs gas money; home is free. Going somewhere
/// reloads the scene with the new spot; money, gear and the cooler come along (they live in the profile).
/// </summary>
public partial class TravelPanel : PanelContainer
{
    public SimDriver Driver { get; set; } = null!;

    private VBoxContainer _list = null!;

    public override void _Ready()
    {
        var sb = new StyleBoxFlat { BgColor = new Color(0.05f, 0.07f, 0.08f, 0.94f) };
        sb.SetCornerRadiusAll(10);
        sb.SetContentMarginAll(18);
        AddThemeStyleboxOverride("panel", sb);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(560, 0) };
        box.AddThemeConstantOverride("separation", 10);
        AddChild(box);
        var title = new Label { Text = "Where to?", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 26);
        box.AddChild(title);
        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 12);
        box.AddChild(_list);
        var close = new Button { Text = "Close", CustomMinimumSize = new Vector2(0, 38), FocusMode = FocusModeEnum.None };
        close.Pressed += () => Visible = false;
        box.AddChild(close);
        Visible = false;
    }

    public void Open()
    {
        Visible = true;
        foreach (var c in _list.GetChildren()) c.QueueFree();
        var p = ProfileStore.Current;
        foreach (var s in SpotInfo.All())
        {
            bool here = s.Id == Driver.Info.Id;
            var row = new HBoxContainer();
            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var name = new Label { Text = s.Name };
            name.AddThemeFontSizeOverride("font_size", 19);
            var place = new Label { Text = s.Place, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(360, 0), Modulate = new Color(0.75f, 0.8f, 0.82f) };
            place.AddThemeFontSizeOverride("font_size", 14);
            text.AddChild(name);
            text.AddChild(place);
            row.AddChild(text);
            string action = here ? "You're here" : s.TravelCost > 0f ? $"Drive  ${s.TravelCost:0.00}" : "Drive home";
            var go = new Button
            {
                Text = action, CustomMinimumSize = new Vector2(150, 36), FocusMode = FocusModeEnum.None,
                Disabled = here || p.Money < s.TravelCost, SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            go.Pressed += () =>
            {
                p.Money -= s.TravelCost;
                p.Spot = s.Id;
                ProfileStore.Save();
                GetTree().ReloadCurrentScene();
            };
            row.AddChild(go);
            _list.AddChild(row);
        }
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
