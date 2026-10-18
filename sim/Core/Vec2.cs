using System.Runtime.CompilerServices;

namespace TheLedger.Sim.Core;

/// <summary>
/// 2D vector for the simulation. Sim space: x right, y up, metres (docs/02_TECH_STACK.md §3.3).
/// Our own type so the sim never depends on Godot.
/// </summary>
public readonly struct Vec2 : IEquatable<Vec2>
{
    public readonly float X;
    public readonly float Y;

    public Vec2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static readonly Vec2 Zero = new(0f, 0f);
    public static readonly Vec2 UnitX = new(1f, 0f);
    public static readonly Vec2 UnitY = new(0f, 1f);

    public float LengthSquared
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => X * X + Y * Y;
    }

    public float Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MathF.Sqrt(X * X + Y * Y);
    }

    /// <summary>Unit vector in the same direction, or zero for a zero-length vector.</summary>
    public Vec2 Normalized()
    {
        float len = Length;
        return len > 1e-12f ? new Vec2(X / len, Y / len) : Zero;
    }

    /// <summary>Counter-clockwise perpendicular (rotated +90°).</summary>
    public Vec2 Perp() => new(-Y, X);

    public static Vec2 FromAngle(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));

    /// <summary>Angle in radians, counter-clockwise from +x.</summary>
    public float Angle() => MathF.Atan2(Y, X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

    /// <summary>2D cross product (z of the 3D cross).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cross(Vec2 a, Vec2 b) => a.X * b.Y - a.Y * b.X;

    public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator -(Vec2 a) => new(-a.X, -a.Y);
    public static Vec2 operator *(Vec2 a, float s) => new(a.X * s, a.Y * s);
    public static Vec2 operator *(float s, Vec2 a) => new(a.X * s, a.Y * s);
    public static Vec2 operator /(Vec2 a, float s) => new(a.X / s, a.Y / s);
    public static bool operator ==(Vec2 a, Vec2 b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vec2 a, Vec2 b) => !(a == b);

    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y);

    public bool Equals(Vec2 other) => this == other;
    public override bool Equals(object? obj) => obj is Vec2 v && this == v;
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
