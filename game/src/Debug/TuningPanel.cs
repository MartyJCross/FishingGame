using Godot;
using TheLedger.Sim.Core;
using TheLedger.SimGlue;

namespace TheLedger.DebugTools;

/// <summary>
/// F1 dev tuning panel: a slider for every [Tunable] SimConfig field, live (CLAUDE.md rule 11). Changes apply to
/// the running sim immediately. "Save" writes game/data/tuning/sim.json when running from source.
/// </summary>
public partial class TuningPanel : CanvasLayer
{
    private SimConfig _config = null!;
    private PanelContainer _panel = null!;
    private Label _status = null!;
    private readonly List<(SimConfig.TunableField field, HSlider slider, Label value)> _rows = new();

    public bool IsOpen => _panel.Visible;

    /// <summary>True when the mouse is over the panel (the rod should ignore the mouse then).</summary>
    public bool MouseOver => _panel.Visible && _panel.GetGlobalRect().HasPoint(_panel.GetViewport().GetMousePosition());

    public void Setup(SimConfig config)
    {
        _config = config;
        Layer = 20;

        _panel = new PanelContainer
        {
            Visible = false,
            AnchorLeft = 1f, AnchorRight = 1f, AnchorTop = 0f, AnchorBottom = 1f,
            OffsetLeft = -560f, OffsetRight = -12f, OffsetTop = 12f, OffsetBottom = -12f,
        };
        AddChild(_panel);

        var outer = new VBoxContainer();
        _panel.AddChild(outer);

        var title = new Label { Text = "Tuning (F1)" };
        title.AddThemeFontSizeOverride("font_size", 26);
        outer.AddChild(title);

        var buttons = new HBoxContainer();
        outer.AddChild(buttons);
        AddButton(buttons, "Save", () => _status.Text = TuningStore.Save(_config));
        AddButton(buttons, "Reload", () =>
        {
            var w = new List<string>();
            TuningStore.Reload(_config, w);
            SyncSliders();
            _status.Text = w.Count == 0 ? "Reloaded" : string.Join("\n", w);
        });
        AddButton(buttons, "Defaults", () =>
        {
            var d = new SimConfig();
            foreach (var t in SimConfig.Tunables) _config.SetFromFloat(t, d.GetAsFloat(t));
            SyncSliders();
            _status.Text = "Defaults (not saved)";
        });

        _status = new Label { Text = $"Saves to {TuningStore.SavePath}", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        outer.AddChild(_status);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        outer.AddChild(scroll);
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(list);

        string? group = null;
        foreach (var t in SimConfig.Tunables.OrderBy(t => t.Info.Group, StringComparer.Ordinal))
        {
            if (t.Info.Group != group)
            {
                group = t.Info.Group;
                var g = new Label { Text = group.ToUpperInvariant() };
                g.AddThemeColorOverride("font_color", new Color(0.95f, 0.8f, 0.45f));
                list.AddChild(g);
            }
            var row = new HBoxContainer { TooltipText = t.Info.Help ?? "" };
            list.AddChild(row);
            row.AddChild(new Label { Text = t.Name, CustomMinimumSize = new Vector2(230, 0) });
            bool isInt = t.Type == typeof(int) || t.Type == typeof(bool);
            var slider = new HSlider
            {
                MinValue = t.Info.Min,
                MaxValue = t.Info.Max,
                Step = isInt ? 1 : (t.Info.Max - t.Info.Min) / 1000.0,
                Value = _config.GetAsFloat(t),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(180, 0),
            };
            var value = new Label { CustomMinimumSize = new Vector2(70, 0) };
            var field = t;
            slider.ValueChanged += v =>
            {
                _config.SetFromFloat(field, (float)v);
                value.Text = Format(_config.GetAsFloat(field));
            };
            value.Text = Format(_config.GetAsFloat(t));
            row.AddChild(slider);
            row.AddChild(value);
            _rows.Add((t, slider, value));
        }
    }

    public void Toggle() => _panel.Visible = !_panel.Visible;

    private void SyncSliders()
    {
        foreach (var (field, slider, value) in _rows)
        {
            slider.SetValueNoSignal(_config.GetAsFloat(field));
            value.Text = Format(_config.GetAsFloat(field));
        }
    }

    private static string Format(float v) => MathF.Abs(v) >= 100 ? v.ToString("0") : v.ToString("0.###");

    private static void AddButton(Container parent, string text, Action onPressed)
    {
        var b = new Button { Text = text };
        b.Pressed += onPressed;
        parent.AddChild(b);
    }
}
