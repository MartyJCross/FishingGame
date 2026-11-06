# Changelog

Newest first. Each version handed to the owner gets an entry: what changed, what to try, known issues.

## v0.18.0: Real sound (2026-11-06)

- **Field recordings** replace the synthesised ambience: Florida pond days (Mount Dora, St. Marks), the dawn chorus
  (St. Augustine), night frogs and insects (Clearwater, Gainesville), water lapping at the bank, wind in the trees,
  light and heavy rain, and a flowing river at the Suwannee. They crossfade with the hour, the weather and the spot.
- **Recorded one-shots:** lure plops and splashes sized by the impact, fish jumping, thunder after lightning, herons,
  egrets, a kingfisher, sandhill cranes and an eagle by day, barred owls and a pig frog at night, Skiff's bark.
- **Music:** five CC0 tracks (guitar, folk, ambient), one at a time with a minute or two of quiet between, fading out
  while a fish is on. The reel, drag, line and rod sounds stay synthesised from the physics.
- **The icon:** a fish leaping at sunset, on the exe and the window (v0.17.2).
- Every recording is CC0 or public domain, checked on its own page (game/assets/audio/SOURCES.md); all are credited
  on the Credits page, with Godot's licence.
- Not playtested (owner's call): the levels are a first guess, tell me what's too loud or quiet.

## v0.17.1: The angler, more of Act 1, rebinding (2026-11-06)

- **The angler moves:** leans into the cast and back against a fish, the reel hand turns the handle as you reel, the
  head follows the lure, plus a fishing vest and sunglasses.
- **Four new story steps:** Junie's Saturday weigh-in (a 42 cm bass from anywhere, $40), the night shift (a channel
  catfish and an American eel after dark, $50), the annual report (12 species in your Ledger, $60) and the Lantern
  (a snook at Kings Bay after dark, $80), each with a new page of Wren's. Your save keeps its place in the story.
- **Key rebinding:** Settings, Controls. Click a binding and press the new key or mouse button; reset to defaults.
  The key hints and Odile's lessons show your keys.
- **Credits** on the title menu.
- Not playtested this round (owner's call): built only.

## v0.17.0: Earn your gear, and a guide for the first hour (2026-11-06)

- **Gear costs real money now.** You start with $15, the medium spinning outfit, a sinker and a worm under a bobber.
  Lures run $5-16, outfits $90-240, and Junie's board pays less. The Trust now pays for each survey you finish
  ($30, $50, $60), so the story is your main income, with the board and the cooler on top.
- **Gear comes in tiers.** Junie stocks more as the story moves on. The jig, popper and ultralight come after the
  first survey; the crankbait and a 10 lb fluoro outfit after the Teal; a 20 lb braid outfit and the baitcaster after
  the spring run survey. Locked gear shows in the shop with what brings it in, and every item says what it's for.
- **Two new outfits:** the 7' spinning rod with 10 lb fluorocarbon, and with 20 lb braid.
- **A broken line loses the lure.** Bought lures are gone when the line snaps (snags too); buy another at Junie's.
  The sinker and the worm rig are always in the box.
- **Odile's first lessons** (top left): open the bail, cast, close the bail, watch the bobber, fish on, land it, keep
  or release, the field notes, the survey. Each one finishes when you do it. Losing a fish sends you back to the
  cast with a word of encouragement. Anglers with an existing save skip them.
- **A goal line, always on screen** (top left): the current field-notes goal and progress, where to travel, or how
  much to save and how.
- **One-off tips** the first time things come up: the cooler and selling fish, Junie's board, new stock in the shop,
  a new spot on the map, night fishing, snags, a bird's nest, having enough money to pay for the Teal. H hides the
  lessons and tips (the goal line stays).

## v0.16.0: A finer simulation (2026-10-31)

- Fish bodies bend on twice as many spine points, so turns and swimming curves are smooth instead of jointed.
- The water surface is simulated in 2.5 cm cells instead of 5 cm: crisper ripples, splashes and wakes.
- The physics runs at 240 steps a second instead of 120. The reel, spool, hooks, fish and water update twice as
  often; casts, drag, hooksets and fights measure the same as before (your mouse cast still flies ~20 m).
- Tried and left out: cutting the fishing line finer (7 cm pieces) made spinning casts ~20% shorter; the line stays
  at 10 cm (owner's choice).

## v0.15.1: Memory check (2026-10-30)

- Checked for memory leaks: 12 scene reloads (the travel and sleep path) and a two-minute session both hold steady
  at the same object, node and memory counts, with no orphaned nodes. `--leak-check N` repeats the check.
- The water renderer reuses its buffers instead of making new ones every frame (less garbage to collect, fewer
  hitches).
- A story radio message can no longer fire into a scene that was already left (sleeping or travelling right after
  finishing a step).

## v0.15.0: Junie's board, the Suwannee, and proper fish shapes (2026-10-30)

- **Junie's board** (top right): three requests from Junie at the bait stand each day: a size bet on one species,
  a release count for the Trust at a given spot, and either a variety day or a catfish or eel after dark. They pay as
  soon as they're done, and your money shows on the board. A new board every game day.
- **New spot: the Suwannee, Old Town bend.** Tea-dark blackwater under cypress and limestone, with Florida gar,
  bowfin, warmouth, bullheads, American eels, mullet, spotted sunfish, bass and catfish. A cypress deadfall and a
  limestone shelf hold fish (and snag hooks). Its legend is Old Ironsides, a 90 cm Florida gar, in summer and
  autumn. The spring run survey now opens it ($9 drive), and a new story step, "The old river", surveys it, with a
  new page of Wren's.
- Station Pond also holds warmouth, a bowfin and one big stocked grass carp (must be released).
- Gar, bowfin, eels, needlefish, snook, sheepshead, mullet and grass carp now have their real body shapes (long
  beaks, deep bodies, eel fins) instead of a generic outline.

## v0.14.0: The catch moment, type and menus (2026-10-30)

- Landing a fish brings down a field-guide card: the fish's picture, its common and scientific names, length and
  weight, what it's worth, and stamps for a NEW SPECIES, a PERSONAL BEST or a LEGEND. It says Kept or Released when
  you decide, then slides away.
- New type throughout: Fraunces (a soft serif) for titles and fish names, Fira Sans for the HUD and menus.
- The title and pause menus show the scene without the HUD, with a big title and paper-style buttons.
- H hides or shows the key hints under the tension bar (remembered).

## v0.13.0: A living world (2026-10-30)

- Flocks of small birds cross the sky by day, and now and then a great egret flaps slowly past low over the water.
- Dragonflies dart and hover over the shallows on warm, bright days.
- Fireflies blink over the bank on warm nights.
- Rain falls as streaks that slant with the wind, with a soft hiss on the water. Thunderstorms flash with lightning,
  and the thunder follows: a crack for close strikes, a long rumble for distant ones.

## v0.12.0: Water and light underwater (2026-10-30)

- God rays fall through the water from the sun's side (slanting the other way in the evening), shimmering and fading
  with depth; murky water swallows them sooner than the clear springs.
- Caustics ripple across the lake bed in sunlit shallows. The bed, plants, logs and rocks fade into the water's
  colour with depth, so the clear springs show far and the pond goes murky fast.
- The top of the water reflects the sky (blue by day, orange at sunset, dark at night) with the far shore's dark
  reflection along the back, a glitter path of twinkling sparkles under the sun or moon, and wind streaks.
- Drifting specks in the water, a bright underside to the surface, and proper low-poly boulders at Kings Bay.

## v0.11.0: Sky and light (2026-10-30)

- A real sky: the sun rises on the left and sets over the far shore, with warm sunrises, golden hours and sunsets,
  a blue hour, then a blue-black night with a moon and twinkling stars. Sunrise and sunset times follow the season.
- Clouds drift with the wind; their amount follows the weather (a few puffs on clear days, a heavy grey deck in a
  thunderstorm), lit warm at sunset and dark underneath in storms.
- The far shore is three layers of hills, hammock trees and scrub that fade into the haze with distance and slide
  past at different speeds as the camera moves. Kings Bay gets cypress and waterfront houses. Mist lies on the water
  at first light and in rain.
- The light on the bank, angler and water follows the sky's colour, and the screen has a soft vignette.

## v0.2.0: M1 Water surface (2026-10-18)

- The pond's water is simulated. Wind makes real-sized waves: a breeze on a small pond gives centimetre ripples, and a
  storm gives choppy whitecaps. The numbers come from the standard wind-wave formulas for that wind and that much open
  water.
- Anything that touches the water pushes it: ripples spread at the right speed, fade, and are absorbed at the edges
  instead of bouncing back. Splashes throw droplets that fall back and dimple the surface. Rain pocks the whole
  surface.
- Ripple rings are drawn exactly where the simulation was hit.
- Try: middle-click the water to drop stones; F4 toggles rain; F1 → "Debug conditions" to change wind, fetch and
  swell live. Start with `-- --weather storm` (or calm, breeze, windy, rain) from the command line.
- Known: the water, sky and bank are placeholders until the art pass (M8); there's no rod or line physics yet (M2).

## v0.1.0: M0 Foundations (2026-10-18)

- The game project exists: Godot 4.7.2 (.NET) with the simulation in its own C# library and automatic tests.
- What you can do: open it and swing a placeholder rod with the mouse or the right stick. F3 shows the debug
  overlay (including your controller's sticks and triggers), F1 opens the live tuning panel.
- Every push builds a Windows version on GitHub (artifact "TheLedger-windows-x64").
- Try: plug in the GameSir, press F3, and check it's listed and its triggers move.

## Plan revision (2026-10-18)

- Applied the owner's answers to all 15 design questions: the game is now **The Ledger**, set in **real places**
  (a Florida Nature Coast home base plus world travel) with the logbook world-tour story and a Comoros finale;
  hardcore physics with information-only assists; fish mostly visible underwater; co-op-ready architecture and a
  co-op milestone (M18); the species list regrouped by real place (about 250 species); the roadmap reordered.

## Plan (2026-10-18)

- Design and build plan written: vision and world, tech stack, fishing mechanics, physics spec, fish and ecology,
  progression and economy, art direction, roadmap, open questions, and the project constitution (`CLAUDE.md`).
