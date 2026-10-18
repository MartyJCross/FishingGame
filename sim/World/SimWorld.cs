using TheLedger.Sim.Angler;
using TheLedger.Sim.Core;
using TheLedger.Sim.Fish;
using TheLedger.Sim.Input;
using TheLedger.Sim.Tackle;
using TheLedger.Sim.Water;

namespace TheLedger.Sim.World;

/// <summary>
/// The whole simulation (docs/02_TECH_STACK.md §3). Step it at the fixed tick with one InputFrame per angler.
/// Rules: no frame time, no System.Random, no allocation inside Step, fixed iteration order.
/// </summary>
public sealed class SimWorld
{
    private readonly AnglerState[] _anglers;

    public SimWorld(SimConfig config, ulong seed, int anglerCount = 1, Spot? spot = null)
    {
        if (anglerCount < 1) throw new ArgumentOutOfRangeException(nameof(anglerCount));
        Config = config;
        Seed = seed;
        Rng = new SimRng(SimRng.Mix(seed, 1));
        Spot = spot ?? Spot.TestPond();
        _anglers = new AnglerState[anglerCount];
        for (int i = 0; i < anglerCount; i++)
            _anglers[i] = new AnglerState(i, Spot.AnglerStart - new Vec2(1.5f * i, 0f), config);

        _lureViews = new LureView[anglerCount];
        Water = new WaterSurface(seed);
        Water.Conditions = ConditionsFromConfig();
        // Patch 0 covers the water in front of the angler (M3 adds the lure patch, M6 the fish patch).
        float span = (config.PatchColumns - 1) * config.PatchSpacing;
        Water.CreatePatch(0, config.PatchColumns, config.PatchSpacing, -1f + 0.5f * span);
    }

    private readonly List<Fish.Fish> _fish = new();
    private readonly LureView[] _lureViews;
    private readonly SplashView[] _splashViews = new SplashView[32];
    private int _splashCount;

    /// <summary>Fish landed this session (the temporary Ledger). Pre-sized; adding never allocates in play.</summary>
    public List<Catch> Catches { get; } = new(512);

    /// <summary>The fish in this spot.</summary>
    public IReadOnlyList<Fish.Fish> Fish => _fish;

    /// <summary>Who lives here and how clear the water is.</summary>
    public SpotStock Stock { get; private set; } = new();

    /// <summary>Hour of day, 0..24 (from <see cref="SimConfig.StartHour"/> and <see cref="SimConfig.TimeScale"/>).</summary>
    public float Hour => (float)((Config.StartHour + Time * Config.TimeScale / 3600.0) % 24.0);

    /// <summary>Game days since the game began (advances at midnight).</summary>
    public int Day => (int)(Config.StartDay + Math.Floor((Config.StartHour + Time * Config.TimeScale / 3600.0) / 24.0));

    /// <summary>Day within the game year (0 .. 4 × DaysPerSeason − 1); the year starts with winter.</summary>
    public int CalendarDay => Day % (4 * Math.Max(1, Config.DaysPerSeason));

    /// <summary>0 winter, 1 spring, 2 summer, 3 autumn; and the day within the season (0-based).</summary>
    public int SeasonIndex => CalendarDay / Math.Max(1, Config.DaysPerSeason);
    public int DayOfSeason => CalendarDay % Math.Max(1, Config.DaysPerSeason);

    /// <summary>Season name for the species' seasonal activity data.</summary>
    public string Season => SeasonIndex switch { 0 => "winter", 1 => "spring", 2 => "summer", _ => "autumn" };

    /// <summary>
    /// The real-calendar day of the year this game moment stands for (0 = 1 January): the short game year is mapped
    /// onto meteorological seasons, winter starting 1 December. Used for water temperature.
    /// </summary>
    public int DayOfYear
    {
        get
        {
            float yearFrac = (CalendarDay + Hour / 24f) / (4f * Math.Max(1, Config.DaysPerSeason));
            return (int)((334f + yearFrac * 365f) % 365f);
        }
    }

    /// <summary>
    /// Water temperature, °C: a Florida shallow-water year (about 15 °C in mid January to 30 °C in mid July) plus an
    /// afternoon warm-up, blended with spring water where the spot has springs (they hold about 22-23 °C all year).
    /// </summary>
    public float WaterTempC
    {
        get
        {
            float season = 22.5f - 7.5f * MathF.Cos(2f * MathF.PI * (DayOfYear - 15f) / 365f);
            float day = 1.2f * MathF.Sin(2f * MathF.PI * (Hour - 10f) / 24f);
            return season + day + (Spot.SpringC - (season + day)) * Spot.SpringMix;
        }
    }

    /// <summary>
    /// Stocks the spot (setup only; allocates). Sizes come from each species' growth curve: a random age, von
    /// Bertalanffy length with ±10% spread, clamped to the species' record.
    /// </summary>
    public void Populate(SpotStock stock, IReadOnlyList<SpeciesSpec> species)
    {
        Stock = stock;
        _fish.Clear();
        var rng = new SimRng(SimRng.Mix(Seed, 77));
        int id = 0;
        foreach (var entry in stock.Stock)
        {
            SpeciesSpec? sp = null;
            foreach (var s in species) if (s.Id == entry.Species) { sp = s; break; }
            if (sp == null) continue;
            for (int i = 0; i < entry.Count; i++)
            {
                var g = sp.Size.Growth;
                float age = rng.Range(1f, MathF.Max(2f, (g.MaxAgeYears ?? 8f) * 0.7f));
                float lenCm = g.Linf_cm * (1f - MathF.Exp(-g.K * age)) * rng.Range(0.9f, 1.1f);
                lenCm = Math.Clamp(lenCm, sp.Size.LengthCm.Typical[0] * 0.6f, sp.Size.LengthCm.Max);
                float x = rng.Range(stock.RangeX[0] + 1f, stock.RangeX[1] - 1f);
                float surface = Water.Height(x, 0.0), ground = Spot.GroundHeight(x);
                float y = rng.Range(ground + 0.2f, surface - 0.2f);
                _fish.Add(new Fish.Fish(id, sp, lenCm * 0.01f, new Vec2(x, y), rng.Chance(0.5f), Seed + (ulong)id * 7919UL));
                id++;
            }
        }
    }

    /// <summary>Adds one fish (tests and debug scenes; allocates).</summary>
    public void AddFish(Fish.Fish fish) => _fish.Add(fish);

    /// <summary>The spot being fished (terrain now; structure and fish later).</summary>
    public Spot Spot { get; }

    /// <summary>Wind as a velocity, m/s (from the debug conditions until weather exists).</summary>
    public Vec2 Wind => new(Config.DebugWindSpeed * (Config.DebugWindDir < 0f ? -1f : 1f), 0f);

    /// <summary>The spot's water surface.</summary>
    public WaterSurface Water { get; }

    /// <summary>Until spots and weather exist (M8+), water conditions come from the debug tunables.</summary>
    public bool ConditionsFromDebugConfig = true;

    private WaterConditions ConditionsFromConfig() => new()
    {
        WindSpeed = Config.DebugWindSpeed,
        WindDir = Config.DebugWindDir,
        Fetch = Config.DebugFetch,
        Depth = Config.DebugDepth,
        SwellHeight = Config.DebugSwellHeight,
        SwellPeriod = Config.DebugSwellPeriod,
        SwellDir = 1f,
        RainRate = Config.DebugRainRate,
        Density = 1000f,
    };

    /// <summary>
    /// Something hits the water (a debug click now; lures, fish and boats later). Returns the impact energy, J,
    /// and queues a Splash event.
    /// </summary>
    public float Splash(float x, float speed, float mass, float radius, float horizontalSpeed = 0f)
    {
        float e = Water.Splash(Config, x, speed, mass, radius, horizontalSpeed);
        Events.Push(new SimEvent
        {
            Type = SimEventType.Splash, Tick = Tick, Angler = -1,
            Position = new Vec2(x, Water.Height(x, Time)), Value = e,
        });
        return e;
    }

    public SimConfig Config { get; }
    public ulong Seed { get; }

    /// <summary>World-level random stream. Subsystems get their own via SimRng.Mix(Seed, streamId).</summary>
    public SimRng Rng;

    /// <summary>Ticks stepped so far.</summary>
    public long Tick { get; private set; }

    /// <summary>Sim time in seconds (Tick × dt, computed to avoid float drift).</summary>
    public double Time => Tick / (double)Config.TickHz;

    public IReadOnlyList<AnglerState> Anglers => _anglers;
    public int AnglerCount => _anglers.Length;

    public SimEventQueue Events { get; } = new();

    /// <summary>Times a NaN/Inf was caught and repaired. Tests require 0.</summary>
    public int FaultCount { get; private set; }

    /// <summary>Advances the world by one fixed tick. <paramref name="inputs"/> holds one frame per angler.</summary>
    public void Step(ReadOnlySpan<InputFrame> inputs)
    {
        if (inputs.Length != _anglers.Length)
            throw new ArgumentException($"Expected {_anglers.Length} input frames, got {inputs.Length}");

        float dt = Config.Dt;
        var env = new TackleEnvironment(Water, Spot, Wind, Time);
        if (_snags.Length != _anglers.Length) _snags = new SnagState[_anglers.Length]; // once
        StepFish(dt);
        _splashCount = 0;
        for (int i = 0; i < _anglers.Length; i++)
        {
            _anglers[i].Step(Config, dt, inputs[i], env);
            if (_anglers[i].Reel.BacklashedNow)
                Events.Push(new SimEvent { Type = SimEventType.Backlash, Tick = Tick, Angler = i, Value = _anglers[i].Reel.Loose });
            var rig = _anglers[i].Rig;
            for (int e = 0; e < rig.EntryCount; e++)
            {
                // Entries, skips and pops splash the surface; fish will hear these (docs/05 §3.4).
                var w = rig.Entries[e];
                float energy = Water.Splash(Config, w.X, MathF.Abs(w.VY), w.Mass, MathF.Max(w.Radius, 0.01f), w.VX);
                if (_splashCount < _splashViews.Length)
                    _splashViews[_splashCount++] = new SplashView { Pos = new Vec2(w.X, Water.Height(w.X, Time)), Energy = energy };
                Events.Push(new SimEvent
                {
                    Type = SimEventType.Splash, Tick = Tick, Angler = i, Id = (int)w.Kind,
                    Position = new Vec2(w.X, Water.Height(w.X, Time)), Value = energy,
                });
            }
        }

        LineForcesOnFish();
        Wakes();
        Snags(dt);
        Failures();
        Landing();

        if (ConditionsFromDebugConfig) Water.Conditions = ConditionsFromConfig();
        Water.Step(Config, dt, Time);

        Tick++;
        GuardFinite();
    }

    /// <summary>Fish see the lures, think and swim; a fish holding a lure pins it to its mouth for the tackle step.</summary>
    private void StepFish(float dt)
    {
        if (_fish.Count == 0) return;
        for (int i = 0; i < _anglers.Length; i++)
        {
            var rig = _anglers[i].Rig;
            var l = rig.Line;
            int t = l.TerminalIndex;
            _lureViews[i] = new LureView
            {
                Angler = i,
                Pos = l.TerminalPos,
                Vel = new Vec2(l.VX[t], l.VY[t]),
                State = rig.Lure,
                Spec = l.Terminal,
                Tension = l.Tension[t],
                Taken = l.Pinned,
            };
        }
        var ctx = new FishContext(Water, Spot, Stock, Time, Hour, _lureViews, _splashViews.AsSpan(0, _splashCount), _fish)
        {
            WaterTempC = WaterTempC, Season = Season,
            Overcast = Config.CloudCover > 0.6f, Raining = Config.DebugRainRate > 1f, PostColdFront = Config.PostColdFront,
            WaterWarming = DayOfYear is > 30 and < 180,
        };
        for (int f = 0; f < _fish.Count; f++)
        {
            var fish = _fish[f];
            var before = fish.State;
            fish.Step(Config, dt, ctx);
            if (fish.State == FishState.Strike && before != FishState.Strike)
                Events.Push(new SimEvent { Type = SimEventType.FishStrike, Tick = Tick, Angler = fish.LureAngler, Id = fish.Id, Position = fish.Mouth });
            if (fish.State == FishState.Hold && before != FishState.Hold)
                Events.Push(new SimEvent { Type = SimEventType.Bite, Tick = Tick, Angler = fish.LureAngler, Id = fish.Id, Position = fish.Mouth });
            if (fish.State == FishState.Hooked && before != FishState.Hooked)
                Events.Push(new SimEvent { Type = SimEventType.HookSet, Tick = Tick, Angler = fish.LureAngler, Id = fish.Id, Position = fish.Mouth });
            if (fish.BreachedOut || fish.SplashedIn)
            {
                float x = fish.HeadX;
                float energy = Water.Splash(Config, x, fish.SplashSpeed, fish.MassKg, 0.15f * fish.LengthM, fish.VX);
                if (_splashCount < _splashViews.Length)
                    _splashViews[_splashCount++] = new SplashView { Pos = new Vec2(x, Water.Height(x, Time)), Energy = energy };
                Events.Push(new SimEvent { Type = SimEventType.Splash, Tick = Tick, Angler = fish.LureAngler, Id = 3, Position = new Vec2(x, Water.Height(x, Time)), Value = energy });
                if (fish.BreachedOut)
                    Events.Push(new SimEvent { Type = SimEventType.FishJumped, Tick = Tick, Angler = fish.LureAngler, Id = fish.Id, Position = fish.Mouth, Value = fish.MassKg });
            }
            if (fish.HookPulledOut)
                Events.Push(new SimEvent { Type = SimEventType.HookPulled, Tick = Tick, Angler = fish.PulledFrom, Id = fish.Id, Position = fish.Mouth });
            if (before == FishState.Hold && fish.State != FishState.Hold && fish.State != FishState.Hooked)
                Events.Push(new SimEvent { Type = SimEventType.FishSpit, Tick = Tick, Angler = -1, Id = fish.Id, Position = fish.Mouth });
        }
        // Pins: one fish per lure.
        for (int i = 0; i < _anglers.Length; i++)
        {
            Fish.Fish? holder = null;
            for (int f = 0; f < _fish.Count; f++)
                if (_fish[f].Holding && _fish[f].LureAngler == i) { holder = _fish[f]; break; }
            var line = _anglers[i].Rig.Line;
            if (holder != null) { _snags[i].On = false; line.Pin(holder.Mouth); }
            else if (_snags[i].On) line.Pin(_snags[i].At);
            else line.Unpin();
        }
    }

    /// <summary>Lures, floats and fish near the surface push the water as they move (docs/04 §7.1).</summary>
    private void Wakes()
    {
        bool ringTick = Tick % 10 == 0;
        for (int i = 0; i < _anglers.Length; i++)
        {
            var l = _anglers[i].Rig.Line;
            int t = l.TerminalIndex;
            float r = 0.5f * MathF.Max(l.Terminal.DiameterM, l.Terminal.LengthM * 0.4f);
            Body(l.X[t], l.Y[t], l.VX[t], l.VY[t], r, ringTick);
            int f = l.FloatIndex;
            if (f >= 0 && f < t) Body(l.X[f], l.Y[f], l.VX[f], l.VY[f], 0.5f * l.Terminal.FloatDiameterM, ringTick);
        }
        for (int k = 0; k < _fish.Count; k++)
        {
            var fish = _fish[k];
            if (fish.State is FishState.InHand or FishState.Kept) continue;
            float r = 0.12f * fish.LengthM;
            Body(fish.HeadX, fish.HeadY, fish.VX, fish.VY, r, (Tick + k) % 14 == 0);
        }

        void Body(float x, float y, float vx, float vy, float r, bool ring)
        {
            float depth = Water.Height(x, Time) - y;
            float reach = 2.5f * r + 0.03f;
            if (depth < -r || depth > reach) return;
            float near = 1f - Math.Clamp(depth, 0f, reach) / reach;
            float speed = MathF.Sqrt(vx * vx + vy * vy);
            Water.Disturb(Config, x, vx, vy, r, near, ring && speed > 0.25f);
        }
    }

    private Fish.Fish? FishOn(int angler)
    {
        for (int f = 0; f < _fish.Count; f++)
            if (_fish[f].Holding && _fish[f].LureAngler == angler) return _fish[f];
        return null;
    }

    private struct SnagState { public bool On; public Vec2 At; public float HoldN; }
    private SnagState[] _snags = Array.Empty<SnagState>();

    /// <summary>Whether angler <paramref name="i"/>'s hook is caught on cover.</summary>
    public bool IsSnagged(int i) => i < _snags.Length && _snags[i].On;

    /// <summary>
    /// Hooks catch on cover they move through (more readily the faster they go), and come free when the line pulls
    /// harder than the snag holds. Pulling harder than the line can take breaks it instead (Failures runs next).
    /// </summary>
    private void Snags(float dt)
    {
        for (int i = 0; i < _anglers.Length; i++)
        {
            var a = _anglers[i];
            var l = a.Rig.Line;
            ref var s = ref _snags[i];
            if (s.On)
            {
                // Reeled back to the rod or reset after a break: nothing left to be snagged.
                if ((l.TerminalPos - s.At).Length > 0.5f || l.LineOut <= Config.RetieMaxLineOut) { s.On = false; continue; }
                if (l.MaxTension > s.HoldN)
                {
                    s.On = false;
                    Events.Push(new SimEvent { Type = SimEventType.SnagFreed, Tick = Tick, Angler = i, Position = s.At, Value = l.MaxTension });
                }
                continue;
            }
            if (FishOn(i) != null || l.Pinned || l.LineOut <= Config.RetieMaxLineOut) continue;
            var p = l.TerminalPos;
            int t = l.TerminalIndex;
            float speed = MathF.Sqrt(l.VX[t] * l.VX[t] + l.VY[t] * l.VY[t]);
            foreach (var z in Spot.Structures)
            {
                if (z.Snag <= 0f || !z.Contains(p)) continue;
                if (!Rng.Chance(z.Snag * MathF.Min(1f, speed / 0.3f) * 0.8f * dt)) continue;
                s.On = true;
                s.At = p;
                s.HoldN = Rng.Range(z.HoldMinN, z.HoldMaxN);
                Events.Push(new SimEvent { Type = SimEventType.Snagged, Tick = Tick, Angler = i, Position = p, Value = s.HoldN });
                break;
            }
        }
    }

    /// <summary>Line breaks above its breaking force (× knot), the rod breaks above its moment limit (docs/04 §4.3).</summary>
    private void Failures()
    {
        for (int i = 0; i < _anglers.Length; i++)
        {
            var a = _anglers[i];
            var l = a.Rig.Line;
            float limit = l.Spec.BreakingForceN * l.Spec.KnotFactor;
            bool lineBroke = l.MaxTension > limit;
            bool rodBroke = a.Rig.Rod.PeakLoadRatio > 1f;
            if (!lineBroke && !rodBroke) continue;
            var fish = FishOn(i);
            fish?.Lose();
            Events.Push(new SimEvent
            {
                Type = rodBroke ? SimEventType.RodBroke : SimEventType.LineBroke, Tick = Tick, Angler = i,
                Id = fish?.Id ?? -1, Value = rodBroke ? a.Rig.Rod.PeakLoadRatio : l.MaxTension, Position = l.TerminalPos,
            });
            // The line parts near the lure; the rest comes back. (A broken rod is swapped for a spare for now.)
            a.RetrieveToTip(Config, lost: lineBroke ? 1f : 0f);
            if (rodBroke) a.Rig.Reset(a.Grip, a.Hands.Angle, MathF.Max(Config.MinLineOut, 0.8f));
        }
    }

    /// <summary>
    /// The land button: net, lip or swing in a hooked fish that is close to the rod tip, near the surface and tired
    /// (or small enough to swing). Logs the catch and releases the fish.
    /// </summary>
    private void Landing()
    {
        for (int i = 0; i < _anglers.Length; i++)
        {
            var a = _anglers[i];
            int pending = PendingCatch(i);
            if (pending >= 0)
            {
                Decide(i, pending, a);
                continue;
            }
            if (!a.LandPressed) continue;
            var fish = FishOn(i);
            if (fish == null || fish.State != FishState.Hooked) continue;
            // Net or hand from where the angler stands: within reach of the bank and up near the surface.
            float surface = Water.Height(fish.HeadX, Time);
            bool close = MathF.Abs(fish.HeadX - a.Position.X) < Config.LandReach && fish.HeadY > surface - 0.6f;
            bool ready = fish.StaminaA < Config.LandStamina || fish.MassKg < Config.SwingInKg;
            if (!close || !ready)
            {
                Events.Push(new SimEvent { Type = SimEventType.LandFailed, Tick = Tick, Angler = i, Id = fish.Id, Position = fish.Mouth });
                continue;
            }
            if (Catches.Count < Catches.Capacity)
                Catches.Add(new Catch
                {
                    Angler = i, Species = fish.Species, LengthCm = fish.LengthM * 100f, WeightKg = fish.MassKg,
                    Hour = Hour, Terminal = a.Rig.Line.Terminal.Id, Tick = Tick, FishId = fish.Id, Pending = true,
                });
            Events.Push(new SimEvent { Type = SimEventType.FishLanded, Tick = Tick, Angler = i, Id = fish.Id, Value = fish.MassKg, Position = fish.Mouth });
            fish.TakeInHand();
            a.RetrieveToTip(Config, 0f);
        }
    }

    /// <summary>Index into <see cref="Catches"/> of the fish this angler is holding, or −1.</summary>
    public int PendingCatch(int angler)
    {
        for (int k = Catches.Count - 1; k >= 0; k--)
            if (Catches[k].Angler == angler && Catches[k].Pending) return k;
        return -1;
    }

    /// <summary>
    /// Why the rules forbid keeping this fish now: 0 = it may be kept, 1 = under the minimum length, 2 = the day's bag
    /// limit for the species is reached, 3 = must be released (protected, or a bag limit of zero). Real-style rules from
    /// the species file, simplified (docs/OPEN_QUESTIONS: regulations real-style, frozen).
    /// </summary>
    public int KeepRule(int angler, Catch c)
    {
        var reg = c.Species.Regulations;
        if (reg.BagLimit is 0) return 3;
        if (reg.MinLengthCm is float min && c.LengthCm < min) return 1;
        if (reg.BagLimit is int bag)
        {
            int kept = 0;
            foreach (var k in Catches) if (k.Angler == angler && k.Kept && k.Species == c.Species) kept++;
            if (kept >= bag) return 2;
        }
        return 0;
    }

    private void Decide(int angler, int index, AnglerState a)
    {
        var c = Catches[index];
        Fish.Fish? fish = null;
        foreach (var f in _fish) if (f.Id == c.FishId) { fish = f; break; }
        if (a.KeepPressed)
        {
            int rule = KeepRule(angler, c);
            if (rule != 0)
            {
                Events.Push(new SimEvent { Type = SimEventType.KeepRefused, Tick = Tick, Angler = angler, Id = c.FishId, Value = rule });
                return;
            }
            c.Pending = false;
            c.Kept = true;
            Catches[index] = c;
            fish?.Keep();
            Events.Push(new SimEvent { Type = SimEventType.FishKept, Tick = Tick, Angler = angler, Id = c.FishId, Value = c.WeightKg });
        }
        else if (a.LandPressed)
        {
            c.Pending = false;
            Catches[index] = c;
            if (fish != null)
            {
                float x = Math.Clamp(a.Position.X + 3f, Stock.RangeX[0], Stock.RangeX[1]);
                fish.ReleaseAfterCatch(new Vec2(x, MathF.Max(Spot.GroundHeight(x) + 0.3f, Water.Height(x, Time) - 0.8f)));
            }
            Events.Push(new SimEvent { Type = SimEventType.FishReleased, Tick = Tick, Angler = angler, Id = c.FishId, Value = c.WeightKg });
        }
    }

    /// <summary>The line pulls on the mouth of whatever fish holds the lure (tension along the last segment).</summary>
    private void LineForcesOnFish()
    {
        for (int f = 0; f < _fish.Count; f++)
        {
            var fish = _fish[f];
            if (!fish.Holding || fish.LureAngler < 0 || fish.LureAngler >= _anglers.Length) { fish.LineForce = Vec2.Zero; continue; }
            var l = _anglers[fish.LureAngler].Rig.Line;
            int t = l.TerminalIndex;
            Vec2 toward = t > 0 ? new Vec2(l.X[t - 1] - l.X[t], l.Y[t - 1] - l.Y[t]) : Vec2.Zero;
            float len = toward.Length;
            fish.LineForce = len > 1e-6f ? toward * (l.Tension[t] / len) : Vec2.Zero;
        }
    }

    private void GuardFinite()
    {
        if (!Water.IsFinite())
        {
            Water.ResetDynamic();
            FaultCount++;
            Events.Push(new SimEvent { Type = SimEventType.SimFault, Tick = Tick, Angler = -1, Id = 1 });
        }
        for (int i = 0; i < _anglers.Length; i++)
        {
            var a = _anglers[i];
            if (a.IsFinite) continue;
            // Repair: reset the broken angler's hands and rig. Later systems may restore a last good snapshot instead.
            a.Hands.Angle = a.Hands.PrevAngle = 0.6f;
            a.Hands.AngularVelocity = 0f;
            a.Hands.Reach = a.Hands.PrevReach = 0f;
            a.Rig.Reset(a.Grip, a.Hands.Angle, MathF.Max(Config.MinLineOut, 1.2f));
            FaultCount++;
            Events.Push(new SimEvent { Type = SimEventType.SimFault, Tick = Tick, Angler = i });
        }
    }

    /// <summary>Hash of the full sim state, for determinism tests.</summary>
    public ulong ComputeHash()
    {
        var h = new StateHasher();
        h.Add(Tick);
        for (int i = 0; i < _anglers.Length; i++) _anglers[i].Hash(ref h);
        for (int f = 0; f < _fish.Count; f++) _fish[f].Hash(ref h);
        Water.Hash(ref h);
        return h.Value;
    }
}
