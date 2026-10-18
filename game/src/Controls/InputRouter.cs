using Godot;
using TheLedger.Boot;
using TheLedger.Sim.Core;
using TheLedger.Sim.Input;

namespace TheLedger.Controls;

/// <summary>
/// Turns mouse, keyboard and gamepad state into one InputFrame per sim tick for one local player
/// (docs/03_FISHING_MECHANICS.md §2). Whichever device was used last drives the rod.
/// </summary>
public partial class InputRouter : Node
{
    public enum Device { MouseKeyboard, Gamepad }

    /// <summary>Which local player this router feeds (co-op ready: CLAUDE.md rule 15).</summary>
    public int PlayerIndex { get; set; }

    public Device ActiveDevice { get; private set; } = Device.MouseKeyboard;

    /// <summary>Where the angler's shoulder is on screen (world px), set by the view each frame.</summary>
    public Vector2 ShoulderPx { get; set; }

    /// <summary>When false, the mouse doesn't move the rod (e.g. while dragging a tuning slider).</summary>
    public bool MouseRodEnabled { get; set; } = true;

    /// <summary>Holding the right button reels at this steady speed, handle turns per second; the wheel adds on top.</summary>
    public float SteadyCrankRate { get; } = 1.5f;

    /// <summary>One-tick presses from the Tab menu (next lure / next rod).</summary>
    public bool PulseRig, PulseRack;

    /// <summary>Max crank rate for a fully pulled right trigger, turns/s.</summary>
    public float TriggerCrankMax = 3.0f;

    /// <summary>One wheel notch = this many handle turns.</summary>
    public float TurnsPerWheelNotch = 1f / 8f;

    /// <summary>Line hold ramps to full over this long when held with a digital button (feathering).</summary>
    public float HoldRampSeconds = 0.25f;

    private float _rodTarget = 0.6f;
    private bool _fine;
    private float _fineAnchor, _fineAim;

    /// <summary>The arm's reach in rod angle (matches SimConfig HandMinAngle / HandMaxAngle).</summary>
    public float ArmMin = -1.2f, ArmMax = 3.1f;

    /// <summary>
    /// Mouse angle into the arm's range. Pointing down and behind the angler is out of reach: the rod stays at
    /// whichever limit it was nearer to, so it never flips from "down in front" to "flat behind" (or back) on a
    /// tiny mouse move. It only goes behind when the cursor actually comes round above.
    /// </summary>
    private float Aim(float a, float previous)
    {
        if (a >= ArmMin) return MathF.Min(a, ArmMax);
        return previous - ArmMin < ArmMax - previous ? ArmMin : ArmMax;
    }
    private float _reach = 0.5f;
    private float _lineHold;
    private float _pendingWheelTurns;
    private int _pendingDragDelta;
    private float _stickAngle, _stickTurns, _stickCrank;
    private bool _stickTracking;

    /// <summary>The left stick must be pushed at least this far for its rotation to count as cranking.</summary>
    public float StickCrankDeadzone = 0.55f;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion mm)
        {
            if (mm.Relative.LengthSquared() > 1f) ActiveDevice = Device.MouseKeyboard;
        }
        else if (e is InputEventMouseButton mb && mb.Pressed)
        {
            ActiveDevice = Device.MouseKeyboard;
            int notch = mb.ButtonIndex switch
            {
                MouseButton.WheelUp => 1,
                MouseButton.WheelDown => -1,
                _ => 0,
            };
            if (notch != 0)
            {
                // Wheel up = crank forward (retrieve). Wheel down never back-reels: anti-reverse.
                // While V is held the wheel zooms the lure cam instead.
                if (notch > 0 && !Input.IsKeyPressed(Key.V)) _pendingWheelTurns += TurnsPerWheelNotch;
            }
        }
        else if (e is InputEventJoypadMotion jm && Mathf.Abs(jm.AxisValue) > 0.3f)
        {
            ActiveDevice = Device.Gamepad;
        }
        else if (e is InputEventJoypadButton { Pressed: true })
        {
            ActiveDevice = Device.Gamepad;
        }

        if (e.IsActionPressed(InputActions.DragTighter)) _pendingDragDelta++;
        if (e.IsActionPressed(InputActions.DragLooser)) _pendingDragDelta--;
    }

    /// <summary>Builds this tick's frame. Call once per sim tick.</summary>
    public InputFrame BuildFrame(float dt, Vector2 mouseWorldPx)
    {
        var f = new InputFrame();

        // Rod direction.
        Vector2 stick = Input.GetVector(InputActions.RodLeft, InputActions.RodRight, InputActions.RodUp, InputActions.RodDown);
        if (ActiveDevice == Device.Gamepad && stick.Length() > 0.3f)
        {
            _rodTarget = Mathf.Atan2(-stick.Y, stick.X);
            _reach = Mathf.Clamp((stick.Length() - 0.3f) / 0.7f, 0f, 1f);
        }
        else if (ActiveDevice == Device.MouseKeyboard && MouseRodEnabled)
        {
            Vector2 d = mouseWorldPx - ShoulderPx;
            // Ignore the cursor right next to the shoulder, where a pixel is a big angle.
            if (d.LengthSquared() > 80f * 80f)
            {
                float aim = Aim(Mathf.Atan2(-d.Y, d.X), _rodTarget);
                // Shift: fine control. The rod moves a quarter as far as the mouse, from where it was when Shift went down.
                if (Input.IsKeyPressed(Key.Shift))
                {
                    if (!_fine) { _fine = true; _fineAnchor = _rodTarget; _fineAim = aim; }
                    _rodTarget = _fineAnchor + (aim - _fineAim) * 0.25f;
                }
                else { _fine = false; _rodTarget = aim; }
            }
            // Arms reach on their own: up and out as the rod goes overhead and back, relaxed when it's low.
            _reach = Mathf.Clamp(0.35f + 0.5f * Mathf.Max(0f, Mathf.Sin(_rodTarget)), 0f, 1f);
        }
        f.RodTargetAngle = _rodTarget;
        f.RodReach = _reach;

        // Line hold: analog on the trigger, ramped on the mouse button.
        float holdStrength = Input.GetActionStrength(InputActions.LineHold);
        if (ActiveDevice == Device.Gamepad) _lineHold = holdStrength;
        else _lineHold = holdStrength > 0.5f ? Mathf.Min(1f, _lineHold + dt / HoldRampSeconds) : 0f;
        f.LineHold = _lineHold;

        // Crank: trigger (analog), right button (steady), or wheel turns spread over a few ticks.
        float crank = ActiveDevice == Device.Gamepad
            ? Input.GetActionStrength(InputActions.Crank) * TriggerCrankMax
            : (Input.IsActionPressed(InputActions.Crank) ? SteadyCrankRate : 0f);
        if (_pendingWheelTurns > 0f)
        {
            float maxTurnsThisTick = 6f * dt; // a fast-spun wheel cranks up to 6 turns/s
            float take = Mathf.Min(_pendingWheelTurns, maxTurnsThisTick);
            _pendingWheelTurns -= take;
            crank += take / dt;
        }
        crank += StickCrank(dt);
        f.CrankRate = crank;

        f.DragDelta = (sbyte)Math.Clamp(_pendingDragDelta, -8, 8);
        _pendingDragDelta = 0;

        f.Move = Input.GetActionStrength(InputActions.MoveRight) - Input.GetActionStrength(InputActions.MoveLeft);

        var b = InputButtons.None;
        if (Input.IsActionPressed(InputActions.Bail)) b |= InputButtons.Bail;
        if (Input.IsActionPressed(InputActions.Land)) b |= InputButtons.Land;
        if (Input.IsActionPressed(InputActions.RigMenu) || PulseRig) b |= InputButtons.RigMenu;
        if (Input.IsActionPressed(InputActions.Keep)) b |= InputButtons.Interact;
        if (Input.IsActionPressed(InputActions.RodRack) || PulseRack) b |= InputButtons.RodRack;
        PulseRig = PulseRack = false;
        if (Input.IsActionPressed(InputActions.BrakeUp)) b |= InputButtons.BrakeUp;
        if (Input.IsActionPressed(InputActions.BrakeDown)) b |= InputButtons.BrakeDown;
        f.Buttons = b;
        return f;
    }

    /// <summary>
    /// Reeling by rotating the left stick like a reel handle: clockwise turns count (anti-reverse ignores the other
    /// way), one full circle = one handle turn. Smoothed over ~0.1 s so a hand-drawn circle gives a steady rate.
    /// </summary>
    private float StickCrank(float dt)
    {
        var pads = Input.GetConnectedJoypads();
        if (pads.Count == 0) return 0f;
        int pad = pads[0];
        var v = new Vector2(Input.GetJoyAxis(pad, JoyAxis.LeftX), Input.GetJoyAxis(pad, JoyAxis.LeftY));
        if (v.Length() > StickCrankDeadzone)
        {
            float a = Mathf.Atan2(v.Y, v.X); // screen space (y down): increasing = clockwise
            if (_stickTracking)
            {
                float d = Mathf.Wrap(a - _stickAngle, -Mathf.Pi, Mathf.Pi);
                if (d > 0f) _stickTurns += d / Mathf.Tau;
                ActiveDevice = Device.Gamepad;
            }
            _stickAngle = a;
            _stickTracking = true;
        }
        else _stickTracking = false;
        float rate = _stickTurns / dt;
        _stickTurns = 0f;
        _stickCrank += (rate - _stickCrank) * MathF.Min(1f, dt / 0.1f);
        if (_stickCrank < 0.05f && !_stickTracking) _stickCrank = 0f;
        return MathF.Min(_stickCrank, 6f);
    }

    /// <summary>Text for the debug overlay: connected controllers and their raw axes.</summary>
    public static string DescribeJoypads()
    {
        var pads = Input.GetConnectedJoypads();
        if (pads.Count == 0) return "Controller: none connected";
        var sb = new System.Text.StringBuilder();
        foreach (int id in pads)
        {
            sb.Append($"Controller {id}: {Input.GetJoyName(id)}\n");
            sb.Append($"  L ({Input.GetJoyAxis(id, JoyAxis.LeftX),5:0.00},{Input.GetJoyAxis(id, JoyAxis.LeftY),5:0.00})");
            sb.Append($"  R ({Input.GetJoyAxis(id, JoyAxis.RightX),5:0.00},{Input.GetJoyAxis(id, JoyAxis.RightY),5:0.00})");
            sb.Append($"  LT {Input.GetJoyAxis(id, JoyAxis.TriggerLeft):0.00}  RT {Input.GetJoyAxis(id, JoyAxis.TriggerRight):0.00}\n");
        }
        return sb.ToString();
    }
}
