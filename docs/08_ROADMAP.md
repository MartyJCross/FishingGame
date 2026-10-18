# 08: Build Roadmap

## How to use this roadmap

- Build **in order**. Each milestone depends on the ones before it. Within a milestone, do the pieces in order and
  **commit + push after each piece** (CLAUDE.md §4, §6). Tick the milestone in CLAUDE.md §8 when its Definition of
  Done (DoD) is met.
- **The core comes first.** Milestones M1-M7 are all about making one pond feel incredible. Don't start the meta
  game (M9+) until the owner has played the vertical slice (end of M6) and the feel pass (M7) is done.
- **Playtest gates** (🎮) are where the owner plays. Stop, hand over a build with a clear "what to try" list, and wait
  for feedback before the next milestone. Expect tuning rounds there.
- If a DoD item turns out wrong or impossible, say so, propose the fix, and update this doc. Don't silently skip it.

---

## M0: Foundations

Pieces:
1. Repo skeleton (§3 of CLAUDE.md), `.gitignore` (`.godot/`, `bin/`, `obj/`, `build/`, `asset-downloads/`,
   `*.user`), `README.md`, `CHANGELOG.md`, `game/assets/CREDITS.md`.
2. Install Godot .NET (latest stable 4.x) + export templates; pin versions in `02_TECH_STACK.md` §2 and CLAUDE.md §5.
3. `sim/` class library, `tests/` xUnit project referencing it, one trivial passing test. `game/` Godot .NET project
   referencing `sim/`. A `Main.tscn` that opens a window with the version label.
4. `SimDriver` (fixed 120 Hz tick, snapshot interpolation), `InputFrame` + `InputRouter` stub, input recording and
   replay file format (`.rec`: header with version + seed, then per-tick packed InputFrames).
5. Command-line args: `--light`, `--screenshot`, `--frames`, `--scene`, `--time`, `--weather`, `--seed`, `--replay`,
   `--smoke`, `--bench`, `--debug-overlay`.
6. Debug overlay framework (immediate-mode 2D drawing of sim state) and the F1 tuning panel framework (reflection
   over `SimConfig` fields → sliders; save/load `game/data/tuning/sim.json`).
7. CI workflow (`02_TECH_STACK.md` §6) producing the `Longwater-windows-x64` artifact.
8. Determinism test scaffold and the zero-allocation tick test scaffold.

**DoD:** CI green on `development`, artifact downloads and runs, `--screenshot` produces a PNG, `--smoke 5` passes
headless, tuning panel opens with F1.

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
screenshots look good already (look at them).

## M2: Rod and line

Pieces:
1. `Hands` kinematic driver with arm limits; mouse absolute mode, mouse relative mode, and right-stick mapping.
2. `Rod`: XPBD chain, stiffness profile from power/action, damping, guides; data for 3 rods (UL spinning, M spinning,
   MH casting).
3. `Line`: variable-length rope with unilateral segments, materials (mono, fluoro, braid), air/water drag, buoyancy,
   surface film; a fixed length out for now with a dummy weight at the end.
4. Guide line-path forces bending the rod under load.
5. Rendering of rod and line (§8 of the art doc), debug overlay for tensions and moments.
6. Tests: rod deflection bands, stretch, sag, determinism, zero allocations.

**DoD:** Swinging the rod with a weight on the line feels physical: the rod loads and whips, the weight swings, the
line sags when slack and snaps tight. Both mouse and controller drive it.

## M3: Reel, casting, entry, skipping, snags

Pieces:
1. `Spool`/`Reel`: line capacity and radius, cranking (wheel, RMB, RT), cranking power limit, drag (static/kinetic,
   detents), bail/line hold (LMB, LT analog).
2. Spinning casts: line hold release, pay-out friction, feathering; spincast variant.
3. Baitcaster: spool inertia, brakes, thumb pressure, backlash detection and the clear-tangle interaction.
4. Lure flight: 2-node lure body, air drag, wind; water entry energy, splash, disturbance events; **skipping**.
5. Terrain and structure collision, snags (sites, pins, freeing by angle/pop/force), line wear, line breaks.
6. Casting lawn practice scene with accuracy hoops and a skip dock.
7. Tests: distance bands table, backlash/no-backlash replays, skip replay, snag freeing, break at weakest point.

**DoD:** Overhead, sidearm, pitch and skip casts all work and feel distinct; baitcaster rewards thumbing and punishes
sloppiness; snags happen and can be freed; replays prove it.

## M4: Lures, rigs and retrieves

Pieces:
1. Lure hydrodynamics: buoyancy classes, lip dive + wobble, spoon flutter, spinner blades, soft plastic tails, jig
   fall rates; lateral pseudo-state.
2. Topwater: popper, walker (walk-the-dog), prop bait, frog; surface interaction through the patch.
3. Terminal rigs as sub-chains: split shot, sliding sinker (Carolina), drop shot, Texas rig, float and slip float with
   bobber stop, bottom rigs, hair rig. Rig menu (R / X).
4. Baits: worm, minnow (live, as a tiny agent), cut bait, corn/dough; bait hold and tear-off on hard casts.
5. Lure data for ~12 lures and ~6 baits; low-poly lure meshes.
6. Tests: crankbait depth vs speed / line out / line diameter, walk-the-dog alternation, float bite display sanity,
   bait tear-off threshold.

**DoD:** Every retrieve in `03` §5 can be performed and looks right underwater. A float rig set to 1.5 m sits at 1.5 m.

## M5: Fish (bodies, senses, bites)

Pieces:
1. Species data loader + schema validation tests; shape specs; 6 tier-S species files fully sourced (bluegill,
   largemouth bass, channel catfish, common carp, golden shiner, and black crappie or pumpkinseed).
2. `FishBody` spine physics, swimming modes, C-start, fins; `FishMeshBuilder` + `fish_skin` shader; turning;
   `fish_lineup` scene.
3. Senses (vision with clarity/light/colour/contrast, lateral line, hearing, smell plume), spook model.
4. Brain: states, habitat selection by comfort, schooling (shiners, bluegill), the strike decision, bite styles,
   suction feeding and hold/reject.
5. Spot population for Millbrook Pond: habitat cells, agents spawn/despawn around the lure and camera, size from
   growth curves.
6. Tests: behaviour tests from `05` §4.4 that apply to these species, suction, C-start timing, lineup screenshot.

**DoD:** Fish swim beautifully and believably (the owner should be able to tell a carp from a bass by how it moves),
investigate lures, follow, refuse, and bite in species-specific ways. Bites read clearly on the rod, line and float.

## M6: Hookset, fight, landing → 🎮 vertical slice

Pieces:
1. Hook-hold model (J, circle, treble; barbless), hookset from rod motion with line-stretch effects.
2. Fight model: thrust, behaviours (run, dive, jump, headshake, structure dash, sulk), stamina pools, temperature
   effect; jumps through the surface.
3. Failures: line break, bite-off (bass teeth are small: mostly abrasion), rod break (high-stick), hook pull, wrapped
   in structure.
4. Landing: hand/lip, net (timing + position), the after-catch flow (measure, weigh, photo, unhook, release with
   revive, keep in a bucket); a simple catch log (temporary Ledger).
5. Audio pass 1: reel, drag, line hiss, splashes, rises, pond ambience (CC0 + synthesis, credited).
6. Time of day basic (sun moves, palette blend between day and night) and its effect on fish activity.
7. Tests: hook model tests from `04` §12, fight sanity (a 2 kg bass on 10 lb mono at 25% drag is landable; at 100%
   drag with a surge, the line breaks), release survival effects.

**DoD / 🎮 gate:** Millbrook Pond is genuinely fun for 30 minutes with three rods (spincast, spinning, baitcaster),
a handful of lures and baits, six species, day and night. Hand over with a "what to try" list: skip a jig under the
dock, walk a topwater at dusk, fish for catfish at night, beat a carp on light line.

## M7: Feel pass, controller, assists

Pieces:
1. Owner feedback round from M6 (tuning in data, not code).
2. Gamepad polish: stick curves, trigger curves, rumble design for every event (`03` §2.2), remapping UI.
3. Assists: casting arc, tension meter, bite indicator, auto-drag (warn-only and full), toggle-hold; **Purist mode**.
4. Accessibility basics; settings menu with input, audio, video, assists.
5. Training drills on the casting lawn (Odile's radio lines placeholder).

**DoD:** The owner signs off on feel with both mouse and controller. 🎮

## M8: Art pass on the pond

Pieces:
1. `LowPolyBuilder` and generators: terrain, trees, reeds, lily pads, rocks, dock, the station exterior.
2. Sky shader, palettes with all six keyframes for Millbrook, re-shading of low-poly by light direction.
3. Water: near band with rings and wakes, far-water band with reflection and glitter, god rays, caustics,
   particulates, bubbles.
4. Weather visuals: rain, wind, fog, storm (lightning + thunder), overcast grading.
5. Angler model with IK and lean, the dog, rod/reel/lure art.
6. Post stack and quality presets; renderer decision (Mobile vs Compatibility) with a benchmark on the owner's PC at
   a time they choose; record the result in `02_TECH_STACK.md`.
7. Screenshot set per `07` §11.

**DoD:** Screenshots at four times × three weathers look beautiful (the AI looks at each one and the owner
approves); 60 FPS at 1080p Medium on the owner's laptop. 🎮

## M9: Meta layer

Pieces:
1. Save/load with versioning and migrations; settings persistence.
2. Inventory, money, the station hub (simple first version): Pell's shop, Fish Market, Trust office.
3. Gear stats from data for all M2-M4 gear plus new tiers; rod/reel/line purchase and equip; lure paint shop basic.
4. The Ledger UI (ink shader renders, records, research tasks, knowledge levels).
5. Licences and regulations (size/bag limits, protected species), Officer Sato basic.
6. Economy: prices, freshness, orders; `tools/economy_sim.py`.

**DoD:** A new game starts at the station, fishes the pond, sells fish, buys gear, fills Ledger pages, saves and loads.

## M10: Boats and Harrow Reservoir

Pieces:
1. Boat physics (`04` §8), rowboat with oars, anchor; boarding and fishing from a boat.
2. Teodor's restoration quest for the *Teal* (simple quest system v0).
3. Region map and travel; Harrow Reservoir spots (drowned forest, points, deep basin, dam face) with thermocline.
4. Reservoir species (tier 1 from `05` §7.2), sonar (2D), trolling, drifting, vertical jigging.
5. Night fishing tools (headlamp, boat light).

**DoD:** Row out on the reservoir, find structure with sonar, catch walleye at dusk on a jig, troll for lake trout
under the thermocline.

## M11: Story Act 1, NPCs, property, tournaments

Pieces:
1. Dialogue system (data-driven branching), NPC presence at the station, radio messages from Odile.
2. Quest system v1 (composable objectives), survey and research quests, Wren's pages.
3. Act 1 story beats; the weir hearing.
4. First property: Kestrel Point Cabin (sleep, storage, brush piles, fast travel).
5. Bass tournament (5-fish limit, livewell, weigh-in scene, Dace).
6. Kesh Tarn region with ice fishing (frozen surface, auger, tip-ups, burbot at night) if seasons are in scope.

**DoD:** Act 1 playable start to end (~8 hours of play). 🎮

## M12: Freshwater complete

Glass Creek with fly fishing (fly line, false/roll casting, mending, stripping, fly tying bench, current seams), the
weir removal event and salmon runs, Blackwater Bayou (jon boat, murky water, gar/bowfin/catfish, crawfish traps,
snakehead bounty, Big Ruth), seasons with spawning behaviour, more properties (Fly Lodge, Stilt House). **DoD:** all
tier-1 freshwater species catchable by their real methods; fly casting signed off by the owner. 🎮

## M13: Saltwater I

Harrow Sound Estuary (tides and tide tables, tidal currents, oyster bars, bridges, dock lights, cast net, crab pots,
blue crabs), Gull Rock Coast (surf with breaking waves, pier, kelp sim, rockfish with barotrauma and descender,
Dungeness rules), saltwater gear corrosion, bay boat, Cannery Pier and Lighthouse properties, Act 2 first half.
**DoD:** tier-1 estuary and coast species; traps loop works; tides change the spots visibly.

## M14: Saltwater II

Calder Keys (skiff, poling, sight fishing, polarized glasses, tailing fish), Calder Reef (deep structure, groupers
diving for holes, chumming), Blue Water (sportfisher, trolling spreads, outriggers, teasers, drop-back, fighting chair,
kite fishing, weedlines, bait balls and blitzes, birds), big-game fight systems (boat-side leadering, tail rope,
release), sharks. **DoD:** tier-1 species of all three regions; a blue marlin fight is the most exciting thing in the
game. 🎮

## M15: Story Acts 2-3 and the finale

Remaining Act 2 beats (Sound protection vote, Grayline), restoration projects with visible world changes, the
Lantern Trench (electric reels, deep-drop, the night rise visuals), the coelacanth finale, the ending and credits.
**DoD:** the full story playable start to finish. 🎮

## M16: Expeditions, property, post-game

The five expeditions with their communities, expedition camps, the complete property system, legendary fish set,
tournament circuit, Grand Slam challenges, guiding clients. **DoD:** tier 2 and 3 content in, Ledger completable.

## M17: Polish and release

Performance pass on the owner's laptop, accessibility review, settings completeness, localization pass readiness,
tutorial polish, bug bash, save migration tests, credits and licences audit, release build and store-page assets if
the owner wants to publish. **DoD:** the owner calls it done.

---

## Piece checklist template (copy into commit messages / reports)

```
Piece: M3.4 Lure flight, entry and skipping
Changed: …
Tests: dotnet test (N passed); replays: skip_cast.rec (3 skips, 4.2 m)
Screenshots: looked at …; what they show: …
Owner should try: …
Known issues: …
```
