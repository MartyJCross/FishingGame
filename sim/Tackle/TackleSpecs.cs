using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheLedger.Sim.Tackle;

/// <summary>
/// A rod blank as data (game/data/gear/rods/*.json). Units: metres, kilograms, newton-metres.
/// Bending stiffness follows EI(s) = EItip + (EIbutt − EItip)·(1 − s)^ActionExponent, s = 0 at the fore grip,
/// 1 at the tip (docs/04_PHYSICS_SPEC.md §4.2).
/// </summary>
public sealed class RodSpec
{
    public string Id { get; set; } = "rod";
    public string Name { get; set; } = "Rod";
    /// <summary>spincast, spinning, casting, fly, surf, ...</summary>
    public string Type { get; set; } = "spinning";
    /// <summary>UL, L, ML, M, MH, H, XH (label only; the physics comes from the numbers).</summary>
    public string Power { get; set; } = "M";
    /// <summary>extra-fast, fast, moderate-fast, moderate, slow (label only).</summary>
    public string Action { get; set; } = "fast";

    /// <summary>Total length, butt cap to tip, m.</summary>
    public float LengthM { get; set; } = 2.13f;
    /// <summary>Butt cap to fore grip (where the hands hold), m.</summary>
    public float HandleLengthM { get; set; } = 0.40f;
    /// <summary>Blank nodes from the fore grip to the tip.</summary>
    public int BlankNodes { get; set; } = 16;
    /// <summary>Bending stiffness at the butt end of the blank, N·m².</summary>
    public float EIButt { get; set; } = 55f;
    /// <summary>Bending stiffness at the tip, N·m².</summary>
    public float EITip { get; set; } = 0.25f;
    /// <summary>
    /// Exponent a. Large = stiffness falls away toward the tip, so the bend concentrates there (fast action); small =
    /// stiffness holds up along the blank and the bend spreads toward the butt (slow). Extra-fast ≈ 3.0, fast ≈ 2.2,
    /// moderate-fast ≈ 1.6, moderate ≈ 1.2, slow ≈ 0.7.
    /// </summary>
    public float ActionExponent { get; set; } = 2.2f;
    /// <summary>Blank mass (without handle and reel), kg.</summary>
    public float BlankMassKg { get; set; } = 0.11f;
    /// <summary>Handle + reel seat + reel mass at the hands, kg (moves with the hands).</summary>
    public float HandleMassKg { get; set; } = 0.25f;
    /// <summary>Blank outside diameter at the butt and tip, m (drag and drawing).</summary>
    public float ButtDiameterM { get; set; } = 0.011f;
    public float TipDiameterM { get; set; } = 0.0018f;
    /// <summary>
    /// Material (Rayleigh) damping time of the bending constraints, milliseconds: graphite ~2-4, fibreglass ~6-10,
    /// bamboo ~5. Higher = the blank stops quivering sooner and feels softer.
    /// </summary>
    public float MaterialDamping { get; set; } = 3f;
    /// <summary>Bending moment the blank survives at the butt and tip, N·m (same taper exponent).</summary>
    public float MaxMomentButt { get; set; } = 45f;
    public float MaxMomentTip { get; set; } = 1.0f;
    /// <summary>Blank nodes (counted from the fore grip, 1..BlankNodes) that carry a guide. The tip is always a guide.</summary>
    public int[] GuideNodes { get; set; } = { 3, 6, 9, 11, 13, 15 };
    /// <summary>Guide quality 0..1 (friction when line pays out; M3).</summary>
    public float GuideQuality { get; set; } = 0.7f;
    public float LineRatingMinLb { get; set; } = 6f;
    public float LineRatingMaxLb { get; set; } = 12f;
    public float LureRatingMinG { get; set; } = 5f;
    public float LureRatingMaxG { get; set; } = 18f;
    public float Price { get; set; }
    public string Material { get; set; } = "graphite";

    public float EI(float s) => EITip + (EIButt - EITip) * MathF.Pow(1f - Math.Clamp(s, 0f, 1f), ActionExponent);
    /// <summary>
    /// Breaking moment along the blank. Bending strength scales with diameter³ while stiffness scales with
    /// diameter⁴, so strength falls off more gently toward the tip than EI does (exponent 0.75 × action).
    /// </summary>
    public float MaxMoment(float s) => MaxMomentTip + (MaxMomentButt - MaxMomentTip) * MathF.Pow(1f - Math.Clamp(s, 0f, 1f), 0.75f * ActionExponent);
    public float Diameter(float s) => ButtDiameterM + (TipDiameterM - ButtDiameterM) * Math.Clamp(s, 0f, 1f);

    public static RodSpec MediumSpinning() => new();
}

/// <summary>Fishing line as data (game/data/gear/lines/*.json). Rated strength is in lb as sold; converted on load.</summary>
public sealed class LineSpec
{
    public string Id { get; set; } = "line";
    public string Name { get; set; } = "Line";
    /// <summary>mono, fluoro, braid, fly, wire.</summary>
    public string Material { get; set; } = "mono";
    public float RatedLb { get; set; } = 10f;
    public float DiameterMm { get; set; } = 0.28f;
    /// <summary>Density, kg/m³ (nylon 1140, fluorocarbon 1780, PE braid 970).</summary>
    public float Density { get; set; } = 1140f;
    /// <summary>Strain at the breaking load (mono 0.15-0.25, fluoro 0.08-0.15, braid 0.02-0.05).</summary>
    public float StretchAtBreak { get; set; } = 0.20f;
    /// <summary>Fly lines: AFTMA weight, grains in the first 30 ft (overrides density-based mass when > 0).</summary>
    public float FlyGrains30Ft { get; set; }
    /// <summary>Bending stiffness, N·m² (≈0 for mono and braid; real for fly lines).</summary>
    public float BendingEI { get; set; }
    public float AbrasionResistance { get; set; } = 0.6f;
    public float Visibility { get; set; } = 0.5f;
    public float KnotFactor { get; set; } = 0.9f;
    public string Color { get; set; } = "#d8e4e0";
    public float Price { get; set; }

    public const float NewtonsPerLb = 4.4482216f;

    public float BreakingForceN => RatedLb * NewtonsPerLb;
    public float DiameterM => DiameterMm * 0.001f;
    public float Area => MathF.PI * 0.25f * DiameterM * DiameterM;
    /// <summary>Axial stiffness EA, N (linear: breaking force / breaking strain).</summary>
    public float EA => BreakingForceN / MathF.Max(StretchAtBreak, 0.005f);

    public float MassPerMetre => FlyGrains30Ft > 0f ? FlyGrains30Ft * 6.479891e-5f / 9.144f : Density * Area;

    public static LineSpec Mono10() => new();
}

/// <summary>
/// What's on the end of the line: a lure, or a hook with bait and its weights, optionally under a fixed bobber
/// (data: game/data/lures/*.json). Simulated as one body at the line's end (docs/04 §6); its action (lip dive,
/// wobble, blade, pop) and what fish sense from it come from these numbers.
/// </summary>
public sealed class TerminalSpec
{
    public string Id { get; set; } = "sinker_half_oz";
    public string Name { get; set; } = "Half-ounce bank sinker";
    /// <summary>sinker, bait, jig, softplastic, crankbait, spinner, spoon, popper.</summary>
    public string Kind { get; set; } = "sinker";
    public float MassKg { get; set; } = 0.014f;
    /// <summary>Volume for buoyancy, m³ (a 14 g lead sinker is ~1.2 cm³; a floating crankbait is more than its mass/1000).</summary>
    public float VolumeM3 { get; set; } = 1.24e-6f;
    /// <summary>Effective diameter for drag, m.</summary>
    public float DiameterM { get; set; } = 0.012f;
    public float DragCoefficient { get; set; } = 0.5f;
    /// <summary>Body length, m (drawing; fish judge size by it).</summary>
    public float LengthM { get; set; } = 0.025f;
    /// <summary>How well it planes on the water, 0..1 (skirted jigs, soft plastics high; sinkers, crankbaits low).</summary>
    public float Planing { get; set; } = 0.15f;
    /// <summary>Diving lip: lift area × coefficient, m². Pulled through water it dives (crankbaits ~2-4e-4).</summary>
    public float LipLiftArea { get; set; }
    /// <summary>Wobble / tail / blade beats per second per m/s of speed through the water.</summary>
    public float WobbleHzPerMps { get; set; }
    /// <summary>Speed below which the action stops (spinner blades stall, cranks stop wobbling), m/s.</summary>
    public float ActionMinSpeed { get; set; } = 0.15f;
    /// <summary>Vibration put out at 1 m/s, 0..1 (lateral line).</summary>
    public float Vibration { get; set; }
    /// <summary>Flash (blades, chrome, spoons), 0..1.</summary>
    public float Flash { get; set; }
    /// <summary>Rattles or pops: sound, 0..1.</summary>
    public float Sound { get; set; }
    /// <summary>Scent (natural or scented baits), 0..1.</summary>
    public float Scent { get; set; }
    /// <summary>Live or natural bait (fish treat it as food, not a lure).</summary>
    public bool Natural { get; set; }
    /// <summary>Key into the species' lureAttraction (or baitAttraction when Natural), e.g. "crankbait", "nightcrawler".</summary>
    public string AttractKey { get; set; } = "";

    /// <summary>Fixed bobber this far above the hook, m (0 = none).</summary>
    public float FloatDepthM { get; set; }
    public float FloatVolumeM3 { get; set; }
    public float FloatMassKg { get; set; }
    public float FloatDiameterM { get; set; } = 0.025f;

    public string BodyColor { get; set; } = "#5a5d63";
    public string BellyColor { get; set; } = "#3e4146";
    public string AccentColor { get; set; } = "#8a8d93";
    public float Price { get; set; }

    public static TerminalSpec HalfOunceSinker() => new();
}

/// <summary>Loads gear JSON (camelCase keys).</summary>
public static class GearJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static RodSpec Rod(string json) => JsonSerializer.Deserialize<RodSpec>(json, Options) ?? throw new InvalidDataException("Empty rod");
    public static LineSpec Line(string json) => JsonSerializer.Deserialize<LineSpec>(json, Options) ?? throw new InvalidDataException("Empty line");
    public static ReelSpec Reel(string json) => JsonSerializer.Deserialize<ReelSpec>(json, Options) ?? throw new InvalidDataException("Empty reel");
    public static TerminalSpec Terminal(string json) => JsonSerializer.Deserialize<TerminalSpec>(json, Options) ?? throw new InvalidDataException("Empty terminal");
}
