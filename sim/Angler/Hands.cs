using TheLedger.Sim.Core;

namespace TheLedger.Sim.Angler;

/// <summary>
/// The angler's hands: the kinematic driver of the rod butt (docs/04_PHYSICS_SPEC.md §4.1).
/// They chase the input's rod-angle target with a spring-damper, limited by a maximum angular speed and
/// acceleration (the arm's strength), so a flick is fast but never instant.
/// M0 version: angle + reach only. M2 adds the rod chain this drives.
/// </summary>
public sealed class Hands
{
    /// <summary>Rod angle at the handle, radians CCW from +x.</summary>
    public float Angle;
    public float AngularVelocity;
    /// <summary>Angle on the previous tick, for render interpolation.</summary>
    public float PrevAngle;
    /// <summary>Reach 0..1.</summary>
    public float Reach;
    public float PrevReach;

    /// <summary>Where the hands hold the rod, relative to the angler's shoulder, metres (filled from reach).</summary>
    public Vec2 GripOffset;

    public Hands(float startAngle = 0.6f)
    {
        Angle = PrevAngle = startAngle;
    }

    public void Step(SimConfig c, float dt, float targetAngle, float targetReach)
    {
        PrevAngle = Angle;
        PrevReach = Reach;

        float target = Math.Clamp(targetAngle, c.HandMinAngle, c.HandMaxAngle);
        float error = WrapAngle(target - Angle);
        float accel = c.HandStiffness * error - c.HandDamping * AngularVelocity;
        accel = Math.Clamp(accel, -c.HandMaxAngularAccel, c.HandMaxAngularAccel);
        AngularVelocity = Math.Clamp(AngularVelocity + accel * dt, -c.HandMaxAngularSpeed, c.HandMaxAngularSpeed);
        Angle += AngularVelocity * dt;

        if (Angle < c.HandMinAngle) { Angle = c.HandMinAngle; AngularVelocity = MathF.Max(0f, AngularVelocity); }
        if (Angle > c.HandMaxAngle) { Angle = c.HandMaxAngle; AngularVelocity = MathF.Min(0f, AngularVelocity); }

        // Reach eases toward its target; arms move in and out more slowly than they swing.
        float r = Math.Clamp(targetReach, 0f, 1f);
        Reach += (r - Reach) * MathF.Min(1f, 10f * dt);
        GripOffset = new Vec2(0.25f + 0.35f * Reach, -0.15f + 0.25f * Reach);
    }

    public static float WrapAngle(float a)
    {
        while (a > MathF.PI) a -= 2f * MathF.PI;
        while (a < -MathF.PI) a += 2f * MathF.PI;
        return a;
    }

    public void Hash(ref StateHasher h)
    {
        h.Add(Angle);
        h.Add(AngularVelocity);
        h.Add(Reach);
    }

    public bool IsFinite => float.IsFinite(Angle) && float.IsFinite(AngularVelocity) && float.IsFinite(Reach);
}
