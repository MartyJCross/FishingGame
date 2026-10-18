# CLAUDE.md: Project Constitution (The Ledger)

Read this file first, every session. It is the operating manual for any AI working in this repo. It covers what the
game is, the rules that must not be broken, how to build, verify and ship, where everything lives, and who the owner
is. **Keep it current.** When you learn something a future session would need (an owner preference, a trap, a tool),
write it here. When a rule here conflicts with a doc in `docs/`, this file wins; fix the doc.

> The game is called **The Ledger** (owner decision, 2026-10-18), after the field guide at the heart of the game.
> The owner's design decisions are recorded in `docs/OPEN_QUESTIONS.md` under "Answered".

---

## 1. What this project is

A **2D side-view fishing game** in the spirit of *Cat Goes Fishing*, but with **best-in-class, physically simulated
fishing**. The rod flexes, the line stretches, sags and goes slack, the reel has a real spool and drag, the water
surface is a live simulation, and every fish is a real species that behaves, feeds and fights like it does in nature.
The game is meant to be **mastered**: skip casts under docks, walk-the-dog topwater retrieves, feathering a baitcaster,
side pressure on a big fish, "bowing to the king" when a tarpon jumps. All of it emerges from the simulation, never
from canned animations.

Around that core sits a full game set in **real places**: a home base on Florida's Nature Coast and real fishing
destinations around the world, from Lake of the Woods to Kona to the Comoros. It has a story, quests, day/night,
tides, seasons, weather and real-world events, gear progression and customization, boats, property, traps for crabs
and lobsters, an optional 2-player co-op later, and a large field guide (the **Ledger**) of real fish.

The design lives in `docs/`. Read in this order the first time, then only what your task needs:

| Doc | What it holds |
|---|---|
| `docs/01_VISION_AND_WORLD.md` | Pitch, pillars, the world, the story, regions, characters |
| `docs/02_TECH_STACK.md` | Engine, languages, architecture, repo layout, testing, CI, performance budget |
| `docs/03_FISHING_MECHANICS.md` | Controls (mouse + controller), casting, retrieves, rigs, bites, hooksets, fights, landing |
| `docs/04_PHYSICS_SPEC.md` | The simulation: rod, line, reel, lure/bait, water surface, currents, boats, fish bodies |
| `docs/05_FISH_AND_ECOLOGY.md` | Fish behaviour model, species data schema, the species list |
| `docs/06_PROGRESSION_AND_ECONOMY.md` | Gear, customization, boats, property, licences, quests, events, weather, economy |
| `docs/07_ART_DIRECTION.md` | Visual style and exactly how to achieve it; audio direction |
| `docs/08_ROADMAP.md` | Milestones in build order, each with a Definition of Done |
| `docs/OPEN_QUESTIONS.md` | The owner's design decisions, and any new open questions |

**The motto: "Feel the line."** When in doubt, choose whatever makes the rod, line and fish feel more physical, more
readable and more real.

---

## 2. Golden rules (do not break these)

1. **The fishing is simulated, never faked.** Rod, line, reel/spool/drag, lures, baits, floats, sinkers, hooks, the
   water surface and fish bodies are driven by the simulation in `sim/` (spec: `docs/04_PHYSICS_SPEC.md`). No canned
   cast animation, no tweened line, no "fish on" cutscene, no fight mini-game bar that replaces physics. A UI may
   *display* sim values; it may never *decide* outcomes. If a technique (skip cast, walk-the-dog, mending, pendulum
   cast) does not emerge from the sim, fix the sim, do not script the technique.
2. **Fixed timestep and determinism.** The sim runs at a fixed tick (`SimConfig.TickHz`, default 120 Hz, with
   substeps). Never use frame time in sim code. Same seed + same input recording = same result (tested). Rendering
   interpolates between the last two sim states. Cosmetics (particles, UI tweens, camera smoothing, audio) may use
   frame time.
3. **The sim library has zero engine dependencies.** `sim/` is plain C# (.NET) with no `using Godot;`. It is testable
   headless with `dotnet test`. Godot code in `game/` reads sim state and feeds it input; it never owns gameplay
   state that the sim needs.
4. **Real fish, real behaviour, real requirements.** Every species is a real, named species (common + scientific name)
   with data drawn from reputable sources (FishBase, IGFA, government fisheries agencies, peer-reviewed papers), and
   each species file records its sources. Fish only live where, when, at the depth and temperature they really would,
   and only bite what and how they really would. Do not invent species. "Legendary" fish are named individuals of
   real species at plausible record-class sizes (never above the real all-tackle world record class). Spec:
   `docs/05_FISH_AND_ECOLOGY.md`.
5. **Fish are never static.** Every fish body bends along a simulated spine with its species' real swimming mode
   (anguilliform, carangiform, thunniform, etc.), fins move, mouths and gills open, and hooked or landed fish thrash
   physically. No rigid sprites sliding around.
6. **Data-driven content.** Species, gear, lures, baits, regions, spots, quests, dialogue and events live in JSON
   under `game/data/`, one file per item where practical, validated by tests against the schemas in the docs. Adding
   a fish must never require new code unless it introduces a new behaviour.
7. **Every sim change needs a test.** Rod, line, reel, water, lure, fish-behaviour and economy changes come with
   checks in `tests/`. Tests print the key measurements (cast distance, sag, tension, break load) so regressions are
   visible, not just pass/fail.
8. **Performance budget: the owner's laptop.** It has an AMD Ryzen 7 4800H with **integrated Radeon graphics only**
   (fill-rate bound). Target **60 FPS at 1920x1080 at the default quality preset** on that machine, with the sim
   costing at most 4 ms a frame. No per-frame heap allocations in hot sim loops (preallocate, reuse arrays, no LINQ
   in the tick). Every visual effect has a quality toggle. Details in `docs/02_TECH_STACK.md` §7.
9. **Look before claiming a visual works.** Use the screenshot harness (§5) and actually look at the PNG. Describe
   what you saw. Say clearly what only the owner can judge (feel, sound, beauty, FPS on their hardware).
10. **Only original, CC0, or properly licensed assets.** Procedural generation first (the art plan is built on it).
    Any downloaded asset must be CC0 or explicitly royalty-free for commercial use, be listed with source and licence
    in `game/assets/CREDITS.md`, and have a fallback so the game runs without it. Fonts must be OFL or similar with
    the licence file included. **Never** use assets, names, characters or art from *Cat Goes Fishing* or any other
    game. Downloads go in `asset-downloads/` (gitignored); never execute anything from there.
11. **Every tunable belongs in tuning data.** Feel numbers (rod stiffness profiles, drag curves, fish power, bite
    windows) live in data or `SimConfig`, are editable live in the dev tuning panel (F1) and saved to
    `game/data/tuning/*.json`. The owner tunes by feel; make that easy.
12. **Hardcore physics, honest information (owner decision).** No setting, preset or difficulty softens outcomes:
    rods break when high-sticked, lines snap, baitcasters backlash, slack loses fish, on every setting. No hidden
    stat makes casting more accurate or fights easier as the player progresses; progression is gear, access,
    knowledge and money. **Information assists** (casting arc preview, tension meter, line-out readout, bite
    indicator) are explicit options, labelled, and all off in "Purist" mode. Input accessibility (toggle-hold,
    remapping) is fine.
13. **SI units inside, angler units outside.** The sim uses metres, kilograms, seconds, newtons. The UI shows lb/oz/ft
    or kg/g/m per the player's setting. Line ratings are entered in data in lb (as sold) and converted on load.
14. **Real places, fictional people.** Places are real and each place's fish community is the real one. Every
    character, business, trust, company and gear brand is invented: never name or portray real businesses, guides,
    agencies or people. Regulations are real-style, simplified and frozen, with the in-game notice that they may be
    out of date. Portray local cultures respectfully. Details: `docs/01_VISION_AND_WORLD.md` §4.1.
15. **Co-op ready from day one.** The sim supports several anglers, input is per angler, there are no player
    singletons, and saves split world state from player state. Details: `docs/02_TECH_STACK.md` §10.

---

## 3. Repository layout (target; create as milestones need it)

```
CLAUDE.md                 this file
README.md                 short public readme
docs/                     the design and build plan (markdown)
sim/                      TheLedger.Sim: pure C# class library, no Godot (rod, line, reel, water, lures, fish, ecology)
tests/                    TheLedger.Sim.Tests: xUnit tests for sim/ and data validation
game/                     the Godot 4 .NET project (project.godot, TheLedger.csproj)
  scenes/                 .tscn scenes (kept small; most content is built from code + data)
  src/                    C# Godot-side code: bootstrap, input, rendering of sim state, UI, audio, save/load
  shaders/                .gdshader files (water, sky, fish skinning, low-poly, post)
  data/                   JSON content: species/, gear/, lures/, baits/, regions/, spots/, quests/, dialogue/,
                          events/, tuning/
  assets/                 audio/, fonts/, (rare) textures/; CREDITS.md
tools/                    Python helpers: data validation reports, screenshot conversion/diff, species data import
asset-downloads/          raw downloads (gitignored, never executed)
.github/workflows/        CI
```

Keep this section accurate as the real layout grows.

---

## 4. The owner and how they work

- The owner is casual and direct and often writes numbered lists of requests, sometimes with typos. When something is
  ambiguous, pick the most plausible reading, do it, and **state your interpretation in the report** so they can
  correct it.
- They judge by **playing**: feel, look and sound are their verdicts. Give them something playable early and often.
- They get frustrated when the same thing misses twice. **Research first, measure, then change**, and explain the
  why in one or two sentences.
- **Big requests go in pieces.** Commit and push after each piece so progress survives if a session ends. Keep the
  progress checklist (§8) updated per piece.
- **Their controls:** mouse + keyboard, and a **GameSir controller in Xbox layout**. Test input on both.
- **Their decisions so far** (2026-10-18): human angler + dog; flat 2D low-poly; fish mostly visible underwater
  (dimmer in murk and at night, never hidden); hardcore physics with no softening presets; Godot 4 + C#; full
  seasons; keep/sell and release both rewarded; full fly fishing; keep a Steam release possible; mostly ambient
  music; real places with a Florida home base and world travel; regulations simplified and frozen; tasteful
  handling of fish deaths; the logbook world-tour story; plan for co-op later; the name "The Ledger".
- **Their machine is also their daily PC.** Automated runs of the game must be quiet and light: muted audio, FPS capped
  at 30, a small window, and no long benchmarks unless they ask. Use the `--light` flag (§5).
- Their email is for commit authorship only; never send it anywhere.
- The owner also builds **Crisp** (a C++ shooter) in `..\Crisp`. Never touch that folder from this project.

---

## 5. Build, run and verify

Exact versions are pinned in `docs/02_TECH_STACK.md` §2 once installed; update them there if they change.

**Tooling on this machine (2026-10-18):** .NET SDK 9.0.305 (runtimes 8.0.8 and 9.0.9) at `C:\Program Files\dotnet`;
Python 3.11 (install Pillow/numpy if missing); Git Bash and PowerShell. **Godot 4.7.2 .NET** is installed at
`C:\Users\matth\Desktop\Apps\Godot_v4.7.2-stable_mono_win64\`: use `Godot_v4.7.2-stable_mono_win64_console.exe` for
command-line runs (it prints to the terminal and returns exit codes). Export templates are **not** installed locally
(1.1 GB); CI exports the Windows build. (An old standard Godot 4.4 also sits in `Desktop\Apps`; don't use it.)

```
GODOT="/c/Users/matth/Desktop/Apps/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe"

# sim + data tests (fast, headless, no Godot)
dotnet test tests/TheLedger.Sim.Tests.csproj

# build the game's C# (Godot loads game/.godot/mono/temp/bin/Debug/TheLedger.dll, so build before running)
dotnet build game/TheLedger.csproj -warnaserror

# once after cloning or adding assets: import the project
"$GODOT" --headless --path game --import

# run the game
"$GODOT" --path game

# headless smoke test: runs N sim seconds as fast as possible, exits 1 on any fault or exception
"$GODOT" --headless --path game -- --smoke 30

# screenshot harness (needs the GPU; quiet and light). PATH may be absolute.
"$GODOT" --path game --resolution 1280x720 -- --light --screenshot out.png --frames 90 --debug-overlay
```

**In-game command-line args** (after `--`; parsed in `game/src/Boot/CommandLine.cs`): `--light` (mute, cap 30 FPS),
`--screenshot PATH` (save PNG after `--frames N` sim-locked frames, then quit), `--seed N`, `--replay FILE` (drive
input from a `.rec` recording), `--record FILE` (save this session's input on exit), `--smoke SECONDS`,
`--debug-overlay`, `--anglers N`. Parsed but not used yet (wire them up as their systems arrive): `--scene NAME`,
`--time HOURS`, `--weather NAME`, `--season NAME`, `--bench SECONDS` (only when the owner asks).

**Verifying by kind of change:**

| Change | Verify with |
|---|---|
| Sim numbers (rod, line, reel, water, lure) | `dotnet test`; read the printed measurements |
| A technique (skip cast, walk-the-dog, figure-8) | an input recording in `tests/replays/` replayed by a test that asserts the outcome |
| Fish behaviour / ecology | sim tests on behaviour (bite odds by condition, habitat checks); data validation |
| Visuals | screenshot harness; look at the PNG; compare day/night/weather variants |
| Performance | `--bench 20` writes `bench.json`; only on request or at milestone ends, never while the owner is using the PC |
| Feel | only the owner can judge; describe exactly what to try |

---

## 6. The GitHub process

1. **Branch:** `development` (the repo's working branch). Remote: `https://github.com/MartyJCross/FishingGame.git`.
   Don't push to other branches, rewrite history, or open PRs unless asked.
2. **Before pushing:** `dotnet build` warning-free, `dotnet test` all green, headless smoke test passes, re-read your
   diff.
3. **Docs:** update `README.md` if player-facing things changed, add the version's notes to `CHANGELOG.md` (newest
   first), keep §7 and §8 of this file current.
4. **Versions:** `0.MINOR.PATCH` in `game/project.godot` (`application/config/version`), shown on the main menu.
   Minor for features/milestones, patch for fixes.
5. **Commit messages:** one-line summary, then bullets for what changed and why, then the attribution line the
   session asks for.
6. **Push with plain `git push`.** The `gh` CLI on this machine is logged in to a *different* GitHub account
   (a work account), so do not use `gh` for this repo. Check CI by polling
   `https://api.github.com/repos/MartyJCross/FishingGame/actions/runs?branch=development&per_page=1` with curl
   (if the repo is private and that returns 404, tell the owner and ask how they want CI checked).
7. **If CI fails, fix it and push again.** Never hand over a red build.
8. **Report to the owner:** what changed in their terms, what you verified, what they should try, the CI run link.

---

## 7. Current state

- **v0.1.0 (M0, 2026-10-18):** Godot 4.7.2 .NET project + pure C# sim library + xUnit tests + CI.
  - `sim/`: `SimWorld` (N anglers, fixed 120 Hz tick, NaN guard, state hash), `Hands` (arm-limited rod driver),
    `InputFrame` + `.rec` recordings, `SimConfig` with `[Tunable]` fields, PCG32 `SimRng`, `SimEventQueue`.
  - `game/`: `Main` (harness args), `SimDriver` (fixed tick, interpolation, lockstep for screenshots/smoke,
    record/replay), `InputRouter` + `InputActions` (mouse/keyboard + Xbox-layout pad), F1 `TuningPanel`, F3
    `DebugOverlay`, `TestView` placeholder (stick-figure angler with a rigid rod).
  - Tests: 18 (determinism, replay, zero allocation per tick, tuning file). CI: tests, build, smoke, export,
    artifact `TheLedger-windows-x64` (~37 MB).
- **v0.2.0 (M1, 2026-10-18):** water. `sim/Water/`: `AmbientWaves` (JONSWAP/PM wind sea + swell, dispersion,
  orbital velocity), `SurfacePatch` (1D heightfield, sponge edges, foam, volume-conserving impulses), `Droplets`,
  `WaterSurface` (tide + ambient + up to 3 patches, rain, `ImpactLog` for rings), conditions from "Debug conditions"
  tunables until weather exists. `game/src/Render/WaterRenderer.cs` (volume gradient, surface line, near band with
  ripple rings, droplets). `--weather calm|breeze|windy|rain|storm`, `--scene water_test`, middle-click splash, F4 rain.
  `tools/sync_tuning.py` keeps `sim.json` in step with `SimConfig`. 32 tests.
- **M2 done (2026-10-18, v0.3.0):** `sim/Tackle/`: `Rod` (XPBD chain, EI taper,
  strength taper, moments), `Line` (variable length, pay-out/merge, solver mass floor), `TackleSolver` (direct banded
  Cholesky over rod stretch + bend + line, double precision, Rayleigh damping), `Rig` (substeps, drag, buoyancy,
  wind, ground contact, guide forces), `TackleSpecs` + data in `game/data/gear/{rods,lines}`, `Spot` terrain in the sim.
  Temporary line control until M3: Bail pays out, cranking retrieves. Tests: rod bands, action ordering, solver
  independence, vibration, line stretch per material, slack sag, pay-out, floating vs sinking line.
  - Lessons: Gauss-Seidel made the rod far too soft (deflection depended on substeps); real-weight line nodes go
    chaotic next to heavy loads, so the solver uses a node mass floor (gravity/drag keep real mass).
  - `game/src/Render/TackleRenderer.cs` draws the simulated rod (tapered two-tone strip at ~4x real width, cork,
    reel, guides) and line (polyline through every node, sinker), interpolated between ticks; with F3 the blank is
    coloured by moment/limit and the line by tension/breaking force. Mouse relative mode moved to M3 (casting).
  - Perf: sim step mean 0.39 ms (≈5% of a core at 120 Hz); the old "7 ms worst step" was JIT warm-up (now skipped).
    V-Sync is off in project.godot (the owner's AMD iGPU driver has no Mailbox, so vsync pinned 60 fps);
    `--max-fps N` caps. **Owner wants high FPS: check it whenever visuals are added.**
- **M3 in progress (2026-10-18):** `sim/Tackle/Reel.cs` (`ReelSpec` + `Reel`: spool radius from line on it, bail
  toggle, drag clicks, static/kinetic drag, crank retrieve = turns × gear × 2πr with a hand-torque stall, finger hold).
  The reel works as a **tension limit at the rod tip**: `TackleSolver.Solve(..., slipForce)` redoes the solve with the
  tip→first-node segment replaced by exactly the slip force when holding would need more, and reports `SlipDistance`
  (the lure keeps its momentum; the tip feels the lip friction or drag). Open bail = lip friction (rises as the spool
  empties) + finger; closed = drag. Line node spacing grows with line out (`LineNodesTarget`). Terminal water entry
  and skipping (`Rig.WaterEntry`, `TerminalSpec.Planing`) → `SimWorld` splashes + Splash events (Id 1 = skip).
  Follow camera zooms out for long casts. Reel HUD top right. Replay `game/data/replays/cast_overhead.rec` (written
  by `ReelTests`): a 14 g sinker flies ~22 m. Controls: Space/B bail, LMB/LT finger, RMB/RT/wheel crank, Q/E drag.
  - Cost mode: baitcaster (M3.3), snags/wear (M3.5) and the casting lawn (M3.6) are deferred until after the demo.
- **M4 (lite, 2026-10-18):** `TerminalSpec` is now lure/bait data (`game/data/lures/*.json`, 7 entries: sinker, worm
  under a bobber, jig, Texas worm, squarebill, spinner, popper), loaded in file-name order into each angler's
  `TackleBox`; R/X cycles when line out ≤ `RetieMaxLineOut`; `--lure ID` picks one at start. Still one body at the
  line's end (no 2-node lure yet): lip lift ½ρv²A across the motion (squarebill ~1 m at 1 m/s), fixed bobber on the
  line node `FloatDepthM` above the hook (`Line.FloatIndex`, partial-submersion buoyancy), popper pops on sharp
  surface twitches. `Rig.Lure` (`LureState`) = heading, wobble phase, speed, depth, vibration/flash/sound: **the
  stimulus fish will read in M5.** Renderer draws each kind as a small flat shape that wobbles, plus the bobber.
  - Deferred past the demo: 2-node lure body, walk-the-dog, spoon flutter, sub-chain rigs (Carolina, drop shot,
    slip float), bait tear-off, live bait agents.
  - **Next: M5 fish** (bodies, senses, bites) for the Station Pond's tier-S species, then M6 (hookset, fight,
    landing) = the demo.
  - **Species data agent (stopped by a usage limit):** partial work is in the git worktree
    `.claude/worktrees/agent-a2366193d8b0027c9` on branch `worktree-agent-a2366193d8b0027c9` (it was writing redear,
    Florida bass, black crappie). Check what it committed or left uncommitted there, finish the tier-S species, and
    merge into `development`.
  - **Owner (2026-10-18): save cost until the first demo (M6): less testing.** Key tests only, one test + build run
    per commit, one screenshot per visual change, CI checked once per milestone.

(Replace this list with a short, accurate description of what exists as the game grows: systems, controls, data
counts, known issues.)

---

## 8. Progress checklist

Tick items as they land (commit hash + date). The full Definition of Done for each is in `docs/08_ROADMAP.md`.

- [x] M0 Foundations: repo skeleton, Godot .NET project, sim library, tests, CI, harness args, debug overlay
  (2026-10-18, commits 49779c1..1ba3030, CI run 39103364556 green; owner still to confirm the GameSir shows in F3)
- [x] M1 Water surface sim + rendering (pond test scene) (2026-10-18, v0.2.0)
- [x] M2 Rod + line sim driven by mouse and controller
- [ ] M3 Reel, spool, casting (spinning + baitcaster), lure flight, water entry, skipping, snags
- [ ] M4 Lure and rig hydrodynamics, retrieves, floats and bottom rigs
- [ ] M5 Fish bodies (procedural mesh, spine bending, swimming modes) + perception + bite model, 6 pond species
- [ ] M6 Hookset, fight, drag, breakage, landing, keep/release → **vertical slice playtest gate**
- [ ] M7 Controller polish (GameSir), haptics, remapping, information assists, Purist mode, feel tuning pass
- [ ] M8 Art pass on the pond: low-poly world builder, sky, day/night, weather, post, audio
- [ ] M9 Meta layer: save/load, money, shop, gear stats, customization, Ledger, licences
- [ ] M10 Boats (rowboat first), boat physics, the Spring Run and the Suwannee, sonar
- [ ] M11 Home salt water I (flats, tides, traps), dialogue, quests, Act 1 start, first property, tournaments
- [ ] M12 Travel system, seasons, Lake of the Woods (ice fishing), the Atchafalaya, end of Act 1
- [ ] M13 Fly fishing, the Madison, the Columbia and Oregon Coast
- [ ] M14 Homosassa, Gulf ledges and offshore, Chesapeake and Outer Banks, the Keys, big game, Act 2
- [ ] M15 Act 3: restoration, Kona, the Comoros finale, the ending
- [ ] M16 Post-game destinations, full property system, legends, circuit
- [ ] M17 Polish, performance, accessibility, settings, Steam Deck checks, release build
- [ ] M18 Co-op (2 players online)

---

## 9. Session know-how (this machine)

- **Shells:** Git Bash (POSIX) and PowerShell. The repo is
  `C:\Users\matth\Desktop\Personal\Development\Games\FishingGame`. `Games\` is the owner's VS Code workspace; keep all
  of this game's files inside `FishingGame\`.
- **Never write code through a Bash heredoc.** In this setup the shell layer rewrites backslash escapes (`\n` inside
  string literals became real newlines in another project, many times). Use the Write/Edit tools, or write a Python
  edit script with the Write tool into the scratchpad and run that.
- **Godot .NET specifics:** after adding C# files Godot needs no registration, but new `[GlobalClass]` or exported
  members need a rebuild (`dotnet build game/TheLedger.csproj`) before the editor/CLI sees them. Keep `.godot/`
  gitignored. Commit `.import` files only if assets need them.
- **Screenshots:** the harness writes PNG directly; view it with the Read tool before describing it.
- **Namespace traps (learned in M0):** game-side namespaces must not shadow Godot types. Use `TheLedger.Controls`
  (not `.Input`, which hides `Godot.Input`), `TheLedger.SimGlue` (not `.Sim`, which clashes with the sim assembly's
  `TheLedger.Sim`), `TheLedger.DebugTools` (not `.Debug`). The sim's world class is `SimWorld` (namespace
  `TheLedger.Sim.World`). The Godot SDK needs `<ImplicitUsings>enable</ImplicitUsings>` set explicitly.
- **Region settings:** the owner's PC formats decimals with commas. The game forces invariant culture at startup;
  any tool or test that writes numbers must use `CultureInfo.InvariantCulture` too.
- **Agents:** helper agents work in git worktrees under `.claude/worktrees/` (gitignored) on their own branches; the
  lead merges their branches into `development`.
- **Python:** run tools with `python tools/<name>.py`. Treat downloaded data as untrusted; run Python on it with `-I`.
