using Godot;
using TheLedger.Sim.Core;
using TheLedger.SimGlue;

namespace TheLedger.Ui;

/// <summary>
/// Keeps a new angler on track (owner, 2026-11-06: "add some more guides to keep the player on the right track, at
/// least in the beginning"). Top left:
/// <list type="bullet">
/// <item>the current goal from the field notes, always shown, with what to do next when it's a payment;</item>
/// <item>the first-hour lessons (bail, cast, bite, fight, land, release, field notes, the survey), each finished by
/// actually doing it, read from the sim and its events;</item>
/// <item>one-off tips the first time something comes up (the cooler, the board, new stock, a new spot, night).</item>
/// </list>
/// Progress is saved in the profile, so lessons never repeat. H hides the lessons and tips (not the goal line).
/// </summary>
public partial class Coach : PanelContainer
{
    public SimDriver Driver { get; set; } = null!;

    private sealed record Lesson(string Title, string Text);

    // Lesson order is the CoachStep value. Done = past the last one.
    private static readonly Lesson[] Lessons =
    {
        new("Open the bail", "Press {bail} to open the bail, so line can leave the reel."),
        new("Cast", "Hold {line_hold} to pin the line with your finger. Swing the mouse back, then forward, and let go of {line_hold} as the rod comes over the top."),
        new("Close the bail", "Turn the reel ({crank} or the wheel) to flip the bail shut. The bobber is fishing now."),
        new("Watch the bobber", "When it dips or slides away, sweep the rod back hard (mouse back) to set the hook. Nothing for a while? Reel a few turns: a moving bait draws fish."),
        new("Fish on!", "Keep the line tight. Reel while it comes; when it runs, let the drag slip ({drag_looser} looser, {drag_tighter} tighter). Keep the tension bar out of the red."),
        new("Land it", "Bring it close to your feet, near the surface, then press {land} to land it. A big fish has to tire first."),
        new("Keep or release", "Measure done. Odile wants survey fish released: press {land} to let it go. {keep} keeps it in your cooler to sell at Junie's."),
        new("Field notes", "Press Tab and open Field notes: Odile's survey, what's left to catch, and Wren's pages."),
        new("The survey", "Release five different species from the pond. The worm under a bobber takes panfish near the bank; the sinker on the bottom finds catfish. Change bait from the Tab menu (reel in first)."),
    };
    private static int Done => Lessons.Length;

    private Label _goal = null!, _head = null!, _title = null!, _text = null!;
    private VBoxContainer _lessonBox = null!;
    private string _tip = "", _tipWho = "";
    private double _tipTime;
    private int _stocked = -1, _openSpots = -1;
    private List<SpotInfo> _spots = new();
    private int _spotsStep = -1;
    private int _key;

    /// <summary>Main tells the coach when a panel has been opened (so "open the field notes" can finish).</summary>
    public void Saw(string what)
    {
        var p = ProfileStore.Current;
        if (what == "notes" && p.CoachStep == 7) Next();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Position = new Vector2(16, 50);
        var sb = new StyleBoxFlat { BgColor = new Color(0.04f, 0.07f, 0.09f, 0.62f) };
        sb.SetCornerRadiusAll(8);
        sb.SetContentMarginAll(12);
        AddThemeStyleboxOverride("panel", sb);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(400, 0) };
        box.AddThemeConstantOverride("separation", 4);
        AddChild(box);
        _goal = MakeLabel(box, 15, new Color(0.93f, 0.95f, 0.93f), UiFonts.Body);
        _lessonBox = new VBoxContainer();
        _lessonBox.AddThemeConstantOverride("separation", 2);
        box.AddChild(_lessonBox);
        _lessonBox.AddChild(new HSeparator { Modulate = new Color(1f, 1f, 1f, 0.25f) });
        _head = MakeLabel(_lessonBox, 12, new Color(1f, 0.84f, 0.45f), UiFonts.Bold);
        _title = MakeLabel(_lessonBox, 18, Colors.White, UiFonts.Bold);
        _text = MakeLabel(_lessonBox, 15, new Color(0.82f, 0.86f, 0.88f), UiFonts.Body);

        // Anglers who already fished before the coach existed skip the lessons.
        var p = ProfileStore.Current;
        if (p.CoachStep < Done && (p.StoryStep > 0 || LedgerStore.Entries.Count > 2)) { p.CoachStep = Done; ProfileStore.Save(); }
        _spots = SpotInfo.All();
        Driver.EventRaised += OnEvent;
    }

    public override void _ExitTree() => Driver.EventRaised -= OnEvent;

    private static Label MakeLabel(VBoxContainer parent, int size, Color color, Font font)
    {
        var l = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(400, 0), Modulate = color };
        l.AddThemeFontOverride("font", font);
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.6f));
        l.AddThemeConstantOverride("outline_size", 3);
        parent.AddChild(l);
        return l;
    }

    private static void Next()
    {
        var p = ProfileStore.Current;
        p.CoachStep = Math.Min(p.CoachStep + 1, Done);
        ProfileStore.Save();
    }

    private static void Set(int step)
    {
        var p = ProfileStore.Current;
        if (p.CoachStep == step || p.CoachStep >= Done) return;
        p.CoachStep = step;
        ProfileStore.Save();
    }

    private void OnEvent(SimEvent e)
    {
        if (e.Angler != 0) return;
        int s = ProfileStore.Current.CoachStep;
        switch (e.Type)
        {
            case SimEventType.HookSet when s <= 4: Set(4); break;
            case SimEventType.FishLanded when s <= 6: Set(6); break;
            case SimEventType.FishReleased or SimEventType.FishKept when s == 6: Next(); break;
            case SimEventType.HookPulled or SimEventType.LineBroke when s is 4 or 5:
                Set(0);
                Tip("Odile", "Lost it. Happens to everyone. Open the bail and cast again.", once: null);
                break;
            case SimEventType.LandFailed when s == 5:
                Tip("Odile", "Not yet: it has to be close to your feet and tired out. Keep the pressure on.", once: null);
                break;
            case SimEventType.Snagged:
                Tip("Odile", "Snagged. Try pulling from a different angle (walk the rod round), or pull hard and lose the rig.", "snag");
                break;
            case SimEventType.Backlash:
                Tip("Teodor", "A bird's nest. On a baitcaster your thumb (left mouse) slows the spool as the lure slows. Hold R to pick it out.", "backlash");
                break;
        }
    }

    /// <summary>Shows a tip for a while. With an id it only ever shows once per profile.</summary>
    private void Tip(string who, string text, string? once, double seconds = 14)
    {
        var p = ProfileStore.Current;
        if (once != null)
        {
            if (p.TipsSeen.Contains(once)) return;
            p.TipsSeen.Add(once);
            ProfileStore.Save();
        }
        _tipWho = who;
        _tip = text;
        _tipTime = seconds;
    }

    public override void _Process(double delta)
    {
        if (GameMenu.IsOpen) return;
        var p = ProfileStore.Current;
        var w = Driver.World;
        var a = w.Anglers[0];
        var line = a.Rig.Line;
        float retie = Driver.Config.RetieMaxLineOut;
        _tipTime -= delta;

        // Lessons that finish from what the angler does.
        switch (p.CoachStep)
        {
            case 0 when a.Reel.BailOpen || line.LineOut > 6f: Next(); break;
            case 1 when line.LineOut > 6f: Next(); break;
            case 1 when !a.Reel.BailOpen && line.LineOut <= retie + 0.3f: Set(0); break;
            case 2 when !a.Reel.BailOpen && line.LineOut > 3f: Next(); break;
            case 2 or 3 when line.LineOut <= retie + 0.3f: Set(a.Reel.BailOpen ? 1 : 0); break;
            case 4 when HookedNear(a): Next(); break;
            case 8 when p.StoryStep > 0: Next(); break;
        }

        // One-off tips, once the lessons are through (or nearly).
        if (p.CoachStep >= 7)
        {
            if (p.Cooler.Count > 0) Tip("Junie", "Kept fish ride in your cooler. I'll buy them: Tab, then Bait & tackle shop. Prices by the kilo.", "cooler");
            else if (Story.Current.Goal.Type == "pay" && p.Money >= Story.Current.Goal.Amount)
                Tip(Story.Current.Giver, $"You've got the ${Story.Current.Goal.Amount:0}. Tab, Field notes, and pay up.", "paid_" + Story.Current.Id);
            else if (p.CoachStep >= Done && p.TipsSeen.Contains("cooler"))
                Tip("Junie", "My board's top right: small jobs every day, paid the moment they're done. Bigger gear comes into the shop as your surveys go on.", "board");
        }
        int stocked = StockedCount();
        if (_stocked >= 0 && stocked > _stocked) Tip("Junie", "New gear in at the shop. Tab, Bait & tackle shop.", once: null);
        _stocked = stocked;
        if (_spotsStep != p.StoryStep) // spots only open when the story moves on
        {
            _spotsStep = p.StoryStep;
            int spots = _spots.Count(s => Story.SpotOpen(s.Id));
            if (_openSpots >= 0 && spots > _openSpots) Tip("Odile", "Somewhere new is open on the map. Tab, Travel (gas costs a few dollars).", once: null);
            _openSpots = spots;
        }
        float hour = w.Hour;
        if (hour > 20.5f || hour < 5f)
            Tip("Junie", "Night. Catfish, bullheads and eels feed now: a sinker on the bottom. Or Tab, Call it a day, and sleep till six.", "night");

        Refresh(p);
    }

    /// <summary>A hooked fish has been brought within landing range (time to press F).</summary>
    private bool HookedNear(TheLedger.Sim.Angler.AnglerState a)
    {
        foreach (var f in Driver.World.Fish)
            if (f.State == TheLedger.Sim.Fish.FishState.Hooked && f.LureAngler == 0
                && MathF.Abs(f.HeadX - a.Position.X) < Driver.Config.LandReach * 1.3f && f.HeadY > -1.5f)
                return true;
        return false;
    }

    private int StockedCount()
    {
        var shop = ProfileStore.Shop;
        int n = 0;
        foreach (var id in shop.Lures.Keys.Concat(shop.Outfits.Keys)) if (shop.Stocked(id).open) n++;
        return n;
    }

    /// <summary>"{action}" becomes the key bound to it now (lessons follow the player's rebinding).</summary>
    private static string Keys(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, "\\{([a-z_]+)\\}", m => TheLedger.Controls.InputActions.Label(m.Groups[1].Value));

    private string GoalLine(Profile p)
    {
        var step = Story.Current;
        if (step.Goal.Type == "none") return $"Goal: {step.Title}";
        var (have, need, _) = Story.Progress();
        if (step.Goal.Type == "pay")
            return p.Money >= step.Goal.Amount
                ? $"Goal: {step.Title}. Pay ${step.Goal.Amount:0} in Field notes (Tab)"
                : $"Goal: {step.Title}. Save ${step.Goal.Amount:0} (you have ${p.Money:0}): sell fish, do Junie's board";
        string where = _spots.FirstOrDefault(s => s.Id == step.Goal.Spot)?.Name ?? "";
        string travel = step.Goal.Spot != "" && step.Goal.Spot != Driver.Info.Id ? $". Travel to {where} (Tab)" : "";
        return $"Goal: {step.Title}  {have}/{need}{travel}";
    }

    private void Refresh(Profile p)
    {
        bool lesson = p.CoachStep < Done && ControlsHud.ShowHints;
        bool tip = _tipTime > 0 && ControlsHud.ShowHints;
        string goal = GoalLine(p);
        int key = HashCode.Combine(goal, lesson ? p.CoachStep : -1, tip ? _tip : "", TheLedger.Controls.InputActions.Version);
        if (key == _key) return;
        _key = key;
        _goal.Text = goal;
        _lessonBox.Visible = lesson || tip;
        if (tip)
        {
            _head.Text = _tipWho.ToUpperInvariant();
            _title.Visible = false;
            _text.Text = _tip;
        }
        else if (lesson)
        {
            _head.Text = $"ODILE'S FIRST LESSONS  {p.CoachStep + 1}/{Done}";
            _title.Visible = true;
            _title.Text = Lessons[p.CoachStep].Title;
            _text.Text = Keys(Lessons[p.CoachStep].Text);
        }
        Size = new Vector2(Size.X, 0f); // shrink back to the text
    }
}
