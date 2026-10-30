using Godot;

namespace TheLedger.Ui;

/// <summary>
/// The game's type (game/assets/CREDITS.md, both SIL OFL 1.1): Fira Sans for the HUD and menus, Fraunces (a soft
/// old-style serif, like a field guide's headings) for titles and fish names. Falls back to Godot's font if missing.
/// </summary>
public static class UiFonts
{
    private static Font? _body, _bold, _title, _titleLight;

    public static Font Body => _body ??= LoadOr("res://assets/fonts/FiraSans-Regular.ttf");
    public static Font Bold => _bold ??= LoadOr("res://assets/fonts/FiraSans-SemiBold.ttf");
    public static Font Title => _title ??= Serif(620f);
    public static Font TitleLight => _titleLight ??= Serif(400f);

    private static Font LoadOr(string path) =>
        ResourceLoader.Exists(path) ? GD.Load<Font>(path) : ThemeDB.FallbackFont;

    /// <summary>Fraunces at a given weight (variable font), with its "soft" axis up for rounder letters.</summary>
    private static Font Serif(float weight)
    {
        const string path = "res://assets/fonts/Fraunces.ttf";
        if (!ResourceLoader.Exists(path)) return ThemeDB.FallbackFont;
        var v = new FontVariation { BaseFont = GD.Load<Font>(path) };
        v.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), weight }, { TextServerManager.GetPrimaryInterface().NameToTag("SOFT"), 50f } };
        return v;
    }

    /// <summary>Makes Fira Sans every control's default (call before any UI is built).</summary>
    public static void Install() => ThemeDB.FallbackFont = Body;
}
