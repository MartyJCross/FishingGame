# 06: Progression, Economy, World Systems

## 1. Progression philosophy

- **Four currencies of progress:** money (gear, boats, property), **Trust reputation** (unlocks restoration
  projects, licences and story), **knowledge** (the Ledger: hints, behaviour notes, legendary leads), and **access**
  (boats, licences, properties, regions). None of them improves the player's hands.
- **Every unlock opens a new way to fish**, not just a bigger number: the rowboat opens offshore structure, the
  baitcaster opens skip-casting heavy cover, fly gear opens the creek, the skiff opens sight fishing, the
  sportfisher opens big game.
- **No grind walls.** Pacing targets: first boat within ~1.5-2 hours; the end of Act 1 around hour 8; main story
  30-40 hours; full Ledger and post-game 100+ hours.
- **Release is rewarded.** Selling fish earns money; **surveys, tagging and releases** earn Trust reputation and
  research grants. Both are viable; the best players do both.

## 2. Unlock path (main line)

| Stage | Unlock | Opens | How |
|---|---|---|---|
| 0 | Spincast combo, worms, bobbers, the casting lawn | Millbrook Pond from the bank and dock | Start |
| 1 | Spinning combo, lure starter box | Lure fishing at the pond | First survey |
| 2 | **The rowboat *Teal*** (restored with Teodor) | Harrow Reservoir; the pond's middle | Money + a Teodor quest chain. **The first big goal.** |
| 3 | Baitcaster | Heavy cover, skip casting heavy jigs, big lures | Pell's quest |
| 4 | Freshwater licence upgrade + trout stamp; fly rod | Glass Creek | Act 1 surveys |
| 5 | Ice gear (auger, short rods, tip-ups, shelter) | Kesh Tarn in winter | Act 1 story |
| 6 | Jon boat with tiller outboard | Blackwater Bayou | Money |
| 7 | Saltwater licence, cast net, crab pots | Harrow Sound Estuary | Act 2 |
| 8 | Bass boat (fast, trolling motor, livewells, sonar) | Tournaments, the whole reservoir fast | Money + tournament wins |
| 9 | Surf gear | Gull Rock beach and jetty | Act 2 |
| 10 | **Flats skiff** with poling platform | Calder Keys | Money + Agnes's quest |
| 11 | Bay boat / centre console | Gull Rock reefs, Calder Reef | Money |
| 12 | Reef permit, descender device | Calder Reef bottom fishing | Trust reputation |
| 13 | **Sportfisher** (*Agnes's old boat*) with outriggers, fighting chair, tower; HMS permit | Blue Water | End of Act 2 |
| 14 | Electric reels, deep-drop rigs, night lights | The Lantern Trench | Act 3 |
| 15 | Seaplane charter | Expeditions | Post-Act 2 side line, then post-game |

## 3. Gear

All gear is data (`game/data/gear/`) with real-world parameters that feed the sim directly (`04`). Gear is **sold in
tiers with honest trade-offs**, not strictly better/worse. A cheap fibreglass rod is more forgiving than an expensive
fast graphite rod; a high-ratio reel is fast but weak.

### 3.1 Rods

Parameters: type (spincast, spinning, casting, fly, surf, trolling, stand-up, deep-drop, ice), length, **power**
(UL, L, ML, M, MH, H, XH), **action** (extra-fast to slow), material (fibreglass, graphite by modulus, composite,
split-cane bamboo for fly), line rating, lure rating, mass, sensitivity, guide count and quality, handle (cork, EVA,
split grip, fighting butt), breaking moment profile, price.

**Rod building** (Pell's workshop, unlocked in Act 1): choose a blank (length/power/action/material), guides (count,
size, quality: affects cast distance, line wear and the bend curve), reel seat, handle and grip length (casting
leverage), and cosmetics: thread wraps (colours, patterns), blank colour, decals, your name on the butt. A custom rod
is just as good as its parts, never magic.

### 3.2 Reels

Types: spincast, spinning, baitcaster (low-profile, round), conventional (star and lever drag, two-speed), fly,
electric. Parameters: gear ratio, max drag, drag smoothness (static/kinetic ratio), spool diameter and width, line
capacity, bearings (spool friction: cast distance), brakes (baitcaster: centrifugal/magnetic, adjustable), clicker
or bait-runner, weight, saltwater sealing (non-sealed reels corrode in salt: a maintenance stat), price.
Cosmetics: body colour, handle knobs, spool colour.

### 3.3 Line and terminal tackle

Line (`04` §3.1) bought per spool and in many strengths and colours; leaders (fluorocarbon, mono, wire, single-strand
titanium); knots (improved clinch, Palomar, uni, double uni, FG, Bimini twist, haywire twist for wire) with real
strength factors per line type (a light, optional "tie it" choice in the rig menu); hooks (J, circle, octopus,
treble, weedless, wide-gap, aberdeen, live-bait; sizes; light or heavy wire; barbed or barbless); sinkers; floats;
swivels; snaps; rattles; beads.

### 3.4 Lures and baits

- **Lures** by category: topwater (popper, walker, prop bait, buzzbait, frog), crankbait (square-bill, shallow,
  medium, deep), lipless crankbait, jerkbait, spinnerbait, chatterbait, inline spinner, spoon, jig (football,
  swim, flipping, hair, bucktail), soft plastics (worm, creature, craw, tube, grub, swimbait, fluke, stick bait),
  hard swimbait, glide bait, metal jig, sabiki, trolling skirts, plugs, teasers. Each has physical parameters and
  colour patterns.
- **Lure customization:** paint shop (base colour, back, belly, pattern, flake, UV), hook swaps (treble to single:
  lower hookup, better release survival), split rings, rattles in/out, skirt colours, trailers. Colour choice matters
  through the perception model (`05` §3.1), never through hidden numbers.
- **Baits:** worms (nightcrawlers, red worms, bloodworms), crickets and grasshoppers, maggots/wax worms, minnows,
  shiners, crayfish, frogs (artificial only where live is unethical), cut bait (from your own catches: real and
  efficient), shrimp (live or dead), crabs, squid, clams, sand crabs, fiddlers, eels, live mullet, pilchards,
  herring, ballyhoo, corn, boilies, bread, dough, chicken liver, stink bait, fruit (expeditions). Live bait lives in
  a bait bucket or livewell and dies if neglected (water temperature and oxygen, light model).
- **Catching your own bait** is a mini-loop: cast net for shrimp and mullet, sabiki for herring and pilchards,
  dig for sand crabs, hand-catch fiddlers, trap crayfish, catch small panfish (legal-size rules apply).

### 3.5 Fly tying

A workbench at the station: choose hook, thread, body, rib, hackle, wing, tail, bead/weight and colours. The result
has real properties (floats or sinks, sink rate, profile, colour, action) and imitates a category (mayfly dun,
emerger, caddis, stonefly nymph, midge, terrestrial, baitfish streamer, leech, crab, shrimp, mouse). Classic
patterns are recipes the Ledger and Pell teach (Adams, Elk Hair Caddis, Pheasant Tail, Woolly Bugger, Clouser
Minnow, Crazy Charlie, Merkin crab, Lefty's Deceiver).

### 3.6 Electronics and tools

Sonar (2D down-imaging: real fish "arches", bait clouds, thermocline line, bottom hardness), later side imaging and
live sonar (forward-facing, high-end); GPS spots; polarized sunglasses (cut surface glare: see into shallow water
better; a real, important tool for sight fishing); headlamp and boat lights; thermometer; barometer; tide tables;
bite alarms; rod holders; net, lip grip, pliers, dehooker, descender, measuring board, scale, camera; cooler and
livewell sizes; waders (wade the creek and flats).

## 4. Boats

| Boat | Role | Key traits |
|---|---|---|
| **Rowboat *Teal*** | First boat | Oars (physical rowing), silent, slow, small, tippy. Upgrades: rod holders, anchor, small electric motor |
| **Jon boat** | Bayou, rivers | Flat-bottomed, shallow draft, tiller outboard, rugged |
| **Bass boat** | Lakes, tournaments | Fast planing hull, bow trolling motor, livewells, sonar, casting decks |
| **Flats skiff** | Calder Keys | Very shallow draft, **poling platform** (higher eye point), push pole, quiet |
| **Bay boat / centre console** | Estuary, coast, reef | Handles chop, bigger range, livewell, outriggers optional |
| **Sportfisher** | Blue Water, the Trench | Fighting chair, outriggers, tuna tower (spot birds and fish), big range, bait tanks |
| **Research vessel (Trust)** | Story finale | Deep winch, ROV camera, the night lights |

Boat upgrades: motors (horsepower, noise), trolling motor (thrust, spot-lock), sonar, livewell size/aeration, cooler,
rod holders/racks, lights, anchors and drift socks, poling platform, tower, hull paint, decals, **boat name**.
Fuel exists (light model: bought at docks; running out means rowing/paddling home or a tow fee).

## 5. Property

Owned places with **exclusive water**, upgrades, and utility. Property is bought with money and sometimes gated by
reputation. Each property's water has its own protected population, so it holds bigger fish if you manage it well.

| Property | Region | Its water | Upgrades and utility |
|---|---|---|---|
| **Longwater Field Station** | Home | The pier and the slough behind it | Restores through the story: Ledger room, aquarium tank (live fish you caught), trophy wall, workshop, fly-tying bench |
| **Kestrel Point Cabin** | Harrow Reservoir | A point with a sharp drop-off and drowned timber | Brush piles you sink to make structure (real practice), dock lights, boat lift |
| **Glass Creek Fly Lodge** | Glass Creek | A private beat (a stretch of river with exclusive rights, a real tradition) | Riverside hut, wading paths, a gillie bench where Pell teaches |
| **Cypress Stilt House** | Blackwater Bayou | An oxbow lake | Trotlines, crawfish trap line, night lights, bait tanks |
| **Old Cannery Pier** | Harrow Sound | A long pier into the tidal channel | Pier lights for night snook and tarpon, bait shop concession (passive income), crab pots |
| **Gull Rock Lighthouse** | Gull Rock Coast | The rocky point and its surf beach | Telescope (spot bird activity offshore), lighthouse beam (night fishing light) |
| **Calder Key Bungalow** | Calder Keys | A private flat and mangrove creek | Skiff dock, fly bench, hammock (sleep) |
| **Outer Calder Mooring** | Blue Water | Base for offshore | Fuel, ice, bait freezer, FAD (a fish attracting device you deploy; real) |
| **Expedition camps** | Each expedition | One special spot each | Bunkhouse, local guide, gear lockers |

Every property: **sleep** (skip to a chosen time and save), **storage** (gear lockers), **fast travel** between owned
properties, **trophy display**, and a small decoration/customization layer (furniture, colours, the dog's bed).

## 6. Trust reputation and restoration

Reputation comes from surveys, tagged releases, research tasks, invasive removals, protected-species handling done
right, data quests, and story beats. It unlocks **restoration projects**, which cost money plus reputation and then
change the world permanently:

| Project | Effect |
|---|---|
| Glass Creek weir removal (story) | The river's profile and pools change; salmon and steelhead runs begin |
| Harrow fish ladder | Lake-to-river migration of species, better walleye spawning |
| Mangrove replanting | Snook and tarpon nurseries; clearer water in the Keys; more juveniles |
| Oyster reef restoration | Clearer estuary water, more sheepshead, drum, seatrout |
| Brush piles and reefs | New structure in reservoirs |
| Sound protection zone (story) | Grayline's trawlers leave; striped bass and red drum recover |
| Invasive control | Fewer snakeheads/lionfish, recovering natives |
| Kelp restoration | Healthier rockfish and lingcod populations |

The world visibly changes: water colour, plant density, fish numbers, birds, NPC dialogue, and the station itself.

## 7. Missions and quests

| Type | Example |
|---|---|
| **Story** | Acts 1-3 (`01` §6) |
| **Survey** (Trust) | "Catch, measure and release 5 brook trout in Glass Creek." "Log 3 species below the thermocline in August." |
| **Tagging** | Tag-and-release striped bass; some tagged fish are re-caught later in other places (and you get the story of where they went) |
| **Research tasks** (Ledger) | Per species: catch at night, on a fly, over a trophy size, observe spawning, photograph a behaviour |
| **Orders** | Restaurant and market orders: "Three black drum, 1.5-3 kg, fresh, by Friday" (freshness matters) |
| **Pell's challenges** | Gear mastery: "Skip a jig under the boathouse dock and catch a bass there" |
| **Rival challenges** | Dace's head-to-heads; Big Ruth's catfish night |
| **Tournaments** | Bass (5-fish limit, weigh-in, live fish penalty for dead fish), catfish night, inshore slam, billfish release, ice derby |
| **Bounties** | Invasive species: snakehead, lionfish |
| **Legend leads** | Rumours → clues → conditions → the encounter |
| **Wren's pages** | Finding the logbook: in bottles, at old camps, by research milestones |
| **Junie's bets** | Light-hearted side contests |

Quest data lives in `game/data/quests/` (objectives as composable conditions: species, size, location, method, time,
weather, release/keep, photo). Dialogue is in `game/data/dialogue/` (simple branching, with conditions and effects).

## 8. Licences and regulations

Real-world-style rules give structure and teach real practice.

- **Licences:** Freshwater (start), Trout stamp (Glass Creek), Saltwater, Trap/Crab permit, Reef permit, HMS
  (highly migratory species: tunas, billfish, sharks) permit. Bought at the Trust office or Pell's.
- **Regulations per species and region** (data): minimum size, maximum size, slot limits, daily bag limits, closed
  seasons (red snapper's short season), protected species (release only), gear rules (circle hooks required for
  some bait fisheries, no gaffing undersized or released fish, descender required at the reef), invasive species
  rules (must not be released alive).
- **Officer Sato** checks licences and coolers at random times. Violations bring fines and reputation loss, never a
  game over. A first violation is a warning with an explanation.
- A **regs card** in the journal shows the rules for where you stand.

## 9. Time, seasons and weather

### 9.1 Time

- Day length default 24 real minutes (setting 12-60). The sun and moon follow a simple astronomical model (sunrise
  and sunset times shift with the season).
- **Moon phase** cycle (default 28 game days per lunar month, tunable): night light levels, tides (spring/neap), and
  species with lunar behaviour (tarpon, snook, some spawning).
- **Seasons:** spring, summer, autumn, winter (default 7 game days each, tunable; OPEN_QUESTIONS Q5). Water
  temperatures lag air temperatures (deep lakes lag most). Lakes stratify in summer, turn over in autumn, freeze in
  winter (Kesh Tarn and Millbrook Pond in cold winters).

### 9.2 Weather

A front-driven weather model, simulated per region with a seed:

- States: high pressure (clear, light wind) → pressure falling (clouding, wind rising: often an active bite) →
  front (rain, storms, wind shift) → post-front (clear, bright, cold, windy: the slowest fishing, "bluebird
  lockjaw") → settling.
- Variables: wind speed and direction, cloud cover, rain/snow, fog, air temperature, pressure, visibility. They feed
  the sim (waves, casting drift, line bow, boat drift), fish behaviour (light, temperature, turbidity after rain,
  a modest pressure-trend effect) and rendering.
- **Hazards:** lightning (warnings; graphite rods are conductors; get off the water), small-craft advisories (high
  wind offshore), fog (navigation by compass and GPS, foghorns), heat (live bait dies faster, fish stressed, lower
  release survival), ice (thin-ice warnings early and late in winter).

## 10. Events (all real phenomena)

| Event | Where/when | What happens |
|---|---|---|
| **Mayfly hatch** | Glass Creek, late spring evenings | Duns on the surface; trout rise everywhere; match the hatch |
| **Salmon run** | Glass Creek, late summer-autumn (after the weir) | Fish stack in pools; aggressive strikes; an optional visual of a bear fishing upstream |
| **Shad run** | Lower river, spring | Shad school upriver; great light-tackle fun |
| **Mullet run** | Estuary and coast, autumn | Huge mullet schools; every predator follows |
| **Bait ball blitz** | Bay and offshore | Birds dive, water boils, tuna/jacks/stripers crash bait |
| **Spawning beds** | Ponds and lakes, spring | Bass and sunfish on visible beds; sight fishing (the game can encourage releasing nest guarders) |
| **Ice-out** | Lakes, early spring | Fish move shallow and feed |
| **Lake turnover** | Deep lakes, autumn | Poor fishing for a few days, murky water |
| **Tarpon migration** | Keys and estuary, early summer | Strings of tarpon rolling at dawn |
| **Full-moon tarpon/snook** | Passes and bridges | Night bites on the tide |
| **King tides** | Coastal, spring tides near perigee | Extreme highs and lows: flats exposed, new access |
| **Algae bloom / red tide** | Estuary and coast, hot calm weeks | Fish move away; avoid; dead fish (sobering, rare) |
| **Bioluminescent bay** | Calder Keys, summer nights | Every cast and fish glows (a real dinoflagellate phenomenon, a beautiful visual) |
| **The Lantern rise** | The Trench, every night | The diel vertical migration, the finale's stage |
| **Aurora** | Kaldfjord expedition, winter nights | Visual |
| **Meteor shower** | Clear nights, a few dates | Visual only |
| **Tournaments** | Scheduled | See §7 |

## 11. Economy

- **Selling:** the Fish Market buys by species, weight and **condition**: fresh on ice is full price, the price
  decays with time out of ice, and damaged fish (gaffed badly, bitten by sharks) sell for less. Market prices
  fluctuate weekly by supply (flooding the market with one species lowers its price) and orders.
- **Costs:** gear, bait (consumable), licences, fuel, ice, repairs (rods, reels, boat), maintenance (salt corrosion
  without rinsing), property and upgrades, restoration contributions, fines.
- **Other income:** survey grants, tournament prizes, orders, bounties, trap catches (crabs and lobster are good
  money), property income (the cannery bait concession), guiding NPC clients (post-game: you take a client out and
  they have to catch the fish).
- **Balancing tool:** `tools/economy_sim.py` simulates a typical player's earnings per hour by stage from the data and
  flags gaps (like a stage where nothing affordable improves fishing). Keep the target unlock times from §1.

## 12. The Ledger (field guide UI)

- One page per species: Wren's drawing style render of the fish (generated from the species mesh with an ink
  shader), names, family, the real facts, your records (count, largest, smallest, total weight, released, method),
  research tasks, Wren's note, a range/habitat strip, and the activity clock (when it's active, unlocked by
  observation).
- Knowledge levels unlock hints gradually: "Unknown" → "Seen" → "Caught" → "Studied" (research tasks) →
  "Mastered".
- Pages for sightings (oarfish, ocean sunfish, lanternfish) and for crustaceans.
- Collections: per region, per family, the slams, legendary individuals.
