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
    public int MaxLineNodes = 200;

    // ---------- tackle solver (docs/04_PHYSICS_SPEC.md §2-§5) ----------

    [Tunable("Solver", 1, 6, Help = "Direct solves (re-linearisations) of rod + line per substep")]
    public int TackleIterations = 2;

    [Tunable("Solver", 0f, 1e-5f, Help = "Rod blank stretch compliance, m/N (0 = rigid)")]
    public float RodStretchCompliance = 1e-8f;

    [Tunable("Solver", 0f, 50f, Help = "Extra velocity damping on all tackle nodes, 1/s. Tests use it to settle static poses; keep 0 in play")]
    public float TackleSettleDamping = 0f;

    [Tunable("Solver", 10f, 300f, Help = "Speed limit for any tackle node, m/s")]
    public float MaxNodeSpeed = 120f;

    [Tunable("Line", 0.02f, 0.5f, Help = "Line node spacing, m")]
    public float LineSegmentLength = 0.1f;

    [Tunable("Line", 0f, 20f, Help = "Line node solver mass grows with tension × this (keeps a taut line from zigzagging)")]
    public float LineTensionMassFactor = 3f;

    [Tunable("Line", 0f, 0.5f, Help = "Line internal damping per substep (pulls node velocities toward their neighbours'; kills sawtooth)")]
    public float LineInternalDamping = 0.3f;

    [Tunable("Line", 0f, 0.002f, Help = "Minimum line node mass inside the solver, kg (stability; gravity and drag use the real mass)")]
    public float LineNodeMinMassKg = 5e-5f;

    [Tunable("Line", 100f, 20000f, Help = "Line nodes are at least terminal mass / this inside the solver (stability)")]
    public float LineMassRatioLimit = 2000f;

    [Tunable("Line", 0.05f, 2f, Help = "Shortest line out from the tip, m")]
    public float MinLineOut = 0.3f;

    [Tunable("Line", 0.2f, 2.5f, Help = "Drag coefficient across the line (cylinder about 1.2)")]
    public float LineNormalDrag = 1.2f;

    [Tunable("Line", 0f, 0.2f, Help = "Skin-friction drag coefficient along the line")]
    public float LineTangentDrag = 0.02f;

    [Tunable("Line", 0f, 1f, Help = "Ground friction for line and rod nodes (0 = ice, 1 = sticks)")]
    public float GroundFriction = 0.6f;

    [Tunable("Line", 50f, 400f, Help = "Line nodes target: spacing grows to line out / this (long casts stay affordable)")]
    public float LineNodesTarget = 130f;

    [Tunable("Reel", 0.5f, 8f, Help = "Most torque a hand puts on the reel handle, N·m (sets the cranking stall force)")]
    public float HandCrankTorque = 3f;

    [Tunable("Water entry", 2f, 15f, Help = "Slowest entry that can skip, m/s")]
    public float SkipMinSpeed = 6f;

    [Tunable("Water entry", 5f, 60f, Help = "Steepest skip angle for a perfectly planing body, degrees")]
    public float SkipMaxAngleDeg = 24f;

    [Tunable("Water entry", 0f, 0.8f, Help = "Vertical bounce kept on a skip")]
    public float SkipRestitution = 0.35f;

    [Tunable("Water entry", 0f, 0.8f, Help = "Horizontal speed lost per skip")]
    public float SkipSpeedLoss = 0.2f;

    [Tunable("Lures", 2f, 60f, Help = "A popper pops when it accelerates harder than this at the surface, m/s²")]
    public float PopAcceleration = 12f;

    [Tunable("Fight", 0.5f, 6f, Help = "How far out from the angler's feet a fish can be netted or lipped, m")]
    public float LandReach = 3.2f;

    [Tunable("Fight", 0f, 1f, Help = "A hooked fish can be landed once its burst stamina is below this")]
    public float LandStamina = 0.4f;

    [Tunable("Fight", 0f, 2f, Help = "Fish lighter than this can be swung in without a net, kg")]
    public float SwingInKg = 0.35f;

    [Tunable("World", 0f, 24f, Help = "Hour of day when a session starts")]
    public float StartHour = 8f;

    /// <summary>Sky cover 0..1 and whether a cold front just passed (set by the game's weather; fish read them).</summary>
    public float CloudCover = 0f;
    public bool PostColdFront = false;

    /// <summary>Day of the year (0 = 1 January) when a session starts; the game keeps it in the profile.</summary>
    public float StartDayOfYear = 120f;

    [Tunable("World", 0f, 600f, Help = "Game seconds per real second (60 = an hour passes in a minute)")]
    public float TimeScale = 24f;

    [Tunable("Lures", 0.5f, 5f, Help = "Most line out at which you can change what's tied on, m")]
    public float RetieMaxLineOut = 2.5f;

    [Tunable("Hands", 1f, 60f, Help = "Fastest the arms can swing the rod, rad/s")]
    public float HandMaxAngularSpeed = 18f;

    [Tunable("Hands", 10f, 2000f, Help = "Arm torque limit as angular acceleration, rad/s²")]
    public float HandMaxAngularAccel = 160f;

    [Tunable("Hands", 1f, 400f, Help = "How quickly the hands close the gap to the input target, 1/s (no overshoot)")]
    public float HandStiffness = 12f;

    [Tunable("Hands", 0f, 60f, Help = "Hand spring damping")]
    public float HandDamping = 10f;

    [Tunable("Hands", -3.2f, 0f, Help = "Lowest rod angle the arms allow, rad (0 = horizontal)")]
    public float HandMinAngle = -1.2f;

    [Tunable("Hands", 0f, 3.2f, Help = "Highest rod angle the arms allow, rad (π/2 = vertical)")]
    public float HandMaxAngle = 3.1f;

    [Tunable("Hands", 1f, 30f, Help = "Fastest arm swing once the lure is out and the bail is closed (no line-snapping yanks), rad/s")]
    public float HandFishingMaxSpeed = 18f;

    [Tunable("Hands", 10f, 1000f, Help = "Arm acceleration limit once the lure is out, rad/s²")]
    public float HandFishingMaxAccel = 160f;

    // ---------- water (docs/04_PHYSICS_SPEC.md §7) ----------

    [Tunable("Water", 0.1f, 3f, Help = "Ripple speed on simulated patches, m/s")]
    public float RippleSpeed = 0.7f;

    [Tunable("Water", 0f, 5f, Help = "Ripple damping, 1/s (higher = ripples die sooner)")]
    public float RippleDamping = 0.6f;

    [Tunable("Water", 0.02f, 0.2f, Help = "Patch column spacing, m (applies to new worlds)")]
    public float PatchSpacing = 0.05f;

    [Tunable("Water", 64, 2048, Help = "Columns per simulated patch (applies to new worlds)")]
    public int PatchColumns = 1024;

    [Tunable("Water", 4, 300, Help = "Absorbing columns at each patch edge")]
    public int SpongeColumns = 48;

    [Tunable("Water", 0f, 40f, Help = "How fast steep or fast water turns to foam")]
    public float FoamGain = 4f;

    [Tunable("Water", 0.05f, 10f, Help = "How fast foam fades, 1/s")]
    public float FoamDecay = 1.2f;

    [Tunable("Water", 0f, 5f, Help = "Scale of the surface push from impacts")]
    public float SplashImpulseScale = 1f;

    [Tunable("Water", 0f, 1000f, Help = "Droplets thrown per joule of impact (max 64 per splash)")]
    public float DropletsPerJoule = 150f;

    [Tunable("Water", 0f, 0.3f, Help = "Wake: how hard a body moving at the surface pushes the water, per tick")]
    public float WakeGain = 0.05f;

    [Tunable("Water", 0f, 1f, Help = "Surface tap when a droplet falls back, m/s")]
    public float DropletTapImpulse = 0.05f;

    [Tunable("Water", 0f, 40f, Help = "Simulated rain drops per second per metre of surface, per mm/h")]
    public float RainDropsPerMmPerMetre = 6f;

    [Tunable("Water", 0f, 1f, Help = "Surface tap from one rain drop, m/s")]
    public float RainDropImpulse = 0.12f;

    // ---------- debug conditions (until spots and weather exist, M8+) ----------

    [Tunable("Debug conditions", 0f, 30f, Help = "Wind speed, m/s")]
    public float DebugWindSpeed = 2f;

    [Tunable("Debug conditions", -1f, 1f, Help = "Wind direction: +1 toward +x, -1 toward -x")]
    public float DebugWindDir = 1f;

    [Tunable("Debug conditions", 1f, 200000f, Help = "Fetch (open water upwind), m")]
    public float DebugFetch = 60f;

    [Tunable("Debug conditions", 0.5f, 500f, Help = "Water depth for wave dispersion, m")]
    public float DebugDepth = 3f;

    [Tunable("Debug conditions", 0f, 4f, Help = "Swell height, m")]
    public float DebugSwellHeight = 0f;

    [Tunable("Debug conditions", 2f, 20f, Help = "Swell period, s")]
    public float DebugSwellPeriod = 8f;

    [Tunable("Debug conditions", 0f, 80f, Help = "Rain, mm/h (light 1, moderate 4, heavy 10+)")]
    public float DebugRainRate = 0f;

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
