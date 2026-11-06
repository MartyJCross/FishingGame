using Godot;

namespace TheLedger.Controls;

/// <summary>
/// All input actions, registered in code so the bindings live in one reviewable place (docs/03_FISHING_MECHANICS.md
/// §2). Defaults are mouse + keyboard and an Xbox-layout controller (the owner uses a GameSir in Xbox layout).
/// Keyboard and mouse bindings can be changed in Settings, Controls (<see cref="Rebind"/>, saved in settings.cfg
/// [keys]); the controller layout stays as designed.
/// </summary>
public static class InputActions
{
    public const string LineHold = "line_hold";
    public const string Crank = "crank";
    public const string DragTighter = "drag_tighter";
    public const string DragLooser = "drag_looser";
    public const string Bail = "bail";
    public const string Land = "land";
    public const string RigMenu = "rig_menu";
    public const string Keep = "keep";
    public const string RodRack = "rod_rack";
    public const string BrakeUp = "brake_up";
    public const string BrakeDown = "brake_down";
    public const string ToggleLedger = "toggle_ledger";
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
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
        // Bail on LB so both thumbs stay on the sticks (owner, 2026-10-18); B still works.
        Add(Bail, 0.5f, KeyEv(Key.Space), Button(JoyButton.LeftShoulder), Button(JoyButton.B));
        Add(Land, 0.5f, KeyEv(Key.F), Button(JoyButton.RightShoulder), Button(JoyButton.A));
        Add(Keep, 0.5f, KeyEv(Key.R), Button(JoyButton.Y));
        Add(ToggleLedger, 0.5f, KeyEv(Key.L), Button(JoyButton.Start));
        Add(RigMenu, 0.5f, Button(JoyButton.X)); // keyboard: Tab menu
        Add(RodRack, 0.5f, Button(JoyButton.RightStick)); // keyboard: Tab menu
        Add(BrakeUp, 0.5f, KeyEv(Key.Bracketright), Button(JoyButton.DpadRight));
        Add(BrakeDown, 0.5f, KeyEv(Key.Bracketleft), Button(JoyButton.DpadLeft));
        Add(MoveLeft, 0.2f, KeyEv(Key.A)); // the left stick reels (rotate it)
        Add(MoveRight, 0.2f, KeyEv(Key.D));
        Add(RodLeft, 0.15f, Axis(JoyAxis.RightX, -1f));
        Add(RodRight, 0.15f, Axis(JoyAxis.RightX, 1f));
        Add(RodUp, 0.15f, Axis(JoyAxis.RightY, -1f));
        Add(RodDown, 0.15f, Axis(JoyAxis.RightY, 1f));
        Add(ToggleTuning, 0.5f, KeyEv(Key.F1));
        Add(ToggleDebug, 0.5f, KeyEv(Key.F3));
        LoadBindings();
    }

    /// <summary>The actions a player can rebind on keyboard and mouse, with the names the Controls page shows.</summary>
    public static readonly (string action, string name)[] Rebindable =
    {
        (Bail, "Open / close the bail"), (LineHold, "Hold the line (finger / thumb)"), (Crank, "Reel"),
        (Land, "Land / release a fish"), (Keep, "Keep a fish / clear a bird's nest"),
        (DragTighter, "Drag tighter"), (DragLooser, "Drag looser"), (BrakeUp, "Baitcaster brake up"), (BrakeDown, "Baitcaster brake down"),
        (MoveLeft, "Walk left"), (MoveRight, "Walk right"), (ToggleLedger, "The Ledger"),
    };

    /// <summary>Goes up on every rebind (the HUD redraws its key hints).</summary>
    public static int Version { get; private set; }

    private const string SettingsPath = "user://settings.cfg";
    private static readonly Dictionary<string, InputEvent> Defaults = new();

    private static bool IsPad(InputEvent e) => e is InputEventJoypadButton or InputEventJoypadMotion;

    /// <summary>The keyboard or mouse event bound to an action (null when it has none).</summary>
    public static InputEvent? KeyboardEvent(string action) =>
        InputMap.HasAction(action) ? InputMap.ActionGetEvents(action).FirstOrDefault(e => !IsPad(e)) : null;

    /// <summary>Short text for a binding: "Space", "LMB", "E".</summary>
    public static string Label(InputEvent? e) => e switch
    {
        InputEventKey k => OS.GetKeycodeString(k.PhysicalKeycode != Key.None ? k.PhysicalKeycode : k.Keycode),
        InputEventMouseButton { ButtonIndex: MouseButton.Left } => "LMB",
        InputEventMouseButton { ButtonIndex: MouseButton.Right } => "RMB",
        InputEventMouseButton { ButtonIndex: MouseButton.Middle } => "MMB",
        InputEventMouseButton { ButtonIndex: MouseButton.Xbutton1 } => "Mouse 4",
        InputEventMouseButton { ButtonIndex: MouseButton.Xbutton2 } => "Mouse 5",
        InputEventMouseButton m => $"Mouse {(int)m.ButtonIndex}",
        _ => "—",
    };

    public static string Label(string action) => Label(KeyboardEvent(action));

    /// <summary>Puts a new key or mouse button on an action (replacing its old keyboard/mouse binding) and saves it.</summary>
    public static void Rebind(string action, InputEvent e)
    {
        if (!Defaults.ContainsKey(action) && KeyboardEvent(action) is { } d) Defaults[action] = d;
        Apply(action, e);
        var cfg = new ConfigFile();
        cfg.Load(SettingsPath);
        cfg.SetValue("keys", action, Encode(e));
        cfg.Save(SettingsPath);
    }

    /// <summary>Back to the keys the game shipped with.</summary>
    public static void ResetBindings()
    {
        foreach (var (action, ev) in Defaults) Apply(action, ev);
        var cfg = new ConfigFile();
        cfg.Load(SettingsPath);
        if (cfg.HasSection("keys")) cfg.EraseSection("keys");
        cfg.Save(SettingsPath);
    }

    private static void Apply(string action, InputEvent e)
    {
        foreach (var old in InputMap.ActionGetEvents(action).Where(x => !IsPad(x)).ToList()) InputMap.ActionEraseEvent(action, old);
        InputMap.ActionAddEvent(action, e);
        Version++;
    }

    private static string Encode(InputEvent e) => e switch
    {
        InputEventKey k => $"key:{(long)(k.PhysicalKeycode != Key.None ? k.PhysicalKeycode : k.Keycode)}",
        InputEventMouseButton m => $"mouse:{(long)m.ButtonIndex}",
        _ => "",
    };

    private static InputEvent? Decode(string s)
    {
        var parts = s.Split(':');
        if (parts.Length != 2 || !long.TryParse(parts[1], out long v)) return null;
        return parts[0] switch
        {
            "key" => new InputEventKey { PhysicalKeycode = (Key)v },
            "mouse" => new InputEventMouseButton { ButtonIndex = (MouseButton)v },
            _ => null,
        };
    }

    private static void LoadBindings()
    {
        foreach (var (action, _) in Rebindable)
            if (!Defaults.ContainsKey(action) && KeyboardEvent(action) is { } d) Defaults[action] = d;
        var cfg = new ConfigFile();
        if (cfg.Load(SettingsPath) != Error.Ok || !cfg.HasSection("keys")) return;
        foreach (var (action, _) in Rebindable)
            if (cfg.HasSectionKey("keys", action) && Decode((string)cfg.GetValue("keys", action)) is { } e) Apply(action, e);
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
