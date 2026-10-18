# 02: Tech Stack, Engine and Architecture

## 1. Decision summary

| Area | Choice | Why |
|---|---|---|
| Engine | **Godot 4.x, .NET build** (latest stable, ≥ 4.5) | Mature 2D renderer with custom shaders, 2D lights, particles, a full UI toolkit, input mapping with first-class gamepads and rumble, audio buses, and one-click Windows export. Fully text-based project files, so an AI can edit everything without the editor GUI. MIT licence, no royalties. |
| Language | **C# (.NET 8+; this machine has the .NET 9 SDK)** everywhere | The sim needs tight numeric loops (rope + rod XPBD with substeps, a heightfield, dozens of fish spines) that GDScript is too slow for. C# is fast, strongly typed, and the sim can be a plain library tested with `dotnet test`. One language for the whole project. No GDScript. |
| Sim | **Own library `TheLedger.Sim`** (pure C#, no Godot) | The physics is the product. Godot's rigid-body physics is not used for rod, line, water, lures or fish. The sim is deterministic, fixed-step and unit-tested, the same split that worked well in the owner's other project. |
| Tests | **xUnit** (`tests/TheLedger.Sim.Tests`) + input-recording replays + a headless smoke run | Fast, headless, CI-friendly. |
| Content | **JSON** in `game/data/`, schemas described in these docs, validated by tests | Diff-friendly, AI-friendly, moddable. |
| Art | **Procedural low-poly 2D** generated from data at load time + shaders | No artist needed. Fish, terrain, plants, boats, sky and props are generated from parameters. See `07_ART_DIRECTION.md`. |
| Audio | Godot audio buses + **CC0 recordings** + **procedural synthesis** for the reel, drag and line | Tactile sounds must respond continuously to sim values (spool speed, slip speed, tension). |
| CI | **GitHub Actions** on `windows-latest`: build, test, smoke, export, upload artifact | The owner gets a playable zip from every push. |
| Tools | **Python 3.11** (Pillow, numpy) | Screenshot conversion and diffs, data reports, species data import helpers. |

### Why not the alternatives

- **Custom C++/SDL3/OpenGL** (like Crisp): maximum control, but this game needs a lot of UI (shops, Ledger, map,
  dialogue, customization, settings), audio, gamepads and save systems. Rebuilding them costs months. The sim, the
  part that needs control, is already ours in C#.
- **Unity**: fine technically, but heavier, its licensing has been unstable, and its project files are much less
  pleasant for AI-only development.
- **Godot with GDScript**: too slow for the sim. With GDExtension C++ it would be fast but would add a second toolchain
  and slow iteration. C# hits the speed target with one toolchain.
- **3D low-poly rendered side-on (2.5D)**: a valid alternative look (OPEN_QUESTIONS Q2). The plan keeps the sim 2D
  either way. If the owner picks 2.5D, only the rendering layer changes.

## 2. Versions and installation

- **Godot:** download the latest **stable Godot 4.x .NET** build for Windows x64 from the official site
  (godotengine.org/download, ".NET" variant) into `C:\Users\matth\Desktop\Apps\Godot_<ver>_mono_win64\`. Record the
  exact version and executable path here and in `CLAUDE.md` §5 when installed. Also install the matching **export
  templates (.NET)** for that version.
- **Pin it.** Don't upgrade Godot in the middle of a milestone. An upgrade is its own task: read the release notes,
  upgrade, run every test and screenshot, then commit.
- **.NET:** use the SDK version Godot's docs require for the pinned version (Godot 4.4+ targets .NET 8; the
  installed 9.0 SDK builds `net8.0` projects). Set `<TargetFramework>` the same in `sim/`, `tests/` and `game/`.
- **Renderer:** start with the **Mobile** renderer (Vulkan, supports 2D HDR and glow, lighter than Forward+). In M8,
  benchmark Mobile against **Compatibility** (OpenGL 3.3) on the owner's iGPU, and keep whichever holds 60 FPS with
  the effects we need. Record the decision here.

Pinned versions (fill in during M0):

```
Godot:            4.7.2 stable mono (released 2026-08-18)
                  C:\Users\matth\Desktop\Apps\Godot_v4.7.2-stable_mono_win64\
.NET target:      net8.0 (Godot 4.7.2's target)   SDK used: 9.0.305, runtime 8.0.8
xUnit:            2.9.2 (runner 2.8.2, Microsoft.NET.Test.Sdk 17.12.0)
Renderer:         mobile for now (Vulkan on the owner's Radeon iGPU); M8 decides mobile vs gl_compatibility
```

## 3. Architecture

```
          ┌──────────────── game/ (Godot, C#) ─────────────────────────┐
 Input ──►│ InputRouter (InputMap: mouse, keyboard, gamepad)           │
          │   └─► InputFrame (per sim tick, plain struct) ──────┐      │
          │                                                     ▼      │
          │                         ┌──────── sim/ (pure C#) ─────────┐│
          │                         │ World.Step(InputFrame) @120 Hz  ││
          │                         │  Water, Current, Wind           ││
          │                         │  Angler (hands), Rod, Line,     ││
          │                         │  Reel/Spool, Terminal rig       ││
          │                         │  Boat, Fish agents, Ecology     ││
          │                         │  Events → SimEvent queue        ││
          │                         └──────────────┬──────────────────┘│
          │ Renderers read state (interpolated) ◄──┘                   │
          │  WaterRenderer, RodRenderer, LineRenderer, FishRenderer,   │
          │  WorldBuilder (low-poly), Sky/Weather, Particles, Camera   │
          │ Audio reads state + SimEvents (drag, clicks, splashes)     │
          │ UI reads state + SimEvents (HUD, Ledger, shop, dialogue)   │
          │ Meta (save, economy, quests) ⇄ sim via commands            │
          └────────────────────────────────────────────────────────────┘
```

### 3.1 The sim boundary

- `TheLedger.Sim` exposes a `SimWorld` with `Step(ReadOnlySpan<InputFrame> inputs)` (one InputFrame per angler, see §10) and
  read-only state accessors
  (`World.Rod.Nodes`, `World.Line.Nodes`, `World.Water.Heights`, `World.Fish[i].Spine`, etc.).
- **InputFrame** is the only way input reaches the sim: rod target (angle, reach), rod angular velocity hint (for
  controller flicks), line-hold pressure (0..1), crank rate (turns/s, signed), drag delta, bail toggle, hookset is not a
  button (it's rod motion), net/land action, boat throttle/steer/oars, and so on. It's recorded and replayed
  bit-for-bit.
- **SimEvents** (a preallocated ring buffer) report discrete things to the game layer: `LureEntered{energy}`,
  `LureSkipped`, `Backlash`, `Snagged`, `Bite{species, style}`, `HookSet{quality}`, `HookPulled`, `LineBroke{where}`,
  `RodBroke`, `FishJumped`, `FishLanded`, `DragSlip{speed}` (also continuous state), `TrapChecked`, and so on.
  Audio, UI, camera shake and haptics subscribe.
- Meta systems (money, inventory, quests) never live in the sim. They send **commands** to it (spawn this spot with
  this population, equip this rig) and read **results** (a `CatchRecord` with species, length, weight, hook position,
  condition).

### 3.2 Time

- Godot `physics/common/physics_ticks_per_second = 120`. `SimDriver` (a Godot node) calls `World.Step` from
  `_PhysicsProcess` with a constant `dt = 1/120`. Inside, the rod/line/lure solver uses **substeps** (default 8,
  tunable) per the "small steps" XPBD approach (see `04_PHYSICS_SPEC.md`).
- Rendering uses `Engine.GetPhysicsInterpolationFraction()` to interpolate between the previous and current sim
  snapshots. The sim keeps two snapshot buffers for that.
- **World clock:** sim time drives the game clock (time of day, tides, moon, season). Default: 1 game day = 24 real
  minutes (tunable 12-60). Sleeping/fast-forward advances the ecology in large abstract steps without running the
  physics.

### 3.3 Coordinate system and units

- Sim: **x right, y up, metres**, y = 0 is still-water level at mean tide for the spot. Mass in kg, force in N.
- Godot 2D is y-down in pixels. Convert only in renderers: `screen = (x * PPM, -y * PPM)`, where `PPM` (pixels per
  metre) is a camera property that zooms.
- Angles: radians, counter-clockwise positive, 0 = +x.

### 3.4 Determinism

- Fixed dt, no frame time, fixed iteration order, our own seeded RNG (`SimRng`, xoshiro or PCG) per subsystem, no
  `System.Random`, no `Dictionary` iteration in gameplay-ordering code, no multithreading inside `Step` until
  profiling demands it (and then only deterministically partitioned work).
- Float results may differ across CPUs and runtimes. Determinism is required **on the same build and machine** (for
  replays, tests and bug repros), not across machines.
- Test: run a 60 s replay twice, hash the state each second, and assert the hashes match.

### 3.5 Levels of detail (LOD) for fish

- **Abstract population** (`Ecology`): per spot, per habitat cell, counts and size distributions per species, updated
  in coarse steps (every game-minute). Handles feeding cycles, spawning migration, depletion and recovery.
- **Agents**: fish that are near the lure, the camera or the player become full agents (spine body, senses, AI).
  Budget: up to 24 full agents (6 near the lure with full senses), the rest "ambient" (simplified steering, cheaper
  spine). Agents return to the population when far away.

## 4. Repo layout (detailed)

```
sim/
  TheLedger.Sim.csproj
  Core/        SimConfig, SimRng, Vec2 (own struct, readonly, inline math), Units, RingBuffer, SimEvents
  Water/       WaterSurface (1D heightfield), AmbientWaves, Current (flow field), Splash particles, Tides
  Tackle/      Rod (XPBD chain), Line (variable-length rope), Spool/Reel, Drag, Terminal (hooks, sinkers,
               floats, swivels, leaders, knots), Lure (hydrodynamic body), Bait
  Angler/      Hands (rod butt kinematic driver), BodyIK hints, cast state, net
  Boat/        Hull buoyancy, motors, oars, push pole, anchor, drift
  Fish/        FishBody (spine), Locomotion modes, Senses, Brain (utility AI), FightModel, HookHold
  Ecology/     Species registry (loaded data), Populations, Habitat cells, Activity curves, Weather inputs
  World/       World, Spot (bathymetry, structure, snags), Step orchestration, Snapshot, Replay
tests/
  TheLedger.Sim.Tests.csproj
  Rod/, Line/, Reel/, Water/, Lure/, Fish/, Ecology/, Data/ (schema validation of game/data), Replays/
  replays/     *.rec input recordings used by tests (binary, small)
game/
  project.godot, TheLedger.csproj (references ../sim)
  src/
    Boot/      Main, CommandLine (harness args), Settings, Version
    Input/     InputRouter, mouse/gamepad mapping, rumble
    Sim/       SimDriver, snapshot interpolation, debug overlay
    Render/    WaterRenderer, RodRenderer, LineRenderer, FishRenderer + FishMeshBuilder, LowPoly/ (builders),
               Sky, Weather, Camera, Post
    Audio/     ReelSynth, DragSynth, LineHum, Splash, Ambience, Music
    UI/        HUD, menus, Ledger, shop, map, dialogue, settings, tuning panel (F1), console (`)
    Meta/      SaveGame, Economy, Inventory, Quests, Licences, Properties, Reputation, Regulations
  scenes/      Main.tscn, Spot.tscn, UI scenes
  shaders/     water_surface, water_volume, sky, fish_skin, lowpoly, godrays, caustics, post
  data/        species/, gear/{rods,reels,lines,hooks,terminal}/, lures/, baits/, boats/, regions/, spots/,
               properties/, quests/, dialogue/, events/, tuning/, loc/en.json
  assets/      audio/{sfx,ambience,music}/, fonts/, CREDITS.md
tools/
  shot.py      PNG crop/compare helpers;  data_report.py  species coverage tables;  import_fishbase.py (optional)
```

## 5. Testing strategy

1. **Unit tests on physics** (`tests/Rod`, `Line`, ...). Examples (the spec lists more):
   - A rod clamped at the butt with a static weight at the tip settles to a deflection curve; the tip angle under a
     reference load falls in the action's band (fast action bends mostly in the top third).
   - Line stretch at 50% of breaking load matches the material's stretch spec within 10%.
   - A 10 g lure cast with a reference overhead input on a medium spinning setup lands 30-50 m out (printed).
   - The water surface returns to flat after a splash with total energy decaying monotonically; no NaNs over
     10 minutes of rain.
   - The drag slips at the set force ±5%; the line breaks at rated strength × knot factor × wear.
2. **Technique replays** (`tests/Replays`): recorded inputs that must produce an outcome: a skip cast skips ≥ 2 times
   and travels ≥ 3 m after first contact; walk-the-dog makes the lure alternate direction ≥ 6 times in 5 s; a
   baitcaster cast with no thumb and brakes at 0 backlashes; with correct thumbing it doesn't.
3. **Behaviour tests** (`tests/Fish`, `Ecology`): a walleye's strike odds at dusk on a rocky drop-off beat noon on a
   sand flat by a factor ≥ 3; no species ever spawns outside its habitat, depth, temperature or salinity range; a
   bass refuses a lure retrieved at 3 m/s; a fish with 0 hunger and high spook never strikes.
4. **Data validation** (`tests/Data`): every JSON file parses, matches its schema, references existing IDs, every
   species has sources, length-weight coefficients give sane weights (record-class length gives a weight within
   ±35% of the known record).
5. **Determinism test** (§3.4).
6. **Headless smoke** (`--smoke 30`): loads every spot in turn for a few seconds with a scripted rig; fails on any
   exception, NaN in sim state, or a sim tick over budget.
7. **Screenshots** for visual work: looked at, not diffed automatically (except a tiny "renders non-black" check).

## 6. CI (GitHub Actions)

`.github/workflows/ci.yml`, on push to `development` and on PRs:

1. `windows-latest`; checkout.
2. `actions/setup-dotnet` with the pinned SDK.
3. `dotnet test tests/TheLedger.Sim.Tests.csproj -c Release` (fail the job on any failure).
4. Download the pinned **Godot .NET** editor and **export templates** (cache them by version). A maintained action
   such as `chickensoft-games/setup-godot` can do this; verify it supports the pinned version before relying on it,
   otherwise download from the official GitHub release URLs.
5. `dotnet build game/TheLedger.csproj -c Release -warnaserror`.
6. Headless smoke: `godot --headless --path game -- --smoke 20`.
7. Export: `godot --headless --path game --export-release "Windows Desktop" ../build/TheLedger.exe` (an
   `export_presets.cfg` with a "Windows Desktop" preset is committed).
8. Upload `build/` as artifact `TheLedger-windows-x64`.

## 7. Performance budget (owner's laptop: Ryzen 7 4800H, integrated Radeon)

| Budget item | Target at 1080p default preset |
|---|---|
| Frame | 16.6 ms (60 FPS), with a 120/144 FPS option for stronger PCs |
| Sim per frame | ≤ 4 ms average (≈ 2 ticks × ≤ 2 ms each) |
| Draw calls | ≤ 250 typical |
| Full-screen passes | ≤ 4 (water volume, god rays, glow, final grade); each toggleable |
| Particles | GPU particles; ≤ 4 k live at default |
| Fish agents | ≤ 24 (6 with full senses) |
| Line nodes | ≤ 160 (adaptive) |
| Water columns | 512-1024 in the active window |
| Memory | < 1.5 GB |

Rules:
- No heap allocation inside `World.Step` (check with a test that runs 1000 ticks and compares
  `GC.GetAllocatedBytesForCurrentThread()` before and after; allow 0 bytes).
- Struct-of-arrays for hot data (positions/prev positions/inverse masses as `float[]`).
- Fish meshes are built once per species variant at load and skinned in the vertex shader (no per-frame mesh
  rebuild). Uniform arrays carry spine data.
- Quality presets: Low / Medium (default) / High / Ultra. Each effect lists its preset in `07_ART_DIRECTION.md`.
- `--bench 20` writes `bench.json` (avg, 1% low, ms per system: sim, water render, fish render, post).

## 8. Save system

- JSON save in `user://saves/slot_N.json`, with `saveVersion` and migration functions per version bump.
- Autosave on sleeping, on leaving a spot, and every 5 game-minutes while not fighting a fish.
- Saves hold meta state only (money, inventory, Ledger, quests, properties, world restoration flags, clock,
  weather seed). Sim state is rebuilt from the spot + seed on load.

## 9. Localization and settings

- All UI strings come from `data/loc/en.json` by key from day one (no hard-coded English in code).
- Settings (`user://settings.json`): units, assists, Purist mode, input bindings, mouse sensitivity and mode,
  controller vibration strength, audio buses, quality preset, individual effect toggles, FPS cap, window mode,
  day length, colour-blind safe line/HUD colours.

## 10. Co-op readiness (owner decision: plan for 2-player co-op later)

Co-op itself is milestone M18, but these rules apply **from M0**, because retrofitting them is very expensive:

1. **The sim supports N anglers.** `World` holds a list of `Angler`s, each with its own hands, rod, line, reel,
   terminal rig and hooked fish. Nothing in the sim assumes a single player (no static "the player", no global rod).
2. **Input is per angler.** `World.Step` takes one `InputFrame` per angler; recordings store all of them.
3. **No player singletons in the game layer.** Camera, HUD, audio listener and input routing take a player index.
   Local split-screen is not a goal, but nothing should prevent it.
4. **World state and player state are separate** in saves: the world (spots, populations, restoration, clock, weather)
   versus each player (inventory, money, Ledger progress, licences, quests).
5. **Ownership is explicit.** Every hooked fish, boat and trap has an owner (angler index). This becomes network
   authority later.
6. **Planned network model (for M18, not before):** the host owns the world (fish population, water, weather, clock);
   each client simulates **its own tackle locally** for zero-latency feel and streams it; when a client hooks a fish,
   **authority over that fish passes to that client** for the fight, then returns. Exact floating-point determinism
   across machines is not assumed. Transport (Steam networking or Godot's ENet) is chosen in M18.
7. **Tests:** from M2 on, at least one sim test runs two anglers in one world.

