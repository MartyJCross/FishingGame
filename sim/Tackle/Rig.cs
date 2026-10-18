using TheLedger.Sim.Core;
using TheLedger.Sim.Water;
using TheLedger.Sim.World;

namespace TheLedger.Sim.Tackle;

/// <summary>What the tackle needs from the world each substep.</summary>
public readonly struct TackleEnvironment
{
    public TackleEnvironment(WaterSurface water, Spot spot, Vec2 wind, double time)
    {
        Water = water;
        Spot = spot;
        Wind = wind;
        Time = time;
    }

    public WaterSurface Water { get; }
    public Spot Spot { get; }
    /// <summary>Wind velocity, m/s.</summary>
    public Vec2 Wind { get; }
    public double Time { get; }
}

/// <summary>
/// One angler's rod + line + terminal tackle, stepped together with XPBD substeps (docs/04_PHYSICS_SPEC.md §2).
/// Order per substep: forces → predict → drive handle → solve rod → solve line (direct) → contacts → velocities.
/// </summary>
public enum ContactKind : byte { Entry, Skip, Pop }

/// <summary>The terminal disturbing the surface: an entry, a skip, or a popper's pop.</summary>
public struct WaterContact
{
    public float X, VX, VY, Mass, Radius;
    public ContactKind Kind;
}

/// <summary>
/// What the lure is doing this tick, for drawing and for fish senses (docs/04 §6.2): heading, wobble, and the
/// stimulus it puts out.
/// </summary>
public struct LureState
{
    /// <summary>Nose direction, radians (sim space).</summary>
    public float Heading;
    /// <summary>Wobble phase and current amplitude 0..1.</summary>
    public float Phase, Wobble;
    /// <summary>Speed through the water, m/s, and depth below the surface, m (≤ 0 above it).</summary>
    public float Speed, Depth;
    public float Vibration, Flash, Sound;
    public bool Wet;
}

public sealed class Rig
{
    public const float AirDensity = 1.2f;

    public Rig(RodSpec rod, LineSpec line, TerminalSpec terminal, int maxLineNodes)
    {
        Rod = new Rod(rod);
        Line = new Line(line, terminal, maxLineNodes);
        _solver = new TackleSolver(Rod.Count, maxLineNodes);
    }

    private readonly TackleSolver _solver;
    public TackleSolver Solver => _solver;

    public Rod Rod { get; }
    public Line Line { get; }

    /// <summary>Guide forces from the last substep's tip tension (the line running through the guides bends the rod).</summary>
    public bool GuideLineForces = true;

    /// <summary>Puts the rod straight at the grip and hangs <paramref name="lineOut"/> m of line from the tip.</summary>
    public void Reset(Vec2 grip, float angle, float lineOut)
    {
        Rod.SetStraight(grip, angle);
        Line.Reset(Rod.TipPos, new Vec2(0f, -1f), lineOut);
    }

    /// <summary>Line that left the reel during the last tick (slip force exceeded), m.</summary>
    public float SlippedThisTick { get; private set; }
    /// <summary>Line actually taken in during the last tick, m (≥ 0).</summary>
    public float TakenThisTick { get; private set; }

    /// <summary>The terminal's water entries and skips during the last tick (at most a few).</summary>
    public readonly WaterContact[] Entries = new WaterContact[4];
    public int EntryCount { get; private set; }
    private bool _terminalWet = true;
    private float _popCooldown;
    private float _prevSpeed;

    /// <summary>The lure's action and stimulus after the last tick.</summary>
    public LureState Lure;

    /// <summary>
    /// Advances the rig one tick. The handle moves from its last target to (grip, angle) over the tick.
    /// <paramref name="payOutDelta"/> changes the line out directly (negative = cranked in); above
    /// <paramref name="slipForce"/> at the rod the reel gives line (open bail, drag).
    /// </summary>
    public void Step(SimConfig c, float dt, in TackleEnvironment env, Vec2 grip, float angle, float payOutDelta,
        float slipForce = float.PositiveInfinity)
    {
        SlippedThisTick = 0f;
        TakenThisTick = 0f;
        EntryCount = 0;
        Rod.SetHandleTarget(grip, angle);
        Line.SetSolverMassFloor(MathF.Max(c.LineNodeMinMassKg, Line.Terminal.MassKg / MathF.Max(c.LineMassRatioLimit, 1f)));
        if (payOutDelta != 0f)
        {
            float before = Line.LineOut;
            Line.PayOut(payOutDelta, c.MinLineOut, Rod.TipPos, new Vec2(Rod.VX[Rod.Tip], Rod.VY[Rod.Tip]));
            TakenThisTick = MathF.Max(0f, before - Line.LineOut);
        }

        int n = Math.Max(1, c.TackleSubsteps);
        float h = dt / n;
        Rod.BeginTick();
        Line.BeginTick();
        for (int s = 0; s < n; s++)
        {
            float f = (s + 1) / (float)n;
            double t = env.Time + f * dt;
            IntegrateRod(c, h, env, t);
            IntegrateLine(c, h, env, t);
            Rod.DriveHandle(f, h);

            _solver.Solve(Rod, Line, h, Rod.Spec.MaterialDamping * 0.001f, c.RodStretchCompliance, c.TackleIterations, slipForce);
            if (_solver.SlipDistance > 0f)
            {
                SlippedThisTick += _solver.SlipDistance;
                Line.PayOut(_solver.SlipDistance, c.MinLineOut, Rod.TipPos, new Vec2(Rod.VX[Rod.Tip], Rod.VY[Rod.Tip]));
            }

            Contacts(c, env);
            UpdateVelocities(c, h);
            WaterEntry(c, env, t);
            Rod.AccumulateMoments(h, n);
            Line.AccumulateTension(h, n);
            if (GuideLineForces) ApplyGuideForces();
        }
        Rod.EndTick();
        Line.EndTick();
        UpdateLure(c, dt, env);
    }

    private void UpdateLure(SimConfig c, float dt, in TackleEnvironment env)
    {
        var l = Line;
        var spec = l.Terminal;
        int k = l.TerminalIndex;
        float surface = env.Water.Height(l.X[k], env.Time + dt);
        bool wet = l.Y[k] < surface;
        Vec2 u = wet ? env.Water.WaveVelocity(l.X[k], l.Y[k], env.Time + dt) : env.Wind;
        float rvx = l.VX[k] - u.X, rvy = l.VY[k] - u.Y;
        float speed = MathF.Sqrt(rvx * rvx + rvy * rvy);

        // The line pulls the nose toward the previous node; moving fast, the body trails its motion.
        float px = k > 0 ? l.X[k - 1] : Rod.X[Rod.Tip], py = k > 0 ? l.Y[k - 1] : Rod.Y[Rod.Tip];
        float lineHeading = MathF.Atan2(py - l.Y[k], px - l.X[k]);
        float target = speed > 0.3f ? MathF.Atan2(rvy, rvx) : lineHeading;
        if (l.Tension[Math.Max(k - 1, 0)] > 0.02f) target = lineHeading;
        Lure.Heading += Angles.Wrap(target - Lure.Heading) * MathF.Min(1f, dt * 12f);

        float active = wet && speed > spec.ActionMinSpeed ? MathF.Min(1f, (speed - spec.ActionMinSpeed) / 0.3f) : 0f;
        Lure.Phase = (Lure.Phase + 2f * MathF.PI * spec.WobbleHzPerMps * speed * dt * active) % (2f * MathF.PI);
        Lure.Wobble += (active - Lure.Wobble) * MathF.Min(1f, dt * 10f);
        Lure.Speed = speed;
        Lure.Depth = surface - l.Y[k];
        Lure.Wet = wet;
        Lure.Vibration = spec.Vibration * MathF.Min(speed, 2f) * Lure.Wobble;
        Lure.Flash = spec.Flash * Lure.Wobble;
        Lure.Sound = spec.Sound * Lure.Wobble;

        // Poppers: a sharp twitch at the surface cups water and pops (docs/04 §6.2).
        _popCooldown -= dt;
        if (spec.Kind == "popper" && MathF.Abs(Lure.Depth) < 0.04f && _popCooldown <= 0f
            && (speed - _prevSpeed) / dt > c.PopAcceleration && speed > 0.4f && EntryCount < Entries.Length)
        {
            Entries[EntryCount++] = new WaterContact { X = l.X[k], VX = l.VX[k], VY = -speed * 0.5f, Mass = spec.MassKg * 4f, Radius = 0.02f, Kind = ContactKind.Pop };
            Lure.Sound = MathF.Max(Lure.Sound, 1f);
            _popCooldown = 0.2f;
        }
        _prevSpeed = speed;
    }

    private void IntegrateRod(SimConfig c, float h, in TackleEnvironment env, double t)
    {
        var r = Rod;
        for (int i = 2; i < r.Count; i++)
        {
            float vx = r.VX[i], vy = r.VY[i];
            vy -= c.Gravity * h;
            vx += r.FX[i] * r.W[i] * h;
            vy += r.FY[i] * r.W[i] * h;
            r.FX[i] = 0f; r.FY[i] = 0f;

            // Drag on the blank (cylinder across the flow), air or water.
            float surface = env.Water.Height(r.X[i], t);
            bool wet = r.Y[i] < surface;
            float rho = wet ? env.Water.Density : AirDensity;
            Vec2 u = wet ? env.Water.WaveVelocity(r.X[i], r.Y[i], t) : env.Wind;
            float segLen = 0.5f * (r.RestLen[i - 1] + (i < r.Count - 1 ? r.RestLen[i] : 0f));
            float k = 0.5f * rho * 1.2f * r.Diameter[i] * segLen * r.W[i];
            Drag(ref vx, ref vy, u, k, h);
            if (wet) vy += (env.Water.Density * MathF.PI * 0.25f * r.Diameter[i] * r.Diameter[i] * segLen) * r.W[i] * c.Gravity * h;

            r.VX[i] = vx; r.VY[i] = vy;
            r.PX[i] = r.X[i]; r.PY[i] = r.Y[i];
            r.X[i] += vx * h;
            r.Y[i] += vy * h;
        }
        r.PX[0] = r.X[0]; r.PY[0] = r.Y[0];
        r.PX[1] = r.X[1]; r.PY[1] = r.Y[1];
    }

    private void IntegrateLine(SimConfig c, float h, in TackleEnvironment env, double t)
    {
        var l = Line;
        var spec = l.Spec;
        float d = spec.DiameterM;
        int term = l.TerminalIndex;
        for (int k = 0; k < l.Count; k++)
        {
            float vx = l.VX[k], vy = l.VY[k];
            vy -= c.Gravity * h;

            float surface = env.Water.Height(l.X[k], t);
            bool wet = l.Y[k] < surface;
            l.Submerged[k] = wet;
            float rho = wet ? env.Water.Density : AirDensity;
            Vec2 u = wet ? env.Water.WaveVelocity(l.X[k], l.Y[k], t) : env.Wind;

            float segAround = 0.5f * ((k == 0 ? l.FirstRest : l.Rest[k - 1]) + (k < l.Count - 1 ? l.Rest[k] : 0f));
            float w = l.W[k];

            if (wet)
            {
                // Buoyancy of the line around this node (+ the terminal's own volume).
                float vol = spec.Area * segAround + (k == term ? l.Terminal.VolumeM3 : 0f);
                vy += env.Water.Density * vol * c.Gravity * w * h;
            }

            if (k == term)
            {
                // The terminal: isotropic sphere-like drag.
                float area = MathF.PI * 0.25f * l.Terminal.DiameterM * l.Terminal.DiameterM;
                float kk = 0.5f * rho * l.Terminal.DragCoefficient * area * w;
                // Plus the line's own drag around this node.
                kk += 0.5f * rho * c.LineNormalDrag * d * segAround * w;
                Drag(ref vx, ref vy, u, kk, h);

                // A diving lip pulled through water: lift across the motion, toward the bottom (docs/04 §6.2).
                if (wet && l.Terminal.LipLiftArea > 0f)
                {
                    float rx = vx - u.X, ry = vy - u.Y;
                    float sp = MathF.Sqrt(rx * rx + ry * ry);
                    if (sp > 0.05f)
                    {
                        float nx = ry / sp, ny = -rx / sp;
                        if (ny > 0f) { nx = -nx; ny = -ny; }
                        float lift = 0.5f * rho * l.Terminal.LipLiftArea * sp * sp * (MathF.Abs(rx) / sp);
                        vx += nx * lift * w * h;
                        vy += ny * lift * w * h;
                    }
                }
            }
            else
            {
                if (k == l.FloatIndex)
                {
                    // The bobber: buoyant in proportion to how much of it is under, plus its own drag.
                    float fd = l.Terminal.FloatDiameterM;
                    float under = Math.Clamp((surface - l.Y[k]) / fd + 0.5f, 0f, 1f);
                    vy += env.Water.Density * l.Terminal.FloatVolumeM3 * under * c.Gravity * w * h;
                    float rhoF = under > 0f ? env.Water.Density : AirDensity;
                    Drag(ref vx, ref vy, u, 0.5f * rhoF * 0.6f * fd * fd * w, h);
                }
                // Line: strong drag across the line, weak along it (that difference is what makes line belly).
                float tx = (k + 1 < l.Count ? l.X[k + 1] : l.X[k]) - (k > 0 ? l.X[k - 1] : Rod.X[Rod.Tip]);
                float ty = (k + 1 < l.Count ? l.Y[k + 1] : l.Y[k]) - (k > 0 ? l.Y[k - 1] : Rod.Y[Rod.Tip]);
                float tl = MathF.Sqrt(tx * tx + ty * ty);
                if (tl > 1e-9f) { tx /= tl; ty /= tl; } else { tx = 1f; ty = 0f; }
                float rvx = vx - u.X, rvy = vy - u.Y;
                float along = rvx * tx + rvy * ty;
                float ntx = rvx - along * tx, nty = rvy - along * ty;
                float vn = MathF.Sqrt(ntx * ntx + nty * nty);
                float kn = 0.5f * rho * c.LineNormalDrag * d * segAround * w;
                float kt = 0.5f * rho * c.LineTangentDrag * MathF.PI * d * segAround * w;
                float sn = 1f / (1f + kn * h * vn);
                float st = 1f / (1f + kt * h * MathF.Abs(along));
                vx = u.X + ntx * sn + along * st * tx;
                vy = u.Y + nty * sn + along * st * ty;
            }

            l.VX[k] = vx; l.VY[k] = vy;
            l.PX[k] = l.X[k]; l.PY[k] = l.Y[k];
            l.X[k] += vx * h;
            l.Y[k] += vy * h;
        }
    }

    /// <summary>
    /// The terminal crossing the surface from above (docs/04 §6.3): skip if it's flat and fast enough and the angle is
    /// shallow (Clanet et al.), otherwise enter. Either way it's recorded for the world to splash and tell the fish.
    /// </summary>
    private void WaterEntry(SimConfig c, in TackleEnvironment env, double t)
    {
        var l = Line;
        int k = l.TerminalIndex;
        float surface = env.Water.Height(l.X[k], t);
        bool wet = l.Y[k] < surface;
        if (wet && !_terminalWet)
        {
            float vx = l.VX[k], vy = l.VY[k];
            float speed = MathF.Sqrt(vx * vx + vy * vy);
            float angleDeg = MathF.Atan2(-vy, MathF.Abs(vx)) * (180f / MathF.PI);
            float planing = l.Terminal.Planing;
            bool skip = vy < 0f && planing > 0f && speed > c.SkipMinSpeed && angleDeg < c.SkipMaxAngleDeg * planing;
            if (skip)
            {
                l.Y[k] = surface;
                l.VY[k] = -vy * c.SkipRestitution;
                l.VX[k] = vx * (1f - c.SkipSpeedLoss);
                wet = false;
            }
            if (EntryCount < Entries.Length)
                Entries[EntryCount++] = new WaterContact { X = l.X[k], VX = vx, VY = vy, Mass = l.Terminal.MassKg, Radius = 0.5f * l.Terminal.DiameterM, Kind = skip ? ContactKind.Skip : ContactKind.Entry };
        }
        _terminalWet = wet;
    }

    /// <summary>Implicit quadratic drag toward the medium velocity u: v ← u + (v−u)/(1 + k·h·|v−u|).</summary>
    private static void Drag(ref float vx, ref float vy, Vec2 u, float k, float h)
    {
        float rx = vx - u.X, ry = vy - u.Y;
        float speed = MathF.Sqrt(rx * rx + ry * ry);
        float s = 1f / (1f + k * h * speed);
        vx = u.X + rx * s;
        vy = u.Y + ry * s;
    }

    private void Contacts(SimConfig c, in TackleEnvironment env)
    {
        var spot = env.Spot;
        var r = Rod;
        for (int i = 2; i < r.Count; i++)
        {
            float g = spot.GroundHeight(r.X[i]) + 0.5f * r.Diameter[i];
            r.InContact[i] = r.Y[i] < g;
            if (r.InContact[i]) r.Y[i] = g;
        }
        var l = Line;
        for (int k = 0; k < l.Count; k++)
        {
            float rad = k == l.TerminalIndex ? 0.5f * l.Terminal.DiameterM : 0.5f * l.Spec.DiameterM;
            float g = spot.GroundHeight(l.X[k]) + rad;
            l.InContact[k] = l.Y[k] < g;
            if (l.InContact[k]) l.Y[k] = g;
        }
    }

    private void UpdateVelocities(SimConfig c, float h)
    {
        float inv = 1f / h;
        float vmax = c.MaxNodeSpeed;
        float settle = 1f / (1f + c.TackleSettleDamping * h);
        var r = Rod;
        for (int i = 2; i < r.Count; i++)
        {
            float vx = (r.X[i] - r.PX[i]) * inv, vy = (r.Y[i] - r.PY[i]) * inv;
            if (r.InContact[i]) Friction(ref vx, ref vy, c.GroundFriction);
            Clamp(ref vx, ref vy, vmax);
            r.VX[i] = vx * settle; r.VY[i] = vy * settle;
        }
        var l = Line;
        for (int k = 0; k < l.Count; k++)
        {
            float vx = (l.X[k] - l.PX[k]) * inv, vy = (l.Y[k] - l.PY[k]) * inv;
            if (l.InContact[k]) Friction(ref vx, ref vy, c.GroundFriction);
            Clamp(ref vx, ref vy, vmax);
            l.VX[k] = vx * settle; l.VY[k] = vy * settle;
        }
    }

    private static void Friction(ref float vx, ref float vy, float mu)
    {
        // Simple Coulomb-like: kill most tangential motion and any motion into the ground.
        float k = Math.Clamp(1f - mu, 0f, 1f);
        vx *= k;
        if (vy < 0f) vy = 0f;
    }

    private static void Clamp(ref float vx, ref float vy, float vmax)
    {
        float s2 = vx * vx + vy * vy;
        if (s2 > vmax * vmax)
        {
            float s = vmax / MathF.Sqrt(s2);
            vx *= s; vy *= s;
        }
    }

    /// <summary>
    /// The line inside the rod runs reel → guides → tip. With tension T it pushes each guide by T·(d_out − d_in)
    /// (docs/04_PHYSICS_SPEC.md §3.3). The tip already feels T·d_out through the line constraint, so it gets −T·d_in.
    /// </summary>
    private void ApplyGuideForces()
    {
        var r = Rod;
        // The tick-averaged tip tension from the previous tick: smooth and stable as an explicit force.
        float T = Line.Tension[0];
        if (T <= 0f) return;
        int prev = 1; // the reel sits at the fore grip
        for (int i = 2; i < r.Count; i++)
        {
            if (!r.IsGuide[i]) continue;
            float inx = r.X[i] - r.X[prev], iny = r.Y[i] - r.Y[prev];
            float inl = MathF.Sqrt(inx * inx + iny * iny);
            if (inl < 1e-6f) continue;
            inx /= inl; iny /= inl;
            if (i == r.Tip)
            {
                r.FX[i] -= T * inx;
                r.FY[i] -= T * iny;
                break;
            }
            int next = i + 1;
            while (next < r.Count && !r.IsGuide[next]) next++;
            float ox = r.X[next] - r.X[i], oy = r.Y[next] - r.Y[i];
            float ol = MathF.Sqrt(ox * ox + oy * oy);
            if (ol < 1e-6f) continue;
            ox /= ol; oy /= ol;
            r.FX[i] += T * (ox - inx);
            r.FY[i] += T * (oy - iny);
            prev = i;
        }
    }

    public bool IsFinite() => Rod.IsFinite() && Line.IsFinite();

    public void Hash(ref StateHasher h)
    {
        Rod.Hash(ref h);
        Line.Hash(ref h);
    }
}
