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
        var dim = new ColorRect { Color = new Color(0.02f, 0.04f, 0.06f, 0.55f), MouseFilter = Control.MouseFilterEnum.Ignore };
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

    private Label Heading(string text, int size)
    {
        var l = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
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
        Heading("The Ledger", 64);
        Heading("Florida's Nature Coast", 22).Modulate = new Color(0.85f, 0.92f, 0.9f);
        _box.AddChild(new Control { CustomMinimumSize = new Vector2(0, 18) });
        bool hasSave = Godot.FileAccess.FileExists("user://profile.json");
        Item(hasSave ? "Continue" : "Start fishing", Close);
        if (hasSave) Item("New game", ConfirmNew);
        Item("Settings", () => Settings(Title));
        Item("Quit", Quit);
        Heading($"v{Version}", 14).Modulate = new Color(1f, 1f, 1f, 0.6f);
    }

    private void Pause()
    {
        Open();
        Heading("Paused", 44);
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
        Heading("Settings", 40);
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
        Item("Back", back);
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
