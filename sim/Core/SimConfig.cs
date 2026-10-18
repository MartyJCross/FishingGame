using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace TheLedger.Sim.Core;

/// <summary>
/// Marks a SimConfig field as live-tunable: it gets a slider in the F1 tuning panel and is saved to
/// game/data/tuning/sim.json (CLAUDE.md rule 11).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class TunableAttribute : Attribute
{
    public TunableAttribute(string group, float min, float max)
    {
        Group = group;
        Min = min;
        Max = max;
    }

    public string Group { get; }
    public float Min { get; }
    public float Max { get; }
    /// <summary>Optional one-line explanation shown in the panel.</summary>
    public string? Help { get; init; }
}

/// <summary>
/// Every simulation tuning number lives here (docs/04_PHYSICS_SPEC.md §11), never as a constant in code.
/// Add fields as systems arrive. Tunable fields must be float, int or bool.
/// </summary>
public sealed class SimConfig
{
    /// <summary>Fixed sim tick rate. Not live-tunable: changing it changes every replay.</summary>
    public int TickHz = 120;

    public float Dt => 1f / TickHz;

    [Tunable("World", 1f, 20f, Help = "Gravity, m/s²")]
    public float Gravity = 9.81f;

    [Tunable("Solver", 1, 32, Help = "XPBD substeps per tick for rod, line and tackle")]
    public int TackleSubsteps = 8;

    [Tunable("Solver", 16, 400, Help = "Most nodes a line may use")]
    public int MaxLineNodes = 160;

    [Tunable("Hands", 1f, 60f, Help = "Fastest the arms can swing the rod, rad/s")]
    public float HandMaxAngularSpeed = 18f;

    [Tunable("Hands", 10f, 2000f, Help = "Arm torque limit as angular acceleration, rad/s²")]
    public float HandMaxAngularAccel = 220f;

    [Tunable("Hands", 1f, 400f, Help = "How hard the hands chase the input target (spring)")]
    public float HandStiffness = 160f;

    [Tunable("Hands", 0f, 60f, Help = "Hand spring damping")]
    public float HandDamping = 24f;

    [Tunable("Hands", -3.2f, 0f, Help = "Lowest rod angle the arms allow, rad (0 = horizontal)")]
    public float HandMinAngle = -1.2f;

    [Tunable("Hands", 0f, 3.2f, Help = "Highest rod angle the arms allow, rad (π/2 = vertical)")]
    public float HandMaxAngle = 2.6f;

    public SimConfig Clone() => (SimConfig)MemberwiseClone();

    // ---------- tunable reflection ----------

    public readonly record struct TunableField(FieldInfo Field, TunableAttribute Info)
    {
        public string Name => Field.Name;
        public Type Type => Field.FieldType;
    }

    private static TunableField[]? _tunables;

    /// <summary>All [Tunable] fields in declaration order.</summary>
    public static IReadOnlyList<TunableField> Tunables =>
        _tunables ??= typeof(SimConfig)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(f => (f, a: f.GetCustomAttribute<TunableAttribute>()))
            .Where(x => x.a != null)
            .OrderBy(x => x.f.MetadataToken)
            .Select(x => new TunableField(x.f, x.a!))
            .ToArray();

    public float GetAsFloat(TunableField t) => t.Field.GetValue(this) switch
    {
        float f => f,
        int i => i,
        bool b => b ? 1f : 0f,
        var other => throw new InvalidOperationException($"Unsupported tunable type {other?.GetType()}"),
    };

    public void SetFromFloat(TunableField t, float value)
    {
        float clamped = Math.Clamp(value, t.Info.Min, t.Info.Max);
        if (t.Type == typeof(float)) t.Field.SetValue(this, clamped);
        else if (t.Type == typeof(int)) t.Field.SetValue(this, (int)MathF.Round(clamped));
        else if (t.Type == typeof(bool)) t.Field.SetValue(this, clamped >= 0.5f);
        else throw new InvalidOperationException($"Unsupported tunable type {t.Type}");
    }

    /// <summary>Tunable fields as JSON (sorted by group, then declaration order).</summary>
    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        var list = Tunables;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            object v = t.Field.GetValue(this)!;
            string text = v switch
            {
                float f => f.ToString("R", CultureInfo.InvariantCulture),
                int n => n.ToString(CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                _ => throw new InvalidOperationException(),
            };
            sb.Append("  \"").Append(t.Name).Append("\": ").Append(text);
            sb.Append(i < list.Count - 1 ? ",\n" : "\n");
        }
        sb.Append("}\n");
        return sb.ToString();
    }

    /// <summary>
    /// Applies tunable values from JSON. Unknown keys are reported in <paramref name="warnings"/> and ignored;
    /// missing keys keep their current value; values are clamped to the field's range.
    /// </summary>
    public void ApplyJson(string json, List<string>? warnings = null)
    {
        using var doc = JsonDocument.Parse(json);
        var byName = Tunables.ToDictionary(t => t.Name, StringComparer.Ordinal);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!byName.TryGetValue(prop.Name, out var t))
            {
                warnings?.Add($"Unknown tuning key '{prop.Name}' ignored");
                continue;
            }
            float value = prop.Value.ValueKind switch
            {
                JsonValueKind.True => 1f,
                JsonValueKind.False => 0f,
                JsonValueKind.Number => (float)prop.Value.GetDouble(),
                _ => float.NaN,
            };
            if (float.IsNaN(value))
            {
                warnings?.Add($"Tuning key '{prop.Name}' has a non-numeric value; ignored");
                continue;
            }
            SetFromFloat(t, value);
        }
    }

    public static SimConfig FromJson(string json, List<string>? warnings = null)
    {
        var c = new SimConfig();
        c.ApplyJson(json, warnings);
        return c;
    }
}
