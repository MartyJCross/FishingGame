using System.Text.Json;

namespace TheLedger.Sim.Fish;

/// <summary>
/// The parts of a species file (game/data/species/*.json, schema docs/05 §2) the sim uses. Unknown keys are ignored,
/// so the files can carry more than the code reads yet.
/// </summary>
public sealed class SpeciesSpec
{
    public string Id { get; set; } = "";
    public string CommonName { get; set; } = "";
    public string ScientificName { get; set; } = "";
    public string Tier { get; set; } = "S";
    public HabitatSpec Habitat { get; set; } = new();
    public SizeSpec Size { get; set; } = new();
    public ActivitySpec Activity { get; set; } = new();
    public FeedingSpec Feeding { get; set; } = new();
    public SensesSpec Senses { get; set; } = new();
    public BehaviourSpec Behaviour { get; set; } = new();
    public FightSpec Fight { get; set; } = new();
    public BodySpec Body { get; set; } = new();
    public RegulationSpec Regulations { get; set; } = new();

    /// <summary>Body shape (loaded from Body.ShapeRef by the caller).</summary>
    public ShapeSpec Shape { get; set; } = new();

    public float WeightKg(float lengthCm) => Size.LengthWeight.A * MathF.Pow(lengthCm, Size.LengthWeight.B) * 0.001f;

    public static SpeciesSpec Load(string json) =>
        JsonSerializer.Deserialize<SpeciesSpec>(json, Tackle.GearJson.Options) ?? throw new InvalidDataException("Empty species");
}

public sealed class HabitatSpec
{
    public string[] Spots { get; set; } = Array.Empty<string>();
    public float[] DepthM { get; set; } = { 0.5f, 5f };
    public Dictionary<string, float> Structure { get; set; } = new();
}

public sealed class SizeSpec
{
    public RangeMax LengthCm { get; set; } = new();
    public LengthWeightSpec LengthWeight { get; set; } = new();
    public GrowthSpec Growth { get; set; } = new();
    public float RecordWeightKg { get; set; }
}

public sealed class RangeMax
{
    public float[] Typical { get; set; } = { 10f, 20f };
    public float Max { get; set; } = 30f;
}

public sealed class LengthWeightSpec
{
    public float A { get; set; } = 0.01f;
    public float B { get; set; } = 3f;
}

public sealed class GrowthSpec
{
    public float Linf_cm { get; set; } = 25f;
    public float K { get; set; } = 0.4f;
    public float? MaxAgeYears { get; set; } = 8f;
}

public sealed class ActivitySpec
{
    /// <summary>dawn, day, dusk, night multipliers 0..1.</summary>
    public Dictionary<string, float> Diel { get; set; } = new();
}

public sealed class FeedingSpec
{
    public string Mode { get; set; } = "suction_picker";
    public Dictionary<string, float> BiteStyles { get; set; } = new();
    public float[] HoldTimeS { get; set; } = { 0.5f, 1.5f };
    public float FollowChance { get; set; } = 0.3f;
    public float[] SpeedPreferenceMps { get; set; } = { 0f, 0.6f };
    public Dictionary<string, float> LureAttraction { get; set; } = new();
    public Dictionary<string, float> BaitAttraction { get; set; } = new();
}

public sealed class SensesSpec
{
    public float Vision { get; set; } = 0.7f;
    public float LowLight { get; set; } = 0.3f;
    public float LateralLine { get; set; } = 0.6f;
    public float Hearing { get; set; } = 0.5f;
    public float Smell { get; set; } = 0.3f;
    public float LineShy { get; set; } = 0.3f;
    public float Spookiness { get; set; } = 0.4f;
}

public sealed class BehaviourSpec
{
    /// <summary>tight, loose_large, loose_small, solitary.</summary>
    public string Schooling { get; set; } = "solitary";
    public float Territorial { get; set; }
}

public sealed class FightSpec
{
    public float PowerCoeff { get; set; } = 1f;
    public Dictionary<string, float> Styles { get; set; } = new();
    public MouthSpec Mouth { get; set; } = new();
}

public sealed class MouthSpec
{
    public float Hardness { get; set; } = 0.5f;
    public float GapeFrac { get; set; } = 0.1f;
}

public sealed class BodySpec
{
    public string Locomotion { get; set; } = "subcarangiform";
    public string? HoverMode { get; set; }
    public int SpineNodes { get; set; } = 8;
    public float Stiffness { get; set; } = 0.5f;
    public string ShapeRef { get; set; } = "";
    public PatternSpec Pattern { get; set; } = new();
}

public sealed class PatternSpec
{
    public string Dorsal { get; set; } = "#4b5a3a";
    public string Flank { get; set; } = "#7a8a6c";
    public string Belly { get; set; } = "#e8e0c0";
    public string Eye { get; set; } = "#202020";
    public List<MarkingSpec> Markings { get; set; } = new();
}

public sealed class MarkingSpec
{
    /// <summary>bars, stripe, spots, opercular_flap, iridescent_band, blotch...</summary>
    public string Type { get; set; } = "";
    public string Color { get; set; } = "#000000";
}

public sealed class RegulationSpec
{
    public float? MinLengthCm { get; set; }
    public int? BagLimit { get; set; }
}

/// <summary>
/// A body outline family (game/data/species/shapes/*.json): half-heights above and below the spine at evenly spaced
/// stations from snout to tail base, as fractions of body length, plus fins.
/// </summary>
public sealed class ShapeSpec
{
    public string Id { get; set; } = "default";
    public float[] Dorsal { get; set; } = { 0.0f, 0.08f, 0.12f, 0.13f, 0.12f, 0.1f, 0.07f, 0.05f, 0.045f };
    public float[] Ventral { get; set; } = { 0.0f, 0.07f, 0.1f, 0.11f, 0.1f, 0.08f, 0.06f, 0.045f, 0.04f };
    /// <summary>Tail fin length and half-spread (fractions of body length); fork depth 0 (square) to 1 (deep).</summary>
    public float TailLength { get; set; } = 0.18f;
    public float TailSpread { get; set; } = 0.12f;
    public float TailFork { get; set; } = 0.3f;
    /// <summary>Dorsal fin: start and end stations (0..1) and height (fraction of length).</summary>
    public float[] DorsalFin { get; set; } = { 0.35f, 0.75f, 0.08f };
    public float[] AnalFin { get; set; } = { 0.6f, 0.78f, 0.05f };
    /// <summary>Mouth position: 0 = terminal, + = upturned, − = underslung.</summary>
    public float MouthTilt { get; set; }
    public bool Barbels { get; set; }

    public static ShapeSpec Load(string json) =>
        JsonSerializer.Deserialize<ShapeSpec>(json, Tackle.GearJson.Options) ?? throw new InvalidDataException("Empty shape");
}

/// <summary>Which fish live in a spot and how clear its water is (game/data/spots/*.json).</summary>
public sealed class SpotStock
{
    public string Id { get; set; } = "";
    /// <summary>Horizontal visibility in the water, m (Secchi-like).</summary>
    public float VisibilityM { get; set; } = 2f;
    /// <summary>Where fish may roam, sim x range, m.</summary>
    public float[] RangeX { get; set; } = { 1f, 40f };
    public List<StockEntry> Stock { get; set; } = new();

    public static SpotStock Load(string json) =>
        JsonSerializer.Deserialize<SpotStock>(json, Tackle.GearJson.Options) ?? throw new InvalidDataException("Empty stock");
}

public sealed class StockEntry
{
    public string Species { get; set; } = "";
    public int Count { get; set; }
}
