# 04: Physics Specification

This is the non-negotiable heart of the game. **The rod, line, reel, terminal tackle, water surface and fish bodies
are simulated.** This spec says how. Starting values are given so tests can be written immediately; they are tuning
starting points and belong in `SimConfig` / tuning JSON (CLAUDE.md rule 11), not constants in code.

Reference reading (worth an hour before M2):
- Macklin, Müller, Chentanez, *XPBD: Position-Based Simulation of Compliant Constrained Dynamics* (2016).
- Macklin et al., *Small Steps in Physics Simulation* (2019): many substeps with one iteration beat many iterations.
- Müller, *Fast Water Simulation for Games Using Height Fields* (GDC 2008).
- Clanet, Hersen, Bocquet, *Secrets of successful stone-skipping*, Nature 427 (2004): best entry angle ≈ 20°.
- Lighthill's elongated-body theory and resistive-force theory (fish swimming); Strouhal number for swimming animals
  (efficient swimmers cruise at St ≈ 0.2-0.4; Taylor, Nudds, Thomas, Nature 2003).

## 1. The 2D world and its "lateral" pseudo-axis

The sim is a 2D vertical plane (x along the view, y up). Real fishing is 3D, so a few things need a **lateral
pseudo-axis** that is simulated as a small separate state but not as full 3D motion:

- **Lure yaw** (`lateralAngle`, `lateralRate`): walk-the-dog zig-zags, crankbait wobble, jerkbait darts. It's a damped
  1-DOF oscillator driven by line jerks on slack line and by the lip at speed. It changes the lure's rendered facing,
  its V-wake on the surface, the vibration it emits, and how fish perceive it. It does not move the lure in the plane.
- **Fish facing** (`yaw` in [-π, π]): fish turn toward or away from the camera when they reverse direction; rendered
  as a pseudo-3D turn (`07` §5.4).
- **Side pressure** in 2D: the player controls the **angle of the pull** on the fish through rod angle and position.
  A pull that has a component across the fish's body axis turns its head. Rod high = lift (pull a grouper off the
  reef), rod low = horizontal pull (steer a running fish out of timber). Moving along the bank or boat changes the
  line angle too.

Everything else (rod, line, water surface, fish spines, boats) is genuinely simulated in 2D.

## 2. Solver: XPBD with small steps

One solver for rod, line, terminal tackle and lure (they are physically connected, so they must be solved together).
Fish spines and boats use their own steps but couple to it through the line's end constraint and the angler's
deck.

**Per sim tick** (dt = 1/120 s), with `n = SimConfig.TackleSubsteps` (default 8, h = dt/n):

```
for each substep:
    1. apply external forces to velocities: gravity, buoyancy, drag (implicit, §3.4), wind, current
    2. predict: p_prev = p; p += v*h
    3. drive kinematic nodes (rod butt/handle from the Hands model, §4.1)
    4. reset λ for all constraints
    5. solve constraints once, in fixed order:
         rod stretch → rod bend → guide line-path forces → line segments (tip to terminal) → terminal joints
         → lure orientation → contacts (ground, structure, surface) → attachments (snag pins, hook in fish)
    6. v = (p - p_prev)/h
    7. velocity-level: friction at contacts, material damping, clamp |v| to a sanity max, NaN guard
then once per tick:
    reel/spool/drag step (§5), water step (§7), fish step (§9), events
```

XPBD update for constraint C with gradient ∇C, compliance α: `α̃ = α / h²`,
`Δλ = (-C - α̃·λ) / (Σ w_i |∇C_i|² + α̃)`, `Δp_i = w_i ∇C_i Δλ`. The constraint force is `λ / h²` along ∇C: **this is
how tension is measured** for the drag, line breaking, the rod and the haptics. Average it over the substeps of a
tick for a stable reading.

Stability rules:
- Unilateral constraints (line segments, contacts) apply only when violated.
- Use continuous collision for fast small bodies (lure vs surface, lure vs thin structure): test the swept segment
  `p_prev → p` each substep.
- Clamp any node speed to 120 m/s (a cast lure peaks around 25-40 m/s; a fly line loop tip can exceed that briefly).
- Every tick: if any position is NaN/Inf, restore the last good snapshot, log a `SimFault` event, and count it.
  Tests assert zero faults.

## 3. The line

### 3.1 Materials (data: `game/data/gear/lines/*.json`)

| Property | Monofilament (nylon) | Fluorocarbon | Braid (PE) | Fly line (PVC/coated) | Wire / leader |
|---|---|---|---|---|---|
| Density kg/m³ | 1140 (near-neutral, slow sink) | 1780 (sinks) | 970 (floats) | per line: floating ≈ 850-950 effective, sinking up to ≈ 1500+ | steel 7900 |
| Stretch at break | 15-25% | 8-15% | 2-5% | ~low (core) | ~1-2% |
| Abrasion resistance | medium | high | low (cuts on rocks) | medium | very high (bite-proof) |
| Visibility to fish | medium | low | high | high | high |
| Knot strength factor | 0.85-0.95 | 0.80-0.90 | 0.65-0.90 (knot-dependent) | n/a (loop-to-loop) | crimp 0.95 |
| Memory (coil set) | medium | high | none | low | high |

Line data per product: rated strength (lb as sold), diameter (mm), material, colour/visibility, price. **Fly lines**
are rated by AFTMA weight: the first 30 ft (9.14 m) weighs a standard number of grains (for example 5-weight = 140
grains ≈ 9.1 g), so a 5-weight line is about 1 g/m. Use the real AFTMA table.

Derived values: `massPerLength = density × π(d/2)²` (or the fly line's grain weight), axial stiffness
`EA = F_break / ε_break` (linear to start; optionally stiffen the last 30% for mono). Leaders and tippets are
separate line sections with their own material, joined by a knot (strength factor) or swivel.

### 3.2 Rope model

- Nodes from the **tip guide** (node 0) to the **terminal** (last node). Node 0 is constrained to the rod's tip guide
  position but the line slides through it: pay-out and retrieve change the **first segment's rest length**
  `r0`.
  - `r0 > 1.5·segLen` → insert a node at the tip end; `r0 < 0.5·segLen` → merge the first two segments.
  - `segLen` adapts so that `nodeCount ≤ SimConfig.MaxLineNodes` (default 160): 0.05 m near the terminal (first 2 m,
    detail for lure motion), growing toward the rod for long lines. Re-sample gradually, never in one tick.
- Segment constraint: **max-distance (unilateral)**, `C = |p_i − p_j| − r_ij` applied only when `C > 0`, compliance
  `α = r_ij / EA`. Slack line is free to sag, bow and pile.
- Bending: none for braid and mono (tiny value for numerical calm); **fly lines need real bending stiffness** for
  loops to form and unroll (start: `EI ≈ 1e-4 N·m²` for a 5-weight, scaled by weight class; tune with the loop test).
- Masses: `massPerLength × segment length`, lumped at nodes.

### 3.3 Line through the rod (guides)

Between the reel and the tip, the line runs through the guides. Represent it as the polyline
`reel → guide_1 → … → guide_k → tip`. With frictionless guides the tension T is the same along it, so each guide
gets the force `T·(d_out − d_in)` (unit directions of the line out of and into that guide). Apply these to the rod
nodes the guides sit on. This bends the rod in its real, smooth curve under load instead of a tip-only kink.
Guide friction (capstan, `T_out = T_in·e^{μθ}`, μ by guide quality) applies to **paying out** line during casts (cast
distance) and gives a small loss under load.

### 3.4 Forces on line nodes

- Gravity; buoyancy when submerged (`ρ_water × volume`; fresh 1000, brackish 1010, salt 1025 kg/m³).
- **Drag** on each segment from velocity relative to the medium (air + wind, or water + current), split into normal
  and tangential components:
  `F_n = −½ ρ C_n d ℓ |v_n| v_n` (C_n ≈ 1.2, cylinder) and `F_t = −½ ρ C_t π d ℓ |v_t| v_t` (C_t ≈ 0.01).
  Apply **implicitly** per node (`v_n ← v_n / (1 + k_n h |v_n|)`) so long lines in fast current stay stable. Water
  drag is what makes a line belly in current and what slows a sinking lure line; air drag and wind give the casting
  bow.
- **Surface film:** nodes of floating lines (fly lines, braid) that reach the surface sit on it (a surface contact
  with extra drag): this anchors roll casts, creates mending behaviour and drag on the fly.

### 3.5 Contacts, wear and snags

- Collision against the spot's **terrain polyline** (banks, bottom) and **structure shapes** (capsules, circles,
  convex polygons: logs, rocks, pilings, dock posts, boat hull, coral heads, branches above water), with Coulomb
  friction.
- **Wear:** each line segment accumulates `wear += abrasiveness(surface) × N × |slip| / abrasionResistance(material)`;
  teeth contact (a fish's mouth node vs line segments near the terminal) adds wear by the species' `teethSharpness`.
  Effective strength = `rated × knotFactor(at knots) × (1 − wear)`. The line breaks at the weakest point where
  tension exceeds it (`LineBroke{where}`).
- **Vegetation:** weeds/kelp/eelgrass are chain-simulated plants (§7.6). Line passing through them gets high drag; a
  lure passing through can **foul** (weed on the hooks, action reduced) until popped free.
- **Snags:** snag sites are defined in spot data (branch forks, rock crevices, timber, oyster shell, coral). When a
  **hook point** contacts a snag site moving into it, roll the snag chance (hook type, weedless flag, speed). A snag
  creates a pin constraint with hold strength. It frees when the pull direction changes past the site's release
  cone (walk or move the boat past it), on a sharp slack-to-taut pop (impulse above a threshold), or when the force
  exceeds the hold (the hook straightens if `hookWireStrength < force`, otherwise the line breaks).

## 4. The rod

### 4.1 The hands (kinematic driver)

- Two kinematic nodes: **butt** and **fore grip** (rod handle). Their pose (position + angle) is driven by a
  spring-damper toward the input target with limits: max angular speed (default 18 rad/s), max angular acceleration
  (the arm's torque limit, default 220 rad/s²), reach limits, and joint limits relative to the body. These limits are
  what makes a flick feel physical rather than instant. Tunable per input device (stick vs mouse).
- On a boat, the hands' base moves with the deck (boat bob reaches the rod tip).
- The arms are drawn with 2-bone IK to the handle (cosmetic).

### 4.2 Blank model

- `N` nodes (default 16; data may set 12-24) from the fore grip to the tip, with spacing finer toward the tip
  (spacing ∝ 1 − 0.5·s, s = 0 at the butt, 1 at the tip).
- Stretch constraints: near-rigid (compliance 1e-7).
- **Bending constraints** between consecutive segments: `C = θ_i − θ_rest,i` (θ_rest = 0, or a small "set" for an old
  rod), compliance `α_i = segLen_i / EI(s_i)`.
- **Stiffness profile** from the rod's **power** and **action**:
  `EI(s) = EI_tip + (EI_butt − EI_tip)·(1 − s)^a`, where power scales `EI_butt` and `EI_tip`, and action sets `a`
  (extra-fast ≈ 3.0, fast ≈ 2.2, moderate-fast ≈ 1.6, moderate ≈ 1.2, slow ≈ 0.7: a larger exponent makes
  stiffness fall away toward the tip, which concentrates the bend there; corrected in M2). Tune so tests pass:
  - fast action: ≥ 70% of the total bend angle under a reference load is in the top 30% of the blank;
  - moderate: the bend spreads over the top 50-60%;
  - slow: bends along most of the length.
  - Reference calibration: a 2.1 m (7 ft) medium-power spinning rod, butt clamped horizontal, 1 kg hung at the tip:
    the tip drops ≈ 40-60% of the rod length. Tests print the deflection for every rod in the data.
- Mass per node from the blank taper (data: total blank mass and taper); the tip is light, which makes it whip.
- Material damping: graphite low (fast recovery, crisp), fibreglass high (soft, forgiving, smooth fights), bamboo
  medium. Apply as relative-velocity damping between neighbour nodes.

### 4.3 Load, sensitivity, breakage

- The rod reports per-joint bending moment `M_i ≈ λ_bend,i / h²`. Breakage: if `M_i > M_max(s_i)` (data per rod,
  from power, material and a safety factor), the blank **breaks at that joint**. The part above the break is a
  separate chain that slides down the line to the lure (real and dramatic). `RodBroke` event; the rod becomes
  "broken" in inventory (repair cost). **High-sticking** (rod near vertical, heavy load at a short line) is the typical
  cause because the load bends the tip section past its limit.
- **Sensitivity**: the rod transmits small high-frequency tension changes (bites, a jig ticking a rock) to the
  angler. Sensitivity (graphite modulus, data) scales how strongly those reach haptics, audio ticks and the visible
  tip vibration, and is the reason expensive rods help with subtle bites (a real benefit with no skill bonus).

## 5. The reel

### 5.1 Spool and line capacity

- Line on the spool `L_spool`; spool effective radius `r(L) = sqrt(r_core² + L·d²/(w·π·k_pack))` (w = spool width,
  k_pack ≈ 0.8). **The retrieve per handle turn falls as line goes out** (real: an emptier spool retrieves less).
- Retrieve rate = `crankRate (turns/s) × gearRatio × 2π r`.
- If `L_spool` reaches 0, the line's end is tied to the spool: tension spikes and the line breaks (spooled).

### 5.2 Cranking power

- The hand applies at most `τ_hand` at the handle (default 3 N·m, tunable). Max line pull from cranking is
  `τ_hand / (gearRatio × r)`. High-speed reels retrieve fast but winch weakly; two-speed offshore reels shift to a
  low gear for big fish (real). This is why pumping with the rod is necessary on heavy fish.
- Electric reels: motor torque curve and speed setting.

### 5.3 Drag

- With the bail closed (or the spool engaged), compare reel-end tension `T` with the drag:
  - `T < F_static` → the spool holds; cranking retrieves.
  - `T ≥ F_static` → the spool slips: integrate spool angular velocity `ω` with inertia
    `I = I_spool + I_line(L_spool)` and torque `(T − F_kinetic)·r`; pay-out speed = `ω r`.
- Drag quality (data): `F_static / F_kinetic` ratio (cheap drags 1.3-1.5 = stick-slip, jerky; good drags 1.02-1.08 =
  smooth), number of detents and the max drag (e.g. light spinning 3-5 kg, offshore lever drags 15-30 kg+).
- **Lever drags** (conventional): preset strike position plus free range.
- **Drag heat/fade** (optional for big game): long slips heat the washers and lower `F_kinetic` (carbon washers fade
  less).
- Cranking while slipping does not retrieve: it adds line twist (spinning reels).
- Haptics/audio read `ω r` (slip speed) and `T`.

### 5.4 Casting behaviour by reel type

- **Spinning:** with the bail open and the finger off, line peels off the spool's lip. Pay-out resistance =
  `f_lip(fill)` (rises as the spool empties, so underfilled spools cast short) + guide friction. No inertia overrun.
  Wind knots: if line is cranked on with `T` below a threshold and there's slack in coils, increment a loop counter;
  probability of a wind knot on the next cast rises with it (light model).
- **Baitcaster:** the spool has state `ω` during the cast:
  `I dω/dt = T_line r − τ_bearing − τ_brake(ω) − τ_thumb`,
  `τ_brake = b_mag·ω` (magnetic) or `b_cent·ω²` (centrifugal), `τ_thumb = pressure × μ × N_thumbMax × r`.
  The spool pays out `ω r`; the line takes what the lure's flight demands. When `ω r` exceeds the line's demanded
  speed, the difference accumulates as **loose line on the spool** `L_loose`. If `L_loose > L_backlash` (≈ 0.4 m,
  tunable by spool design), fire `Backlash`: the spool locks, the remaining flight is stopped dead, and the cast
  ends in a tangle that costs an interaction to clear (and may require cutting a length of line).
  **Thumbing before entry** stops the spool and avoids the most common backlash (the lure decelerating at
  splash-down).
- **Spincast:** spinning model with a cover (more friction, shorter range, no knots).
- **Fly:** pay-out only by shooting line (line hold released while the line is moving) and stripping.

## 6. Terminal tackle, lures and baits

The terminal rig is a sub-chain at the end of the line: swivel, leader, sinker, float, hook, bait, lure, each a body
with mass, volume, drag and attachment rules.

### 6.1 Body types

| Body | Model |
|---|---|
| **Sinker** (split shot, egg, bank, pyramid, bell, sliding) | Point mass. A sliding sinker has a slider constraint along the line between a stop and the swivel. Pyramids/sputniks have high bottom grip (surf). |
| **Float** | Buoyant body (volume, mass, shape: stick/waggler/bobber/slip float). Slider constraint on the line up to the **bobber stop** (depth setting). Rides on the water surface patch (§7) so it bobs with waves. Its visible tip shows bites. |
| **Swivel** | Light node; prevents line twist from spinning lures. |
| **Hook** | Node with a **hook point** (direction, gap, size, wire strength, sharpness, barb, type: J, circle, treble, octopus, weedless). The hook point drives snags (§3.5) and hook holds (§9.5). |
| **Bait** | Attached to a hook with a hook-hold value (worm, cut bait, live minnow, shrimp, crab, squid, corn, boilie, dough, chicken liver, cricket...). It tears off when acceleration/force exceeds its hold (hard casts, nibbling fish). Emits **scent** (rate, decaying) and, if alive, **vibration** and movement: a live bait is a small fish agent on the hook that swims, tires and panics near predators. |
| **Lure** | A 2-node rigid body (nose, tail) with orientation, plus its lateral pseudo-state (§1). |

### 6.2 Lure hydrodynamics (data: `game/data/lures/*.json`)

Each lure has: mass, volume (float / suspend / sink rate), length, drag coefficients (axial, normal), centre of
buoyancy offset, and an **action model**:

- **Lip (crankbaits, jerkbaits):** pulled forward through water, the lip produces a downward force and a nose-down
  moment proportional to `v²` and lip area/angle. A crankbait dives until the line's upward pull, buoyancy and lip
  force balance, so its running depth **emerges**: deeper with faster retrieve (to a limit), with more line out
  (long casts dive deeper), and with **thinner line** (less line drag). All three are real and must show in tests.
  The lip also drives the lateral wobble (frequency ∝ speed / length).
- **Topwater (poppers, walkers, prop baits, frogs, buzzbaits):** buoyant; interact with the surface patch.
  Walkers: a slack-line twitch kicks the lateral oscillator to the opposite side (walk-the-dog). Poppers: a sharp
  pull displaces water (surface impulse + splash particles + "bloop" sound and vibration). Prop baits/buzzbaits:
  continuous surface spray while moving.
- **Spinners/spinnerbaits:** blade rotation rate ∝ speed (a blade "starts" above a minimum speed, real), producing
  flash and vibration; a spinnerbait's wire arm makes it nearly snag-free and helicopters on the fall.
- **Spoons:** wobble on retrieve and **flutter on the fall** (oscillating lift).
- **Jigs/soft plastics:** fall rate from mass and drag (the jig head weight is the key choice); tails vibrate ∝
  speed; Texas/weedless rigging lowers snag odds.
- **Swimbaits, glide baits, flies, metal jigs:** variants of the above.

Every lure reports **stimulus output** each tick: vibration amplitude and frequency, flash intensity, sound (rattles,
pops, props), silhouette size and contrast, speed, and depth. Fish perceive these (`05` §3).

### 6.3 Water entry and skipping

When a body crosses the surface from above:
- Entry angle θ (to the surface) and speed v. Planing factor `P` from the lure's shape (flat, light for its area:
  skirted jigs and soft plastics high, crankbaits low).
- **Skip** if `θ < θ_max(P)` (≈ 20-25° for high-P lures, per Clanet et al.) and `v > v_min` (≈ 6 m/s): reflect the
  vertical velocity with restitution e (0.2-0.5), keep the horizontal component times `(1 − loss)`, push an impulse
  into the surface patch, and spawn spray. Repeat while conditions hold. Test: the skip replay produces ≥ 2 skips and
  ≥ 3 m travel after first contact.
- Otherwise **enter**: splash energy `E = ½ m v² × shapeFactor` → splash particles, a surface impulse, a sound, and a
  **disturbance event** fish hear and may spook from (`05` §3.4). Pitching and flipping make small E; a heavy lure
  slammed in makes a big one.

## 7. Water

### 7.1 Surface: ambient waves + simulated patches

`surfaceHeight(x, t) = tideLevel(t) + ambient(x, t) + patchHeight(x, t)`.

- **Ambient** (analytic, everywhere): a sum of 4-8 wave trains from a simplified wind-sea spectrum (direction mostly
  along ±x in the 2D plane) by wind speed and the spot's fetch, plus swell for coastal/offshore spots. Use Gerstner
  displacement for crest shape (render) and expose vertical height, slope and **orbital velocity** (decaying with depth
  as `e^{ky}`) to the sim, so floats bob, lures in waves surge and chop hides float bites (real).
- **Simulated patches** (`SurfacePatch`, up to 3 active): 1D heightfield columns around the **angler/boat**, the
  **terminal tackle/lure**, and a **hooked fish**. Each 256-512 columns, spacing 0.04-0.1 m, with absorbing
  sponge boundaries that blend into the ambient field. Patches follow their target (shift by whole columns).
- Patch integration (per tick, with water substeps if CFL needs them):
  `v_i += h·(c²·(H_{i−1} + H_{i+1} − 2H_i)/dx² − k_damp·v_i)`, `H_i += h·v_i`, with `c·h/dx < 0.5`. Start with
  c ≈ 0.7 m/s and k_damp ≈ 0.6/s for ripples (tune for look).
- Inputs into patches: entry/skip impulses, popper and prop-bait pushes, floats and boat hulls by **displacement**
  (the change in submerged volume per tick pushes the columns, Müller 2008), fish breaking the surface (rises, jumps,
  tailing, boils), rain drops, the line cutting the surface at speed (a small V-wake), oar strokes, wading legs.
- **Rule:** if something touches the water in the sim, the water responds. Rise rings, nervous water from bait
  schools and a tailing bonefish are drawn from the sim, not painted on.

### 7.2 Splash particles

GPU-cheap droplets spawned from impact energy with ballistic flight. When a droplet falls back it adds a tiny
impulse to the patch (bounded count). Foam is a scalar per column (rises with slope/velocity, decays), used by the
renderer.

### 7.3 Currents (flow field)

`current(x, y, t)` per spot from data:
- **Rivers:** a surface speed profile along x (riffles fast, pools slow, tail-outs), **seams** and **eddies** (reverse
  flow behind boulders and points, where trout and salmon hold), vertical profile `u(y) = u_s·(z/d)^{1/7}` (z = height
  above bottom, d = depth; the classic 1/7 power law), and turbulence noise.
- **Tidal:** `u_tide(t)` strongest mid-tide, zero at slack, direction flips on the flood/ebb; amplified in channels
  and passes.
- **Ocean:** slow ocean currents (for example the Gulf Stream's edge off the Keys), weedline convergences,
  and Saltstraumen's huge tidal current in Norway.
Lures, baits, lines, floats, bait scent, drifting boats and fish all feel the current.

### 7.4 Tides

Semi-diurnal tide with period 12.42 h (the real M2 constituent) in game time, amplitude per spot (data, e.g. 0.6-2.5
m), with a **spring/neap** modulation tied to the moon phase (spring tides near new and full moon). The water level
moves the surface baseline: flats drain and flood, oyster bars and sandbars appear, and a boat can **run aground**.
Tide tables are shown in-game (a real angler tool).

### 7.5 Water properties per spot

`temperature(y)` (with a summer **thermocline** in deep lakes: warm epilimnion, sharp drop, cold hypolimnion; mixed in
spring and autumn "turnover"), **clarity** (visibility distance, m), **colour** (absorption per RGB channel), salinity,
dissolved oxygen (low in hot stagnant water and below the thermocline in summer), turbidity after rain. These feed
both rendering (`07` §6) and fish behaviour (`05`). **The same numbers drive both.**

### 7.6 Plants

Kelp, eelgrass, lily pads, reeds, coontail, hydrilla: chains (3-12 nodes) with buoyancy, rooted at the bottom,
pushed by current and wave orbital velocity, collidable with line/lures (drag, fouling) and fish (cover). Simulated
at low rate (30 Hz) when far from the lure. Lily pads float on the surface patch.

## 8. Boats

- 2D rigid body (x, y, angle). The hull is a polygon with 8-16 sample points along the keel and sides. Buoyancy at
  each sample from submerged depth (against `surfaceHeight`) and hull width, giving force + torque: the boat bobs and
  pitches on waves and reacts when the angler walks (trim) or a fish pulls.
- Hydrodynamic drag (hull form, planing above a speed threshold for planing hulls), wind on freeboard, motor thrust
  at the stern (with propeller wash into the patch and noise), oar strokes as impulses with a stroke cycle, push-pole
  impulses against the bottom (fails in deep water), anchor (rope sim to an anchor body with bottom grip), drift
  sock (drag body), spot-lock (late-game thrust controller).
- Running aground: hull samples vs terrain; damage above an impact energy.
- The line can contact the hull (a fish under the boat), and big fish tow the boat through the line tension at the
  angler or rod holder.

## 9. Fish bodies

### 9.1 Spine

- Chain of `N` nodes (data: 6-10 typical, 12-20 for eels and gars) from snout to tail fork, with masses from the
  body profile (most mass mid-body), distance constraints, and bending compliance per joint from the species'
  **body stiffness** profile (tuna stiff to the peduncle, eels flexible throughout).
- **Hybrid control:** the head is driven by a **thrust force and steering torque** from the brain (not set
  positions), and the body has **target curvature** from the swimming wave (below), enforced through the compliant
  bending constraints. External forces (line at the mouth, collisions, gravity out of water) therefore bend and
  move the fish physically.
- Hydrodynamics per segment: anisotropic drag (normal ≫ tangential, resistive-force style), buoyancy ≈ neutral via
  the swim bladder (fish without one, like sharks, tunas and flatfish, sink slowly when not swimming), added mass
  (approximate).

### 9.2 Swimming modes (data `locomotion.mode`)

The target curvature is a travelling wave `κ(s, t) = A(s)·sin(2π(s/λ − f t))` with amplitude envelope `A(s)` and
wavelength `λ` by mode. Frequency comes from speed: `f = St·U / A_tail` with Strouhal St ≈ 0.3 and tail amplitude
`A_tail` ≈ 0.1-0.2 body lengths.

| Mode | Envelope and wave | Examples |
|---|---|---|
| Anguilliform | Whole body, growing from the head, λ ≈ 0.6 L | Eels, burbot, lampreys |
| Subcarangiform | Rear 1/2-2/3, λ ≈ 1 L | Trout, salmon, bass, carp, pike, catfish, most freshwater |
| Carangiform | Rear 1/3, λ > 1 L | Jacks, mackerel, snapper, bluefish |
| Thunniform | Body stiff; motion at the peduncle and the lunate tail, high f | Tunas, mackerel sharks, billfish, wahoo |
| Amiiform | Body nearly straight; long dorsal fin undulates | Bowfin (gars are subcarangiform with stiff bodies) |
| Balistiform | Body rigid; dorsal and anal fins flap | Triggerfish |
| Labriform | Body rigid; pectoral fins row | Wrasses, hogfish, surfperch at low speed, parrotfish |
| Rajiform | Undulating pectoral "wings" | Rays |
| Pleuronectiform | Flatfish: body wave in the vertical plane (they lie on their side) | Flounder, halibut |
| Sculpin/benthic | Hops and rests on pectorals | Sculpins, gobies |

Most fish switch modes: labriform hovering → subcarangiform cruising → full-body burst ("C-start" escape: a fast
C-shaped bend then a powerful stroke). Implement the **C-start** for spooks and strikes: it is the most recognisable
fish motion there is.

### 9.3 Speeds and power (data `performance`)

- Cruise and burst speeds in body lengths per second (typical cruise 1-2 BL/s, burst 5-12 BL/s for short bursts;
  tunas and wahoo much faster in absolute terms). Use measured values from literature, and avoid popular myths
  (e.g. widely quoted 100+ km/h sailfish figures are disputed by measurements).
- Thrust for the fight: `F_max = powerCoeff × mass^{2/3}` (muscle cross-section scaling), times a temperature
  performance curve (ectotherm: peak at the species' optimum temperature, falling off on both sides), times stamina
  state. Starting calibration: a 2 kg largemouth's peak burst tension should land around 15-25 N; tune by feel with
  the owner and lock in tests.
- Stamina: an anaerobic pool (bursts) and an aerobic pool (sustained), recovery rates, lactate penalty for
  release survival.

### 9.4 Out of water

Landed or jumping fish: thrust off, gravity on, ground/deck contact with friction, and **flop** impulses (alternating
whole-body curvature pulses) decaying with exhaustion. Jumps: the brain picks a jump; the fish accelerates
upward through the surface (surface impulse, spray), headshakes in the air (the curvature pulses transmit through the
line if it's tight), and re-enters with a splash.

### 9.5 Mouth, suction and hook hold

- Mouth: a capsule at the head node; gape from data (sets the largest prey/lure the fish can take: a lure much
  bigger than the gape is refused or short-struck).
- **Suction feeding** (most bony fish): opening the mouth creates a brief inflow toward the mouth over ≈ 1-2 mouth
  diameters. Implement as a short velocity field pulling nearby small bodies (the lure, bait) into the mouth. The
  line feels this as the "tick" of an inhale. **Ram feeders** (pike, barracuda, tunas, billfish slashing) close on the
  prey at speed instead; **biters** (bluefish, piranha, sheepshead) nip.
- **Hook hold** when a hook point is inside the mouth capsule and the line pulls: penetration `p` grows when the force
  along the hook point exceeds a threshold set by sharpness, wire gauge and tissue hardness at that location (lip,
  jaw corner, roof, tongue, throat, foul-hooked body; data per species: e.g. tarpon bony/hard, crappie papery/soft).
  Hold strength degrades with: **slack time** (fast for barbless), **shakes and jumps on a taut line**, tissue tear
  above a force limit (soft mouths), and trebles under heavy load. Below zero, the hook pulls (`HookPulled`).
- **Circle hooks** roll into the jaw corner only when tension builds smoothly while the fish moves away; a sharp
  sweep pulls them out. Model as a rotation state that advances with smooth tension and resets on spikes.
- Hook location affects release survival (throat hooks are bad) and is recorded in the catch.

## 10. Debug overlay (`--debug-overlay`, toggle in the F1 panel)

Draw: line and rod nodes, segment tension (colour), guide forces, bending moment per rod joint vs its limit, drag
state (static/slipping, slip speed), spool ω and loose line (baitcaster), lure stimulus output, surface patches'
extents and boundaries, current vectors, fish spines, brain state text, sense ranges (vision cone, lateral line),
hook-hold value, and per-system timings. This is how the AI and the owner see what the sim is doing.

## 11. SimConfig (starting list)

`TickHz` 120, `TackleSubsteps` 8, `MaxLineNodes` 160, `MinSegLen` 0.05, hand limits, gravity 9.81, densities, drag
coefficients, wave patch params (c, damping, spacing, sponge width), skip thresholds and restitution, baitcaster
`L_backlash`, `τ_hand`, fish Strouhal number, thrust coefficient scale, hook-hold degradation rates. Every one of these
is editable live in the F1 panel and saved to `game/data/tuning/sim.json`.

## 12. Required tests (minimum, grow as you go)

| Area | Test |
|---|---|
| Rod | Static deflection bands per action; tip-frequency printout per rod; break at `M_max` under high-sticking; no break at the same load with the rod at 45° and line out |
| Line | Stretch at 50% load within 10% of spec; slack line sags (catenary-like) and never pushes; breaks at the weakest point; wear accumulates on rock contact; 0 allocations per tick |
| Reel | Retrieve per turn falls with line out; drag slips at ±5% of setting; stick-slip ratio shows jerky tension on cheap drags; spooling breaks at 0 line |
| Casting | Reference casts land in distance bands per setup (printed table); baitcaster without thumb/brakes backlashes, with them doesn't; wind reduces upwind distance |
| Water entry | Skip replay ≥ 2 skips; steep entry never skips; splash energy ordering pitch < overhead < slam |
| Lures | Crankbait depth rises with speed (to a cap), with line out, and with thinner line; spoon flutters (oscillation detected) on the fall; walk-the-dog replay alternates ≥ 6 times in 5 s |
| Water | Patch energy decays after impulse; no NaNs in 10 min of storm rain; floats bob with ambient waves; boundaries absorb (no reflection > 10%) |
| Fish | Spine follows wave with the mode's envelope; C-start completes in < 0.15 s for a 30 cm fish; landed fish flops and calms; suction pulls a 5 g lure within 1 mouth diameter |
| Hook | Circle hook holds with smooth tension, pulls with a sharp sweep; barbless loses hold faster on slack; mono at 40 m gives lower hook force than braid for the same sweep |
| Determinism | Replay twice, identical state hashes |
