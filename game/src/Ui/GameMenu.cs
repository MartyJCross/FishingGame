using Godot;
using TheLedger.Controls;

namespace TheLedger.Ui;

/// <summary>
/// Title screen (once per launch), Esc pause menu and Settings. The game pauses underneath (the pond keeps drawing
/// behind the title). New game asks first: it wipes the profile and the Ledger.
/// </summary>
public partial class GameMenu : CanvasLayer
{
    public InputRouter Router { get; set; } = null!;
    public string Version { get; set; } = "";

    /// <summary>True while any menu page is up (Main frees the mouse and stops the rod).</summary>
    public static bool IsOpen { get; private set; }

    private static bool _titleShown;
    private Control _root = null!;
    private VBoxContainer _box = null!;

    private const string SettingsPath = "user://settings.cfg";

    public override void _Ready()
    {
        Layer = 20;
        ProcessMode = ProcessModeEnum.Always;
        _root = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);
        _root.Theme = MenuTheme();
        var dim = new ColorRect { Color = new Color(0.02f, 0.04f, 0.06f, 0.38f), MouseFilter = Control.MouseFilterEnum.Ignore };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(dim);
        var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(center);
        _box = new VBoxContainer { CustomMinimumSize = new Vector2(420, 0) };
        _box.AddThemeConstantOverride("separation", 12);
        center.AddChild(_box);
        Close();
    }

    /// <summary>Shows the title the first time the game loads in a session (not after travel or sleep).</summary>
    public void ShowTitleOnce()
    {
        if (_titleShown) return;
        _titleShown = true;
        Title();
    }

    private void Open()
    {
        _root.Visible = true;
        IsOpen = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = true;
        foreach (var c in _box.GetChildren()) c.QueueFree();
    }

    private void Close()
    {
        _root.Visible = false;
        IsOpen = false;
        if (IsInsideTree()) GetTree().Paused = false;
    }

    /// <summary>Menu look: translucent dark buttons that turn to field-guide paper when pointed at.</summary>
    private static Theme MenuTheme()
    {
        var t = new Theme { DefaultFont = UiFonts.Bold, DefaultFontSize = 21 };
        StyleBoxFlat Box(Color bg, Color border)
        {
            var b = new StyleBoxFlat { BgColor = bg, BorderColor = border, ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 8, ContentMarginBottom = 8 };
            b.SetBorderWidthAll(1);
            b.SetCornerRadiusAll(5);
            return b;
        }
        var paper = new Color(0.95f, 0.92f, 0.84f, 0.96f);
        var ink = new Color(0.13f, 0.15f, 0.16f);
        t.SetStylebox("normal", "Button", Box(new Color(0.04f, 0.07f, 0.09f, 0.66f), new Color(1f, 1f, 1f, 0.14f)));
        t.SetStylebox("hover", "Button", Box(paper, new Color(0.62f, 0.52f, 0.36f)));
        t.SetStylebox("pressed", "Button", Box(paper.Darkened(0.12f), new Color(0.62f, 0.52f, 0.36f)));
        t.SetStylebox("focus", "Button", new StyleBoxEmpty());
        t.SetColor("font_color", "Button", new Color(0.93f, 0.95f, 0.95f));
        t.SetColor("font_hover_color", "Button", ink);
        t.SetColor("font_pressed_color", "Button", ink);
        return t;
    }

    private Label Heading(string text, int size, Font? font = null)
    {
        var l = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        if (font != null) l.AddThemeFontOverride("font", font);
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_outline_color", Colors.Black);
        l.AddThemeConstantOverride("outline_size", 6);
        _box.AddChild(l);
        return l;
    }

    private Button Item(string text, Action onPress)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(0, 46), FocusMode = Control.FocusModeEnum.None };
        b.AddThemeFontSizeOverride("font_size", 21);
        b.Pressed += onPress;
        _box.AddChild(b);
        return b;
    }

    private void Title()
    {
        Open();
        Heading("The Ledger", 104, UiFonts.Title).AddThemeConstantOverride("outline_size", 10);
        Heading("A field guide to Florida's Nature Coast", 24, UiFonts.TitleLight).Modulate = new Color(0.9f, 0.94f, 0.92f);
        _box.AddChild(new Control { CustomMinimumSize = new Vector2(0, 18) });
        bool hasSave = Godot.FileAccess.FileExists("user://profile.json");
        Item(hasSave ? "Continue" : "Start fishing", Close);
        if (hasSave) Item("New game", ConfirmNew);
        Item("Settings", () => Settings(Title));
        Item("Credits", Credits);
        Item("Quit", Quit);
        Heading($"v{Version}", 14).Modulate = new Color(1f, 1f, 1f, 0.6f);
    }

    /// <summary>Who made what: game/data/credits.json (sections of lines), in a scrolling page.</summary>
    private void Credits()
    {
        Open();
        Heading("Credits", 48, UiFonts.Title);
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(640, 440), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _box.AddChild(scroll);
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(list);
        try
        {
            var root = System.Text.Json.JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://data/credits.json")).RootElement;
            foreach (var sec in root.GetProperty("sections").EnumerateArray())
            {
                var h = new Label { Text = sec.GetProperty("title").GetString() };
                h.AddThemeFontOverride("font", UiFonts.Bold);
                h.AddThemeFontSizeOverride("font_size", 19);
                h.AddThemeColorOverride("font_color", new Color(1f, 0.84f, 0.45f));
                list.AddChild(h);
                foreach (var line in sec.GetProperty("lines").EnumerateArray())
                {
                    var l = new Label { Text = line.GetString(), AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(620, 0) };
                    l.AddThemeFontOverride("font", UiFonts.Body);
                    l.AddThemeFontSizeOverride("font_size", 15);
                    list.AddChild(l);
                }
                list.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
            }
        }
        catch (Exception ex) { list.AddChild(new Label { Text = $"Couldn't read the credits ({ex.Message})" }); }
        Item("Back", Title);
    }

    private void Pause()
    {
        Open();
        Heading("Paused", 56, UiFonts.Title);
        Item("Resume", Close);
        Item("Settings", () => Settings(Pause));
        Item("Quit to desktop", Quit);
    }

    private void ConfirmNew()
    {
        Open();
        Heading("Start a new game?", 32);
        var warn = Heading("This wipes your money, gear, story progress and the whole Ledger.", 17);
        warn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        warn.CustomMinimumSize = new Vector2(420, 0);
        Item("Yes, start over", () =>
        {
            ProfileStore.Reset();
            LedgerStore.Reset();
            Close();
            GetTree().ReloadCurrentScene();
        });
        Item("No, go back", Title);
    }

    private void Settings(Action back)
    {
        Open();
        Heading("Settings", 48, UiFonts.Title);
        var cfg = new ConfigFile();
        cfg.Load(SettingsPath);

        Slider("Master volume", 0, 1, Mathf.DbToLinear(AudioServer.GetBusVolumeDb(0)), v =>
        {
            AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(MathF.Max((float)v, 0.0001f)));
            Save("audio", "master", v);
        }, v => $"{v * 100:0}%");
        TheLedger.Audio.MusicEngine.LoadVolume();
        Slider("Music", 0, 1, TheLedger.Audio.MusicEngine.Volume, v => TheLedger.Audio.MusicEngine.SetVolume((float)v), v => $"{v * 100:0}%");
        Slider("Rod sensitivity", 0.2, 3, Router.MouseSensitivity, v => Router.SetMouseSensitivity((float)v), v => $"{v:0.00}");

        bool full = DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
        var fs = new CheckButton { Text = "Fullscreen (F11)", ButtonPressed = full, FocusMode = Control.FocusModeEnum.None };
        fs.Toggled += on => DisplayServer.WindowSetMode(on ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
        _box.AddChild(fs);
        var dog = new CheckButton { Text = "Skiff comes along", ButtonPressed = TheLedger.Render.TestView.DogOn, FocusMode = Control.FocusModeEnum.None };
        dog.Toggled += on => { TheLedger.Render.TestView.DogOn = on; Save("game", "dog", on); };
        _box.AddChild(dog);
        Item("Controls", () => Controls(back));
        Item("Back", back);
    }

    private string? _rebinding;
    private Button? _rebindButton;

    /// <summary>Keyboard and mouse bindings: click one, then press the new key or mouse button (Esc cancels).</summary>
    private void Controls(Action back)
    {
        Open();
        _rebinding = null;
        Heading("Controls", 44, UiFonts.Title);
        var hint = new Label { Text = "Click a binding, then press a key or mouse button. Esc cancels. The controller layout is fixed.", HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(520, 0) };
        hint.AddThemeFontSizeOverride("font_size", 15);
        _box.AddChild(hint);
        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 4);
        _box.AddChild(grid);
        foreach (var (action, name) in InputActions.Rebindable)
        {
            var l = new Label { Text = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            l.AddThemeFontSizeOverride("font_size", 17);
            grid.AddChild(l);
            var b = new Button { Text = InputActions.Label(action), CustomMinimumSize = new Vector2(150, 34), FocusMode = Control.FocusModeEnum.None };
            b.AddThemeFontSizeOverride("font_size", 17);
            string a = action;
            b.Pressed += () =>
            {
                if (_rebindButton != null && _rebinding != null) _rebindButton.Text = InputActions.Label(_rebinding);
                _rebinding = a;
                _rebindButton = b;
                b.Text = "press a key…";
            };
            grid.AddChild(b);
        }
        Item("Reset to defaults", () => { InputActions.ResetBindings(); Controls(back); });
        Item("Back", () => { _rebinding = null; Settings(back); });
    }

    public override void _Input(InputEvent e)
    {
        if (_rebinding == null || !IsOpen) return;
        bool done = false;
        if (e is InputEventKey { Pressed: true, Echo: false } k)
        {
            if (k.PhysicalKeycode != Key.Escape) InputActions.Rebind(_rebinding, new InputEventKey { PhysicalKeycode = k.PhysicalKeycode });
            done = true;
        }
        else if (e is InputEventMouseButton { Pressed: true } m && m.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown or MouseButton.WheelLeft or MouseButton.WheelRight))
        {
            // Buttons fire on release, so the click that started rebinding is over: this press is the new binding.
            InputActions.Rebind(_rebinding, new InputEventMouseButton { ButtonIndex = m.ButtonIndex });
            done = true;
        }
        if (!done) return;
        if (_rebindButton != null) _rebindButton.Text = InputActions.Label(_rebinding);
        _rebinding = null;
        _rebindButton = null;
        GetViewport().SetInputAsHandled();
    }

    private void Slider(string name, double min, double max, double value, Action<double> set, Func<double, string> fmt)
    {
        var label = new Label { Text = $"{name}   {fmt(value)}" };
        label.AddThemeFontSizeOverride("font_size", 18);
        _box.AddChild(label);
        var s = new HSlider { MinValue = min, MaxValue = max, Step = (max - min) / 100.0, Value = value, FocusMode = Control.FocusModeEnum.None };
        s.ValueChanged += v => { set(v); label.Text = $"{name}   {fmt(v)}"; };
        _box.AddChild(s);
    }

    private static void Save(string section, string key, Variant v)
    {
        var cfg = new ConfigFile();
        cfg.Load(SettingsPath);
        cfg.SetValue(section, key, v);
        cfg.Save(SettingsPath);
    }

    /// <summary>Applies saved settings that live outside other nodes (master volume).</summary>
    public static void ApplySaved()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SettingsPath) != Error.Ok) return;
        float master = (float)(double)cfg.GetValue("audio", "master", 1.0);
        AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(MathF.Max(master, 0.0001f)));
    }

    private void Quit()
    {
        (GetTree().CurrentScene as TheLedger.Boot.Main)?.SaveClock();
        GetTree().Quit();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape }) return;
        if (!IsOpen && TheLedger.Boot.Main.PanelOpen) return; // the open panel closes on Esc instead
        if (IsOpen)
        {
            if (_titleShown && GetTree().Paused) Close();
        }
        else Pause();
        GetViewport().SetInputAsHandled();
    }
}
