using TheLedger.Sim.Core;

namespace TheLedger.Sim.World;

/// <summary>
/// A fishing spot's side-view cross-section: the ground profile (bank and bottom) as a height function y(x).
/// Structure (rocks, logs, docks, weeds) and snag sites arrive with M3; spot data files with M5.
/// </summary>
public sealed class Spot
{
    private readonly float[] _xs;
    private readonly float[] _ys;

    public Spot(string id, IReadOnlyList<Vec2> profile)
    {
        if (profile.Count < 2) throw new ArgumentException("A spot needs at least two profile points");
        Id = id;
        _xs = new float[profile.Count];
        _ys = new float[profile.Count];
        for (int i = 0; i < profile.Count; i++)
        {
            if (i > 0 && profile[i].X <= profile[i - 1].X) throw new ArgumentException("Profile x must increase");
            _xs[i] = profile[i].X;
            _ys[i] = profile[i].Y;
        }
    }

    public string Id { get; }
    public int PointCount => _xs.Length;
    public Vec2 Point(int i) => new(_xs[i], _ys[i]);

    /// <summary>Where the angler stands at the start, m.</summary>
    public Vec2 AnglerStart { get; init; } = new(-2f, 0.6f);
    /// <summary>Where the bank meets still water, m.</summary>
    public float WaterlineX { get; init; }

    /// <summary>Spring water temperature, °C, and how much of the spot's water it makes up (0 = none, 1 = all).</summary>
    public float SpringC { get; init; } = 22.5f;
    public float SpringMix { get; init; }

    /// <summary>Cover in the water (fallen trees, weed beds, lily pads, rock): fish use it, hooks snag in it.</summary>
    public IReadOnlyList<StructureZone> Structures { get; init; } = Array.Empty<StructureZone>();

    /// <summary>Ground height at x (flat beyond the ends).</summary>
    public float GroundHeight(float x)
    {
        if (x <= _xs[0]) return _ys[0];
        int n = _xs.Length;
        if (x >= _xs[n - 1]) return _ys[n - 1];
        int lo = 0, hi = n - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) >> 1;
            if (_xs[mid] <= x) lo = mid; else hi = mid;
        }
        float t = (x - _xs[lo]) / (_xs[hi] - _xs[lo]);
        return _ys[lo] + (_ys[hi] - _ys[lo]) * t;
    }

    /// <summary>Ground slope dy/dx at x.</summary>
    public float GroundSlope(float x)
    {
        int n = _xs.Length;
        if (x <= _xs[0] || x >= _xs[n - 1]) return 0f;
        int lo = 0, hi = n - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) >> 1;
            if (_xs[mid] <= x) lo = mid; else hi = mid;
        }
        return (_ys[hi] - _ys[lo]) / (_xs[hi] - _xs[lo]);
    }

    /// <summary>The placeholder test pond: a bank on the left sloping to a 3 m deep bottom.</summary>
    public static Spot TestPond() => new("test_pond", new Vec2[]
    {
        new(-40f, 0.6f), new(-1.2f, 0.6f), new(0.4f, -0.4f), new(2.0f, -2.5f), new(4.5f, -3.0f),
        new(20f, -3.3f), new(40f, -3.0f), new(60f, -3.2f),
    })
    {
        AnglerStart = new Vec2(-2f, 0.6f),
        WaterlineX = -0.24f,
    };
}

/// <summary>
/// A patch of cover, as a box in the cross-section. <see cref="Kind"/> matches the species' habitat.structure keys
/// (wood, weeds, lilyPads, rock). <see cref="Snag"/> is how readily a moving hook catches in it (0..1); a snag lets go
/// when the line pulls harder than a hold force drawn from <see cref="HoldMinN"/>..<see cref="HoldMaxN"/> (a twig
/// snaps, weed tears), which can be more than the line can take.
/// </summary>
public sealed class StructureZone
{
    public string Kind { get; init; } = "wood";
    public float X0 { get; init; }
    public float X1 { get; init; }
    public float Y0 { get; init; }
    public float Y1 { get; init; }
    public float Snag { get; init; }
    public float HoldMinN { get; init; } = 3f;
    public float HoldMaxN { get; init; } = 10f;

    public bool Contains(Vec2 p) => p.X >= X0 && p.X <= X1 && p.Y >= Y0 && p.Y <= Y1;
}
