using TheLedger.Sim.Core;
using TheLedger.Sim.Tackle;
using TheLedger.Sim.Water;
using TheLedger.Sim.World;

namespace TheLedger.Sim.Fish;

public enum FishState : byte { Cruise, Investigate, Follow, Strike, Hold, Flee, Hooked }

/// <summary>What a hooked fish is doing (species data fight.styles).</summary>
public enum FightMove : byte { Run, Dive, Shake, Circle, Sulk }

/// <summary>How a fish takes the bait or lure (species data feeding.biteStyles).</summary>
public enum BiteStyle : byte { Inhale, Nibble, Run, PickupDrop, Sip }

/// <summary>A lure or bait in the water, as the fish see it this tick.</summary>
public struct LureView
{
    public int Angler;
    public Vec2 Pos, Vel;
    public LureState State;
    public TerminalSpec Spec;
    /// <summary>Another fish has it in its mouth.</summary>
    public bool Taken;
    /// <summary>Tension in the line at the lure, N (fish feel resistance through it).</summary>
    public float Tension;
}

/// <summary>A splash or pop this tick (fish hear these, and big ones close by spook them).</summary>
public struct SplashView
{
    public Vec2 Pos;
    public float Energy;
}

/// <summary>Everything a fish reads from the world for one tick.</summary>
public readonly ref struct FishContext
{
    public FishContext(WaterSurface water, Spot spot, SpotStock stock, double time, float hourOfDay,
        ReadOnlySpan<LureView> lures, ReadOnlySpan<SplashView> splashes, IReadOnlyList<Fish> all)
    {
        Water = water; Spot = spot; Stock = stock; Time = time; Hour = hourOfDay;
        Lures = lures; Splashes = splashes; All = all;
    }

    public WaterSurface Water { get; }
    public Spot Spot { get; }
    public SpotStock Stock { get; }
    public double Time { get; }
    public float Hour { get; }
    public ReadOnlySpan<LureView> Lures { get; }
    public ReadOnlySpan<SplashView> Splashes { get; }
    public IReadOnlyList<Fish> All { get; }

    /// <summary>Daylight 0 (night) .. 1 (day), with dawn and dusk ramps.</summary>
    public float Daylight
    {
        get
        {
            float h = Hour;
            if (h < 5.5f || h > 20.5f) return 0.04f;
            if (h < 7.5f) return 0.04f + 0.96f * (h - 5.5f) / 2f;
            if (h > 18.5f) return 0.04f + 0.96f * (20.5f - h) / 2f;
            return 1f;
        }
    }
}

/// <summary>
/// One fish: a spine in the vertical plane driven at the head by thrust, drag and the line (docs/04 §9), a facing
/// yaw on the lateral pseudo-axis (docs/04 §1), and a utility-plus-state brain driven by its species data
/// (docs/05 §3-4). The body behind the head is kept in body space (forward = +x) so turning around never folds it.
/// </summary>
public sealed class Fish
{
    public Fish(int id, SpeciesSpec species, float lengthM, Vec2 head, bool faceRight, ulong seed)
    {
        Id = id;
        Species = species;
        LengthM = lengthM;
        MassKg = MathF.Max(0.002f, species.WeightKg(lengthM * 100f));
        Rng = new SimRng(seed, (ulong)(1000 + id));
        int n = Math.Clamp(species.Body.SpineNodes, 4, 20);
        Count = n;
        X = new float[n]; Y = new float[n]; PX = new float[n]; PY = new float[n];
        _bx = new float[n]; _by = new float[n];
        Seg = lengthM / (n - 1);
        Yaw = PrevYaw = _yawTarget = faceRight ? 0f : MathF.PI;
        HeadX = head.X; HeadY = head.Y;
        for (int i = 0; i < n; i++) { _bx[i] = -i * Seg; _by[i] = 0f; }
        UpdateWorldNodes();
        for (int i = 0; i < n; i++) { PX[i] = X[i]; PY[i] = Y[i]; }
        _wander = head;
        _decisionOffset = id % 12;
        Hunger = Rng.Range(0.4f, 1f);

        // Thrust and drag: peak thrust ~ powerCoeff × m^(2/3) (docs/04 §9.3); drag sized so peak thrust gives burst speed.
        MaxThrust = 12f * species.Fight.PowerCoeff * MathF.Pow(MassKg, 2f / 3f);
        BurstSpeed = 8f * lengthM;
        CruiseSpeed = 1.1f * lengthM;
        _dragK = MaxThrust / (BurstSpeed * BurstSpeed);
    }

    public int Id { get; }
    public SpeciesSpec Species { get; }
    public float LengthM { get; }
    public float MassKg { get; }
    public SimRng Rng;
    public int Count { get; }
    public float Seg { get; }

    /// <summary>Spine nodes in world space, snout first; P* = last tick (render interpolation).</summary>
    public readonly float[] X, Y, PX, PY;
    private readonly float[] _bx, _by; // body space, head at 0, forward +x

    public float HeadX, HeadY, VX, VY;
    /// <summary>Facing on the lateral pseudo-axis: 0 = facing right (+x), π = left; ±π/2 faces the camera.</summary>
    public float Yaw, PrevYaw;
    /// <summary>Tail-beat phase and amplitude 0..1 (drawn as the lateral wave).</summary>
    public float TailPhase, TailBeat;
    public FishState State { get; private set; }
    public float StateTime;
    public BiteStyle Style;
    public float Hunger, Spook;
    /// <summary>Which angler's lure this fish is interested in or holding (−1 none).</summary>
    public int LureAngler = -1;
    /// <summary>Line force on the mouth from last tick, N (set by the world while holding or hooked).</summary>
    public Vec2 LineForce;
    public float MaxThrust { get; }
    public float BurstSpeed { get; }
    public float CruiseSpeed { get; }

    public bool Holding => State is FishState.Hold or FishState.Hooked;
    public float FacingSign => MathF.Cos(Yaw) >= 0f ? 1f : -1f;
    public Vec2 Mouth => new(X[0], Y[0]);

    private readonly float _dragK;
    private readonly int _decisionOffset;
    private Vec2 _wander;
    private float _wanderTime, _cooldown, _holdTime, _decideTime, _evalTime, _resist;
    private Vec2 _desired;
    private float _desiredSpeedCap;
    private Vec2 _fleeFrom;
    private Vec2 _lastLureVel;

    /// <summary>How far the hook point has gone in while holding, 0..1 (1 = hooked).</summary>
    public float HookDepth;
    /// <summary>How well the hook holds once set, 0..1 (0 = it pulls out).</summary>
    public float HookHold;
    /// <summary>Anaerobic (bursts) and aerobic (sustained) stamina, 0..1 (docs/04 §9.3).</summary>
    public float StaminaA = 1f, StaminaB = 1f;
    public FightMove Move;
    /// <summary>The hook pulled out on the last tick.</summary>
    public bool HookPulledOut;
    /// <summary>Whose line it threw the hook from (valid when <see cref="HookPulledOut"/>).</summary>
    public int PulledFrom = -1;
    private float _moveTime, _effort = 1f;
    /// <summary>The terminal it holds or is hooked on (hook data).</summary>
    public TerminalSpec? HeldTerminal;

    /// <summary>Strikes, holds and spits since spawn (debug and tests).</summary>
    public int Strikes, Holds, Spits;

    // ---------------------------------------------------------------- brain

    /// <summary>Activity 0..1 at this hour from the species' diel data (dawn, day, dusk, night).</summary>
    public float Activity(float hour)
    {
        var d = Species.Activity.Diel;
        float Get(string k) => d.TryGetValue(k, out float v) ? v : 0.5f;
        if (hour >= 5f && hour < 8f) return Get("dawn");
        if (hour >= 8f && hour < 18.5f) return Get("day");
        if (hour >= 18.5f && hour < 21f) return Get("dusk");
        return Get("night");
    }

    /// <summary>How much this fish wants a given terminal, 0..1 (species attraction data).</summary>
    public float Attraction(TerminalSpec t)
    {
        var f = Species.Feeding;
        string key = t.AttractKey;
        if (string.IsNullOrEmpty(key)) return 0f;
        if (t.Natural) return f.BaitAttraction.TryGetValue(key, out float b) ? b : 0f;
        return f.LureAttraction.TryGetValue(key, out float a) ? a : 0f;
    }

    /// <summary>Distance at which this fish notices the lure (vision, lateral line, smell, hearing; docs/05 §3).</summary>
    public float DetectionRange(in LureView l, in FishContext ctx)
    {
        var s = Species.Senses;
        float light = ctx.Daylight;
        float lightF = light + (1f - light) * s.LowLight * 0.7f;
        float size = MathF.Sqrt(MathF.Max(l.Spec.LengthM, 0.01f) / 0.05f);
        float vision = ctx.Stock.VisibilityM * s.Vision * lightF * size * (1f + l.State.Flash * light);
        float lateral = 1.6f * s.LateralLine * MathF.Min(l.State.Vibration, 1.5f);
        float smell = 3f * s.Smell * l.Spec.Scent;
        float hearing = 4f * s.Hearing * l.State.Sound;
        return MathF.Max(MathF.Max(vision, lateral), MathF.Max(smell, hearing));
    }

    private void SetState(FishState s)
    {
        State = s;
        StateTime = 0f;
    }

    /// <summary>Releases what's in its mouth (spits it or loses it).</summary>
    public void Release(float cooldown)
    {
        if (State == FishState.Hold) Spits++;
        LureAngler = -1;
        HookDepth = 0f;
        HeldTerminal = null;
        _cooldown = cooldown;
        SetState(FishState.Cruise);
    }

    /// <summary>Hooks it directly (tests, debug scenes). In play the hook is set by line tension during a hold.</summary>
    public void SetHooked(int angler, TerminalSpec terminal)
    {
        LureAngler = angler;
        HeldTerminal = terminal;
        HookDepth = 1f;
        HookHold = 1f;
        _moveTime = 0f;
        SetState(FishState.Hooked);
    }

    private void Think(float dt, in FishContext ctx)
    {
        StateTime += dt;
        _cooldown -= dt;
        Spook = MathF.Max(0f, Spook - dt * 0.05f);
        var feed = Species.Feeding;
        float act = Activity(ctx.Hour);

        // Splashes: small ones nearby draw attention, big close ones spook (docs/05 §3.3, §3.6).
        foreach (var sp in ctx.Splashes)
        {
            float d = (sp.Pos - new Vec2(HeadX, HeadY)).Length;
            float scare = sp.Energy * Species.Senses.Spookiness / (0.3f + d * d);
            if (scare > 0.6f && State != FishState.Hooked && State != FishState.Hold)
            {
                Spook = MathF.Min(1f, Spook + 0.5f);
                _fleeFrom = sp.Pos;
                LureAngler = -1;
                SetState(FishState.Flee);
            }
        }

        bool decide = _decideTime <= 0f;
        _decideTime -= dt;
        if (decide) _decideTime = 0.1f;

        int li = FindLure(ctx);
        LureView lure = li >= 0 ? ctx.Lures[li] : default;
        Vec2 head = new(HeadX, HeadY);

        switch (State)
        {
            case FishState.Cruise:
                _desiredSpeedCap = CruiseSpeed * (0.5f + 0.5f * act);
                Wander(dt, ctx);
                if (decide && _cooldown <= 0f && Spook < 0.5f)
                {
                    for (int i = 0; i < ctx.Lures.Length; i++)
                    {
                        var l = ctx.Lures[i];
                        if (l.Taken || !l.State.Wet) continue;
                        float d = (l.Pos - head).Length;
                        if (d > DetectionRange(l, ctx)) continue;
                        float interest = act * Attraction(l.Spec) * (0.5f + 0.5f * Hunger) * (1f - Spook);
                        if (Rng.Chance(MathF.Min(0.9f, interest * 1.5f)))
                        {
                            LureAngler = l.Angler;
                            SetState(FishState.Investigate);
                        }
                        else _cooldown = Rng.Range(2f, 6f);
                        break;
                    }
                }
                break;

            case FishState.Investigate:
                if (li < 0 || lure.Taken || !lure.State.Wet) { Release(Rng.Range(1f, 3f)); break; }
                _desired = lure.Pos - head;
                _desiredSpeedCap = CruiseSpeed * 2f;
                if (_desired.Length < LengthM * 2.5f) SetState(FishState.Follow);
                if (StateTime > 10f) Release(Rng.Range(3f, 8f));
                break;

            case FishState.Follow:
            {
                if (li < 0 || lure.Taken || !lure.State.Wet) { Release(Rng.Range(1f, 3f)); break; }
                // Hang just behind and below the lure, matching its pace.
                Vec2 back = lure.Vel.Length > 0.05f ? -lure.Vel.Normalized() : new Vec2(-FacingSign, 0f);
                Vec2 spot = lure.Pos + back * (LengthM * 1.3f) + new Vec2(0f, -0.25f * LengthM);
                _desired = spot - head;
                _desiredSpeedCap = MathF.Max(CruiseSpeed * 2.5f, lure.Vel.Length * 1.3f);
                _evalTime -= dt;
                if (_evalTime <= 0f)
                {
                    _evalTime = 0.5f;
                    float pStrike = StrikeChance(lure, act);
                    if (Rng.Chance(pStrike))
                    {
                        Style = PickStyle(lure.Spec);
                        Strikes++;
                        SetState(FishState.Strike);
                        break;
                    }
                    // Lost interest?
                    if (StateTime > Rng.Range(4f, 14f) * (0.5f + feed.FollowChance)) Release(Rng.Range(4f, 10f));
                }
                _lastLureVel = lure.Vel;
                break;
            }

            case FishState.Strike:
            {
                if (li < 0 || lure.Taken) { Release(Rng.Range(2f, 5f)); break; }
                _desired = (lure.Pos + lure.Vel * 0.08f) - head;
                _desiredSpeedCap = BurstSpeed * (Style == BiteStyle.Nibble ? 0.35f : 0.8f);
                float reach = 0.02f + 0.5f * Species.Fight.Mouth.GapeFrac * LengthM;
                if ((lure.Pos - Mouth).Length < reach)
                {
                    Holds++;
                    Hunger = MathF.Max(0f, Hunger - 0.1f);
                    var r = feed.HoldTimeS;
                    _holdTime = Style == BiteStyle.Nibble ? Rng.Range(0.12f, 0.35f) : Rng.Range(r[0], r[Math.Min(1, r.Length - 1)]);
                    _resist = 0f;
                    _wander = head + new Vec2(Rng.Range(-1f, 1f), Rng.Range(-0.6f, 0.2f)) * (LengthM * 8f);
                    SetState(FishState.Hold);
                }
                else if (StateTime > 1.2f) SetState(FishState.Follow);
                break;
            }

            case FishState.Hold:
            {
                // With the bait in its mouth: nibblers tug and let go, inhalers back off slowly, runners swim off.
                switch (Style)
                {
                    case BiteStyle.Run:
                        _desired = _wander - head;
                        _desiredSpeedCap = CruiseSpeed * 2.5f;
                        break;
                    case BiteStyle.Nibble:
                        _desired = new Vec2(-FacingSign * 0.05f, -0.03f);
                        _desiredSpeedCap = CruiseSpeed * 0.6f;
                        break;
                    default:
                        _desired = (_wander - head).Normalized() * 0.2f;
                        _desiredSpeedCap = CruiseSpeed * 0.8f;
                        break;
                }
                // Hookset: line tension above what the hook point needs drives it in (sharp sweep, or a fish running
                // against a closed bail sets itself). Threshold from mouth hardness and the hook (docs/04 §9.5).
                if (li >= 0) HeldTerminal = lure.Spec;
                float ease = HeldTerminal?.HookEase ?? 0f;
                if (ease > 0f)
                {
                    float th = (0.3f + 2.5f * Species.Fight.Mouth.Hardness) / ease;
                    float t = LineForce.Length;
                    if (t > th) HookDepth += (t - th) / th * dt * 6f;
                    if (HookDepth >= 1f)
                    {
                        HookHold = 1f;
                        _moveTime = 0f;
                        SetState(FishState.Hooked);
                        break;
                    }
                }
                // Resistance: line pull felt at the mouth makes it drop the bait sooner (docs/05 §4.3 refusal).
                float feel = LineForce.Length / MathF.Max(0.3f, 2f * MassKg);
                _resist += dt * Math.Clamp(feel - 0.4f, 0f, 1.5f) * (0.5f + Species.Senses.LineShy); // ~0.3 s reaction at the soonest
                if (StateTime > _holdTime || _resist > 0.5f)
                {
                    bool nibble = Style == BiteStyle.Nibble;
                    Release(nibble ? Rng.Range(0.3f, 1.2f) : Rng.Range(5f, 12f));
                    Spook = MathF.Min(1f, Spook + (nibble ? 0.02f : 0.25f));
                    if (nibble && li >= 0) { LureAngler = lure.Angler; SetState(FishState.Follow); }
                }
                break;
            }

            case FishState.Flee:
                _desired = (head - _fleeFrom).Normalized() + new Vec2(0f, -0.4f);
                _desiredSpeedCap = BurstSpeed * 0.9f;
                if (StateTime > 1.5f) Release(Rng.Range(6f, 15f));
                break;

            case FishState.Hooked:
                Fight(dt, ctx);
                break;
        }
    }

    /// <summary>The fight brain: moves from the species' fight styles, two stamina pools, and the hook's hold.</summary>
    private void Fight(float dt, in FishContext ctx)
    {
        var fs = Species.Fight;
        Vec2 head = new(HeadX, HeadY);
        float t = LineForce.Length;
        Vec2 away = t > 0.05f ? -LineForce / t : new Vec2(FacingSign, 0f);

        _moveTime -= dt;
        if (_moveTime <= 0f)
        {
            _moveTime = Rng.Range(0.8f, 2.6f);
            Move = PickMove();
        }
        bool tired = StaminaA < 0.12f;
        switch (tired ? FightMove.Sulk : Move)
        {
            case FightMove.Run:
                _desired = new Vec2(away.X, MathF.Min(away.Y, 0f) - 0.15f);
                _effort = 1f;
                break;
            case FightMove.Dive:
                _desired = new Vec2(away.X * 0.3f, -1f);
                _effort = 0.85f;
                break;
            case FightMove.Shake:
                _desired = new Vec2(away.X * 0.2f, MathF.Sin(StateTime * 25f));
                _effort = 0.6f;
                break;
            case FightMove.Circle:
                // Sunfish turn their flat side to the pull and plane sideways: across the line, alternating.
                _desired = new Vec2(-away.Y, away.X) * (MathF.Sin(StateTime * 1.3f) > 0f ? 1f : -1f) + away * 0.5f;
                _effort = 0.7f;
                break;
            default:
                _desired = new Vec2(away.X * 0.2f, -0.6f);
                _effort = tired ? 0.15f : 0.4f;
                break;
        }
        _desiredSpeedCap = BurstSpeed;

        // Stamina: bursts drain the anaerobic pool fast; it recovers from the aerobic pool when easing off.
        float use = _effort * (0.25f + 0.75f * StaminaA);
        StaminaA = Math.Clamp(StaminaA - use * dt / 7f + (1f - _effort) * StaminaB * dt * 0.04f, 0f, 1f);
        StaminaB = Math.Clamp(StaminaB - use * dt / 90f, 0f, 1f);

        // Hook hold: slack lets it work loose (fast if barbless), headshakes worry it, and too much pull tears a soft mouth.
        float hard = fs.Mouth.Hardness;
        bool barbless = HeldTerminal?.Barbless ?? false;
        if (t < 0.15f) HookHold -= dt * (barbless ? 0.25f : 0.008f);
        if (Move == FightMove.Shake && t > 0.5f) HookHold -= dt * 0.015f * (1.2f - hard);
        float tear = 4f + 26f * hard;
        if (t > tear) HookHold -= (t - tear) / tear * dt * 2f;
        if (HookHold <= 0f)
        {
            HookPulledOut = true;
            PulledFrom = LureAngler;
            HookDepth = 0f;
            HeldTerminal = null;
            _fleeFrom = head - away;
            LureAngler = -1;
            Spook = 1f;
            _cooldown = 60f;
            SetState(FishState.Flee);
        }
    }

    private FightMove PickMove()
    {
        var styles = Species.Fight.Styles;
        float total = 0f;
        foreach (var kv in styles) total += kv.Value;
        float r = Rng.Range(0f, MathF.Max(total, 1e-6f));
        foreach (var kv in styles)
        {
            r -= kv.Value;
            if (r > 0f) continue;
            return kv.Key switch
            {
                "run" or "structureDash" => FightMove.Run,
                "dive" or "bulldog" => FightMove.Dive,
                "headshake" or "jump" or "roll" => FightMove.Shake,
                "circle" => FightMove.Circle,
                _ => FightMove.Sulk,
            };
        }
        return FightMove.Run;
    }

    /// <summary>Landed and let go: back in the water a little way out, tired and wary, not biting for a while.</summary>
    public void ReleaseAfterCatch(Vec2 at)
    {
        Teleport(at);
        LureAngler = -1;
        HookDepth = 0f;
        HookHold = 0f;
        HeldTerminal = null;
        StaminaA = 0.2f;
        Spook = 1f;
        _fleeFrom = at - new Vec2(FacingSign, 0f);
        _cooldown = 120f;
        SetState(FishState.Flee);
    }

    /// <summary>The line or rod broke: it swims off (with the lure, for now it just shakes it).</summary>
    public void Lose()
    {
        LureAngler = -1;
        HookDepth = 0f;
        HeldTerminal = null;
        Spook = 1f;
        _fleeFrom = new Vec2(HeadX - FacingSign, HeadY);
        _cooldown = 90f;
        SetState(FishState.Flee);
    }

    private int FindLure(in FishContext ctx)
    {
        if (LureAngler < 0) return -1;
        for (int i = 0; i < ctx.Lures.Length; i++) if (ctx.Lures[i].Angler == LureAngler) return i;
        return -1;
    }

    /// <summary>docs/05 §4.3, simplified: activity × match × naturalness × triggers × (1 − spook), per half-second look.</summary>
    private float StrikeChance(in LureView l, float act)
    {
        var f = Species.Feeding;
        float match = Attraction(l.Spec);
        float gape = Species.Fight.Mouth.GapeFrac * LengthM;
        float sizeFit = l.Spec.LengthM < gape * 3.5f ? 1f : MathF.Max(0f, 1f - (l.Spec.LengthM - gape * 3.5f) / (gape * 3f));
        float sp = l.State.Speed;
        float lo = f.SpeedPreferenceMps[0], hi = f.SpeedPreferenceMps[Math.Min(1, f.SpeedPreferenceMps.Length - 1)];
        float natural = sp < lo ? 0.5f : sp <= hi ? 1f : MathF.Max(0f, 1f - (sp - hi) / MathF.Max(hi, 0.2f));
        if (l.Spec.Natural) natural = MathF.Max(natural, 0.7f);
        // Triggers: a sudden change of speed or direction (twitch, pause) provokes reaction strikes.
        float change = (l.Vel - _lastLureVel).Length;
        float trigger = 1f + MathF.Min(1.5f, change * 6f);
        float p = act * match * sizeFit * natural * trigger * (0.4f + 0.6f * Hunger) * (1f - Spook);
        return Math.Clamp(p * 0.6f, 0f, 0.95f);
    }

    private BiteStyle PickStyle(TerminalSpec t)
    {
        var styles = Species.Feeding.BiteStyles;
        float total = 0f;
        foreach (var kv in styles) total += kv.Value;
        float r = Rng.Range(0f, MathF.Max(total, 1e-6f));
        foreach (var kv in styles)
        {
            r -= kv.Value;
            if (r > 0f) continue;
            return kv.Key switch
            {
                "nibble" => BiteStyle.Nibble,
                "run" => BiteStyle.Run,
                "pickupDrop" => BiteStyle.PickupDrop,
                "sip" => BiteStyle.Sip,
                _ => BiteStyle.Inhale,
            };
        }
        return BiteStyle.Inhale;
    }

    private void Wander(float dt, in FishContext ctx)
    {
        Vec2 head = new(HeadX, HeadY);
        _wanderTime -= dt;
        if (_wanderTime <= 0f || (_wander - head).Length < LengthM)
        {
            _wanderTime = Rng.Range(3f, 9f);
            var range = ctx.Stock.RangeX;
            float x = Math.Clamp(HeadX + Rng.Range(-4f, 4f), range[0], range[1]);
            float surface = ctx.Water.Height(x, ctx.Time);
            float ground = ctx.Spot.GroundHeight(x);
            var dm = Species.Habitat.DepthM;
            // Depth band from data, kept inside this water column.
            float dMin = MathF.Min(dm[0], surface - ground - 0.2f), dMax = MathF.Min(dm[Math.Min(1, dm.Length - 1)], surface - ground - 0.1f);
            float depth = Rng.Range(MathF.Max(0.15f, dMin), MathF.Max(0.2f, dMax));
            // Bottom feeders hug the bottom.
            if (Species.Feeding.Mode.StartsWith("bottom")) depth = MathF.Max(depth, surface - ground - 0.25f);
            _wander = new Vec2(x, surface - depth);
        }
        _desired = _wander - head;

        // Schooling: drift toward nearby schoolmates and away from crowding (boids-lite, docs/05 §5).
        string school = Species.Behaviour.Schooling;
        if (school != "solitary")
        {
            float tight = school == "tight" ? 1f : 0.4f;
            Vec2 coh = Vec2.Zero, sep = Vec2.Zero;
            int n = 0;
            for (int i = 0; i < ctx.All.Count; i++)
            {
                var o = ctx.All[i];
                if (o == this || o.Species != Species) continue;
                Vec2 d = new Vec2(o.HeadX, o.HeadY) - head;
                float dist = d.Length;
                if (dist > 3f) continue;
                coh += d;
                n++;
                if (dist < LengthM * 1.5f && dist > 1e-4f) sep -= d / (dist * dist) * LengthM * 0.5f;
            }
            if (n > 0) _desired += (coh / n) * tight + sep;
        }
    }

    // ---------------------------------------------------------------- body

    /// <summary>One tick: think, steer (thrust), integrate the head, keep it in the water, drag the body behind.</summary>
    public void Step(SimConfig c, float dt, in FishContext ctx)
    {
        for (int i = 0; i < Count; i++) { PX[i] = X[i]; PY[i] = Y[i]; }
        PrevYaw = Yaw;
        HookPulledOut = false;
        Think(dt, ctx);

        // Desired velocity → thrust (P-control on velocity, capped by the muscle limit).
        Vec2 dir = _desired.Length > 1e-4f ? _desired.Normalized() : Vec2.Zero;
        float arrive = MathF.Min(1f, _desired.Length / MathF.Max(LengthM * 0.8f, 0.05f));
        Vec2 vDes = dir * (_desiredSpeedCap * arrive);
        float fx = MassKg * 6f * (vDes.X - VX), fy = MassKg * 6f * (vDes.Y - VY);
        float burst = State == FishState.Hooked ? _effort * (0.25f + 0.75f * StaminaA)
            : State is FishState.Strike or FishState.Flee ? 1f : 0.35f;
        float fmax = MaxThrust * burst;
        float fl = MathF.Sqrt(fx * fx + fy * fy);
        if (fl > fmax) { fx *= fmax / fl; fy *= fmax / fl; }

        // Facing: turn toward where it wants to go (through facing the camera); thrust works poorly mid-turn.
        // Turn around only for a clear wish to go the other way, finish each turn, and don't turn again straight
        // after one (no dithering).
        _turnCooldown -= dt;
        bool urgent = State is FishState.Strike or FishState.Flee or FishState.Hooked;
        if (MathF.Abs(Yaw - _yawTarget) < 1e-3f && (_turnCooldown <= 0f || urgent))
        {
            float margin = 0.25f * CruiseSpeed;
            if (vDes.X > margin && _yawTarget != 0f) { _yawTarget = 0f; _turnCooldown = 0.8f; }
            else if (vDes.X < -margin && _yawTarget == 0f) { _yawTarget = MathF.PI; _turnCooldown = 0.8f; }
        }
        float want = _yawTarget;
        float diff = want - Yaw;
        if (MathF.Abs(diff) > 1e-3f)
        {
            float signBefore = FacingSign;
            float rate = (State is FishState.Strike or FishState.Flee ? 16f : 8f) * dt;
            if (MathF.Abs(diff) <= rate) Yaw = want;
            else Yaw += MathF.Sign(diff) * rate;
            // The body is stored behind the mouth, so a facing flip mirrors it about the mouth. A free fish turns about
            // its middle instead: move the mouth to the mirror point so the body stays where it was. (A fish held by
            // the line pivots on its mouth, which is what a hooked fish does.)
            if (FacingSign != signBefore && !Holding)
            {
                float mid = 0f;
                for (int i = 0; i < Count; i++) mid += X[i];
                mid /= Count;
                float shift = 2f * (mid - HeadX);
                HeadX += shift;
                _lastHeadX += shift;
            }
        }
        float facing = MathF.Abs(MathF.Cos(Yaw));
        if (fx * FacingSign < 0f) fx *= 0.15f;
        fx *= 0.3f + 0.7f * facing;

        // Integrate: thrust + line + drag; neutrally buoyant (swim bladder).
        VX += (fx + LineForce.X) / MassKg * dt;
        VY += (fy + LineForce.Y) / MassKg * dt;
        float sp = MathF.Sqrt(VX * VX + VY * VY);
        float damp = 1f / (1f + _dragK / MassKg * sp * dt);
        VX *= damp; VY *= damp;
        HeadX += VX * dt;
        HeadY += VY * dt;

        // Stay in the water and above the bottom (hooked fish may breach later: M6 jumps).
        float surface = ctx.Water.Height(HeadX, ctx.Time);
        float ground = ctx.Spot.GroundHeight(HeadX);
        float half = 0.1f * LengthM;
        if (HeadY > surface - half) { HeadY = surface - half; if (VY > 0f) VY = 0f; }
        if (HeadY < ground + half) { HeadY = ground + half; if (VY < 0f) VY = 0f; }
        var range = ctx.Stock.RangeX;
        if (!Holding)
        {
            if (HeadX < range[0]) { HeadX = range[0]; if (VX < 0f) VX = 0f; }
            if (HeadX > range[1]) { HeadX = range[1]; if (VX > 0f) VX = 0f; }
        }

        // Tail beat from speed (Strouhal ~0.3, tail amplitude ~0.2 L), plus a slow idle fin scull.
        float freq = Math.Clamp(0.3f * sp / (0.2f * LengthM), 1.2f, 14f);
        TailPhase = (TailPhase + 2f * MathF.PI * freq * dt) % (2f * MathF.PI);
        TailBeat += (Math.Clamp(sp / (CruiseSpeed * 2f), 0.15f, 1f) - TailBeat) * MathF.Min(1f, dt * 6f);

        FollowBody(dt);
        UpdateWorldNodes();
    }

    /// <summary>Body-space follow-the-leader: the head's motion drags the body along its path; stiffness straightens it.</summary>
    private void FollowBody(float dt)
    {
        float f = FacingSign;
        float dxBody = (HeadX - _lastHeadX) * f, dyBody = HeadY - _lastHeadY;
        _lastHeadX = HeadX; _lastHeadY = HeadY;
        if (!_bodyInit) { _bodyInit = true; dxBody = dyBody = 0f; }
        // Big jumps (teleports, turn flips) don't drag the body.
        if (dxBody * dxBody + dyBody * dyBody > Seg * Seg * 4f) { dxBody = 0f; dyBody = 0f; }
        for (int i = 1; i < Count; i++) { _bx[i] -= dxBody; _by[i] -= dyBody; }
        float stiff = Math.Clamp(Species.Body.Stiffness, 0.05f, 0.95f) * 0.35f;
        for (int i = 1; i < Count; i++)
        {
            float px = _bx[i - 1], py = _by[i - 1];
            // Straighten toward the extension of the previous segment (or straight back behind the head).
            float ex, ey;
            if (i >= 2) { ex = px + (px - _bx[i - 2]); ey = py + (py - _by[i - 2]); }
            else { ex = px - Seg; ey = py; }
            _bx[i] += (ex - _bx[i]) * stiff;
            _by[i] += (ey - _by[i]) * stiff;
            float dx = _bx[i] - px, dy = _by[i] - py;
            float d = MathF.Sqrt(dx * dx + dy * dy);
            if (d < 1e-6f) { dx = -1f; dy = 0f; d = 1f; }
            // A fish body can't bend forward past its head: keep each node behind the previous one in body space.
            if (dx > -0.2f * d) { dx = -0.2f * d; }
            float nl = MathF.Sqrt(dx * dx + dy * dy);
            _bx[i] = px + dx / nl * Seg;
            _by[i] = py + dy / nl * Seg;
        }
    }

    private float _lastHeadX, _lastHeadY, _turnCooldown, _yawTarget;
    private bool _bodyInit;

    private void UpdateWorldNodes()
    {
        float f = FacingSign;
        for (int i = 0; i < Count; i++)
        {
            X[i] = HeadX + _bx[i] * f;
            Y[i] = HeadY + _by[i];
        }
    }

    /// <summary>Places the fish (spawn, tests).</summary>
    public void Teleport(Vec2 head)
    {
        HeadX = head.X; HeadY = head.Y; VX = VY = 0f;
        _lastHeadX = HeadX; _lastHeadY = HeadY;
        UpdateWorldNodes();
        for (int i = 0; i < Count; i++) { PX[i] = X[i]; PY[i] = Y[i]; }
    }

    public bool IsFinite() => float.IsFinite(HeadX) && float.IsFinite(HeadY) && float.IsFinite(VX) && float.IsFinite(VY);

    public void Hash(ref StateHasher h)
    {
        h.Add(HeadX); h.Add(HeadY); h.Add(VX); h.Add(VY); h.Add(Yaw); h.Add((int)State);
    }
}
