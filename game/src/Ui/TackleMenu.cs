using Godot;
using TheLedger.Controls;
using TheLedger.SimGlue;

namespace TheLedger.Ui;

/// <summary>
/// Tab: the tackle menu (change lure, change rod, open the Ledger). Gear can only be changed with the line reeled in;
/// the change still goes through the sim as a one-tick button press, so replays see it.
/// </summary>
public partial class TackleMenu : PanelContainer
{
    public SimDriver Driver { get; set; } = null!;
    public InputRouter Router { get; set; } = null!;
    public Action OpenLedger { get; set; } = () => { };

    private Button _lure = null!, _rod = null!;
    private Label _note = null!;

    public override void _Ready()
    {
        var sb = new StyleBoxFlat { BgColor = new Color(0.04f, 0.07f, 0.09f, 0.9f) };
        sb.SetCornerRadiusAll(10);
        sb.SetContentMarginAll(18);
        AddThemeStyleboxOverride("panel", sb);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(380, 0) };
        box.AddThemeConstantOverride("separation", 10);
        AddChild(box);
        var title = new Label { Text = "Tackle", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 26);
        box.AddChild(title);
        _lure = MakeButton(box, () => Router.PulseRig = true);
        _rod = MakeButton(box, () => Router.PulseRack = true);
        MakeButton(box, () => { Visible = false; OpenLedger(); }).Text = "The Ledger (catch log)";
        _note = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 0.8f, 0.5f) };
        box.AddChild(_note);
        MakeButton(box, () => Visible = false).Text = "Close (Tab)";
        Visible = false;
    }

    private static Button MakeButton(VBoxContainer box, Action onPress)
    {
        var b = new Button { CustomMinimumSize = new Vector2(0, 40), FocusMode = FocusModeEnum.None };
        b.AddThemeFontSizeOverride("font_size", 19);
        b.Pressed += onPress;
        box.AddChild(b);
        return b;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Tab }) Visible = !Visible;
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        Position = ((GetViewportRect().Size - Size) * 0.5f).Floor(); // centred (anchors don't apply under a CanvasLayer)
        var w = Driver.World;
        var a = w.Anglers[0];
        bool reeledIn = a.Rig.Line.LineOut <= w.Config.RetieMaxLineOut;
        _lure.Text = $"Lure:  {a.Rig.Line.Terminal.Name}   ▸";
        _rod.Text = $"Rod:  {(a.Rack.Count > 0 ? a.Rack[a.RackIndex].Name : a.Rig.Rod.Spec.Name)}   ▸";
        _lure.Disabled = _rod.Disabled = !reeledIn;
        _rod.Disabled |= a.Rack.Count < 2;
        _note.Text = reeledIn ? "" : "Reel in to change lure or rod";
    }
}
