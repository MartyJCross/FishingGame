using Godot;
using TheLedger.Boot;
using TheLedger.SimGlue;

namespace TheLedger.Ui;

/// <summary>
/// Picture-in-picture view of the water around the lure (bottom right), so floats, follows and bites at 20 m are
/// visible. It renders the same world (shared World2D) through its own zoomed camera, and shows only while the lure
/// is out in the water.
/// </summary>
public partial class LureCam : SubViewportContainer
{
    public SimDriver Driver { get; set; } = null!;

    private SubViewport _view = null!;
    private Camera2D _cam = null!;
    private float _show;

    public override void _Ready()
    {
        Stretch = true;
        AnchorLeft = 1f; AnchorRight = 1f; AnchorTop = 1f; AnchorBottom = 1f;
        OffsetLeft = -500f; OffsetRight = -16f; OffsetTop = -330f; OffsetBottom = -70f;
        MouseFilter = MouseFilterEnum.Ignore;
        _view = new SubViewport { World2D = GetViewport().World2D, TransparentBg = false, Msaa2D = Viewport.Msaa.Msaa2X };
        AddChild(_view);
        _cam = new Camera2D { Zoom = new Vector2(3.2f, 3.2f) };
        _view.AddChild(_cam);
        _cam.MakeCurrent();

        var frame = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        frame.SetAnchorsPreset(LayoutPreset.FullRect);
        var sb = new StyleBoxFlat { DrawCenter = false, BorderColor = new Color(1f, 1f, 1f, 0.55f) };
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(6);
        frame.AddThemeStyleboxOverride("panel", sb);
        AddChild(frame);
        var label = new Label { Text = "lure", Position = new Vector2(10, 4) };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 3);
        AddChild(label);
    }

    public override void _Process(double delta)
    {
        var w = Driver.World;
        var a = w.Anglers[0];
        var line = a.Rig.Line;
        var p = line.TerminalPos;
        bool want = a.Rig.Lure.Wet && p.X - a.Position.X > 6f;
        _show = Mathf.MoveToward(_show, want ? 1f : 0f, (float)delta * 3f);
        Visible = _show > 0.01f;
        Modulate = new Color(1f, 1f, 1f, _show);
        if (!Visible) return;
        // Frame the lure and the float (if any), a little toward the surface.
        var focus = p;
        if (line.FloatIndex >= 0 && line.FloatIndex < line.Count) focus = new TheLedger.Sim.Core.Vec2(0.5f * (p.X + line.X[line.FloatIndex]), 0.5f * (p.Y + line.Y[line.FloatIndex]));
        var target = SimSpace.ToPx(focus.X, focus.Y + 0.15f);
        _cam.Position = _cam.Position.DistanceTo(target) > 400f ? target : _cam.Position.Lerp(target, 1f - MathF.Exp(-8f * (float)delta));
    }
}
