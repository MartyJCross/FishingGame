# 08: Build Roadmap

## How to use this roadmap

- Build **in order**. Each milestone depends on the ones before it. Within a milestone, do the pieces in order and
  **commit + push after each piece** (CLAUDE.md §4, §6). Tick the milestone in CLAUDE.md §8 when its Definition of
  Done (DoD) is met.
- **The core comes first.** Milestones M1-M7 are all about making one pond feel incredible. Don't start the meta
  game (M9+) until the owner has played the vertical slice (end of M6) and the feel pass (M7) is done.
- **Playtest gates** (🎮) are where the owner plays. Stop, hand over a build with a clear "what to try" list, and wait
  for feedback before the next milestone. Expect tuning rounds there.
- **Co-op comes later (M18), but its rules apply from M0** (`02_TECH_STACK.md` §10): multiple anglers in the sim, no
  player singletons, per-player input.
- The owner tests with **mouse + keyboard** and a **GameSir controller (Xbox layout)**. Every input feature is checked
  on both.
- If a DoD item turns out wrong or impossible, say so, propose the fix, and update this doc. Don't silently skip it.

---

## M0: Foundations

Pieces:
1. Repo skeleton (CLAUDE.md §3), `.gitignore`, `README.md`, `CHANGELOG.md`, `game/assets/CREDITS.md`.
2. Install Godot .NET (latest stable 4.x) + export templates; pin versions in `02_TECH_STACK.md` §2 and CLAUDE.md §5.
3. `sim/` class library, `tests/` xUnit project referencing it, one trivial passing test. `game/` Godot .NET project
   referencing `sim/`. A `Main.tscn` that opens a window showing the title "The Ledger" and the version.
4. `SimDriver` (fixed 120 Hz tick, snapshot interpolation), `InputFrame` (per player) + `InputRouter` stub, input
   recording and replay file format (`.rec`: header with version, seed and player count, then per-tick packed
   InputFrames per player).
5. Command-line args: `--light`, `--screenshot`, `--frames`, `--scene`, `--time`, `--weather`, `--seed`, `--replay`,
   `--smoke`, `--bench`, `--debug-overlay`.
6. Debug overlay framework (immediate-mode 2D drawing of sim state) and the F1 tuning panel framework (reflection
   over `SimConfig` fields → sliders; save/load `game/data/tuning/sim.json`).
7. CI workflow (`02_TECH_STACK.md` §6) producing the `TheLedger-windows-x64` artifact.
8. Determinism test scaffold and the zero-allocation tick test scaffold.

**DoD:** CI green on `development`, the artifact downloads and runs, `--screenshot` produces a PNG, `--smoke 5`
passes headless, the tuning panel opens with F1, and the GameSir controller is detected (a debug readout of sticks and
triggers).

## M1: Water surface

Pieces:
1. `WaterSurface`: ambient waves (wind-driven trains + optional swell) and `SurfacePatch` heightfield with sponge
   boundaries; tide level hook (constant for now).
2. Impulse API (splashes), displacement API (floating bodies), foam scalar, splash particle events.
3. Pond test scene: a flat-shaded placeholder bank and bottom; surface line + near band + water volume shader with
   depth colour; click-to-splash debug tool.
4. Rain debug toggle feeding drops into the patch.
5. Tests: energy decay, boundary absorption, no NaNs in a 10-minute storm, CFL guard.

**DoD:** Clicking the water makes believable ripples that travel and fade; rain pocks the surface; wind roughens it;
screenshots already look good (look at them).

## M2: Rod and line

Pieces:
1. `Hands` kinematic driver with arm limits; mouse absolute mode, mouse relative mode, and right-stick mapping.
2. `Rod`: XPBD chain, stiffness profile from power/action, damping, guides; data for 3 rods (UL spinning, M spinning,
   MH casting).
3. `Line`: variable-length rope with unilateral segments, materials (mono, fluoro, braid), air/water drag, buoyancy,
   surface film; a fixed length out for now with a dummy weight on the end.
4. Guide line-path forces bending the rod under load.
5. Rendering of rod and line (`07` §8), debug overlay for tensions and moments.
6. Tests: rod deflection bands, stretch, sag, determinism, zero allocations. The sim supports two anglers in one world
   (a test with two rods).

**DoD:** Swinging the rod with a weight on the line feels physical: the rod loads and whips, the weight swings, the
line sags when slack and snaps tight. Both mouse and controller drive it.

## M3: Reel, casting, entry, skipping, snags

Pieces:
1. `Spool`/`Reel`: line capacity and radius, cranking (wheel, RMB, RT), cranking power limit, drag (static/kinetic,
   detents), bail/line hold (LMB, LT analog).
2. Spinning casts: line-hold release, pay-out friction, feathering; spincast variant.
3. Baitcaster: spool inertia, brakes, thumb pressure, backlash detection and the clear-tangle interaction.
4. Lure flight: 2-node lure body, air drag, wind; water entry energy, splash, disturbance events; **skipping**.
5. Terrain and structure collision, snags (sites, pins, freeing by angle/pop/force), line wear, line breaks.
6. Casting lawn practice scene with accuracy hoops and a skip dock.
7. Tests: distance bands table, backlash/no-backlash replays, skip replay, snag freeing, break at the weakest point.

**DoD:** Overhead, sidearm, pitch and skip casts all work and feel distinct; the baitcaster rewards thumbing and
punishes sloppiness; snags happen and can be freed; replays prove it.

## M4: Lures, rigs and retrieves

Pieces:
1. Lure hydrodynamics: buoyancy classes, lip dive + wobble, spoon flutter, spinner blades, soft plastic tails, jig
   fall rates; lateral pseudo-state.
2. Topwater: popper, walker (walk-the-dog), prop bait, frog; surface interaction through the patch.
3. Terminal rigs as sub-chains: split shot, sliding sinker (Carolina), drop shot, Texas rig, float and slip float with
   bobber stop, bottom rigs, hair rig. Rig menu (R / X).
4. Baits: worm, live minnow or shiner (a tiny agent), cut bait, cricket, dough; bait hold and tear-off on hard casts.
5. Lure data for ~12 lures and ~6 baits; low-poly lure meshes.
6. Tests: crankbait depth vs speed / line out / line diameter, walk-the-dog alternation, float bite display sanity,
   bait tear-off threshold.

**DoD:** Every retrieve in `03` §5 can be performed and looks right underwater. A float rig set to 1.5 m sits at 1.5 m.

## M5: Fish (bodies, senses, bites)

Pieces:
1. Species data loader + schema validation tests; shape specs; the 6 tier-S species files fully sourced (bluegill,
   redear sunfish, Florida bass, black crappie, channel catfish, golden shiner).
2. `FishBody` spine physics, swimming modes, C-start, fins; `FishMeshBuilder` + `fish_skin` shader; turning;
   the `fish_lineup` scene.
3. Senses (vision with clarity/light/colour/contrast, lateral line, hearing, smell plume), spook model.
4. Brain: states, habitat selection by comfort, schooling (shiners, bluegill), the strike decision, bite styles,
   suction feeding and hold/reject.
5. Spot population for the Station Pond: habitat cells, agents spawn/despawn around the lure and camera, sizes from
   growth curves.
6. Tests: the behaviour tests from `05` §4.4 that apply to these species, suction, C-start timing, lineup screenshot.

**DoD:** Fish swim beautifully and believably (the owner should be able to tell a catfish from a bass by how it
moves), investigate lures, follow, refuse, and bite in species-specific ways. Bites read clearly on the rod, line and
float. Fish stay visible to the player in murky water and at night, only dimmer (`07` §6.4).

## M6: Hookset, fight, landing → 🎮 vertical slice

Pieces:
1. Hook-hold model (J, circle, treble; barbless), hookset from rod motion with line-stretch effects.
2. Fight model: thrust, behaviours (run, dive, jump, headshake, structure dash, sulk), stamina pools, temperature
   effect; jumps through the surface.
3. Failures: line break, abrasion, rod break (high-stick), hook pull, wrapped in structure.
4. Landing: hand/lip, net (timing + position), the after-catch flow (measure, weigh, photo, unhook, release with
   revive, keep in a bucket); a simple catch log (temporary Ledger).
5. Audio pass 1: reel, drag, line hiss, splashes, rises, pond ambience (CC0 + synthesis, credited).
6. Basic time of day (the sun moves, palettes blend between day and night) and its effect on fish activity.
7. Tests: the hook model tests from `04` §12, fight sanity (a 2 kg bass on 10 lb mono at 25% drag is landable; at
   100% drag with a surge, the line breaks), release survival effects.

**DoD / 🎮 gate:** The Station Pond is genuinely fun for 30 minutes with three rods (spincast, spinning, baitcaster),
a handful of lures and baits, six species, day and night. Hand it over with a "what to try" list: skip a jig under
the dock, walk a topwater at dusk, fish for catfish at night, fool a big redear on ultralight, and break a rod by
high-sticking a big bass at the bank.

## M7: Feel pass, controller, information assists

Pieces:
1. The owner's feedback round from M6 (tuning in data, not code).
2. Gamepad polish on the GameSir: stick curves, trigger curves, rumble design for every event (`03` §2.2), remapping UI.
3. **Information assists only** (the physics is never softened): casting arc, tension meter, line-out readout, bite
   indicator, toggle-hold for line hold and crank. **Purist mode** turns them all off.
4. Accessibility basics; settings menu with input, audio, video and assists.
5. Training drills on the casting lawn (Odile's radio lines as placeholders).

**DoD:** The owner signs off on feel with both mouse and controller. 🎮

## M8: Art pass on the pond

Pieces:
1. `LowPolyBuilder` and generators: terrain, Florida trees (live oak with Spanish moss, cabbage palm, cypress, pine),
   reeds, lily pads, rocks, the dock, the station exterior.
2. Sky shader, the Station Pond palettes with all six keyframes, low-poly re-shading by light direction.
3. Water: near band with rings and wakes, far-water band with reflection and glitter, god rays, caustics,
   particulates, bubbles.
4. Weather visuals: rain, wind, fog, storm (lightning + thunder), overcast grading.
5. Angler model with IK and lean, the dog, rod/reel/lure art.
6. Post stack and quality presets; renderer decision (Mobile vs Compatibility) with a benchmark on the owner's PC at
   a time they choose; record the result in `02_TECH_STACK.md`.
7. The screenshot set from `07` §11.

**DoD:** Screenshots at four times × three weathers look beautiful (the AI looks at each one and the owner approves);
60 FPS at 1080p Medium on the owner's laptop. 🎮

## M9: Meta layer

Pieces:
1. Save/load with versioning and migrations (world state separate from per-player state); settings persistence.
2. Inventory, money, the station hub (first version): Pell's shop, the fish house, the Trust office.
3. Gear stats from data for all M2-M4 gear plus new tiers; buying and equipping; a basic lure paint shop.
4. The Ledger UI (ink shader renders, records, research tasks, knowledge levels).
5. Licences and regulations (size and bag limits, protected species, the "simplified and may be out of date"
   notice), the wildlife officer (basic).
6. Economy: prices, freshness, orders; `tools/economy_sim.py`.

**DoD:** A new game starts at the station, fishes the pond, sells fish, buys gear, fills Ledger pages, saves and loads.

## M10: Boats and home fresh water

Pieces:
1. Boat physics (`04` §8), the rowboat with oars, anchor; boarding and fishing from a boat.
2. Teodor's restoration quest for the *Teal* (quest system v0).
3. The home map and travel between home spots; the **Spring Run** (clear water, sight fishing, snook in cold snaps)
   and **the Suwannee** (current, limestone, jon boat).
4. Their tier-1 species (`05` §7.2-7.3), sonar (2D), drifting, night fishing tools (headlamp, boat light).

**DoD:** Row up the spring run and sight-fish bass; run the Suwannee in the jon boat and catch a Suwannee bass on a
crayfish imitation.

## M11: Home salt water I, story Act 1 start, NPCs, tournaments

Pieces:
1. Tides (with in-game tide tables), tidal currents, oyster bars that appear at low tide; **Waccasassa Flats and Oyster
   Bars** with their species (`05` §7.4); cast net, crab traps, blue and stone crabs; saltwater gear corrosion.
2. Dialogue system (data-driven branching), NPCs at the station, radio messages from Odile.
3. Quest system v1 (composable objectives), survey and research quests, Wren's pages.
4. Act 1 story beats at home; the first property (Island Cottage or Spring Cabin: sleep, storage, fast travel).
5. Bass tournament (5-fish limit, livewell, weigh-in, Dace).

**DoD:** The first half of Act 1 is playable; a tide cycle visibly changes the flats. 🎮

## M12: Travel, seasons, Lake of the Woods, the Atchafalaya

Pieces:
1. The map room and travel system (trip costs, baggage limit, arriving at a destination base, rented local boats).
2. Seasons with spawning behaviour, latitude-dependent day length, and destination climates.
3. **Lake of the Woods**: walleye at dusk, pike, musky (figure-8), smallmouth, lake sturgeon; **ice fishing** in winter
   (frozen surface, auger, tip-ups, burbot at night); Ingrid and her lodge.
4. **The Atchafalaya and Louisiana marsh**: mud boat, murky water, gar, bowfin, catfish, crawfish traps, silver carp
   jumping, bull reds; Big Ruth.
5. The end of Act 1 (the dredge permit delay).

**DoD:** Act 1 is playable start to end (about 10 hours). 🎮

## M13: Fly fishing, the Madison, the Columbia and Oregon Coast

Pieces:
1. Fly fishing: fly lines, false and roll casts, shooting line, mending, stripping, the fly-tying bench.
2. **The Madison** and Beartooth lakes: current seams, hatches (salmonfly, mayfly, caddis), drift boat, wading.
3. **The Columbia**: salmon and steelhead runs, white sturgeon, shad.
4. **The Oregon Coast**: surf with breaking waves, kelp simulation, rockfish with barotrauma and the descender,
   lingcod, halibut, Dungeness crab rules.

**DoD:** Fly casting signed off by the owner; tier-1 species of these places catchable by their real methods. 🎮

## M14: Home salt water II, Chesapeake and Outer Banks, the Keys, big game

Pieces:
1. **Homosassa**: the flats skiff, poling platform and push pole, sight fishing for rolling tarpon, cobia, tripletail
   at trap buoys, sharks; polarized glasses.
2. **Nature Coast Ledges and Wrecks**: the bay boat, groupers diving for holes, snapper, amberjack, the reef permit,
   red snapper's season, lionfish bounty.
3. **The Middle Grounds and Deep Edge**: the sportfisher, trolling spreads, outriggers, teasers, drop-backs, fighting
   chair, weedlines, bait balls and birds; deep-drop with electric reels.
4. **Chesapeake and Outer Banks**: striped bass, blue crabs, snakehead bounty, surf fishing for big red drum.
5. **The Florida Keys**: bonefish, permit, tarpon on the flats, the reef, sailfish, and daytime deep-drop swordfish.
6. Big-game fight systems: boat-side leadering, tail rope, release; sharks.
7. Act 2 story beats through the permit hearing.

**DoD:** Act 2 is playable; a Keys grand slam is possible; a sailfish on the troll is thrilling. 🎮

## M15: Act 3, restoration and the finale

Remaining restoration projects with visible changes at home, **Kona** (Pacific blue marlin, tuna, ulua from the lava
rocks, Kalani), the **Comoros** (night deep handline fishing with Mzé, the Lantern rise visuals, the coelacanth
finale with the descender release), the ending and credits. **DoD:** the full story is playable start to finish. 🎮

## M16: Post-game, property, legends

The post-game destinations (Rio Negro, Thailand, Northern Norway, England and Spain, Northern Australia) with their
communities and camps, the complete property system, the full legendary set, the tournament circuit, slam challenges,
guiding clients. **DoD:** tier 2 and 3 content is in and the Ledger is completable.

## M17: Polish and release readiness

A performance pass on the owner's laptop, an accessibility review, complete settings, localization readiness,
tutorial polish, a bug bash, save migration tests, a credits and licences audit (everything commercial-safe), Steam
Deck checks (controller-only play, 1280×800 readability), and a release build. **DoD:** the owner calls it done.

## M18: Co-op (2 players online)

Built on the rules followed since M0 (`02_TECH_STACK.md` §10): host-authoritative world, each player's own tackle
simulated locally, hooked-fish authority handed to the angler who hooked it, shared or split camera framing, a
two-player boat, co-op-aware quests and saves. Networking through Steam networking or Godot's ENet, decided then.
**DoD:** two players can fish the same spot, share a boat, and each fight their own fish without lag. 🎮

---

## Piece checklist template (copy into commit messages and reports)

```
Piece: M3.4 Lure flight, entry and skipping
Changed: …
Tests: dotnet test (N passed); replays: skip_cast.rec (3 skips, 4.2 m)
Screenshots: looked at …; what they show: …
Owner should try: …
Known issues: …
```
