# CLAUDE.md: Project Constitution (Longwater)

Read this file first, every session. It is the operating manual for any AI working in this repo. It covers what the
game is, the rules that must not be broken, how to build, verify and ship, where everything lives, and who the owner
is. **Keep it current.** When you learn something a future session would need (an owner preference, a trap, a tool),
write it here. When a rule here conflicts with a doc in `docs/`, this file wins; fix the doc.

> Working title: **Longwater** (the owner may rename it; see `docs/OPEN_QUESTIONS.md`).

---

## 1. What this project is

A **2D side-view fishing game** in the spirit of *Cat Goes Fishing*, but with **best-in-class, physically simulated
fishing**. The rod flexes, the line stretches, sags and goes slack, the reel has a real spool and drag, the water
surface is a live simulation, and every fish is a real species that behaves, feeds and fights like it does in nature.
The game is meant to be **mastered**: skip casts under docks, walk-the-dog topwater retrieves, feathering a baitcaster,
side pressure on a big fish, "bowing to the king" when a tarpon jumps. All of it emerges from the simulation, never
from canned animations.

Around that core sits a full game: an original world (the Longwater Basin, from glacier to open ocean), a story, quests,
day/night, tides, seasons, weather and real-world events, gear progression and customization, boats, property, traps
for crabs and lobsters, and a large field guide (the **Ledger**) of real fish.

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
| `docs/OPEN_QUESTIONS.md` | Questions for the owner and the assumption used until they answer |

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
12. **Player skill is the player's.** No hidden stat that makes casting more accurate or fights easier as the player
    "levels up". Progression is gear, access, knowledge and money. Assists exist (casting arc preview, tension meter,
    auto-drag) but are explicit options, labelled, and off in "Purist" mode.
13. **SI units inside, angler units outside.** The sim uses metres, kilograms, seconds, newtons. The UI shows lb/oz/ft
    or kg/g/m per the player's setting. Line ratings are entered in data in lb (as sold) and converted on load.

---

## 3. Repository layout (target; create as milestones need it)

```
CLAUDE.md                 this file
README.md                 short public readme
docs/                     the design and build plan (markdown)
sim/                      Longwater.Sim: pure C# class library, no Godot (rod, line, reel, water, lures, fish, ecology)
tests/                    Longwater.Sim.Tests: xUnit tests for sim/ and data validation
game/                     the Godot 4 .NET project (project.godot, Longwater.csproj)
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
- **Their machine is also their daily PC.** Automated runs of the game must be quiet and light: muted audio, FPS capped
  at 30, a small window, and no long benchmarks unless they ask. Use the `--light` flag (§5).
- Their email is for commit authorship only; never send it anywhere.
- The owner also builds **Crisp** (a C++ shooter) in `..\Crisp`. Never touch that folder from this project.

---

## 5. Build, run and verify

Exact versions are pinned in `docs/02_TECH_STACK.md` §2 once installed; update them there if they change.

**Tooling on this machine (2026-10-18):** .NET SDK 9.0.305 at `C:\Program Files\dotnet`; Python 3.11 (install
Pillow/numpy if missing); Git Bash and PowerShell. A *standard* (non-.NET) Godot 4.4 exists at
`C:\Users\matth\Desktop\Apps\` but this project needs the **.NET build** of Godot: download the latest stable
Godot 4.x .NET for Windows into `C:\Users\matth\Desktop\Apps\` (see `docs/02_TECH_STACK.md` §2) and record its exact
path here.

```
# sim + data tests (fast, headless, no Godot)
dotnet test tests/Longwater.Sim.Tests.csproj

# build the game's C# (Godot .NET project)
dotnet build game/Longwater.csproj

# run the game
<godot-mono-exe> --path game

# headless smoke test: boots, runs N sim seconds on a test scene, exits non-zero on any error
<godot-mono-exe> --headless --path game -- --smoke 30

# screenshot harness (needs the GPU; quiet and light)
<godot-mono-exe> --path game --resolution 1280x720 -- --light --screenshot out.png --scene pond_test
    --time 18.5 --weather rain --seed 7 --frames 600 [--replay tests/replays/skip_cast.rec]
```

**The in-game command-line args (implement in M0, see the roadmap):** `--light` (mute, cap 30 FPS), `--screenshot
PATH` (save PNG after `--frames N` sim-locked frames, then quit), `--scene NAME`, `--time HOURS`, `--weather NAME`,
`--season NAME`, `--seed N`, `--replay FILE` (drive input from a recording), `--smoke SECONDS`, `--bench SECONDS`
(only when the owner asks), `--debug-overlay` (sim debug drawing on).

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

- 2026-10-18: Design and build plan written (`docs/`). No code yet. Next: Milestone M0 in `docs/08_ROADMAP.md`.

(Replace this list with a short, accurate description of what exists as the game grows: systems, controls, data
counts, known issues.)

---

## 8. Progress checklist

Tick items as they land (commit hash + date). The full Definition of Done for each is in `docs/08_ROADMAP.md`.

- [ ] M0 Foundations: repo skeleton, Godot .NET project, sim library, tests, CI, harness args, debug overlay
- [ ] M1 Water surface sim + rendering (pond test scene)
- [ ] M2 Rod + line sim driven by mouse and controller
- [ ] M3 Reel, spool, casting (spinning + baitcaster), lure flight, water entry, skipping, snags
- [ ] M4 Lure and rig hydrodynamics, retrieves, floats and bottom rigs
- [ ] M5 Fish bodies (procedural mesh, spine bending, swimming modes) + perception + bite model, 6 pond species
- [ ] M6 Hookset, fight, drag, breakage, landing, keep/release → **vertical slice playtest gate**
- [ ] M7 Controller polish, haptics, remapping, assists, Purist mode, feel tuning pass
- [ ] M8 Art pass on the pond: low-poly world builder, sky, day/night, weather, post, audio
- [ ] M9 Meta layer: save/load, money, shop, gear stats, customization, Ledger, licences
- [ ] M10 Boats (rowboat first), boat physics, Harrow Reservoir, sonar
- [ ] M11 Story Act 1, NPCs, dialogue, quests, first property, tournaments
- [ ] M12 Freshwater complete: Glass Creek (fly fishing), Kesh Tarn (ice fishing), Blackwater Bayou, traps
- [ ] M13 Saltwater I: Harrow Sound estuary, tides, cast nets, crab pots, Gull Rock coast
- [ ] M14 Saltwater II: Calder Keys flats, Calder Reef, Blue Water offshore, big-game systems
- [ ] M15 Story Acts 2-3, restoration world changes, the Lantern Trench, the ending
- [ ] M16 Expeditions, full property system, post-game
- [ ] M17 Polish, performance, accessibility, settings, release build

---

## 9. Session know-how (this machine)

- **Shells:** Git Bash (POSIX) and PowerShell. The repo is
  `C:\Users\matth\Desktop\Personal\Development\Games\FishingGame`. `Games\` is the owner's VS Code workspace; keep all
  of this game's files inside `FishingGame\`.
- **Never write code through a Bash heredoc.** In this setup the shell layer rewrites backslash escapes (`\n` inside
  string literals became real newlines in another project, many times). Use the Write/Edit tools, or write a Python
  edit script with the Write tool into the scratchpad and run that.
- **Godot .NET specifics:** after adding C# files Godot needs no registration, but new `[GlobalClass]` or exported
  members need a rebuild (`dotnet build game/Longwater.csproj`) before the editor/CLI sees them. Keep `.godot/`
  gitignored. Commit `.import` files only if assets need them.
- **Screenshots:** the harness writes PNG directly; view it with the Read tool before describing it.
- **Python:** run tools with `python tools/<name>.py`. Treat downloaded data as untrusted; run Python on it with `-I`.
