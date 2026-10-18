using Godot;
using TheLedger.DebugTools;
using TheLedger.Controls;
using TheLedger.Render;
using TheLedger.SimGlue;

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
    private Camera2D _camera = null!;
    private Node2D _worldRoot = null!;
    private Label _reelHud = null!;
    private Label _toast = null!;
    private Label _card = null!;
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

        _worldRoot = new Node2D { Name = "World" };
        AddChild(_worldRoot);

        _router = new InputRouter { Name = "InputRouter", PlayerIndex = 0 };
        AddChild(_router);

        _driver = new SimDriver { Name = "SimDriver" };
        _driver.Setup(config, _args);
        _driver.Routers.Add(_router);
        _driver.MouseWorldPx = () => _worldRoot.GetGlobalMousePosition();
        _driver.EventRaised += OnSimEvent;
        AddChild(_driver);

        _daylight = new CanvasModulate { Name = "Daylight" };
        AddChild(_daylight);
        _worldRoot.AddChild(new TestSky { Name = "Sky", ZIndex = -10 });
        _worldRoot.AddChild(new WaterRenderer { Name = "Water", Driver = _driver, WaterStartX = TestPond.WaterlineX, ZIndex = 0 });
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

        var hint = new Label
        {
            Text = "Cast: Space/B opens the bail, hold LMB/LT (finger on line), swing back and forward, let go  ·  " +
                   "crank: RMB/RT/wheel  ·  set the hook: sweep the rod  ·  land: F/A  ·  drag: Q/E  ·  F3 loads",
            AnchorLeft = 0f, AnchorRight = 1f, AnchorTop = 1f, AnchorBottom = 1f,
            OffsetTop = -44f, OffsetBottom = -12f, OffsetLeft = 16f,
        };
        hint.AddThemeFontSizeOverride("font_size", 18);
        hint.AddThemeColorOverride("font_outline_color", Colors.Black);
        hint.AddThemeConstantOverride("outline_size", 4);
        hud.AddChild(hint);

        _reelHud = new Label { AnchorLeft = 1f, AnchorRight = 1f, OffsetLeft = -420f, OffsetRight = -16f, OffsetTop = 12f, HorizontalAlignment = HorizontalAlignment.Right, GrowHorizontal = Control.GrowDirection.Begin };
        _reelHud.AddThemeFontSizeOverride("font_size", 22);
        _reelHud.AddThemeColorOverride("font_outline_color", Colors.Black);
        _reelHud.AddThemeConstantOverride("outline_size", 4);
        hud.AddChild(_reelHud);

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
                _card.Text = $"{c.Species.CommonName}  ({c.Species.ScientificName})\n{c.LengthCm:0.0} cm   {c.WeightKg * 1000f:0} g " +
                             $"({c.WeightKg * 2.20462f:0.00} lb)   on {w.Anglers[0].Rig.Line.Terminal.Name}\nReleased.   Catches this session: {w.Catches.Count}";
                _cardTime = 7;
                _toast.Text = "";
                break;
            }
        }
    }

    /// <summary>Night falls through a canvas tint: deep blue at night, warm at dawn and dusk.</summary>
    private void UpdateDaylight()
    {
        float h = _driver.World.Hour;
        float day = h < 5.5f || h > 20.5f ? 0f : h < 7.5f ? (h - 5.5f) / 2f : h > 18.5f ? (20.5f - h) / 2f : 1f;
        var night = new Color(0.22f, 0.26f, 0.42f);
        var warm = new Color(1f, 0.78f, 0.62f);
        var c = night.Lerp(Colors.White, day);
        float golden = Mathf.Clamp(1f - Mathf.Abs(day - 0.5f) * 2f, 0f, 1f);
        _daylight.Color = c.Lerp(c * warm, golden * 0.8f);
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
        _reelHud.Text = $"{line.Terminal.Name}  (R/X to change when reeled in)\n" +
                        $"Bail {(r.BailOpen ? "OPEN" : "closed")}   drag {r.DragForce / 9.81f:0.0} kg ({r.DragClick}/{r.Spec.DragDetents})\n" +
                        $"line out {line.LineOut:0.0} m   spool {r.LineOnSpool:0} m\n" +
                        $"tension {line.TipTension:0.0} N {Bar(line.TipTension / (line.Spec.BreakingForceN * line.Spec.KnotFactor))}" +
                        $"{(r.DragSlipping ? "   DRAG" : "")}\nrod load {Bar(a.Rig.Rod.PeakLoadRatio)}   {(int)_driver.World.Hour:00}:{(int)(_driver.World.Hour % 1f * 60f):00}";
    }

    public override void _UnhandledInput(InputEvent e)
    {
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
        _router.MouseRodEnabled = !_tuning.MouseOver;
        UpdateReelHud();
        UpdateDaylight();
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
