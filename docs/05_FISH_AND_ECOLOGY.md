# 05: Fish and Ecology

## 1. The realism contract

1. **Every fish is a real species**, with its real common and scientific names.
2. **It lives where, when and how it really does**: water type and salinity, temperature, depth, habitat
   structure, time of day, season, tide and moon.
3. **It eats what it really eats and bites how it really bites.** Catching requirements are real angling
   requirements. A carp is caught on a hair-rigged boilie or corn on the bottom, not on a topwater frog. A bonefish
   is sight-cast on a rising tide. A flathead catfish wants live fish at night near wood. A sockeye salmon almost
   never strikes a lure.
4. **It fights how it really fights** (`03` §7.3) and **looks and swims like itself** (`04` §9, `07` §5).
5. **Data must be sourced.** Each species file has a `sources` list (FishBase, IGFA records, government fisheries
   agencies such as NOAA Fisheries and state/provincial agencies, peer-reviewed papers, reputable angling references
   for methods). Where sources disagree, prefer measured science for biology and multiple consistent angling
   sources for methods.
6. **Be honest about weak evidence.** Some angler beliefs are poorly supported (for example, strong effects of
   barometric pressure on feeding). Keep such effects **modest**. The strong drivers are temperature, light, prey
   availability, season and spawning, tide and current, and fishing pressure.
7. **Conservation is real.** Protected and endangered species are catch-and-release only (or sighting-only), with
   the real handling practices (descenders for barotrauma, no gaffs, keep in the water). The game teaches this
   without lecturing.

## 2. Species data schema (`game/data/species/<id>.json`)

The numbers below are **illustrative placeholders, not verified values**. Fill each species from sources and keep
the sources list complete. Tests validate the schema and sanity-check values.

```json
{
  "id": "largemouth_bass",
  "commonName": "Largemouth bass",
  "scientificName": "Micropterus salmoides",
  "family": "Centrarchidae",
  "tier": "S",
  "sources": [
    { "name": "FishBase species summary", "url": "https://www.fishbase.se/..." },
    { "name": "IGFA all-tackle record", "url": "https://igfa.org/..." }
  ],
  "water": {
    "salinityPpt": [0, 5],
    "temperatureC": { "lethalMin": 2, "optimum": [24, 30], "lethalMax": 36 },
    "oxygenMinMgL": 3.0,
    "clarityToleranceM": [0.3, 15]
  },
  "habitat": {
    "spots": ["millbrook_pond", "harrow_reservoir", "blackwater_bayou"],
    "depthM": [0.3, 8],
    "structure": { "weeds": 0.9, "wood": 0.9, "docks": 0.8, "lilyPads": 0.8, "rock": 0.5, "dropOff": 0.6, "openWater": 0.1 }
  },
  "size": {
    "lengthCm": { "typical": [25, 45], "max": 97 },
    "lengthWeight": { "a": 0.0, "b": 3.0, "units": "W_g = a * L_cm^b", "source": "FishBase LWR" },
    "growth": { "Linf_cm": 0, "k": 0, "maxAgeYears": 16, "source": "FishBase growth" },
    "recordWeightKg": 0
  },
  "activity": {
    "diel": { "dawn": 1.0, "day": 0.45, "dusk": 1.0, "night": 0.35 },
    "lunar": 0.1,
    "tide": null,
    "seasonal": { "winter": 0.25, "spring": 0.9, "summer": 0.8, "autumn": 1.0 },
    "weather": { "overcast": 0.15, "rain": 0.1, "postColdFront": -0.35, "risingWaterTemp": 0.2 }
  },
  "diet": {
    "items": { "fish": 0.5, "crayfish": 0.2, "frogs": 0.1, "insects": 0.1, "small_mammals": 0.02 },
    "preyLengthMaxFrac": 0.4
  },
  "feeding": {
    "mode": "suction_ambush",
    "biteStyles": { "inhale": 0.6, "crash": 0.3, "slam": 0.1 },
    "holdTimeS": [0.6, 1.8],
    "followChance": 0.15,
    "speedPreferenceMps": [0.1, 1.2],
    "lureAttraction": {
      "soft_plastic_worm": 0.9, "jig": 0.85, "spinnerbait": 0.8, "crankbait": 0.8, "topwater_walker": 0.85,
      "popper": 0.8, "frog": 0.85, "swimbait": 0.85, "jerkbait": 0.7, "inline_spinner": 0.55, "spoon": 0.5,
      "streamer_fly": 0.7, "popper_fly": 0.7
    },
    "baitAttraction": { "live_minnow": 0.9, "nightcrawler": 0.7, "live_crayfish": 0.85, "frog": 0.6, "cut_bait": 0.15 }
  },
  "senses": {
    "vision": 1.0, "spectral": { "r": 0.9, "g": 1.0, "b": 0.45 }, "lowLight": 0.5,
    "lateralLine": 0.9, "hearing": 0.6, "smell": 0.3, "electro": 0.0,
    "lineShy": 0.35, "spookiness": 0.5
  },
  "behaviour": {
    "schooling": "loose_small",
    "territorial": 0.5,
    "spawn": { "season": "spring", "tempC": [15, 21], "habitat": "shallow_firm_bottom", "mode": "nest_guard", "biteMotive": "defense" },
    "migration": null
  },
  "fight": {
    "powerCoeff": 1.0,
    "stamina": { "anaerobic": 1.0, "aerobic": 0.7, "recovery": 0.6 },
    "styles": { "jump": 0.35, "headshake": 0.3, "structureDash": 0.25, "run": 0.1 },
    "mouth": { "hardness": 0.4, "gapeFrac": 0.2, "teeth": 0.05, "gillPlateCut": 0.0 }
  },
  "body": {
    "locomotion": "subcarangiform",
    "spineNodes": 8,
    "stiffness": 0.5,
    "shapeRef": "shapes/centrarchid_bass.json",
    "pattern": { "base": "olive_green", "lateralBand": "dark_blotched", "belly": "cream" },
    "variants": ["juvenile", "spawning_male"]
  },
  "regulations": { "minLengthCm": 30, "bagLimit": 5, "closedSeason": null, "protected": false, "invasive": false },
  "economy": { "edible": true, "pricePerKg": 0, "marketDemand": "medium" },
  "ledger": {
    "facts": ["Largemouth bass can engulf prey up to ~40% of their own length."],
    "research": [
      { "id": "lmb_topwater", "task": "Catch one on a topwater lure at dawn" },
      { "id": "lmb_bed", "task": "Observe a male guarding a nest (photo)" },
      { "id": "lmb_trophy", "task": "Catch one over 4 kg" }
    ],
    "wrenNote": "Pressure them on a cold front and they'll sit tight in the weeds. Slow down. Slower than that."
  }
}
```

Other files: `game/data/baits/*.json`, `lures/*.json` (each lure has a `category` matching `lureAttraction` keys and
its physical parameters from `04` §6), `spots/*.json` (bathymetry, structure, snag sites, water properties, species
populations), `regions/*.json`.

**Data validation tests** (`tests/Data`): schema conformance, unique IDs, every referenced spot/lure/bait exists,
`tier` is S/1/2/3, the length-weight relation gives a weight within ±35% of `recordWeightKg` at `lengthCm.max`, every
species has at least two sources, spawn temperatures sit inside the tolerance range, and no species is placed in a
spot whose salinity or temperature range it can't tolerate.

## 3. Perception

Fish don't "see the player's lure" by magic. Each fish agent senses **stimuli** emitted by lures, baits, prey, the
angler, the boat and other fish.

### 3.1 Vision

- Detection range = `species.vision × waterClarity × lightFactor × contrast × size`, capped by the water's visibility
  distance.
- **Light:** time of day, cloud cover, moon phase, depth, surface chop, artificial lights (dock and bridge lights
  attract bait and predators at night: snook, tarpon, seatrout, striped bass).
- **Colour:** the lure's colour is attenuated per RGB channel by the water's absorption over depth plus viewing
  distance (red vanishes first, then orange and yellow). The result is weighted by the species' spectral
  sensitivity. Contrast against the background (bright surface above, dark bottom below, or open water) matters
  more than hue. This gives the real rules of thumb for free: natural colours in clear water, bright or dark
  high-contrast colours in stained water, dark silhouettes at night and against the surface.
- **Flash** from spinners and spoons in sunlight, **silhouette** from below against the surface.
- **Line visibility:** diameter × material visibility × clarity × light. Line-shy species (trout, bonefish, snapper,
  carp, tuna in clear water) refuse more often with thick, visible line close to the lure. Fluorocarbon leaders help.

### 3.2 Lateral line

Detects vibration and water movement at short range (a few body lengths) in any light and clarity. This is how fish
find lures in muddy water and at night. Spinnerbaits, crankbaits, rattles, paddle tails and struggling live bait are
strong emitters. Murky bayou and night fishing favour vibration.

### 3.3 Hearing

Rattles, pops, prop baits, splashes, boat motors, oars knocking, footsteps on a dock or deck, anchors dropping.
Attractive at low level, alarming at high level (§3.4).

### 3.4 Smell and taste

Bait emits a **scent plume** advected by the current and diffusing (a cheap particle/cell-based plume). Catfish,
carp, sharks, eels and drum follow plumes upstream. Scent also affects how long a fish holds a bait before
rejecting it.

### 3.5 Electroreception

Sharks and rays: short-range detection of buried or still prey (and a final strike accuracy bonus).

### 3.6 Disturbance and spooking

The **spook** state rises from: big splashes nearby, the angler's shadow or silhouette over shallow clear water,
boat noise and wakes, a fish fighting nearby (the school scatters), line touching the fish, a lure moving
unnaturally fast toward the fish, being hooked and released recently (individual memory), and fishing pressure on
the spot (accumulates over the day, decays overnight). Spooked fish flee with a C-start, stop feeding for a while,
and may move the school. **Stealth is a skill**: quiet entries, approach from downwind/downcurrent, low profile,
poling instead of motoring on the flats.

## 4. Decision making (the brain)

A **utility AI** with a small state machine on top. Each tick (staggered, 10-30 Hz per fish), compute utilities and
switch states with hysteresis.

### 4.1 Internal state

- **Hunger** (0-1): rises with metabolic rate (a temperature curve, roughly doubling per 10 °C toward the optimum),
  falls when feeding.
- **Comfort**: how well the current depth, temperature, oxygen, light and cover match preferences. Drives habitat
  selection (bass in shade cover at noon, walleye deep in daylight, lake trout below the thermocline in summer).
- **Activity**: the diel × lunar × tidal × seasonal × weather multipliers from data.
- **Spook**, **stamina**, **aggression/territoriality** (spawning guards attack intruders without being hungry).

### 4.2 States

`Rest` → `Cruise/Patrol` (within the home range/structure) → `Forage` (searching for natural prey) →
`Investigate` (a stimulus detected: approach) → `Follow` (track the lure, evaluating) → `Strike` (bite style from
data) → `Hold` (has the bait/lure in its mouth; will reject after `holdTimeS` or on resistance) → `Hooked` (fight
model) → `Exhausted` → `Released/Recovering` (low activity for a while). Plus `Flee`, `School` (follows its school's
leader with boids rules), `Spawn` (nest guard, run upstream, broadcast spawn), `SurfaceFeed` (rises, blitzes,
tailing on flats), `Air` (gulping air: gars, bowfin, arapaima, tarpon roll; visible tells).

### 4.3 The strike decision

When following, each evaluation tick compute:

```
p(strike) = activity × hungerTerm × match × naturalness × triggers × (1 − spook) × (1 − refusal)
  match       = attraction[lure category or bait] × size fit (vs gape, prey size) × colour/contrast fit
  naturalness = speed within speedPreference; action; depth relative to fish (most strike level or upward);
                for flies: drag-free drift vs the current; for bait: alive/fresh, scent
  triggers    = reaction boosts: sudden change of direction or speed (twitch, pause, deflection off cover,
                figure-8), fleeing prey cue (lure moving away), competition (school mates nearby),
                territorial intrusion (spawning)
  refusal     = line visibility, unnatural sinker/rig resistance felt during Hold, repeated presentations
                of the same lure (habituation), recent spook
```

The same model gives the real patterns: reaction strikes from inactive fish with fast, erratic lures; finesse wins
in cold water and after fronts; matching the hatch for trout; figure-8 for musky; feeding frenzies in blitzes.

### 4.4 Behaviour tests (examples)

- A walleye's strike odds at dusk on a rocky drop-off are ≥ 3× those at noon on a sand flat.
- A largemouth refuses a lure retrieved at 3 m/s; a bluefish chases one.
- Trout refuse a dry fly that drags faster than the local current by more than 0.15 m/s.
- A river-run chinook's strike odds come mostly from `triggers` (aggression), with near-zero hunger.
- A grass carp almost never strikes an artificial lure.
- With zero hunger, no trigger and high spook, no fish ever strikes.
- A channel catfish finds a cut-bait plume from downstream in muddy water at night.

## 5. Schooling and groups

Boids (separation, alignment, cohesion) with species parameters: tight bait balls (menhaden, sardines, herring)
that compress and flash when attacked, loose schools (perch, crappie, snapper), predator packs (striped bass, jacks,
tuna) that herd bait to the surface (**blitzes**: birds diving, water boiling; very visible surface events driven
through the water sim), and solitary ambushers (pike, musky, grouper, flathead). Hooking one fish affects the
school: some scatter, some (mahi, yellowtail snapper) follow the hooked fish, a real trick anglers use.

## 6. Populations, spawning and migration

- Each spot has **habitat cells** (a coarse grid over x and depth layers) with populations per species: counts by
  size class. Sizes come from age classes with the von Bertalanffy growth curve (`growth` data) plus individual
  variation, so trophy fish are genuinely rare.
- Populations follow seasons: spawning movements (to shallows, upstream, to the passes), **runs** as events (salmon
  in autumn up Glass Creek after the weir comes down, shad in spring, mullet in autumn, tarpon in early summer),
  thermocline and turnover movements in lakes, winter slowdown, ice cover.
- **Fishing pressure and depletion:** kept fish leave the population; released fish return with a survival chance
  (by hook location, fight length, water temperature, handling, barotrauma treatment). Populations regrow per
  season with a logistic rule. Over-keeping big fish at a spot visibly shifts its size distribution down. This makes
  release meaningful without preaching.
- **Restoration** projects (`06` §6) change habitat cells and populations (oyster reefs, mangrove nurseries, brush
  piles, fish ladders).

## 7. The species list

**Tiers:** **S** = vertical slice (M5-M6), **1** = v1.0 target, **2** = later content, **3** = expedition/post-game.
Target: about 200 catchable species, plus crustaceans and "sighting-only" species. The list is a starting point;
Opus should verify every row against sources when writing the species file, and may add real species that fit a
water body's community.

Column key: **Act** = activity (D diurnal, C crepuscular dawn/dusk, N nocturnal, T tide-driven). **Loco** =
locomotion (`04` §9.2): Ang anguilliform, Sub subcarangiform, Car carangiform, Thu thunniform, Ami amiiform, Bal
balistiform, Lab labriform, Raj rajiform, Fla flatfish, Ben benthic hopper.

### 7.1 Millbrook Pond (warm farm pond)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| S | Bluegill | *Lepomis macrochirus* | Sub/Lab | D | Worm or cricket under a small float, tiny jigs, small dry flies and poppers | Colonial nests in late spring; circles sideways on the line |
| 1 | Pumpkinseed | *Lepomis gibbosus* | Sub/Lab | D | Worms, small jigs near weeds | Eats snails |
| 1 | Green sunfish | *Lepomis cyanellus* | Sub | D | Worms, small spinners by rocks and wood | Big mouth for a sunfish |
| S | Largemouth bass | *Micropterus salmoides* | Sub | C | Topwater at dawn, Texas-rigged plastics in cover, frogs over pads, spinnerbaits, crankbaits | Guards nests in spring; dives into cover when hooked |
| 1 | Black crappie | *Pomoxis nigromaculatus* | Sub | C/N | Small jigs or live minnows under a float near brush and docks | Papery mouth tears |
| S | Channel catfish | *Ictalurus punctatus* | Sub | N | Cut bait, chicken liver, stink bait, nightcrawlers on a bottom rig | Hunts by smell and taste |
| 1 | Brown bullhead | *Ameiurus nebulosus* | Sub | N | Worms on the bottom over mud | Small, sturdy |
| S | Common carp | *Cyprinus carpio* | Sub | C/N | Hair-rigged corn, boilies or dough on the bottom; floating bread on the surface | Very wary; long powerful runs; introduced in the Americas |
| S | Golden shiner | *Notemigonus crysoleucas* | Sub | D | Tiny hooks with bread or dough | Bait fish |
| 2 | Grass carp | *Ctenopharyngodon idella* | Sub | D | Bread, fruit, plant baits; sight fishing | Herbivore stocked for weed control; often must be released |

### 7.2 Harrow Reservoir (large lake, drowned forest, thermocline)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Smallmouth bass | *Micropterus dolomieu* | Sub | D | Tubes, drop shots, jerkbaits, crankbaits on rocky points | Acrobatic jumper |
| 1 | Spotted bass | *Micropterus punctulatus* | Sub | D | Jigs, drop shots on deeper rock | |
| 1 | Walleye | *Sander vitreus* | Sub | C/N | Jig and minnow on drop-offs, crawler harnesses, trolled crankbaits at night | Light-sensitive eyes (tapetum); deep by day |
| 1 | Yellow perch | *Perca flavescens* | Sub | D | Small jigs, minnows, worms near bottom | Schools |
| 1 | Northern pike | *Esox lucius* | Sub | D | Spoons, inline spinners, jerkbaits, swimbaits, dead bait; wire leader | Teeth cause bite-offs |
| 1 | Muskellunge | *Esox masquinongy* | Sub | D | Big bucktails, glide baits, jerkbaits; figure-8 at the boat | Follows often; very rare strikes |
| 1 | White crappie | *Pomoxis annularis* | Sub | C | Small jigs and minnows over brush and timber | |
| 1 | White bass | *Morone chrysops* | Car | D | Spoons and small swimbaits into surface blitzes on shad | Schools |
| 1 | Lake trout | *Salvelinus namaycush* | Sub | D | Deep trolled spoons, vertical jigging below the thermocline | Needs cold water |
| 1 | Freshwater drum | *Aplodinotus grunniens* | Sub | D | Crayfish, worms or jigs on the bottom | Makes drumming sounds |
| 2 | Rock bass | *Ambloplites rupestris* | Sub | D | Small jigs and worms on rocks | |
| 2 | Lake whitefish | *Coregonus clupeaformis* | Sub | D | Small jigs and spoons deep, wax worms through ice | Soft mouth |
| 2 | White sucker | *Catostomus commersonii* | Sub | D | Worms on the bottom | Bait and Ledger |
| 2 | Gizzard shad | *Dorosoma cepedianum* | Car | D | Cast net (filter feeder, rarely bites) | Key forage |
| — | (plus pond species in coves) | | | | | |

### 7.3 Glass Creek (cold mountain river)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Rainbow trout | *Oncorhynchus mykiss* | Sub | D/C | Dry flies, nymphs, small spinners, worms in riffles and seams | Jumps |
| 1 | Steelhead | *Oncorhynchus mykiss* (sea-run) | Sub | D | Swung flies, drifted egg patterns, spoons in winter | Same species, sea-run life history; a separate Ledger entry |
| 1 | Brown trout | *Salmo trutta* | Sub | C/N | Nymphs, streamers; mouse flies at night for big fish | Very wary; introduced in North America |
| 1 | Brook trout | *Salvelinus fontinalis* | Sub | D | Small dries, nymphs, small spinners in cold headwaters | Actually a char |
| 1 | Cutthroat trout | *Oncorhynchus clarkii* | Sub | D | Dry flies, nymphs | Red throat slash |
| 1 | Arctic grayling | *Thymallus arcticus* | Sub | D | Small dry flies | Sail-like dorsal fin |
| 2 | Mountain whitefish | *Prosopium williamsoni* | Sub | D | Nymphs in riffles | |
| 2 | Bull trout | *Salvelinus confluentus* | Sub | C | Streamers in big pools | Protected: release only |
| 1 | Chinook salmon | *Oncorhynchus tshawytscha* | Sub | D/C | Spinners, plugs, cured roe drifted in runs (fish don't feed in the river, they strike from aggression) | Run event after the weir comes down |
| 1 | Coho salmon | *Oncorhynchus kisutch* | Sub | D | Spinners, flies | Acrobatic |
| 2 | Sockeye salmon | *Oncorhynchus nerka* | Sub | D | Very rarely strikes; small flies drifted at its level | Turns bright red with a green head in the river |
| 2 | Pink salmon | *Oncorhynchus gorbuscha* | Sub | D | Small pink spoons and jigs | Two-year life cycle: runs in alternate years |
| 2 | Chum salmon | *Oncorhynchus keta* | Sub | D | Flies, spoons | Tiger-striped spawning colours |
| 2 | Mottled sculpin | *Cottus bairdii* | Ben | N | Tiny baits; mostly observed | Trout food |

### 7.4 Kesh Tarn (glacial lake; ice in winter)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Arctic char | *Salvelinus alpinus* | Sub | D | Jigs and spoons, through the ice in winter | Spawning colours are spectacular |
| 1 | Burbot | *Lota lota* | Ang | N | Jigging spoons and dead minnows on the bottom at night, especially under ice | The only freshwater cod; spawns under ice in midwinter |
| 2 | Kokanee | *Oncorhynchus nerka* (landlocked) | Sub | D | Slow trolling with dodgers and small pink lures | Landlocked sockeye |
| 2 | Cisco | *Coregonus artedi* | Sub | D | Small jigs through the ice | |
| — | Lake trout, lake whitefish, brook trout | | | | | |

### 7.5 Blackwater Bayou (warm swamp)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Alligator gar | *Atractosteus spatula* | Sub (stiff) | C/N | Cut bait under a float, let it run, then set hard | Bony mouth; air-breathes (rolls at the surface) |
| 1 | Longnose gar | *Lepisosteus osseus* | Sub (stiff) | D | Rope lures that tangle in the teeth, small cut bait | |
| 2 | Spotted gar | *Lepisosteus oculatus* | Sub (stiff) | D | As above | |
| 1 | Bowfin | *Amia calva* | Ami | C/N | Spinnerbaits, frogs, cut bait in weedy backwaters | Ancient lineage; air-breathes; violent fighter |
| 1 | Flathead catfish | *Pylodictis olivaris* | Sub | N | Live fish on the bottom near wood | Rarely takes dead bait |
| 1 | Blue catfish | *Ictalurus furcatus* | Sub | N/D | Fresh cut shad or herring on the bottom in deep channels | Grows very large |
| 1 | Chain pickerel | *Esox niger* | Sub | D | Spoons and spinners along weed edges | |
| 2 | Warmouth | *Lepomis gulosus* | Sub | D | Worms, small jigs near cover | |
| 2 | Redear sunfish | *Lepomis microlophus* | Sub/Lab | D | Worms on the bottom | "Shellcracker": eats snails |
| 2 | Flier | *Centrarchus macropterus* | Sub/Lab | D | Tiny jigs, worms | |
| 2 | Yellow bullhead | *Ameiurus natalis* | Sub | N | Worms on the bottom | |
| 2 | Bigmouth buffalo | *Ictiobus cyprinellus* | Sub | D | Dough baits on the bottom (rare) | Filter feeder |
| 1 | American eel | *Anguilla rostrata* | Ang | N | Worms or cut bait on the bottom | Rolls and knots the line; conservation concern |
| 1 | Northern snakehead | *Channa argus* | Sub | D | Frogs and topwater in pads | **Invasive**: bounty, must not be released alive (real regulation in many areas) |
| — | Largemouth bass, black crappie, channel catfish | | | | | |

### 7.6 Harrow Sound Estuary (brackish delta, harbour, bridges)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Striped bass | *Morone saxatilis* | Car | C/N/T | Live eels, chunked menhaden, topwater and swimbaits in rips and at bridges | Anadromous |
| 1 | Red drum | *Sciaenops ocellatus* | Sub | T | Gold spoons, cut mullet, shrimp; sight-cast to tailing fish | Slot limit; spot on the tail |
| 1 | Black drum | *Pogonias cromis* | Sub | D/N | Crab or clam on the bottom near oyster bars | Loud croaker |
| 1 | Spotted seatrout | *Cynoscion nebulosus* | Sub | C/N | Popping cork with shrimp, topwater, soft plastics over grass | Soft mouth |
| 2 | Weakfish | *Cynoscion regalis* | Sub | C/N | Soft plastics, shrimp | Very soft mouth |
| 1 | Southern flounder | *Paralichthys lethostigma* | Fla | D/T | Slowly dragged jigs, live minnows on a Carolina rig; let it eat before setting | Ambusher buried in sand |
| 1 | Sheepshead | *Archosargus probatocephalus* | Sub | D | Fiddler crabs or barnacles on small strong hooks at pilings | Notorious bait thief; human-like teeth |
| 2 | Atlantic croaker | *Micropogonias undulatus* | Sub | D/N | Shrimp or bloodworms on the bottom | |
| 2 | Spot | *Leiostomus xanthurus* | Sub | D | Bloodworms on the bottom | |
| 1 | Pinfish | *Lagodon rhomboides* | Sub | D | Tiny hooks; a top live bait | |
| 1 | Striped mullet | *Mugil cephalus* | Sub | D | Cast net (rarely takes a hook) | Jumps; the autumn mullet run is a major event |
| 2 | Atlantic menhaden | *Brevoortia tyrannus* | Car | D | Cast net | The key bait fish |
| 1 | Bluefish | *Pomatomus saltatrix* | Car | D | Metal lures, poppers; wire leader | Razor teeth |
| 1 | Tarpon | *Megalops atlanticus* | Sub | N/C | Live mullet or crabs on circle hooks at bridges and passes; plugs; flies | Spectacular jumps; very hard mouth; catch and release |
| 1 | Common snook | *Centropomus undecimalis* | Sub | N | Live bait, jigs and flies under dock lights at night | Gill plates cut line; cold-sensitive; slot limit |
| 2 | Ladyfish | *Elops saurus* | Sub | D | Small jigs and spoons | Jumps constantly |
| 2 | Hardhead catfish | *Ariopsis felis* | Sub | D/N | Any bottom bait | Nuisance bycatch; venomous spines |
| 2 | Gafftopsail catfish | *Bagre marinus* | Sub | D | Cut bait | Slimy; long dorsal filament |
| 1 | Bull shark | *Carcharhinus leucas* | Car | N | Cut bait on circle hooks with wire leaders | Enters rivers |
| 2 | Atlantic stingray | *Hypanus sabinus* | Raj | D | Bycatch on bottom baits | Careful handling |
| 1 | White sturgeon | *Acipenser transmontanus* | Sub | N | Shrimp, cut bait or roe on the bottom in deep river holes | Can jump; ancient; legendary "Old Ironsides"; release only |
| 2 | American shad | *Alosa sapidissima* | Car | D | Shad darts and small spoons during the spring run | "Poor man's tarpon" |

### 7.7 Gull Rock Coast (cold kelp coast, surf beach, lighthouse)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Lingcod | *Ophiodon elongatus* | Sub | D | Big swimbaits, jigs, live bait on rocky reefs | Often grabs a hooked rockfish and won't let go |
| 1 | Cabezon | *Scorpaenichthys marmoratus* | Ben | D | Jigs and crab baits on rocks | |
| 2 | Kelp greenling | *Hexagrammos decagrammus* | Sub | D | Small jigs in kelp | |
| 1 | Black rockfish | *Sebastes melanops* | Sub | D | Small swimbaits and jigs, mid-water schools | Barotrauma: use a descender to release |
| 2 | Copper rockfish | *Sebastes caurinus* | Sub | D | Jigs on rocky bottom | Barotrauma |
| 2 | Yelloweye rockfish | *Sebastes ruberrimus* | Sub | D | Deep jigs | Protected in many areas: descend and release |
| 1 | Pacific halibut | *Hippoglossus stenolepis* | Fla | D | Big jigs, herring on circle hooks on the bottom | Huge; dangerous on deck |
| 2 | Starry flounder | *Platichthys stellatus* | Fla | D | Worms and small baits on sand | |
| 1 | Redtail surfperch | *Amphistichus rhodoterus* | Sub/Lab | D/T | Sand crabs or small grubs in the surf | Live-bearer |
| 2 | Striped seaperch | *Embiotoca lateralis* | Lab | D | Small baits at jetties and kelp | |
| 2 | Pile perch | *Rhacochilus vacca* | Lab | D | Mussels, crabs at pilings | |
| 1 | Pacific herring | *Clupea pallasii* | Car | D | Herring jigs (sabiki-style multi-hook rigs) | Bait |
| 2 | Pacific cod | *Gadus macrocephalus* | Sub | D | Jigs deep | |
| 2 | Pacific spiny dogfish | *Squalus suckleyi* | Car | D/N | Any bait | Schools; bait thief |
| 2 | Big skate | *Beringraja binoculata* | Raj | D | Bottom baits | |
| 1 | Chinook salmon (ocean) | *Oncorhynchus tshawytscha* | Sub | D | "Mooching" cut herring, trolling flashers and hoochies | Feeding ocean fish |

### 7.8 Calder Keys (warm flats and mangroves)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Bonefish | *Albula vulpes* | Sub | T/D | Sight-cast shrimp, or shrimp/crab flies ahead of tailing fish on the rising tide | Blistering runs; spooky; release |
| 1 | Permit | *Trachinotus falcatus* | Car | T/D | Live crab or crab flies, sight-cast | Famously hard to fool; release |
| 1 | Great barracuda | *Sphyraena barracuda* | Sub | D | Tube lures retrieved very fast; wire leader | Big reef fish risk ciguatera (don't sell) |
| 1 | Gray snapper | *Lutjanus griseus* | Sub | D/N | Live shrimp on light fluorocarbon near mangroves and docks | Wary |
| 1 | Lemon shark | *Negaprion brevirostris* | Car | D | Cut bait, sight fishing on flats | |
| 2 | Bonnethead | *Sphyrna tiburo* | Car | D | Crab or shrimp on flats | Small hammerhead |
| 2 | Blacktip shark | *Carcharhinus limbatus* | Car | D | Cut bait, live bait | Spinning jumps |
| 1 | Jack crevalle | *Caranx hippos* | Car | D | Poppers and jigs into blitzes | Dogged circles |
| 2 | Houndfish | *Tylosurus crocodilus* | Sub | D | Fast-moving small lures | Leaps; needle beak |
| 2 | Silver jenny | *Eucinostomus gula* | Sub | D | Tiny hooks | Bait |
| — | Tarpon (juveniles), snook, pinfish | | | | | |

### 7.9 Calder Reef (coral reef and wrecks)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Gag grouper | *Mycteroperca microlepis* | Sub | D | Live bait or big jigs; lock the drag and pull it off the bottom immediately | Dives for holes |
| 1 | Red grouper | *Epinephelus morio* | Sub | D | Cut bait, squid on the bottom | |
| 2 | Black grouper | *Mycteroperca bonaci* | Sub | D | Live bait on wrecks | |
| 2 | Nassau grouper | *Epinephelus striatus* | Sub | D | Bycatch on bottom baits | Critically endangered: release only |
| 1 | Goliath grouper | *Epinephelus itajara* | Sub | D | Large bait on very heavy tackle at wrecks | Protected: release in the water; legendary "The Bishop" |
| 1 | Red snapper | *Lutjanus campechanus* | Sub | D | Cut bait and squid on the bottom over structure | Short open season (real); barotrauma |
| 1 | Mutton snapper | *Lutjanus analis* | Sub | D | Bottom baits, flies on flats edges | |
| 1 | Yellowtail snapper | *Ocyurus chrysurus* | Car | D/N | Chum slick with small baits drifting back on light line | Line-shy |
| 2 | Cubera snapper | *Lutjanus cyanopterus* | Sub | N | Big live bait at night on wrecks | Largest snapper |
| 1 | Greater amberjack | *Seriola dumerili* | Car | D | Vertical jigging and live bait over wrecks | "Reef donkey": brutal |
| 1 | Cobia | *Rachycentron canadum* | Sub | D | Sight-cast jigs or live eels to fish following rays, turtles or buoys | Goes wild at the boat |
| 2 | Hogfish | *Lachnolaimus maximus* | Lab | D | Crabs or shrimp on the bottom (rarely hooked) | |
| 2 | Queen triggerfish | *Balistes vetula* | Bal | D | Small strong hooks with squid or crab | Bait stealer; tough mouth |
| 2 | Gray triggerfish | *Balistes capriscus* | Bal | D | As above | |
| 1 | Red lionfish | *Pterois volitans* | Lab | C | Rarely hooked on small baits | **Invasive**: bounty; venomous spines |
| 1 | King mackerel | *Scomberomorus cavalla* | Thu | D | Slow-trolled live bait with wire stinger rigs | Blistering runs |
| 2 | Spanish mackerel | *Scomberomorus maculatus* | Thu | D | Small spoons, fast retrieves | |
| 2 | Cero | *Scomberomorus regalis* | Thu | D | As above, on reefs | |
| 2 | Caribbean reef shark | *Carcharhinus perezi* | Car | D/N | Takes hooked fish; cut bait | |
| 2 | Nurse shark | *Ginglymostoma cirratum* | Ang/Sub | N | Bottom baits at night | Sulks on the bottom |
| 2 | Horse-eye jack | *Caranx latus* | Car | N | Jigs, live bait | |
| 2 | Bar jack | *Caranx ruber* | Car | D | Small jigs | |
| 2 | White grunt | *Haemulon plumierii* | Sub | D | Small bottom baits | Grunts audibly |

### 7.10 Blue Water (open ocean)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Mahi-mahi | *Coryphaena hippurus* | Car | D | Trolling, casting to weedlines and floating debris; keep one hooked to hold the school | Acrobatic; colours fade fast when landed (real) |
| 1 | Wahoo | *Acanthocybium solandri* | Thu | D | High-speed trolling with wire | One of the fastest runs in the sea |
| 1 | Yellowfin tuna | *Thunnus albacares* | Thu | D | Chunking, live bait, poppers on blitzes | Death circles under the boat |
| 1 | Blackfin tuna | *Thunnus atlanticus* | Thu | D/N | Chunking, small jigs | |
| 2 | Skipjack tuna | *Katsuwonus pelamis* | Thu | D | Feathers and small lures | Bait for marlin |
| 1 | Atlantic bluefin tuna | *Thunnus thynnus* | Thu | D | Live bait, chunking, trolling on the cold Kesh side | Giant; tightly regulated; needs a permit |
| 2 | Albacore | *Thunnus alalunga* | Thu | D | Trolled feathers | Long pectoral fins |
| 2 | Little tunny | *Euthynnus alletteratus* | Thu | D | Small metal lures into blitzes | "False albacore" |
| 1 | Atlantic sailfish | *Istiophorus platypterus* | Thu | D | Kite-fished live bait, trolled ballyhoo with a drop-back, circle hooks | Tail-walks; release |
| 2 | White marlin | *Kajikia albida* | Thu | D | Trolled ballyhoo, drop-back | Release |
| 1 | Blue marlin | *Makaira nigricans* | Thu | D | Big trolled skirted lures, live tuna | Legendary "Grandfather"; release |
| 1 | Swordfish | *Xiphias gladius* | Thu | N (D deep) | Drifted squid with light sticks at night; daytime deep-drop at 400-500 m | Follows squid up at night |
| 2 | Shortfin mako | *Isurus oxyrinchus* | Thu | D | Chum slicks, live bait; heavy wire | Spectacular leaps; Atlantic retention banned: release |
| 2 | Blue shark | *Prionace glauca* | Car | D | Chum slicks | |
| 1 | Tripletail | *Lobotes surinamensis* | Sub | D | Sight-cast shrimp to fish floating beside debris and buoys | Floats on its side like a leaf |
| 2 | Rainbow runner | *Elagatis bipinnulata* | Car | D | Small jigs and lures | |
| 2 | Almaco jack | *Seriola rivoliana* | Car | D | Jigs over deep structure | |
| 2 | Opah | *Lampris guttatus* | Lab | D | Rare deep-water catch | The first known fully warm-blooded fish |
| — | Oarfish | *Regalecus glesne* | Ami | — | **Sighting only** (Ledger "observed") | Rarely seen alive |
| — | Ocean sunfish | *Mola mola* | — | D | **Sighting only** | |
| 2 | Atlantic flyingfish | *Cheilopogon melanurus* | Car | N | Dip net under a light at night | Bait; glides above the surface |

### 7.11 The Lantern Trench (deep volcanic slopes)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Snowy grouper | *Hyporthodus niveatus* | Sub | D | Deep-drop rigs with squid or cut bait, electric reel | 100-400 m |
| 2 | Yellowedge grouper | *Hyporthodus flavolimbatus* | Sub | D | As above | |
| 2 | Warsaw grouper | *Hyporthodus nigritus* | Sub | D | Large baits deep | Protected: release |
| 1 | Golden tilefish | *Lopholatilus chamaeleonticeps* | Sub | D | Deep-drop baits over mud bottoms | Lives in burrows |
| 2 | Blueline tilefish | *Caulolatilus microps* | Sub | D | Deep-drop | |
| 1 | Wreckfish | *Polyprion americanus* | Sub | D | Deep-drop over steep rock | |
| 2 | Barrelfish | *Hyperoglyphe perciformis* | Sub | D | Deep-drop | |
| 2 | Queen snapper | *Etelis oculatus* | Sub | D | Deep-drop | Huge eyes |
| 1 | Bluntnose sixgill shark | *Hexanchus griseus* | Ang/Sub | N | Big baits deep at night | Rises at night |
| 1 | Oilfish | *Ruvettus pretiosus* | Sub | N | Deep handlines at night | The fish Comoran fishers targeted when they caught coelacanths as bycatch |
| 1 | West Indian Ocean coelacanth | *Latimeria chalumnae* | Lab (paired-fin "walking" gait) | N | Deep night drop near caves, as Comoran handline fishers once caught it by accident; the finale | Protected, release only; drifts head-down; moves its lobed fins in a diagonal gait |
| — | Lanternfish (Myctophidae) | | — | N | **Sighting**: the Lantern rise | |

### 7.12 Expeditions (tier 3)

| Expedition | Species (common, scientific) |
|---|---|
| **Verdant Basin** (Amazon-type river and flooded forest) | Arapaima *Arapaima gigas* (surfaces to breathe air every few minutes: a visible tell), speckled peacock bass *Cichla temensis*, butterfly peacock bass *Cichla ocellaris*, red-bellied piranha *Pygocentrus nattereri*, payara *Hydrolycus scomberoides*, silver arowana *Osteoglossum bicirrhosum* (leaps to grab prey above water), redtail catfish *Phractocephalus hemioliopterus*, piraíba *Brachyplatystoma filamentosum*, tambaqui *Colossoma macropomum* (eats fruit: fruit baits), pirapitinga *Piaractus brachypomus*, trahira *Hoplias malabaricus*, aimara *Hoplias aimara*, jaú catfish *Zungaro zungaro*, ocellate river stingray *Potamotrygon motoro* |
| **Mekong Reach** (great Asian river) | Mekong giant catfish *Pangasianodon gigas* (critically endangered: release), giant barb *Catlocarpio siamensis* (critically endangered: release), giant snakehead *Channa micropeltes*, striped catfish *Pangasianodon hypophthalmus*, giant freshwater stingray *Urogymnus polylepis*, Jullien's golden carp *Probarbus jullieni*, wallago *Wallago attu*, Thai mahseer *Tor tambroides* |
| **Kaldfjord** (Norwegian-type fjord, Arctic coast) | Atlantic cod *Gadus morhua*, Atlantic halibut *Hippoglossus hippoglossus*, saithe *Pollachius virens*, pollack *Pollachius pollachius*, ling *Molva molva*, tusk *Brosme brosme*, Atlantic wolffish *Anarhichas lupus*, golden redfish *Sebastes norvegicus*, haddock *Melanogrammus aeglefinus*, Greenland shark *Somniosus microcephalus* (deep, cold, extremely slow), Atlantic salmon *Salmo salar* (river), sea trout *Salmo trutta* (sea-run), Atlantic mackerel *Scomber scombrus*, spurdog *Squalus acanthias*, red king crab *Paralithodes camtschaticus* (introduced in Norway) |
| **Old Country Canals** (European lowland rivers and lakes) | Wels catfish *Silurus glanis*, zander *Sander lucioperca*, European perch *Perca fluviatilis*, tench *Tinca tinca*, common bream *Abramis brama*, roach *Rutilus rutilus*, rudd *Scardinius erythrophthalmus*, barbel *Barbus barbus*, chub *Squalius cephalus*, European grayling *Thymallus thymallus*, crucian carp *Carassius carassius*, asp *Leuciscus aspius*, ide *Leuciscus idus*, common dace *Leuciscus leuciscus*, gudgeon *Gobio gobio*, bleak *Alburnus alburnus*, European eel *Anguilla anguilla* (critically endangered: release), mirror and leather carp (scale variants of *Cyprinus carpio*: Ledger variants), northern pike |
| **Northern Billabong** (Australian tropical rivers and estuaries) | Barramundi *Lates calcarifer*, Murray cod *Maccullochella peelii*, Gulf saratoga *Scleropages jardinii*, mangrove jack *Lutjanus argentimaculatus*, sooty grunter *Hephaestus fuliginosus*, golden perch *Macquaria ambigua*, oxeye herring *Megalops cyprinoides*, giant trevally *Caranx ignobilis*, queenfish *Scomberoides commersonnianus*, king threadfin *Polydactylus macrochir*, mud crab *Scylla serrata* (pots) |

Murray cod is a temperate inland species and barramundi a tropical one: split the Billabong into a tropical
coastal river spot and an inland river spot so each community stays honest.

### 7.13 Crustaceans and other catches

| Catch | Scientific name | Where | How | Real rules to model |
|---|---|---|---|---|
| Red swamp crayfish | *Procambarus clarkii* | Bayou | Crawfish traps | Bait for bass and catfish |
| Signal crayfish | *Pacifastacus leniusculus* | Glass Creek | Traps | |
| Blue crab | *Callinectes sapidus* | Estuary | Crab pots, hand line with a chicken neck and dip net | Size limits; release egg-bearing females |
| Dungeness crab | *Metacarcinus magister* | Gull Rock | Pots, ring nets | Keep males only above a size (real rule) |
| Red rock crab | *Cancer productus* | Gull Rock | Pots | |
| Stone crab | *Menippe mercenaria* | Calder Keys | Traps | Take one claw (legal size) and release the crab to regrow it (real) |
| Caribbean spiny lobster | *Panulirus argus* | Calder Keys, Reef | Traps | Carapace minimum, season |
| Red king crab | *Paralithodes camtschaticus* | Kaldfjord | Pots | |
| Mud crab | *Scylla serrata* | Northern Billabong | Pots | |
| White shrimp | *Litopenaeus setiferus* | Estuary | Cast net | Live bait |
| Fiddler crab | *Uca* spp. | Estuary, Keys | By hand at low tide | Bait for sheepshead |
| Sand crab | *Emerita analoga* | Gull Rock surf | Dug by hand in the swash | Bait for surfperch |
| Junk and finds | — | Everywhere | Snagged | Old lures (usable), bottles with Wren's pages, boots, a lost rod |
| Bycatch animals | Snapping turtle *Chelydra serpentina*, alligator, cormorant, pelican | Various | Accidental | Released carefully; a short, respectful interaction |
