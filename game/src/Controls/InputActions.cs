using Godot;

namespace TheLedger.Controls;

/// <summary>
/// All input actions, registered in code so the bindings live in one reviewable place (docs/03_FISHING_MECHANICS.md
/// §2). Defaults are mouse + keyboard and an Xbox-layout controller (the owner uses a GameSir in Xbox layout).
/// Rebinding UI arrives in M7; it will edit these actions through InputMap.
/// </summary>
public static class InputActions
{
    public const string LineHold = "line_hold";
    public const string Crank = "crank";
    public const string DragTighter = "drag_tighter";
    public const string DragLooser = "drag_looser";
    public const string CrankSpeedUp = "crank_speed_up";
    public const string CrankSpeedDown = "crank_speed_down";
    public const string Bail = "bail";
    public const string Land = "land";
    public const string RigMenu = "rig_menu";
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string ReachUp = "reach_up";
    public const string ReachDown = "reach_down";
    public const string RodLeft = "rod_left";
    public const string RodRight = "rod_right";
    public const string RodUp = "rod_up";
    public const string RodDown = "rod_down";
    public const string ToggleTuning = "toggle_tuning";
    public const string ToggleDebug = "toggle_debug";

    public static void Register()
    {
        Add(LineHold, 0.05f, Mouse(MouseButton.Left), Axis(JoyAxis.TriggerLeft, 1f));
        Add(Crank, 0.05f, Mouse(MouseButton.Right), Axis(JoyAxis.TriggerRight, 1f));
        Add(DragTighter, 0.5f, KeyEv(Key.E), Button(JoyButton.DpadUp));
        Add(DragLooser, 0.5f, KeyEv(Key.Q), Button(JoyButton.DpadDown));
        Add(CrankSpeedUp, 0.5f, KeyEv(Key.C));
        Add(CrankSpeedDown, 0.5f, KeyEv(Key.Z));
        // Bail on LB so both thumbs stay on the sticks (owner, 2026-10-18); B still works.
        Add(Bail, 0.5f, KeyEv(Key.Space), Button(JoyButton.LeftShoulder), Button(JoyButton.B));
        Add(Land, 0.5f, KeyEv(Key.F), Button(JoyButton.RightShoulder), Button(JoyButton.A));
        Add(RigMenu, 0.5f, KeyEv(Key.R), Button(JoyButton.X), Button(JoyButton.DpadRight));
        Add(MoveLeft, 0.2f, KeyEv(Key.A)); // the left stick reels (rotate it)
        Add(MoveRight, 0.2f, KeyEv(Key.D));
        Add(ReachUp, 0.2f, KeyEv(Key.W));
        Add(ReachDown, 0.2f, KeyEv(Key.S));
        Add(RodLeft, 0.15f, Axis(JoyAxis.RightX, -1f));
        Add(RodRight, 0.15f, Axis(JoyAxis.RightX, 1f));
        Add(RodUp, 0.15f, Axis(JoyAxis.RightY, -1f));
        Add(RodDown, 0.15f, Axis(JoyAxis.RightY, 1f));
        Add(ToggleTuning, 0.5f, KeyEv(Key.F1));
        Add(ToggleDebug, 0.5f, KeyEv(Key.F3));
    }

    private static void Add(string action, float deadzone, params InputEvent[] events)
    {
        if (InputMap.HasAction(action)) return;
        InputMap.AddAction(action, deadzone);
        foreach (var e in events) InputMap.ActionAddEvent(action, e);
    }

    private static InputEvent KeyEv(Key k) => new InputEventKey { PhysicalKeycode = k };
    private static InputEvent Mouse(MouseButton b) => new InputEventMouseButton { ButtonIndex = b };
    private static InputEvent Button(JoyButton b) => new InputEventJoypadButton { ButtonIndex = b, Device = -1 };
    private static InputEvent Axis(JoyAxis a, float dir) =>
        new InputEventJoypadMotion { Axis = a, AxisValue = dir, Device = -1 };
}
