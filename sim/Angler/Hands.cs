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

    /// <summary>0 = free casting arms, 1 = calm fishing arms (lure out, bail closed); eased so limits never snap.</summary>
    public float Calm;

    public void Step(SimConfig c, float dt, float targetAngle, float targetReach, bool fishing = false)
    {
        PrevAngle = Angle;
        PrevReach = Reach;

        Calm += ((fishing ? 1f : 0f) - Calm) * MathF.Min(1f, dt / 0.4f);
        float maxSpeed = c.HandMaxAngularSpeed + (c.HandFishingMaxSpeed - c.HandMaxAngularSpeed) * Calm;
        float maxAccel = c.HandMaxAngularAccel + (c.HandFishingMaxAccel - c.HandMaxAngularAccel) * Calm;

        // The arm's range spans more than half a turn (flat behind to below horizontal), so no angle wrapping here:
        // the input side hands over a target already in [min, max].
        // With line out and the bail closed the rod stops just past upright (the fighting position): sweeping it
        // back over your shoulder against a fish bends it backwards (high-sticking) and breaks rods.
        float maxAngle = fishing ? MathF.Min(c.HandMaxAngle, c.HandFishingMaxAngle) : c.HandMaxAngle;
        float target = Math.Clamp(targetAngle, c.HandMinAngle, maxAngle);
        float error = target - Angle;
        // Track the target with no overshoot: head for it at a speed proportional to the gap (HandStiffness = 1/time
        // constant), within the arm's top speed and acceleration. Predictable: the rod stops where the input stops.
        // Never faster than the arm can still brake from before the target (√(2·a·gap)), so it stops on it.
        float gap = MathF.Abs(error);
        float wantSpeed = MathF.Min(MathF.Min(maxSpeed, gap * c.HandStiffness), MathF.Sqrt(2f * maxAccel * gap));
        float wantVel = MathF.Sign(error) * wantSpeed;
        float accel = dt > 0f ? Math.Clamp((wantVel - AngularVelocity) / dt, -maxAccel, maxAccel) : 0f;
        AngularVelocity += accel * dt;
        Angle += AngularVelocity * dt;

        if (Angle < c.HandMinAngle) { Angle = c.HandMinAngle; AngularVelocity = MathF.Max(0f, AngularVelocity); }
        if (Angle > maxAngle)
        {
            // Closing the bail with the rod back: bring it up to the limit smoothly rather than snapping it.
            if (Angle - AngularVelocity * dt <= maxAngle) { Angle = maxAngle; AngularVelocity = MathF.Min(0f, AngularVelocity); }
        }

        // Reach eases toward its target; arms move in and out more slowly than they swing.
        float r = Math.Clamp(targetReach, 0f, 1f);
        Reach += (r - Reach) * MathF.Min(1f, 10f * dt);
        // The arms swing with the rod: hands low in front when it points at the water, up by the head when it's
        // vertical, and over and behind the shoulder on a full backswing (arm angle trails the rod angle).
        float arm = 0.75f * Angle - 0.35f;
        float len = 0.38f + 0.2f * Reach;
        GripOffset = new Vec2(len * MathF.Cos(arm), len * MathF.Sin(arm) - 0.08f);
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
