using Godot;
using TheLedger.Sim.World;

namespace TheLedger.SimGlue;

/// <summary>
/// Nature Coast weather (simplified): each day gets a type picked from the season's odds (seeded by the date, so a
/// day's weather is the same if you come back to it), and summer afternoons can build into thunderstorms. Wind, rain
/// and cloud ease in and out; the sim's water and fish read them from the config. Off in automated runs.
/// </summary>
public partial class WeatherDirector : Node
{
    public SimDriver Driver { get; set; } = null!;

    public enum Kind { Clear, Breezy, Overcast, Rain, Storm }

    /// <summary>What it's doing now (for the HUD).</summary>
    public Kind Now { get; private set; }

    public string Describe => Now switch
    {
        Kind.Clear => "clear", Kind.Breezy => Driver.Config.PostColdFront ? "cold front, breezy" : "breezy",
        Kind.Overcast => "overcast", Kind.Rain => "rain", _ => "thunderstorm",
    };

    public override void _Process(double delta)
    {
        var w = Driver.World;
        var c = Driver.Config;
        int day = w.Day;
        float hour = w.Hour;
        var (kind, front) = DayType(day, w.Season);
        // Summer: most afternoons build a storm (about 2-6 pm).
        if (w.Season == "summer" && Hash(day, 7) < 0.6f && hour > 14f + 1.5f * Hash(day, 8) && hour < 17.5f) kind = Kind.Storm;
        Now = kind;

        (float wind, float fetch, float rain, float cloud) target = kind switch
        {
            Kind.Clear => (1.5f, 40f, 0f, 0.1f),
            Kind.Breezy => (7f, 400f, 0f, 0.25f),
            Kind.Overcast => (3f, 100f, 0f, 0.8f),
            Kind.Rain => (4f, 150f, 6f, 0.9f),
            _ => (11f, 1200f, 25f, 1f),
        };
        float k = 1f - MathF.Exp(-(float)delta / 6f); // weather changes over several seconds, not instantly
        c.DebugWindSpeed += (target.wind - c.DebugWindSpeed) * k;
        c.DebugFetch += (target.fetch - c.DebugFetch) * k;
        c.DebugRainRate += (target.rain - c.DebugRainRate) * k;
        c.CloudCover += (target.cloud - c.CloudCover) * k;
        c.PostColdFront = front;
    }

    /// <summary>The day's weather type and whether it's the clear, breezy day behind a cold front (winter).</summary>
    private static (Kind, bool) DayType(int day, string season)
    {
        float r = Hash(day, 1);
        return season switch
        {
            "winter" => r < 0.45f ? (Kind.Clear, false) : r < 0.75f ? (Kind.Breezy, true) : r < 0.92f ? (Kind.Overcast, false) : (Kind.Rain, false),
            "summer" => r < 0.7f ? (Kind.Clear, false) : r < 0.85f ? (Kind.Overcast, false) : (Kind.Rain, false),
            _ => r < 0.5f ? (Kind.Clear, false) : r < 0.78f ? (Kind.Breezy, false) : r < 0.93f ? (Kind.Overcast, false) : (Kind.Rain, false),
        };
    }

    /// <summary>A stable 0..1 number for (day, salt).</summary>
    private static float Hash(int day, int salt)
    {
        uint h = (uint)(day * 374761393 + salt * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0x1000000;
    }
}
