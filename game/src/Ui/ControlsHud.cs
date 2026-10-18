using Godot;
using TheLedger.Controls;
using TheLedger.SimGlue;

namespace TheLedger.Ui;

/// <summary>
/// The fishing HUD (owner's plan, 2026-10-18): bottom left, a line tension bar, the drag setting, metres of line out,
/// and a strip of key chips. Chips light up while their key is held, and a value pops when it changes, so adjusting
/// something is visible at a glance. Nothing else stays on screen.
/// </summary>
public partial class ControlsHud : Control
{
    public SimDriver Driver { get; set; } = null!;
    public InputRouter Router { get; set; } = null!;

    private int _lastDrag = -1;
    private float _dragPop, _bailPop;
    private bool _lastBail;

    private static readonly Color Panel = new(0.04f, 0.07f, 0.09f, 0.72f);
    private static readonly Color Dim = new(0.75f, 0.8f, 0.82f);
    private static readonly Color Lit = new(1f, 0.85f, 0.45f);
    private static readonly Color BarBack = new(1f, 1f, 1f, 0.12f);

    private static readonly (string key, string label, Func<bool> held)[] Chips =
    {
        ("Space", "bail", () => Input.IsActionPressed(InputActions.Bail)),
        ("LMB", "hold line", () => Input.IsActionPressed(InputActions.LineHold)),
        ("RMB / wheel", "reel", () => Input.IsActionPressed(InputActions.Crank)),
        ("Q / E", "drag", () => Input.IsActionPressed(InputActions.DragLooser) || Input.IsActionPressed(InputActions.DragTighter)),
        ("Mouse ←→", "rod back / forward", () => false),
        ("Shift", "fine rod", () => Input.IsKeyPressed(Key.Shift)),
        ("F", "land", () => Input.IsActionPressed(InputActions.Land)),
        ("R", "keep", () => Input.IsActionPressed(InputActions.Keep)),
        ("V", "lure cam", () => Input.IsKeyPressed(Key.V)),
        ("Tab", "tackle", () => Input.IsKeyPressed(Key.Tab)),
    };

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        var r = Driver.World.Anglers[0].Reel;
        if (_lastDrag >= 0 && r.DragClick != _lastDrag) _dragPop = 1f;
        if (r.BailOpen != _lastBail) _bailPop = 1f;
        _lastDrag = r.DragClick;
        _lastBail = r.BailOpen;
        _dragPop = MathF.Max(0f, _dragPop - (float)delta * 2.5f);
        _bailPop = MathF.Max(0f, _bailPop - (float)delta * 2.5f);
        // Redraw only when something shown changes (re-laying out text every frame cost ~15% of the frame rate).
        int held = 0;
        for (int i = 0; i < Chips.Length; i++) if (Chips[i].held()) held |= 1 << i;
        var a = Driver.World.Anglers[0];
        var line = a.Rig.Line;
        int fishKey = 0;
        foreach (var f in Driver.World.Fish)
            if (f.State == TheLedger.Sim.Fish.FishState.Hooked && f.LureAngler == 0)
            {
                fishKey = HashCode.Combine((int)(f.StaminaA * 50f), (int)(f.HeadX * 10f), (int)(f.HeadY * 10f));
                break;
            }
        int key = HashCode.Combine((int)(line.TipTension * 4f), r.DragClick, (int)(line.LineOut * 10f), r.BailOpen, r.DragSlipping,
                                   HashCode.Combine(held, (int)(_dragPop * 20f), (int)(_bailPop * 20f), fishKey, (int)(r.Loose * 50f), r.BrakeClick,
                                                    (int)GetViewportRect().Size.Y));
        if (key != _drawnKey) { _drawnKey = key; QueueRedraw(); }
    }

    private int _drawnKey;

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        var a = Driver.World.Anglers[0];
        var r = a.Reel;
        var line = a.Rig.Line;
        float vh = GetViewportRect().Size.Y;

        const float w0 = 500f, h = 160f, pad = 14f;
        const float w = w0;
        var box = new Rect2(16f, vh - 16f - h, w, h);
        DrawRect(box, Panel);
        float x = box.Position.X + pad, y = box.Position.Y + pad;

        // Tension: how close the line is to breaking (knot strength), green → amber → red.
        float limit = line.Spec.BreakingForceN * line.Spec.KnotFactor;
        float frac = Math.Clamp(line.TipTension / MathF.Max(limit, 1e-3f), 0f, 1f);
        DrawString(font, new Vector2(x, y + 15f), "TENSION", HorizontalAlignment.Left, -1, 15, Dim);
        var bar = new Rect2(x + 84f, y + 3f, w - 2f * pad - 84f, 14f);
        DrawRect(bar, BarBack);
        var col = frac < 0.5f ? new Color(0.45f, 0.85f, 0.5f) : frac < 0.8f ? new Color(1f, 0.75f, 0.3f) : new Color(1f, 0.3f, 0.25f);
        DrawRect(new Rect2(bar.Position, new Vector2(bar.Size.X * frac, bar.Size.Y)), col);
        if (r.DragSlipping)
            DrawString(font, new Vector2(bar.End.X - 52f, y + 15f), "DRAG", HorizontalAlignment.Left, -1, 14, Colors.White);
        y += 30f;

        // Drag setting and line out.
        float pop = _dragPop;
        DrawString(font, new Vector2(x, y + 18f), $"Drag {r.DragForce / 9.81f:0.0} kg", HorizontalAlignment.Left, -1,
                   (int)(20 + 6 * pop), Dim.Lerp(Lit, pop));
        DrawString(font, new Vector2(x + 200f, y + 18f), $"Line out {line.LineOut:0.0} m", HorizontalAlignment.Left, -1, 20, Dim);
        y += 30f;
        string bail = r.BailOpen ? (r.Spec.IsBaitcaster ? "Free spool" : "Bail OPEN") : "Bail closed";
        DrawString(font, new Vector2(x, y + 16f), bail, HorizontalAlignment.Left, -1, (int)(17 + 5 * _bailPop),
                   r.BailOpen ? Lit : Dim.Lerp(Lit, _bailPop));
        if (r.Spec.IsBaitcaster)
        {
            // Baitcaster: brake setting ([ / ]) and how much loose line the spool has overrun (a bird's nest at full).
            DrawString(font, new Vector2(x + 130f, y + 16f), $"Brake {r.BrakeClick}/{r.Spec.BrakeClicks}  [ / ]", HorizontalAlignment.Left, -1, 15, Dim);
            float over = Math.Clamp(r.Loose / r.Spec.BacklashLooseM, 0f, 1f);
            var ob = new Rect2(x + 300f, y + 5f, 140f, 10f);
            DrawRect(ob, BarBack);
            DrawRect(new Rect2(ob.Position, new Vector2(ob.Size.X * over, ob.Size.Y)), over < 0.6f ? Lit : new Color(1f, 0.3f, 0.25f));
        }
        y += 28f;

        // Fish on: how far out it is and how much fight it has left, so you can see when F will work.
        TheLedger.Sim.Fish.Fish? hooked = null;
        foreach (var f in Driver.World.Fish) if (f.State == TheLedger.Sim.Fish.FishState.Hooked && f.LureAngler == 0) { hooked = f; break; }
        if (hooked != null)
        {
            var wd = Driver.World;
            float dist = MathF.Abs(hooked.HeadX - a.Position.X);
            float depth = wd.Water.Height(hooked.HeadX, wd.Time) - hooked.HeadY;
            bool reach = (dist < wd.Config.LandReach || (hooked.Mouth - a.Rig.Rod.TipPos).Length < 1.6f) && depth < 0.6f;
            bool tired = hooked.StaminaA < wd.Config.LandStamina || hooked.MassKg < wd.Config.SwingInKg;
            var fishBox = new Rect2(box.Position.X, box.Position.Y - 40f, w0, 34f);
            DrawRect(fishBox, Panel);
            string state = reach ? (tired ? "in reach and beaten: press F" : "in reach: F to try, it's still lively") : $"{dist - wd.Config.LandReach:0.0} m too far to net";
            DrawString(font, new Vector2(x, fishBox.Position.Y + 23f), $"FISH  {state}", HorizontalAlignment.Left, -1, 16, reach ? Lit : Dim);
            var eb = new Rect2(fishBox.End.X - pad - 110f, fishBox.Position.Y + 12f, 110f, 10f);
            DrawRect(eb, BarBack);
            DrawRect(new Rect2(eb.Position, new Vector2(eb.Size.X * Math.Clamp(hooked.StaminaA, 0f, 1f), eb.Size.Y)), new Color(0.9f, 0.55f, 0.3f));
        }

        // Key chips, two rows; a chip lights while its key is held.
        float cx = x, cy = y;
        foreach (var (key, label, held) in Chips)
        {
            bool on = held();
            float kw = font.GetStringSize(key, HorizontalAlignment.Left, -1, 15).X + 10f;
            float lw = font.GetStringSize(label, HorizontalAlignment.Left, -1, 15).X;
            if (cx + kw + lw + 6f > box.End.X - pad) { cx = x; cy += 25f; }
            DrawRect(new Rect2(cx, cy, kw, 20f), on ? Lit : new Color(1f, 1f, 1f, 0.16f));
            DrawString(font, new Vector2(cx + 5f, cy + 15f), key, HorizontalAlignment.Left, -1, 15, on ? Colors.Black : Colors.White);
            DrawString(font, new Vector2(cx + kw + 4f, cy + 15f), label, HorizontalAlignment.Left, -1, 15, on ? Lit : Dim);
            cx += kw + lw + 14f;
        }
    }
}
