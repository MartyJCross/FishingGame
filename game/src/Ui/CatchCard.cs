using Godot;
using TheLedger.Sim.World;

namespace TheLedger.Ui;

/// <summary>
/// The moment a fish is landed: a field-guide card slides down with the fish's picture, names, length and weight,
/// stamps for a new species, a personal best or a legend, and what it's worth. It waits for keep or release, says
/// which, then slides away.
/// </summary>
public partial class CatchCard : Control
{
    private static readonly Color Paper = new(0.95f, 0.92f, 0.84f, 0.97f);
    private static readonly Color PaperEdge = new(0.62f, 0.52f, 0.36f);
    private static readonly Color Ink = new(0.13f, 0.15f, 0.16f);
    private static readonly Color InkSoft = new(0.36f, 0.37f, 0.36f);
    private static readonly Color Red = new(0.72f, 0.16f, 0.12f);
    private static readonly Color Gold = new(0.7f, 0.5f, 0.08f);
    private static readonly Color Green = new(0.18f, 0.45f, 0.25f);

    private const float W = 760f, H = 236f, Top = 84f;

    private PanelContainer _card = null!;
    private FishPortrait _portrait = null!;
    private Label _name = null!, _sci = null!, _size = null!, _note = null!, _footer = null!;
    private HBoxContainer _stamps = null!;
    private Tween? _tween;
    private double _hideIn = -1;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        var style = new StyleBoxFlat
        {
            BgColor = Paper, BorderColor = PaperEdge, CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6, ShadowColor = new Color(0f, 0f, 0f, 0.35f), ShadowSize = 14, ShadowOffset = new Vector2(0f, 6f),
            ContentMarginLeft = 20, ContentMarginRight = 22, ContentMarginTop = 16, ContentMarginBottom = 14,
        };
        style.SetBorderWidthAll(2);
        _card = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _card.AddThemeStyleboxOverride("panel", style);
        _card.AnchorLeft = _card.AnchorRight = 0.5f;
        _card.OffsetLeft = -W / 2f; _card.OffsetRight = W / 2f;
        _card.OffsetTop = Top; _card.OffsetBottom = Top + H;
        AddChild(_card);

        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 18);
        _card.AddChild(row);
        _portrait = new FishPortrait { CustomMinimumSize = new Vector2(280f, 150f), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        row.AddChild(_portrait);

        var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(400f, 0f) };
        col.AddThemeConstantOverride("separation", 2);
        row.AddChild(col);
        _stamps = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _stamps.AddThemeConstantOverride("separation", 10);
        col.AddChild(_stamps);
        _name = MakeLabel(col, UiFonts.Title, 38, Ink);
        _sci = MakeLabel(col, UiFonts.TitleLight, 19, InkSoft);
        col.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 8f), MouseFilter = MouseFilterEnum.Ignore });
        _size = MakeLabel(col, UiFonts.Bold, 24, Ink);
        _note = MakeLabel(col, UiFonts.Body, 17, InkSoft);
        _note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _note.CustomMinimumSize = new Vector2(400f, 0f);
        col.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        _footer = MakeLabel(col, UiFonts.Bold, 18, Green);
    }

    private static Label MakeLabel(Control parent, Font font, int size, Color c)
    {
        var l = new Label { MouseFilter = MouseFilterEnum.Ignore };
        l.AddThemeFontOverride("font", font);
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", c);
        parent.AddChild(l);
        return l;
    }

    private void Stamp(string text, Color c)
    {
        var box = new StyleBoxFlat { BgColor = new Color(c, 0.08f), BorderColor = c, ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 1, ContentMarginBottom = 1 };
        box.SetBorderWidthAll(2);
        box.SetCornerRadiusAll(3);
        var p = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Rotation = -0.05f };
        p.AddThemeStyleboxOverride("panel", box);
        var l = MakeLabel(p, UiFonts.Bold, 15, c);
        l.Text = text;
        _stamps.AddChild(p);
    }

    /// <summary>A fish is in hand: show its card and what the angler may do with it.</summary>
    public void ShowCatch(Catch c, string legend, string choice, bool newSpecies, float previousBestKg)
    {
        foreach (var child in _stamps.GetChildren()) child.QueueFree();
        if (legend != "") Stamp($"LEGEND  ·  {legend.ToUpperInvariant()}", Gold);
        if (newSpecies) Stamp("NEW SPECIES", Red);
        else if (c.WeightKg > previousBestKg) Stamp("PERSONAL BEST", Gold);
        if (_stamps.GetChildCount() == 0 && !newSpecies) _stamps.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 22f) });

        _portrait.Species = c.Species;
        _name.Text = c.Species.CommonName;
        _sci.Text = c.Species.ScientificName;
        _size.Text = $"{c.LengthCm:0.0} cm  ·  {LedgerPanel.Weight(c.WeightKg)}      ({c.LengthCm / 2.54f:0.0} in  ·  {c.WeightKg * 2.20462f:0.00} lb)";
        float price = ProfileStore.Shop.PricePerKg(c.Species.Id) * c.WeightKg;
        _note.Text = (newSpecies ? "First one in your Ledger.  " : !newSpecies && previousBestKg > 0f && c.WeightKg > previousBestKg ? $"Beats your best of {LedgerPanel.Weight(previousBestKg)}.  " : "")
                     + (price > 0f ? $"Worth about ${price:0.00} at the bait shop." : "A game fish: not for sale.");
        _footer.Text = choice;
        _footer.AddThemeColorOverride("font_color", Green);
        _hideIn = -1;
        SlideIn();
    }

    /// <summary>Screenshot check (--panel catch): a sample card for the given species.</summary>
    public void ShowSample(string speciesId)
    {
        var driver = GetTree().CurrentScene.GetNode<TheLedger.SimGlue.SimDriver>("SimDriver");
        var sp = driver.Species.First(x => x.Id == speciesId);
        ShowCatch(new Catch { Species = sp, LengthCm = 46.5f, WeightKg = 1.62f }, "", "Keep: R     Release: F", true, 0f);
    }

    /// <summary>The angler decided: say so, then slide away after a moment.</summary>
    public void Resolve(bool kept, string summary)
    {
        _footer.Text = (kept ? "Kept.  " : "Released.  ") + summary;
        _footer.AddThemeColorOverride("font_color", kept ? Gold : Green);
        _hideIn = 3.5;
    }

    public void HideNow()
    {
        _hideIn = -1;
        SlideOut();
    }

    private void SlideIn()
    {
        _tween?.Kill();
        _card.Visible = true;
        _card.Size = new Vector2(W, H);
        _card.Modulate = new Color(1f, 1f, 1f, 0f);
        _card.Position = _card.Position with { Y = Top - 40f };
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(_card, "modulate:a", 1f, 0.25f);
        _tween.TweenProperty(_card, "position:y", Top, 0.45f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    private void SlideOut()
    {
        if (!_card.Visible) return;
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(_card, "modulate:a", 0f, 0.35f);
        _tween.TweenProperty(_card, "position:y", Top - 30f, 0.35f).SetTrans(Tween.TransitionType.Sine);
        _tween.Chain().TweenCallback(Callable.From(() => _card.Visible = false));
    }

    public override void _Process(double delta)
    {
        // Shrink to the content: a wrapping label can grow the card on an early layout pass (sizes clamp to the minimum).
        if (_card.Visible) _card.Size = new Vector2(W, 0f);
        if (_hideIn < 0) return;
        _hideIn -= delta;
        if (_hideIn < 0) SlideOut();
    }
}
