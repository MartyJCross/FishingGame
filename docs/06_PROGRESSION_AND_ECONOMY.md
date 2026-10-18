# 06: Progression, Economy, World Systems

## 1. Progression philosophy

- **Four currencies of progress:** money (gear, boats, travel, property), **Trust reputation** (unlocks restoration
  projects, permits and story), **knowledge** (the Ledger: hints, behaviour notes, legendary leads, Wren's pages), and
  **access** (boats, licences, destinations, properties). None of them improves the player's hands.
- **Every unlock opens a new way to fish**, not just a bigger number: the rowboat opens the spring run, the
  baitcaster opens skip casting heavy cover, fly gear opens the Madison, the skiff opens sight fishing, the
  sportfisher opens big game.
- **No grind walls.** Pacing targets: first boat within about 1.5-2 hours; end of Act 1 around hour 10; main story
  35-45 hours; full Ledger and post-game 120+ hours.
- **Release is rewarded.** Selling fish earns money; surveys, tagging and releases earn Trust reputation and research
  grants. Both are viable; the best players do both.
- **Hardcore physics, honest information.** No difficulty preset softens the sim (CLAUDE.md rule 12). Progression
  never sells "easier fights"; it sells the right tool for the job.

## 2. Unlock path (main line)

| Stage | Unlock | Opens | How |
|---|---|---|---|
| 0 | Spincast combo, worms, floats, the casting lawn | Station Pond from the bank and dock | Start |
| 1 | Spinning combo, starter lure box | Lure fishing at the pond | First survey |
| 2 | **The rowboat *Teal*** (restored with Teodor) | The Spring Run; the pond's middle | Money + a Teodor quest chain. **The first big goal.** |
| 3 | Florida saltwater licence, cast net, crab traps | Waccasassa Flats and Oyster Bars (wading and by rowboat on calm days) | Act 1 |
| 4 | Baitcaster | Heavy cover, skip casting heavy jigs, big lures | Pell's quest |
| 5 | **Jon boat** with tiller outboard | The Suwannee, longer flats runs | Money |
| 6 | First travel: **Lake of the Woods** (truck + lodge) | Walleye, pike, musky; ice fishing in winter | Wren's first page |
| 7 | **Atchafalaya** trip | Swamp fishing, crawfish traps, the marsh | Wren's second page |
| 8 | Bass boat (fast, trolling motor, livewells, sonar) | Tournaments; fast lake travel | Money + tournament wins |
| 9 | Fly rod and fly-tying bench; travel to the **Madison** | Fly fishing | Act 2 |
| 10 | **Flats skiff** with poling platform | Homosassa tarpon grounds; later the Keys | Money + Agnes's quest |
| 11 | Bay boat / centre console | Nature Coast ledges and wrecks; Chesapeake | Money |
| 12 | Reef permit, descender device | Gulf bottom fishing (descender required, real practice) | Trust reputation |
| 13 | Travel: **Columbia and Oregon Coast**, **Chesapeake and Outer Banks**, **Florida Keys** | Salmon, sturgeon, kelp coast, stripers, surf drum, flats grand slam | Act 2 |
| 14 | **Sportfisher** (Agnes's old boat): outriggers, fighting chair, tower; highly migratory species permit | The Middle Grounds and Deep Edge; Keys offshore | End of Act 2 |
| 15 | Electric reels, deep-drop rigs, night lights; travel to **Kona** and the **Comoros** | Big game and the finale | Act 3 |
| 16 | Post-game travel | Amazon, Thailand, Norway, England and Spain, Australia | After the finale |

**Travel** is a map-room action: pick a destination, pay the trip (fuel or flights, lodging), choose which gear goes
(a baggage limit makes gear choice matter), and arrive at that destination's base. Boats at destinations are rented
or come with the lodge, except where a property is bought. Time passes during travel.

## 3. Gear

All gear is data (`game/data/gear/`) with real-world parameters that feed the sim directly (`04`). Gear is **sold in
tiers with honest trade-offs**, not strictly better or worse. A cheap fibreglass rod is more forgiving than an
expensive fast graphite rod; a high-ratio reel is fast but winches weakly. Brands are invented; never use real
brand names.

### 3.1 Rods

Parameters: type (spincast, spinning, casting, fly, surf, trolling, stand-up, deep-drop, ice), length, **power**
(UL, L, ML, M, MH, H, XH), **action** (extra-fast to slow), material (fibreglass, graphite by modulus, composite,
split-cane bamboo for fly), line rating, lure rating, mass, sensitivity, guide count and quality, handle (cork, EVA,
split grip, fighting butt), breaking moment profile, price.

**Rod building** (Pell's workshop, unlocked in Act 1): choose a blank (length, power, action, material), guides
(count, size, quality: they affect cast distance, line wear and the bend curve), reel seat, handle and grip length
(casting leverage), and cosmetics: thread wraps (colours, patterns), blank colour, decals, your name on the butt. A
custom rod is exactly as good as its parts, never magic.

### 3.2 Reels

Types: spincast, spinning, baitcaster (low-profile, round), conventional (star and lever drag, two-speed), fly,
electric. Parameters: gear ratio, max drag, drag smoothness (static/kinetic ratio), spool diameter and width, line
capacity, bearings (spool friction: cast distance), brakes (baitcaster: centrifugal or magnetic, adjustable), clicker
or bait-runner, weight, saltwater sealing (unsealed reels corrode in salt: a maintenance stat), price. Cosmetics: body
colour, handle knobs, spool colour.

### 3.3 Line and terminal tackle

Line (`04` §3.1) bought per spool in many strengths and colours; leaders (fluorocarbon, mono, wire, single-strand
titanium); knots (improved clinch, Palomar, uni, double uni, FG, Bimini twist, haywire twist for wire) with real
strength factors per line type (an optional "tie it" choice in the rig menu); hooks (J, circle, octopus, treble,
weedless, wide-gap, aberdeen, live-bait; sizes; light or heavy wire; barbed or barbless); sinkers; floats; swivels;
snaps; rattles; beads.

### 3.4 Lures and baits

- **Lures** by category: topwater (popper, walker, prop bait, buzzbait, frog), crankbait (square-bill, shallow,
  medium, deep), lipless crankbait, jerkbait, spinnerbait, bladed jig, inline spinner, spoon, jig (football, swim,
  flipping, hair, bucktail), soft plastics (worm, creature, craw, tube, grub, swimbait, fluke, stick bait), hard
  swimbait, glide bait, metal jig, sabiki, trolling skirts, plugs, teasers. Each has physical parameters and colour
  patterns.
- **Lure customization:** paint shop (base colour, back, belly, pattern, flake, UV), hook swaps (treble to single:
  lower hookup, better release survival), split rings, rattles in or out, skirt colours, trailers. Colour choice
  matters through the perception model (`05` §3.1), never through hidden numbers.
- **Baits:** worms (nightcrawlers, red worms, bloodworms), crickets, maggots and wax worms, minnows and wild shiners,
  crayfish, cut bait (from your own catches: real and efficient), shrimp (live or dead), crabs, squid, clams, sand
  crabs, fiddlers, eels, live mullet, pinfish, pilchards, herring, ballyhoo, corn, boilies, bread, dough, chicken
  liver, stink bait, fruit (Amazon). Live bait lives in a bait bucket or livewell and dies if neglected (water
  temperature and oxygen; a light model).
- **Catching your own bait** is a mini-loop: cast net for shrimp, mullet and pilchards, sabiki for herring and
  pinfish, dig sand crabs, hand-catch fiddlers, trap crayfish, catch small legal panfish.

### 3.5 Fly tying

A workbench at the station: choose hook, thread, body, rib, hackle, wing, tail, bead or weight, and colours. The
result has real properties (floats or sinks, sink rate, profile, colour, action) and imitates a category (mayfly
dun, emerger, caddis, stonefly, salmonfly, midge, terrestrial, baitfish streamer, leech, crab, shrimp, mouse).
Classic patterns are recipes taught by the Ledger and Pell (Adams, Elk Hair Caddis, Pheasant Tail, Woolly Bugger,
Clouser Minnow, Crazy Charlie, Merkin crab, Lefty's Deceiver, Sofa Pillow for salmonflies).

### 3.6 Electronics and tools

Sonar (2D down-imaging: real fish "arches", bait clouds, the thermocline, bottom hardness), later side imaging and
live forward-facing sonar; GPS spots; polarized sunglasses (cut surface glare, so fish in shallow water show crisper
and are easier to identify); headlamp and boat lights; thermometer; barometer; tide tables; bite alarms; rod holders;
net, lip grip, pliers, dehooker, descender, measuring board, scale, camera; cooler and livewell sizes; waders; ice
gear (auger, shelter, tip-ups) for Lake of the Woods.

## 4. Boats

| Boat | Role | Key traits |
|---|---|---|
| **Rowboat *Teal*** | First boat | Oars (physical rowing), silent, slow, small, tippy. Upgrades: rod holders, anchor, small electric motor |
| **Jon boat** | Rivers, swamps, calm flats | Flat-bottomed, shallow draft, tiller outboard, rugged |
| **Bass boat** | Lakes, tournaments | Fast planing hull, bow trolling motor, livewells, sonar, casting decks |
| **Flats skiff** | Homosassa, the Keys | Very shallow draft, **poling platform** (higher eye point), push pole, quiet |
| **Bay boat / centre console** | Ledges, wrecks, bays | Handles chop, longer range, livewell, outriggers optional |
| **Sportfisher** | Offshore, Kona, the Comoros | Fighting chair, outriggers, tuna tower (spot birds and fish), long range, bait tanks |
| **Local boats** | Destinations | Rented or with the lodge: a Lake of the Woods walleye boat, an Atchafalaya mud boat, a drift boat on the Madison, a Comorian outrigger canoe-style boat with Mzé |

Boat upgrades: motors (horsepower, noise), trolling motor (thrust, spot-lock), sonar, livewell size and aeration,
cooler, rod holders and racks, lights, anchors and drift socks, poling platform, tower, hull paint, decals, **boat
name**. Fuel is a light model: bought at docks; running out means rowing home or paying a tow.

## 5. Property

Owned places with **exclusive water**, upgrades and utility. Each property's water has its own protected population,
so it holds bigger fish if you manage it well. Bought with money, sometimes gated by reputation.

| Property | Where | Its water | Upgrades and utility |
|---|---|---|---|
| **Cedar Shoals Field Station** | Home | The pond, the pier and the marsh creek | Restores through the story: Ledger room, aquarium tank (live fish you caught), trophy wall, workshop, fly-tying bench |
| **Spring Cabin** | Home, the Spring Run | A private stretch of spring run | Dock, kayak rack, night lights; snook hideaway in cold snaps |
| **Suwannee River Camp** | Home, the Suwannee | A private bend with a deep limestone hole | Jon-boat ramp, trotline, campfire |
| **Island Cottage** | Home, near Cedar Key | A private flat and oyster bar | Skiff dock, crab traps, artificial reef you can deploy offshore |
| **Lake of the Woods Cabin** | Lake of the Woods | A rocky bay with a reef | Ice house in winter, brush piles, boat lift |
| **Bayou Stilt House** | Atchafalaya | An oxbow lake | Crawfish trap line, trotlines, bait tanks, night lights |
| **Madison River Lodge** | Madison River | A private stretch of bank | Wading paths, drift-boat access, fly bench |
| **Keys Bungalow** | Florida Keys | A private flat and mangrove creek | Skiff dock, lobster traps, hammock |
| **Kona Hale** | Kona | A lava-rock shore spot for ulua | Rod holders on the rocks, night lanterns, boat mooring |
| **Post-game camps** | Each post-game destination | One special spot each | Bunkhouse, local guide, gear lockers |

Every property gives **sleep** (skip to a chosen time and save), **storage** (gear lockers), **fast travel** between
owned properties at the same destination, **trophy display**, and a small decoration layer (furniture, colours, the
dog's bed).

## 6. Trust reputation and restoration (home)

Reputation comes from surveys, tagged releases, research tasks, invasive removals, protected species handled right,
data quests and story beats. It unlocks **restoration projects at home**. They cost money plus reputation, and change
the coast permanently. Every one is a real kind of restoration:

| Project | Effect |
|---|---|
| Oyster reef rebuild | Clearer water, more sheepshead, drum, seatrout; oyster bars reappear at low tide |
| Seagrass replanting | More juvenile fish, scallops, redfish and seatrout on the flats; clearer water |
| Spring clean-up (nutrient reduction) | Clearer spring water, eelgrass returns, manatee sightings |
| Living shoreline and mangrove planting | Snook and tarpon nurseries; more juveniles in the creeks |
| Artificial reefs | New offshore structure; more snapper and grouper |
| Invasive removal | Fewer lionfish; natives recover |
| Fishing line recycling bins | Fewer tangled birds and turtles (a small story beat each) |
| Dredge permit defeated (story) | The flats are saved; the Grayline marina never happens |

The coast visibly changes: water colour, plant density, fish numbers, birds, NPC dialogue, and the station itself.
Destinations have smaller conservation tasks (tagging studies, invasive bounties) but no permanent restoration.

## 7. Missions and quests

| Type | Example |
|---|---|
| **Story** | Acts 1-3 (`01` §6) |
| **Survey** (Trust) | "Catch, measure and release 5 redfish on the flats." "Log 3 species in the Spring Run after a cold front." |
| **Tagging** | Tag-and-release red drum, tarpon and striped bass; some tagged fish are re-caught later in other places, and you get the story of where they went |
| **Research tasks** (Ledger) | Per species: catch at night, on a fly, over a trophy size, observe spawning, photograph a behaviour |
| **Orders** | Fish house and restaurant orders: "Three sheepshead, 1-3 kg, fresh, by Friday" (freshness matters) |
| **Pell's challenges** | Gear mastery: "Skip a jig under the boathouse dock and catch a bass there" |
| **Rival challenges** | Dace's head-to-heads; Big Ruth's catfish night |
| **Tournaments** | Bass (5-fish limit, weigh-in, penalty for dead fish), catfish night, inshore slam, billfish release, ice derby on Lake of the Woods |
| **Bounties** | Invasive species: lionfish, snakehead, silver carp, blue catfish in the Chesapeake, carp in the Murray |
| **Legend leads** | Rumours → clues → conditions → the encounter |
| **Wren's pages** | Held by people at destinations, in bottles, at old camps, behind research milestones |
| **Junie's bets** | Light-hearted side contests |

Quest data lives in `game/data/quests/` (objectives as composable conditions: species, size, place, method, time,
weather, release/keep, photo). Dialogue is in `game/data/dialogue/` (simple branching, with conditions and effects).

## 8. Licences and regulations

Real-style rules give structure and teach real practice. **They're based on each place's real rules as written when
the data is authored, simplified, and frozen.** Each regulation data file records the source and date it was based
on. A notice in the game (settings, the regs card and the first licence purchase) says: *"Regulations in this game
are simplified and may be out of date. Always check current local rules before fishing."*

- **Licences per destination,** simplified to what that place really requires: for example a freshwater and a
  saltwater licence at home, a non-resident licence at Lake of the Woods, and so on. Where a real place requires no
  recreational licence for a kind of fishing, the game doesn't either.
- **Permits:** reef fish (with a descender required, real practice in the Gulf), highly migratory species (tunas,
  billfish, sharks), trap permits.
- **Regulations per species and place** (data): minimum and maximum sizes, slot limits, daily bag limits, closed
  seasons (Gulf red snapper's short season), protected species (release only, no gaffs, keep in the water), gear rules
  (circle hooks where required, non-stainless hooks for reef fish), invasive rules (must not be released alive).
- **The wildlife officer** checks licences and coolers at random. Violations bring fines and reputation loss, never a
  game over. A first violation at each destination is a warning with an explanation.
- A **regs card** in the journal shows the rules for where you stand.

## 9. Time, seasons and weather

### 9.1 Time

- Day length default 24 real minutes (setting 12-60). The sun and moon follow a simple astronomical model, with
  sunrise and sunset shifting by season **and latitude** (long summer days at Lake of the Woods and in Norway;
  the midnight sun and polar night up north).
- **Moon phase** cycle (default 28 game days per lunar month, tunable): night light, spring/neap tides, and lunar
  behaviour (tarpon, snook, some spawning).
- **Seasons:** spring, summer, autumn and winter, 7 game days each by default (tunable). The calendar is global,
  and each destination has its own climate. Northern hemisphere seasons apply at most destinations; **the Rio Negro
  has wet and dry seasons instead, and Northern Australia has the wet and the dry**. Water temperatures lag air
  temperatures. Deep lakes stratify in summer, turn over in autumn, and freeze at Lake of the Woods in winter.
  Florida winters bring cold fronts, not ice.

### 9.2 Weather

A front-driven weather model per destination, from a seed:

- States: high pressure (clear, light wind) → pressure falling (clouding, wind rising, often an active bite) → front
  (rain, storms, wind shift) → post-front (clear, bright, cold, windy: the slowest fishing) → settling. Tropical
  places have afternoon thunderstorms and trade winds instead of strong fronts.
- Variables: wind speed and direction, cloud cover, rain or snow, fog, air temperature, pressure, visibility. They
  feed the sim (waves, casting drift, line bow, boat drift), fish behaviour (light, temperature, turbidity after
  rain, a modest pressure-trend effect) and rendering.
- **Hazards:** lightning (warnings; graphite rods conduct; get off the water), small-craft advisories, fog (navigation
  by compass and GPS), heat (live bait dies faster, fish stress, lower release survival), thin ice early and late in
  winter, and a tropical storm watch at home in late summer (a reason to stay in, not a disaster scene).

## 10. Events (all real phenomena, at their real places)

| Event | Where and when | What happens |
|---|---|---|
| **Spawning beds** | Station Pond, spring | Bass and sunfish on visible beds; sight fishing; the game encourages releasing nest guarders |
| **Tarpon migration** | Homosassa and the Keys, May-June | Strings of tarpon rolling at dawn |
| **Mullet run** | Nature Coast, autumn | Huge mullet schools; every predator follows |
| **Cold snap** | Nature Coast, winter fronts | Snook and other fish pile into warm springs; fish sluggish on the flats |
| **Red tide** | Gulf coast, some late summers | Fish move away or die; dead-fish smell; avoid; a sobering, rare event |
| **Gag grouper move inshore** | Nature Coast, autumn and winter | Grouper on shallow ledges |
| **Ice-out** | Lake of the Woods, spring | Fish move shallow and feed |
| **Walleye spawn run** | Lake of the Woods rivers, spring | Fish stack in current |
| **Lake turnover** | Lake of the Woods, autumn | A few days of poor fishing and murky water |
| **Silver carp jumping** | Atchafalaya, any time a motor runs | Fish leap out of the water at boat noise (real) |
| **Salmonfly hatch** | Madison River, early summer | Giant stoneflies; trout feed recklessly on the surface |
| **Mayfly and caddis hatches** | Madison River, summer evenings | Trout rise everywhere; match the hatch |
| **Salmon and steelhead runs** | Columbia River, spring to autumn | Fish stack in the river; aggressive strikes |
| **Shad run** | Columbia and Chesapeake, spring | Great light-tackle fishing |
| **Bait ball blitz** | Chesapeake, Keys offshore, Kona, the Comoros | Birds dive, water boils, predators crash bait |
| **Bioluminescent nights** | Calm summer lagoons at home, and the Comoros | Every cast and fish glows (a real dinoflagellate phenomenon) |
| **The Lantern rise** | The Comoros, every night | The diel vertical migration, the finale's stage |
| **Humpback season** | The Comoros, winter | Whales breaching (sighting) |
| **Aurora** | Northern Norway, winter nights | Visual |
| **Midnight sun** | Northern Norway, summer | It never gets dark |
| **Meteor shower** | Clear nights, a few dates | Visual only |
| **Tournaments** | Scheduled | See §7 |

## 11. Economy

- **Selling:** the fish house buys by species, weight and **condition**: fresh on ice is full price, the price decays
  with time out of ice, and damaged fish (gaffed badly, bitten by sharks) sell for less. Prices fluctuate weekly by
  supply (flooding the market with one species lowers its price) and orders. At destinations, fish sell to a local
  buyer or are released (some destinations are release-only by tradition or law).
- **Costs:** gear, bait (consumable), licences, fuel, ice, travel, lodging, boat rental, repairs (rods, reels,
  boat), maintenance (salt corrosion without rinsing), property and upgrades, restoration contributions, fines.
- **Other income:** survey grants, tournament prizes, orders, bounties, trap catches (crabs, stone crab claws and
  lobster are good money), property income (a bait concession at the station), and guiding NPC clients (post-game:
  you take a client out and they have to catch the fish).
- **Balancing tool:** `tools/economy_sim.py` simulates a typical player's earnings per hour at each stage from the
  data and flags gaps (like a stage where nothing affordable improves fishing). Keep the target unlock times from §1.

## 12. The Ledger (field guide UI)

- One page per species: a render of the fish in Wren's drawing style (generated from the species mesh with an ink
  shader), names, family, the real facts, your records (count, largest, smallest, total weight, released, method,
  place), research tasks, Wren's note, a world map of where it lives, and its activity clock (when it's active,
  unlocked by observation).
- Knowledge levels unlock hints gradually: "Unknown" → "Seen" → "Caught" → "Studied" (research tasks) →
  "Mastered".
- Pages for sightings (manatee, Gulf sturgeon, Atlantic sturgeon, great hammerhead, humpback, lanternfish) and for crustaceans.
- Collections: per destination, per family, the slams, legendary individuals.
