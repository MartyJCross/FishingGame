using Godot;
using TheLedger.Controls;
using TheLedger.Sim.Fish;
using TheLedger.SimGlue;

namespace TheLedger.Ui;

/// <summary>
/// Temporary controls display until the M7 HUD: a "what now" panel that follows the fishing (ready → cast → close the
/// bail → fish → fish on → land) and a full controls card (H / View to toggle). Key names follow the device in use.
/// Readiness to land is shown because the angler can see it (fish at the surface, beaten), not as an assist.
/// </summary>
public partial class ControlsHud : Control
{
    public SimDriver Driver { get; set; } = null!;
    public InputRouter Router { get; set; } = null!;

    private PanelContainer _nowPanel = null!, _cardPanel = null!;
    private RichTextLabel _now = null!, _card = null!;
    private bool _cardShown;
    private string _lastNow = "", _lastCardDevice = "";

    private static readonly Color Key = new(1f, 0.85f, 0.45f);

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _nowPanel = MakePanel();
        _nowPanel.AnchorLeft = 0f; _nowPanel.AnchorTop = 1f; _nowPanel.AnchorBottom = 1f;
        _nowPanel.OffsetLeft = 16f; _nowPanel.OffsetBottom = -16f; _nowPanel.OffsetTop = -16f;
        _nowPanel.GrowVertical = GrowDirection.Begin;
        _now = MakeText(620f, 21);
        _nowPanel.AddChild(_now);
        AddChild(_nowPanel);

        _cardPanel = MakePanel();
        _cardPanel.AnchorLeft = 0f; _cardPanel.AnchorTop = 0f;
        _cardPanel.OffsetLeft = 16f; _cardPanel.OffsetTop = 56f;
        _card = MakeText(560f, 19);
        _cardPanel.AddChild(_card);
        _cardPanel.Visible = false;
        AddChild(_cardPanel);
    }

    private static PanelContainer MakePanel()
    {
        var p = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.08f, 0.1f, 0.72f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 10, ContentMarginBottom = 10,
            BorderColor = new Color(1f, 1f, 1f, 0.12f), BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
        };
        p.AddThemeStyleboxOverride("panel", sb);
        return p;
    }

    private static RichTextLabel MakeText(float width, int size)
    {
        var t = new RichTextLabel
        {
            BbcodeEnabled = true, FitContent = true, ScrollActive = false, AutowrapMode = TextServer.AutowrapMode.Off,
            CustomMinimumSize = new Vector2(width, 0f), MouseFilter = MouseFilterEnum.Ignore,
        };
        t.AddThemeFontSizeOverride("normal_font_size", size);
        t.AddThemeFontSizeOverride("bold_font_size", size);
        return t;
    }

    private bool Pad => Router.ActiveDevice == InputRouter.Device.Gamepad;

    /// <summary>A key chip: [color]name[/color].</summary>
    private string K(string mouseKeys, string pad) => $"[color=#{Key.ToHtml(false)}][b]{(Pad ? pad : mouseKeys)}[/b][/color]";

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Godot.Key.H }
            || e is InputEventJoypadButton { Pressed: true, ButtonIndex: JoyButton.Back })
            _cardShown = !_cardShown;
    }

    public override void _Process(double delta)
    {
        string now = NowText();
        if (now != _lastNow) { _now.Text = now; _lastNow = now; }
        _cardPanel.Visible = _cardShown;
        string dev = Pad ? "pad" : "mk";
        if (_cardShown && dev != _lastCardDevice) { _card.Text = CardText(); _lastCardDevice = dev; }
    }

    private string NowText()
    {
        var w = Driver.World;
        var a = w.Anglers[0];
        var reel = a.Reel;
        var line = a.Rig.Line;
        Fish? on = null;
        foreach (var f in w.Fish) if (f.Holding && f.LureAngler == 0) { on = f; break; }
        string rod = Pad ? "right stick" : "mouse";
        string help = $"   [color=#8a9aa0]{K("H", "View")} all controls[/color]";

        if (on != null && on.State == FishState.Hooked)
        {
            float surface = w.Water.Height(on.HeadX, w.Time);
            bool close = MathF.Abs(on.HeadX - a.Position.X) < w.Config.LandReach && on.HeadY > surface - 0.6f;
            bool beat = on.StaminaA < w.Config.LandStamina || on.MassKg < w.Config.SwingInKg;
            if (close && beat)
                return $"[b]FISH ON[/b] - it's beaten and close: press {K("F", "RB")} to land it!" + help;
            return $"[b]FISH ON[/b] - keep the rod up ({rod}), crank {K("RMB / wheel", "RT or circle the left stick")} when it rests, let the drag work ({K("Q / E", "D-pad down / up")}).\n" +
                   $"Bring it in close to the bank, tired and near the surface, then {K("F", "RB")} to land." + help;
        }
        if (reel.BailOpen)
        {
            if (line.LineOut < 3f)
                return $"Bail open. Hold {K("LMB", "LT")} (finger on the line), swing the rod back and forward with the {rod}, " +
                       $"let go of {K("LMB", "LT")} as it comes forward.\nChanged your mind? Crank {K("RMB / wheel", "RT or circle the left stick")} to close the bail." + help;
            return $"[b]Lure's out - close the bail:[/b] turn the handle once ({K("RMB / wheel up", "RT / circle left stick")}).\n" +
                   "With the bail open the line just runs off the spool: you can't set the hook or fight a fish." + help;
        }
        if (line.LineOut < 3f)
            return $"[b]Ready.[/b] {K("Space", "LB")} opens the bail to cast.   {K("R", "X / D-pad right")} changes lure ({line.Terminal.Name}).\n" +
                   $"Point the rod with the {rod}{(Pad ? "" : ", W / S reach")}." + help;
        string watch = line.FloatIndex >= 0 ? "watch the float: a twitch is a nibble, when it [b]goes under[/b]"
                                            : "watch the line and the lure cam: when a fish [b]takes it[/b]";
        return $"Fishing. Crank {K("RMB / wheel", "RT or circle the left stick")} to retrieve, twitch the rod for action, or wait.\n" +
               $"Bites: {watch}, sweep the rod up hard ({rod} up) to set the hook." + help;
    }

    private string CardText()
    {
        string rod = Pad ? "Right stick" : "Mouse";
        return "[b]Controls[/b]\n" +
               $"{K("Mouse", "Right stick")}   point the rod (sweep up to set the hook)\n" +
               (Pad ? "" : $"{K("W / S", "")}   reach out / in\n") +
               $"{K("Space", "LB")}   open / close the bail\n" +
               $"{K("Hold LMB", "Hold LT")}   finger on the line (release to cast; feather to slow it)\n" +
               $"{K("RMB / wheel up", "RT / circle left stick")}   crank (closes the bail){(Pad ? "" : $"   {K("Z / C", "")} steady crank speed")}\n" +
               $"{K("Q / E", "D-pad down / up")}   drag looser / tighter\n" +
               $"{K("F", "RB")}   land the fish (net / lip)\n" +
               $"{K("R", "X / D-pad right")}   change lure (when reeled in)\n" +
               (Pad ? "" : $"{K("A / D", "")}   (walk: later)\n") +
               $"{K("F11", "F11")} fullscreen   {K("F3", "F3")} rod and line loads   {K("F4", "F4")} rain   {K("F1", "F1")} tuning   {K("Middle click", "-")} splash\n" +
               $"[color=#8a9aa0]{rod} sets where the rod points; the rod and line are simulated, so speed matters.[/color]";
    }
}
