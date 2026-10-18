using Godot;
using TheLedger.DebugTools;
using TheLedger.Controls;
using TheLedger.Render;
using TheLedger.SimGlue;
using TheLedger.Ui;

namespace TheLedger.Boot;

/// <summary>
/// Entry point. Parses the command line (CLAUDE.md §5), loads tuning, builds the sim driver, the view, the debug
/// overlay and the tuning panel, and runs the screenshot / smoke harness modes.
/// </summary>
public partial class Main : Node2D
{
    private CommandLine _args = null!;
    private SimDriver _driver = null!;
    private InputRouter _router = null!;
    private DebugOverlay _overlay = null!;
    private TuningPanel _tuning = null!;
    private bool _automated;
    private TheLedger.SimGlue.WeatherDirector? _weather;
    private Camera2D _camera = null!;
    private Node2D _worldRoot = null!;
    private Label _reelHud = null!;
    private Label _toast = null!, _radio = null!;
    private double _radioTime;
    private StoryPanel _story = null!;
    private Label _card = null!;
    private LedgerPanel _ledger = null!;
    private TheLedger.Ui.TackleMenu _menu = null!;
    private TheLedger.Ui.ShopPanel _shop = null!;
    private TheLedger.Ui.TravelPanel _travel = null!;
    private CanvasModulate _daylight = null!;
    private double _toastTime, _cardTime;
    private double _benchTime, _benchWorst;
    private int _benchFrames;
    private int _frame;
    private bool _capturing;

    public static string Version => (string)ProjectSettings.GetSetting("application/config/version", "0.0.0");

    public override void _Ready()
    {
        // Logs, overlays and files always use '.' decimals, whatever the PC's region setting.
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        _args = CommandLine.Parse(OS.GetCmdlineUserArgs());
        foreach (string e in _args.Errors) GD.PushWarning($"Command line: {e}");

        bool automated = _automated = _args.Background || _args.ScreenshotPath != null || _args.SmokeSeconds != null;
        if (!automated) DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen); // F11 toggles
        if (_args.Background)
        {
            // Automated runs stay out of the owner's way: never take focus, sit off-screen (pair with
            // --position -4000,-4000 on the Godot command line so the window is never shown on screen at all).
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.NoFocus, true);
            DisplayServer.WindowSetPosition(new Vector2I(-4000, -4000));
        }
        if (_args.Light)
        {
            Engine.MaxFps = 30;
            AudioServer.SetBusMute(0, true);
        }
        else if (_args.MaxFps is int fps)
        {
            Engine.MaxFps = fps;
        }

        InputActions.Register();

        var warnings = new List<string>();
        var config = TuningStore.Load(warnings);
        foreach (string w in warnings) GD.PushWarning($"Tuning: {w}");
        ApplyWeather(config, _args.Weather);
        if (_args.TimeOfDay is float hour) config.StartHour = hour;
        else if (!_automated) { config.StartHour = ProfileStore.Current.Hour; config.StartDayOfYear = ProfileStore.Current.Day; }

        _worldRoot = new Node2D { Name = "World" };
        AddChild(_worldRoot);

        _router = new InputRouter { Name = "InputRouter", PlayerIndex = 0 };
        AddChild(_router);

        _driver = new SimDriver { Name = "SimDriver" };
        if (_args.Spot != null) _driver.SpotId = _args.Spot;
        else if (!_automated) _driver.SpotId = ProfileStore.Current.Spot;
        _driver.Setup(config, _args);
        if (!_automated) CallDeferred(nameof(StoryBriefing));
        if (!_automated && _args.Weather == null)
        {
            _weather = new TheLedger.SimGlue.WeatherDirector { Name = "Weather", Driver = _driver };
            AddChild(_weather);
        }
        _driver.Routers.Add(_router);
        _driver.MouseWorldPx = () => _worldRoot.GetGlobalMousePosition();
        _driver.EventRaised += OnSimEvent;
        AddChild(new TheLedger.Audio.SoundEngine { Name = "Sound", Driver = _driver, Router = _router });
        AddChild(_driver);

        _daylight = new CanvasModulate { Name = "Daylight" };
        AddChild(_daylight);
        _worldRoot.AddChild(new TestSky { Name = "Sky", ZIndex = -10 });
        _worldRoot.AddChild(new WaterRenderer
        {
            Name = "Water", Driver = _driver, WaterStartX = _driver.Info.Spot.WaterlineX, ZIndex = 0,
            Shallow = _driver.Info.WaterShallow with { A = 0.96f }, Deep = _driver.Info.WaterDeep,
        });
        _worldRoot.AddChild(new FishRenderer { Name = "Fish", Driver = _driver, ZIndex = 1 });
        _worldRoot.AddChild(new TestView { Name = "TestView", Driver = _driver, ZIndex = 10 });
        var tackle = new TackleRenderer { Name = "Tackle", Driver = _driver, ZIndex = 11 };
        _worldRoot.AddChild(tackle);
        ApplyScene();

        _camera = new Camera2D { Name = "Camera", Position = SimSpace.ToPx(2.5f, 0.5f) };
        _worldRoot.AddChild(_camera);
        _camera.MakeCurrent();

        BuildHud();

        _overlay = new DebugOverlay { Name = "DebugOverlay" };
        AddChild(_overlay);
        _overlay.Setup(_driver, _router, _worldRoot, _args.DebugOverlay);
        tackle.ShowLoads = () => _overlay.Shown;

        _tuning = new TuningPanel { Name = "TuningPanel" };
        AddChild(_tuning);
        _tuning.Setup(config);

        GD.Print($"The Ledger {Version} | Godot {Engine.GetVersionInfo()["string"]} | renderer {RenderingServer.GetCurrentRenderingMethod()} | seed {_driver.World.Seed}");
    }

    private void BuildHud()
    {
        var hud = new CanvasLayer { Name = "Hud", Layer = 5 };
        AddChild(hud);
        var version = new Label { Text = $"The Ledger  v{Version}", Position = new Vector2(16, 12) };
        version.AddThemeFontSizeOverride("font_size", 24);
        version.AddThemeColorOverride("font_outline_color", Colors.Black);
        version.AddThemeConstantOverride("outline_size", 4);
        hud.AddChild(version);

        hud.AddChild(new TheLedger.Ui.LureCam { Name = "LureCam", Driver = _driver });
        hud.AddChild(new TheLedger.Ui.ControlsHud { Name = "Controls", Driver = _driver, Router = _router });
        _shop = new TheLedger.Ui.ShopPanel { Name = "Shop", Driver = _driver };
        _travel = new TheLedger.Ui.TravelPanel { Name = "Travel", Driver = _driver };
        _story = new StoryPanel { Name = "Story", Finished = StoryMoment };
        _menu = new TheLedger.Ui.TackleMenu { Name = "TackleMenu", Driver = _driver, Router = _router, OpenLedger = () => _ledger.Toggle(), OpenShop = () => _shop.Open(), OpenTravel = () => _travel.Open(), OpenStory = () => _story.Open() };

        _reelHud = new Label { AnchorLeft = 1f, AnchorRight = 1f, OffsetLeft = -420f, OffsetRight = -16f, OffsetTop = 12f, HorizontalAlignment = HorizontalAlignment.Right, GrowHorizontal = Control.GrowDirection.Begin };
        _reelHud.AddThemeFontSizeOverride("font_size", 22);
        _reelHud.AddThemeColorOverride("font_outline_color", Colors.Black);
        _reelHud.AddThemeConstantOverride("outline_size", 4);
        hud.AddChild(_reelHud);

        _radio = new Label
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f, OffsetLeft = -480f, OffsetRight = 480f, OffsetTop = 170f,
            HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _radio.AddThemeFontSizeOverride("font_size", 21);
        _radio.AddThemeColorOverride("font_color", new Color(0.85f, 0.95f, 0.85f));
        _radio.AddThemeColorOverride("font_outline_color", Colors.Black);
        _radio.AddThemeConstantOverride("outline_size", 5);
        hud.AddChild(_radio);
        _toast = new Label { AnchorLeft = 0.5f, AnchorRight = 0.5f, OffsetLeft = -400f, OffsetRight = 400f, OffsetTop = 110f, HorizontalAlignment = HorizontalAlignment.Center };
        _toast.AddThemeFontSizeOverride("font_size", 34);
        _toast.AddThemeColorOverride("font_outline_color", Colors.Black);
        _toast.AddThemeConstantOverride("outline_size", 6);
        hud.AddChild(_toast);

        _card = new Label { AnchorLeft = 0.5f, AnchorRight = 0.5f, OffsetLeft = -420f, OffsetRight = 420f, OffsetTop = 170f, HorizontalAlignment = HorizontalAlignment.Center };
        _card.AddThemeFontSizeOverride("font_size", 26);
        _card.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.8f));
        _card.AddThemeColorOverride("font_outline_color", Colors.Black);
        _card.AddThemeConstantOverride("outline_size", 6);
        hud.AddChild(_card);

        _ledger = new LedgerPanel { Name = "Ledger" };
        hud.AddChild(_ledger);
        hud.AddChild(_menu);
        hud.AddChild(_shop);
        hud.AddChild(_travel);
        hud.AddChild(_story);
    }

    private void Toast(string text, double seconds = 2.5)
    {
        _toast.Text = text;
        _toastTime = seconds;
    }

    /// <summary>What the angler would notice: hooksets, breaks, a thrown hook, a landed fish (no bite alerts: watch the float).</summary>
    private void OnSimEvent(TheLedger.Sim.Core.SimEvent e)
    {
        if (e.Angler != 0) return;
        var w = _driver.World;
        switch (e.Type)
        {
            case TheLedger.Sim.Core.SimEventType.HookSet: Toast("Fish on!"); break;
            case TheLedger.Sim.Core.SimEventType.LineBroke: Toast($"The line broke ({e.Value:0.0} N)", 3.5); break;
            case TheLedger.Sim.Core.SimEventType.RodBroke: Toast("The rod snapped! (high-sticking under load)", 4); break;
            case TheLedger.Sim.Core.SimEventType.HookPulled:
                Toast("It threw the hook", 3);
                break;
            case TheLedger.Sim.Core.SimEventType.LandFailed: Toast("Not yet: bring it closer and tire it out", 2); break;
            case TheLedger.Sim.Core.SimEventType.FishLanded:
            {
                var c = w.Catches[^1];
                int rule = w.KeepRule(0, c);
                string pad = _router.ActiveDevice == InputRouter.Device.Gamepad ? "Y" : "R";
                string rel = _router.ActiveDevice == InputRouter.Device.Gamepad ? "RB / A" : "F";
                string choice = rule switch
                {
                    1 => $"Must go back: under the {c.Species.Regulations.MinLengthCm:0.#} cm minimum.   Release: {rel}",
                    2 => $"Bag limit reached for {c.Species.CommonName} today.   Release: {rel}",
                    3 => $"Must be released here.   Release: {rel}",
                    _ => $"Keep: {pad}     Release: {rel}",
                };
                _card.Text = CatchLine(c) + $"\n[ {choice} ]";
                _cardTime = 1e9;
                _toast.Text = "";
                break;
            }
            case TheLedger.Sim.Core.SimEventType.FishKept:
            case TheLedger.Sim.Core.SimEventType.FishReleased:
            {
                bool kept = e.Type == TheLedger.Sim.Core.SimEventType.FishKept;
                var c = w.Catches.Last(k => k.FishId == e.Id);
                if (kept && !_automated)
                {
                    ProfileStore.Current.Cooler.Add(new CoolerFish { Species = c.Species.Id, CommonName = c.Species.CommonName, WeightKg = c.WeightKg });
                    ProfileStore.Save();
                    float price = ProfileStore.Shop.PricePerKg(c.Species.Id) * c.WeightKg;
                    Toast(price > 0f ? $"In the cooler: worth about ${price:0.00} at the shop" : "In the cooler (game fish: not for sale)", 2.5);
                }
                LedgerStore.Add(new LedgerEntry
                {
                    Species = c.Species.Id, CommonName = c.Species.CommonName, LengthCm = c.LengthCm, WeightKg = c.WeightKg,
                    Kept = kept, Lure = w.Anglers[0].Rig.Line.Terminal.Name, GameHour = c.Hour, Spot = _driver.Info.Id,
                    When = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                });
                if (!_automated && Story.CurrentComplete) StoryMoment(Story.Advance());
                int keptCount = w.Catches.Count(k => k.Kept);
                _card.Text = CatchLine(c) + $"\n{(kept ? "Kept" : "Released")}.   This session: {w.Catches.Count(k => !k.Pending)} landed, {keptCount} kept.   Ledger: {LedgerStore.Entries.Count}";
                _cardTime = 5;
                if (_ledger.Visible) _ledger.Refresh();
                break;
            }
            case TheLedger.Sim.Core.SimEventType.KeepRefused:
                Toast(e.Value switch { 1f => "Too short to keep: it has to go back", 2f => "That's your bag limit for this species", _ => "This one must be released" }, 2.5);
                break;
            case TheLedger.Sim.Core.SimEventType.Backlash:
                Toast("Bird's nest! Thumb the spool as the lure slows. Hold R / Y to pick it out", 3.5);
                break;
            case TheLedger.Sim.Core.SimEventType.FishJumped:
                Toast("It jumped! Keep the line tight", 1.5);
                break;
        }
    }

    private float _lastTension;

    /// <summary>
    /// Controller rumble from the line: sudden tension changes (taps, a bite, a head shake) buzz the light motor,
    /// sustained load on a fish works the heavy one. Pure information from the sim, like feeling the rod.
    /// </summary>
    private void Rumble(double delta)
    {
        if (_router.ActiveDevice != InputRouter.Device.Gamepad || Input.GetConnectedJoypads().Count == 0) return;
        var line = _driver.World.Anglers[0].Rig.Line;
        float t = line.TipTension;
        float change = MathF.Abs(t - _lastTension) / MathF.Max((float)delta, 1e-3f);
        _lastTension = t;
        float weak = Mathf.Clamp(change * 0.03f, 0f, 0.7f);
        float strong = Mathf.Clamp(t / (line.Spec.BreakingForceN * 0.8f), 0f, 1f) * (line.Pinned ? 1f : 0.3f);
        if (weak < 0.03f && strong < 0.05f) return;
        Input.StartJoyVibration(Input.GetConnectedJoypads()[0], weak, strong, 0.12f);
    }

    private static string CatchLine(TheLedger.Sim.World.Catch c) =>
        $"{c.Species.CommonName}  ({c.Species.ScientificName})\n{c.LengthCm:0.0} cm ({c.LengthCm / 2.54f:0.0} in)   " +
        $"{LedgerPanel.Weight(c.WeightKg)} ({c.WeightKg * 2.20462f:0.00} lb)";

    /// <summary>Night falls through a canvas tint: deep blue at night, warm at dawn and dusk.</summary>
    private void UpdateDaylight()
    {
        float h = _driver.World.Hour;
        float day = h < 5.5f || h > 20.5f ? 0f : h < 7.5f ? (h - 5.5f) / 2f : h > 18.5f ? (20.5f - h) / 2f : 1f;
        var night = new Color(0.22f, 0.26f, 0.42f);
        var warm = new Color(1f, 0.78f, 0.62f);
        var c = night.Lerp(Colors.White, day);
        float golden = Mathf.Clamp(1f - Mathf.Abs(day - 0.5f) * 2f, 0f, 1f);
        c = c.Lerp(c * warm, golden * 0.8f);
        // Cloud cover dims and greys the light.
        float cloud = _driver.Config.CloudCover;
        _daylight.Color = c.Lerp(c * new Color(0.72f, 0.75f, 0.8f), cloud);
    }

    private static string Bar(float frac)
    {
        int n = Math.Clamp((int)MathF.Round(frac * 10f), 0, 10);
        return "[" + new string('|', n) + new string('.', 10 - n) + "]";
    }

    /// <summary>Keeps the angler and the end of the line in view, zooming out for long casts (smoothed).</summary>
    private void FollowCamera(double delta)
    {
        if (_args.Look is { } look)
        {
            _camera.Position = SimSpace.ToPx(look[0], look[1]);
            _camera.Zoom = new Vector2(look[2], look[2]);
            return;
        }
        var a = _driver.World.Anglers[0];
        float left = a.Position.X - 2.5f;
        float right = MathF.Max(a.Position.X + 6f, a.Rig.Line.TerminalPos.X + 3f);
        float span = right - left;
        float zoom = Math.Clamp(15f / span, 0.35f, 1f);
        var target = SimSpace.ToPx(0.5f * (left + right), 0.5f + 1.5f * (1f - zoom));
        float k = 1f - MathF.Exp(-3f * (float)delta);
        _camera.Position = _camera.Position.Lerp(target, k);
        _camera.Zoom = _camera.Zoom.Lerp(new Vector2(zoom, zoom), k);
    }

    /// <summary>Reel readout until the M7 HUD: bail, drag, line out, tension.</summary>
    private void UpdateReelHud()
    {
        var a = _driver.World.Anglers[0];
        var r = a.Reel;
        var line = a.Rig.Line;
        _reelHud.Text = r.Backlashed ? $"BIRD'S NEST - hold R / Y to pick it out {Bar(r.ClearProgress)}"
                      : $"{new DateTime(2026, 1, 1).AddDays(_driver.World.DayOfYear):MMM d}   " +
                        $"{(int)_driver.World.Hour:00}:{(int)(_driver.World.Hour % 1f * 60f):00}   water {_driver.World.WaterTempC:0.0} °C" +
                        (_weather != null ? $"   {_weather.Describe}" : "");
        _ = line;
    }

    private void StoryBriefing()
    {
        var s = Story.Current;
        Radio(s.Giver, s.Intro, 12);
    }

    /// <summary>Someone on the radio: a few lines across the top of the screen for a while.</summary>
    private void Radio(string who, string text, double seconds = 9)
    {
        _radio.Text = $"{who} (radio):  {text}";
        _radioTime = seconds;
    }

    /// <summary>A story step finished: the giver's sign-off, a new page, then the next step's brief.</summary>
    private void StoryMoment(StoryStep done)
    {
        Radio(done.Giver, done.Done + (done.Page != null ? "   [New page of Wren's in your Field notes: Tab]" : ""), 12);
        Toast($"{done.Title}: complete", 3);
        GetTree().CreateTimer(13.0).Timeout += () => { var n = Story.Current; if (n.Id != done.Id) Radio(n.Giver, n.Intro, 12); };
    }

    /// <summary>Keeps the calendar going between sessions: the date and time are saved on quit (and on travel).</summary>
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && !_automated) SaveClock();
    }

    public void SaveClock()
    {
        var p = ProfileStore.Current;
        p.Day = _driver.World.DayOfYear;
        p.Hour = _driver.World.Hour;
        ProfileStore.Save();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.M })
            AudioServer.SetBusMute(0, !AudioServer.IsBusMute(0));
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F11 })
        {
            bool full = DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
            DisplayServer.WindowSetMode(full ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
        }
        if (e.IsActionPressed(InputActions.ToggleLedger)) _ledger.Toggle();
        if (e.IsActionPressed(InputActions.ToggleTuning)) _tuning.Toggle();
        if (e.IsActionPressed(InputActions.ToggleDebug)) _overlay.Toggle();
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F4 })
        {
            var c = _driver.Config;
            c.DebugRainRate = c.DebugRainRate > 0f ? 0f : 8f;
            GD.Print($"Rain {(c.DebugRainRate > 0 ? "on (8 mm/h)" : "off")}");
        }
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Middle })
        {
            // Debug splash: a 50 g stone dropped from about a metre.
            float x = SimSpace.ToSim(_worldRoot.GetGlobalMousePosition()).X;
            _driver.QueueSplash(x, 4.5f, 0.05f, 0.05f);
        }
    }

    /// <summary>--weather presets until the weather system exists (M8).</summary>
    private static void ApplyWeather(TheLedger.Sim.Core.SimConfig c, string? weather)
    {
        switch (weather)
        {
            case null: break;
            case "calm": c.DebugWindSpeed = 0.5f; c.DebugRainRate = 0f; break;
            case "breeze": c.DebugWindSpeed = 5f; c.DebugFetch = 60f; break;
            case "windy": c.DebugWindSpeed = 9f; c.DebugFetch = 600f; break;
            case "rain": c.DebugWindSpeed = 3f; c.DebugRainRate = 8f; break;
            case "storm": c.DebugWindSpeed = 14f; c.DebugFetch = 2000f; c.DebugRainRate = 35f; break;
            default: GD.PushWarning($"Unknown --weather '{weather}' (calm, breeze, windy, rain, storm)"); break;
        }
    }

    /// <summary>--scene presets. "water_test" drops a few stones in for screenshots.</summary>
    private void ApplyScene()
    {
        if (_args.Scene != "water_test") return;
        _driver.QueueSplash(2.5f, 3f, 0.02f, 0.03f, atTick: 20);
        _driver.QueueSplash(6.0f, 6f, 0.08f, 0.05f, atTick: 70);
        _driver.QueueSplash(9.5f, 9f, 0.2f, 0.07f, atTick: 150);
        _driver.QueueSplash(4.0f, 2f, 0.01f, 0.02f, atTick: 165);
    }

    public override void _Process(double delta)
    {
        _router.ShoulderPx = SimSpace.ToPx(_driver.World.Anglers[0].Shoulder);
        _router.RodAngle = _driver.World.Anglers[0].Hands.Angle;
        bool panel = _menu.Visible || _shop.Visible || _travel.Visible || _story.Visible;
        _radioTime -= delta;
        _radio.Visible = _radioTime > 0;
        _router.MouseRodEnabled = !_tuning.MouseOver && !panel;
        // The mouse is captured (hidden) while fishing; any menu or panel gives the cursor back.
        bool wantCapture = !_automated && !panel && !_ledger.Visible && !_tuning.IsOpen
                           && DisplayServer.WindowIsFocused();
        var mode = wantCapture ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        if (Input.MouseMode != mode) Input.MouseMode = mode;
        UpdateReelHud();
        UpdateDaylight();
        Rumble(delta);
        _toastTime -= delta;
        _cardTime -= delta;
        if (_toastTime <= 0) _toast.Text = "";
        if (_cardTime <= 0) _card.Text = "";
        FollowCamera(delta);

        if (_args.SmokeSeconds is float smoke)
        {
            RunSmoke(smoke);
            return;
        }

        if (_args.Lockstep) _driver.StepFrame(_driver.TicksPerFrame);

        if (_args.BenchSeconds is float bench)
        {
            // Real-time run (uncapped): report frame rate and frame-time spikes, then quit.
            _benchTime += delta;
            _benchFrames++;
            if (_benchTime > 1.0 && delta > _benchWorst) _benchWorst = delta;
            if (_benchTime >= bench)
            {
                GD.Print($"BENCH: {_benchFrames / _benchTime:0} fps average over {_benchTime:0.0} s, worst frame {_benchWorst * 1000:0.0} ms, " +
                         $"sim step mean {_driver.MeanStepMs:0.000} ms, {_driver.World.Fish.Count} fish");
                GetTree().Quit(0);
                _args.BenchSeconds = null;
            }
        }

        _frame++;
        if (_args.ScreenshotPath != null && _frame >= _args.Frames && !_capturing)
        {
            _capturing = true;
            CaptureAndQuit(_args.ScreenshotPath);
        }
    }

    /// <summary>Simulates the requested seconds as fast as possible (a few hundred ticks per frame), then exits.</summary>
    private void RunSmoke(float seconds)
    {
        long target = (long)(seconds * _driver.Config.TickHz);
        _driver.StepFrame(600);
        if (_driver.World.Tick < target) return;

        var w = _driver.World;
        bool ok = w.FaultCount == 0 && _driver.ExceptionCount == 0;
        GD.Print($"SMOKE {(ok ? "OK" : "FAILED")}: {w.Tick} ticks ({w.Time:0.0} s), faults {w.FaultCount}, " +
                 $"exceptions {_driver.ExceptionCount}, step mean {_driver.MeanStepMs:0.000} / worst {_driver.WorstStepMs:0.000} ms, hash {w.ComputeHash():X16}");
        GetTree().Quit(ok ? 0 : 1);
        _args.SmokeSeconds = null;
    }

    private async void CaptureAndQuit(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = GetViewport().GetTexture().GetImage();
        string full = ProjectSettings.GlobalizePath(path);
        Error err = image.SavePng(full);
        GD.Print(err == Error.Ok ? $"Screenshot saved to {full} ({image.GetWidth()}x{image.GetHeight()})" : $"Screenshot failed: {err}");
        GetTree().Quit(err == Error.Ok ? 0 : 1);
    }
}
