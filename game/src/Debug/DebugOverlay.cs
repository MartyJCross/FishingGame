using System.Text;
using Godot;
using TheLedger.Boot;
using TheLedger.Controls;
using TheLedger.Sim.Core;
using TheLedger.SimGlue;

namespace TheLedger.DebugTools;

/// <summary>
/// Sim debug drawing (docs/04_PHYSICS_SPEC.md §10). World-space shapes are drawn by <see cref="DebugWorldDraw"/>;
/// text goes on a screen-space label. Toggle with F3 or start with --debug-overlay.
/// Systems add their own drawing here as they arrive (rod moments, line tension, fish brains...).
/// </summary>
public partial class DebugOverlay : CanvasLayer
{
    private SimDriver _driver = null!;
    private InputRouter _router = null!;
    private Label _text = null!;
    private DebugWorldDraw _world = null!;
    private readonly StringBuilder _sb = new();

    public bool Shown { get; private set; }

    public void Setup(SimDriver driver, InputRouter router, Node2D worldParent, bool shown)
    {
        _driver = driver;
        _router = router;
        Layer = 10;
        _text = new Label { Position = new Vector2(16, 64) };
        _text.AddThemeFontSizeOverride("font_size", 18);
        _text.AddThemeColorOverride("font_color", new Color(0.85f, 1f, 0.85f));
        _text.AddThemeColorOverride("font_outline_color", Colors.Black);
        _text.AddThemeConstantOverride("outline_size", 4);
        AddChild(_text);

        _world = new DebugWorldDraw { Driver = driver, ZIndex = 100 };
        worldParent.AddChild(_world);
        SetShown(shown);
    }

    public void Toggle() => SetShown(!Shown);

    private void SetShown(bool shown)
    {
        Shown = shown;
        _text.Visible = shown;
        _world.Visible = shown;
    }

    public override void _Process(double delta)
    {
        if (!Shown) return;
        var w = _driver.World;
        _sb.Clear();
        _sb.Append($"FPS {Engine.GetFramesPerSecond():0}   tick {w.Tick}   sim {w.Time:0.00}s   step mean {_driver.MeanStepMs:0.000} / worst {_driver.WorstStepMs:0.000} ms\n");
        _sb.Append($"Faults {w.FaultCount}   exceptions {_driver.ExceptionCount}   events dropped {w.Events.Dropped}\n");
        _sb.Append($"Input: {_router.ActiveDevice}   steady crank {_router.SteadyCrankRate:0.00} t/s\n");
        foreach (var a in w.Anglers)
        {
            var i = a.LastInput;
            _sb.Append($"Angler {a.Index}: rod {Mathf.RadToDeg(a.Hands.Angle):0}° (target {Mathf.RadToDeg(i.RodTargetAngle):0}°) ");
            _sb.Append($"ω {a.Hands.AngularVelocity:0.0} rad/s  reach {a.Hands.Reach:0.00}\n");
            _sb.Append($"  hold {i.LineHold:0.00}  crank {i.CrankRate:0.00} t/s  drag {i.DragDelta:+0;-0;0}  move {i.Move:0.00}  buttons {i.Buttons}\n");
        }
        _sb.Append(InputRouter.DescribeJoypads());
        _text.Text = _sb.ToString();
        _world.QueueRedraw();
    }
}

/// <summary>World-space debug shapes, drawn in sim coordinates converted to pixels.</summary>
public partial class DebugWorldDraw : Node2D
{
    public SimDriver Driver { get; set; } = null!;

    public override void _Draw()
    {
        float alpha = Driver.Alpha;
        foreach (var a in Driver.World.Anglers)
        {
            Vec2 shoulder = Vec2.Lerp(a.PrevPosition, a.Position, alpha) + new Vec2(0f, a.ShoulderHeight);
            float target = a.LastInput.RodTargetAngle;
            Vector2 s = SimSpace.ToPx(shoulder);
            DrawLine(s, s + SimSpace.ToPx(Vec2.FromAngle(target) * 2.6f), new Color(1f, 0.9f, 0.2f, 0.5f), 2f);
            DrawCircle(s, 6f, new Color(1f, 0.4f, 0.3f));
        }
    }
}
