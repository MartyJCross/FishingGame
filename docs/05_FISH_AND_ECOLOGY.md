# 05: Fish and Ecology

## 1. The realism contract

1. **Every fish is a real species**, with its real common and scientific names.
2. **It lives where, when and how it really does**, at the real places where it lives: water type and salinity, temperature, depth, habitat
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
  "tier": "1",
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
    "spots": ["atchafalaya_swamp", "lake_of_the_woods_bays"],
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
  up the Columbia, shad in spring, mullet along the Nature Coast in autumn, tarpon in early summer),
  thermocline and turnover movements in lakes, winter slowdown, ice cover.
- **Fishing pressure and depletion:** kept fish leave the population; released fish return with a survival chance
  (by hook location, fight length, water temperature, handling, barotrauma treatment). Populations regrow per
  season with a logistic rule. Over-keeping big fish at a spot visibly shifts its size distribution down. This makes
  release meaningful without preaching.
- **Restoration** projects (`06` §6) change habitat cells and populations (oyster reefs, seagrass,
  mangrove nurseries, artificial reefs).

## 7. The species list

**Tiers:** **S** = vertical slice (M5-M6), **1** = v1.0 target, **2** = later content, **3** = post-game
destinations. Target: about 250 catchable species, plus crustaceans and "sighting-only" species. **Every place's
list is its real fish community** (`01` §4.1). The list is a starting point: Opus verifies every row against sources
when writing the species file, and may add real species that really live at that place. A species that lives in
several places has one species file listing all its spots, and only appears under its first place below.

Column key: **Act** = activity (D diurnal, C crepuscular dawn/dusk, N nocturnal, T tide-driven). **Loco** =
locomotion (`04` §9.2): Ang anguilliform, Sub subcarangiform, Car carangiform, Thu thunniform, Ami amiiform, Bal
balistiform, Lab labriform, Raj rajiform, Fla flatfish, Ben benthic hopper.

### 7.1 Home: Station Pond (Florida pond)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| S | Bluegill | *Lepomis macrochirus* | Sub/Lab | D | Worm or cricket under a small float, tiny jigs, small poppers | Colonial nests in spring; circles sideways on the line |
| S | Redear sunfish | *Lepomis microlophus* | Sub/Lab | D | Worms on the bottom | "Shellcracker": crushes snails |
| S | Florida bass | *Micropterus floridanus* | Sub | C | Topwater at dawn, Texas-rigged plastics and frogs in cover, live wild shiners | Recognised as its own species, split from the largemouth; famous for size; warier |
| S | Black crappie | *Pomoxis nigromaculatus* | Sub | C/N | Small jigs or live minnows under a float near brush and docks | "Speck"; papery mouth tears |
| S | Channel catfish | *Ictalurus punctatus* | Sub | N | Cut bait, chicken liver, stink bait, nightcrawlers on a bottom rig | Hunts by smell and taste |
| S | Golden shiner | *Notemigonus crysoleucas* | Sub | D | Tiny hooks with bread or dough | Bait for big bass |
| 1 | Warmouth | *Lepomis gulosus* | Sub | D | Worms, small jigs near cover | |
| 1 | Spotted sunfish | *Lepomis punctatus* | Sub/Lab | D | Worms, crickets | "Stumpknocker" |
| 1 | Brown bullhead | *Ameiurus nebulosus* | Sub | N | Worms on a muddy bottom | |
| 1 | Florida gar | *Lepisosteus platyrhincus* | Sub (stiff) | D | Rope lures or small cut bait | Air-breathes |
| 1 | Bowfin | *Amia calva* | Ami | C/N | Spinnerbaits, frogs, cut bait in weedy water | Ancient lineage; air-breathes; violent fighter |
| 2 | Grass carp | *Ctenopharyngodon idella* | Sub | D | Bread, fruit, plant baits; sight fishing | Sterile fish stocked for weed control; must be released |

### 7.2 Home: Spring Run

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Striped mullet | *Mugil cephalus* | Sub | D | Cast net; occasionally tiny hooks with bread | Jumps; huge autumn runs on the coast |
| 1 | Common snook | *Centropomus undecimalis* | Sub | N/C | Live shrimp or pilchards, jigs, flies | Moves into warm springs in cold snaps (real); gill plates cut line; slot limit |
| 1 | Sheepshead | *Archosargus probatocephalus* | Sub | D | Fiddler crabs, shrimp or barnacles on small strong hooks near rocks and pilings | Notorious bait thief; human-like teeth |
| 2 | Atlantic needlefish | *Strongylura marina* | Sub | D | Small fast lures | Leaps; needle beak |
| 2 | American eel | *Anguilla rostrata* | Ang | N | Worms or cut bait on the bottom | Rolls and knots the line; conservation concern |
| — | West Indian manatee | *Trichechus manatus* | — | — | **Sighting only** | Never disturb; boats go slow |
| — | Florida bass, bluegill, redear, spotted sunfish, gray snapper (near the coast) | | | | | |

### 7.3 Home: The Suwannee

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Suwannee bass | *Micropterus notius* | Sub | D | Crayfish imitations, small crankbaits and jigs on limestone shoals | Found in almost no other river system |
| 1 | Redbreast sunfish | *Lepomis auritus* | Sub/Lab | D | Worms, crickets, small spinners | |
| 1 | White catfish | *Ameiurus catus* | Sub | N | Cut bait, worms on the bottom | |
| 1 | Chain pickerel | *Esox niger* | Sub | D | Spoons and spinners along weed edges | Teeth |
| 2 | Redfin pickerel | *Esox americanus* | Sub | D | Small spinners | |
| — | Gulf sturgeon | *Acipenser oxyrinchus desotoi* | Sub | — | **Sighting only**: protected; leaps out of the river in summer (real) | |
| — | Florida bass, channel catfish, Florida gar, bowfin, spotted sunfish | | | | | |

### 7.4 Home: Waccasassa Flats and Oyster Bars

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Red drum | *Sciaenops ocellatus* | Sub | T | Gold spoons, cut mullet, shrimp; sight-cast to tailing fish | Slot limit; spot on the tail |
| 1 | Spotted seatrout | *Cynoscion nebulosus* | Sub | C/N | Popping cork with shrimp, topwater, soft plastics over grass | Soft mouth |
| 1 | Black drum | *Pogonias cromis* | Sub | D/N | Crab or shrimp on the bottom near oyster bars | Loud croaker |
| 1 | Southern flounder | *Paralichthys lethostigma* | Fla | D/T | Slowly dragged jigs, live minnows on a Carolina rig; let it eat before setting | Ambusher buried in sand |
| 2 | Gulf flounder | *Paralichthys albigutta* | Fla | D | As above | Three eye-spots |
| 1 | Pinfish | *Lagodon rhomboides* | Sub | D | Tiny hooks | A top live bait |
| 1 | Ladyfish | *Elops saurus* | Sub | D | Small jigs and spoons | Jumps constantly |
| 1 | Gray snapper | *Lutjanus griseus* | Sub | D/N | Live shrimp on light fluorocarbon near oysters, rocks and docks | Wary; "mangrove snapper" |
| 1 | Jack crevalle | *Caranx hippos* | Car | D | Poppers and jigs into busting schools | Dogged circles |
| 2 | Silver perch | *Bairdiella chrysoura* | Sub | D | Small baits | |
| 2 | Atlantic croaker | *Micropogonias undulatus* | Sub | D/N | Shrimp on the bottom | |
| 2 | Hardhead catfish | *Ariopsis felis* | Sub | D/N | Any bottom bait | Nuisance bycatch; venomous spines |
| 2 | Gafftopsail catfish | *Bagre marinus* | Sub | D | Cut bait | Slimy; long dorsal filament |
| 2 | Atlantic stingray | *Hypanus sabinus* | Raj | D | Bycatch on bottom baits | Careful handling |
| 1 | Bonnethead | *Sphyrna tiburo* | Car | D | Crab or shrimp on the flats | Small hammerhead |

### 7.5 Home: Homosassa Tarpon Grounds

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Tarpon | *Megalops atlanticus* | Sub | C/N | Sight-cast flies, live crabs or mullet on circle hooks to rolling and laid-up fish | Spectacular jumps; very hard mouth; release only |
| 1 | Cobia | *Rachycentron canadum* | Sub | D | Sight-cast jigs or live eels/pinfish to fish following rays, turtles or markers | Goes wild at the boat |
| 1 | Spanish mackerel | *Scomberomorus maculatus* | Thu | D | Small spoons, fast retrieves, light wire | |
| 1 | Tripletail | *Lobotes surinamensis* | Sub | D | Sight-cast shrimp to fish floating beside crab-trap buoys (a real Nature Coast specialty) | Floats on its side like a leaf |
| 1 | Blacktip shark | *Carcharhinus limbatus* | Car | D | Cut bait, live bait; wire | Spinning jumps |
| 1 | Bull shark | *Carcharhinus leucas* | Car | N/D | Big cut bait on circle hooks with wire | |
| 2 | Atlantic sharpnose shark | *Rhizoprionodon terraenovae* | Car | D | Cut bait | |
| — | Great hammerhead | *Sphyrna mokarran* | Car | — | **Sighting/release only**: protected | Follows tarpon |

### 7.6 Home: Nature Coast Ledges and Wrecks

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Gag grouper | *Mycteroperca microlepis* | Sub | D | Live pinfish or big jigs; lock the drag and pull it off the bottom at once | Dives for holes; the Nature Coast's famous grouper |
| 1 | Red grouper | *Epinephelus morio* | Sub | D | Cut bait or squid on the bottom | |
| 2 | Scamp | *Mycteroperca phenax* | Sub | D | Live bait on ledges | |
| 1 | Goliath grouper | *Epinephelus itajara* | Sub | D | Large bait on very heavy tackle at wrecks | Protected: release in the water; legendary "The Bishop" |
| 1 | Red snapper | *Lutjanus campechanus* | Sub | D | Cut bait and squid over structure | Short open season (real); barotrauma, descender |
| 2 | Lane snapper | *Lutjanus synagris* | Sub | D | Small baits | |
| 2 | Vermilion snapper | *Rhomboplites aurorubens* | Sub | D | Small baits off the bottom | |
| 1 | Greater amberjack | *Seriola dumerili* | Car | D | Vertical jigging and live bait over wrecks | "Reef donkey": brutal |
| 1 | Gray triggerfish | *Balistes capriscus* | Bal | D | Small strong hooks with squid | Bait stealer; tough mouth |
| 2 | Hogfish | *Lachnolaimus maximus* | Lab | D | Crabs or shrimp on the bottom (rarely hooked) | |
| 1 | King mackerel | *Scomberomorus cavalla* | Thu | D | Slow-trolled live bait on wire stinger rigs | Blistering runs |
| 2 | Atlantic spadefish | *Chaetodipterus faber* | Lab | D | Tiny pieces of jellyfish or clam in a slick | |
| 2 | Black sea bass | *Centropristis striata* | Sub | D | Small baits and jigs | |
| 2 | White grunt | *Haemulon plumierii* | Sub | D | Small bottom baits | Grunts audibly |
| 1 | Red lionfish | *Pterois volitans* | Lab | C | Rarely hooked on small baits | **Invasive**: bounty; venomous spines |
| 2 | Nurse shark | *Ginglymostoma cirratum* | Ang/Sub | N | Bottom baits at night | Sulks on the bottom |
| 2 | Sandbar shark | *Carcharhinus plumbeus* | Car | D | Release only in many waters (real) | |

### 7.7 Home: The Middle Grounds and Deep Edge

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Mahi-mahi | *Coryphaena hippurus* | Car | D | Trolling, casting to weedlines and floating debris; keep one hooked to hold the school | Acrobatic; colours fade fast when landed (real) |
| 1 | Blackfin tuna | *Thunnus atlanticus* | Thu | D/N | Chunking, small jigs, trolled feathers | |
| 2 | Little tunny | *Euthynnus alletteratus* | Thu | D | Small metal lures into blitzes | "False albacore" |
| 1 | Atlantic sailfish | *Istiophorus platypterus* | Thu | D | Live bait, trolled ballyhoo with a drop-back, circle hooks | Tail-walks; release |
| 2 | Wahoo | *Acanthocybium solandri* | Thu | D | High-speed trolling with wire | One of the fastest runs in the sea |
| 2 | Almaco jack | *Seriola rivoliana* | Car | D | Jigs over deep structure | |
| 1 | Snowy grouper | *Hyporthodus niveatus* | Sub | D | Deep-drop rigs with squid or cut bait, electric reel | 100-400 m |
| 2 | Yellowedge grouper | *Hyporthodus flavolimbatus* | Sub | D | As above | |
| 2 | Warsaw grouper | *Hyporthodus nigritus* | Sub | D | Large baits deep | No harvest: release |
| 1 | Golden tilefish | *Lopholatilus chamaeleonticeps* | Sub | D | Deep-drop baits over mud bottoms | Lives in burrows |
| 2 | Blueline tilefish | *Caulolatilus microps* | Sub | D | Deep-drop | |
| 2 | Barrelfish | *Hyperoglyphe perciformis* | Sub | D | Deep-drop | |
| 2 | Queen snapper | *Etelis oculatus* | Sub | D | Deep-drop | Huge eyes |

### 7.8 Lake of the Woods (Minnesota/Ontario)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Walleye | *Sander vitreus* | Sub | C/N | Jig and minnow on rocky reefs and drop-offs, trolled crankbaits at night, jigging through the ice | Light-sensitive eyes (tapetum); deep by day |
| 1 | Sauger | *Sander canadensis* | Sub | C | Jigs and minnows in current and deep water | |
| 1 | Northern pike | *Esox lucius* | Sub | D | Spoons, inline spinners, jerkbaits, dead bait; wire leader | Teeth cause bite-offs |
| 1 | Muskellunge | *Esox masquinongy* | Sub | D | Big bucktails, glide baits, jerkbaits; figure-8 at the boat | Follows often; very rare strikes; legendary "Lantern Jaw" |
| 1 | Smallmouth bass | *Micropterus dolomieu* | Sub | D | Tubes, drop shots, jerkbaits, crankbaits on rocky points | Acrobatic jumper |
| 1 | Yellow perch | *Perca flavescens* | Sub | D | Small jigs, minnows, worms near the bottom | Schools |
| 1 | Lake sturgeon | *Acipenser fulvescens* | Sub | D/N | Nightcrawlers on the bottom in current | Ancient; huge; strictly limited, release |
| 1 | Burbot | *Lota lota* | Ang | N | Jigging spoons and dead minnows on the bottom at night, especially under ice | Freshwater member of the cod order; spawns under ice in midwinter |
| 2 | Lake whitefish | *Coregonus clupeaformis* | Sub | D | Small jigs through the ice | Soft mouth |
| 2 | Cisco | *Coregonus artedi* | Sub | D | Small jigs through the ice | "Tullibee" |
| 2 | Lake trout | *Salvelinus namaycush* | Sub | D | Deep jigging and trolling in cold northern bays | Needs cold water |
| 2 | Rock bass | *Ambloplites rupestris* | Sub | D | Small jigs and worms on rocks | |
| 2 | Freshwater drum | *Aplodinotus grunniens* | Sub | D | Crayfish, worms or jigs on the bottom | Makes drumming sounds |
| 2 | White sucker | *Catostomus commersonii* | Sub | D | Worms on the bottom | Bait |
| — | Black crappie, largemouth bass (*Micropterus salmoides*) in weedy bays | | | | | |

### 7.9 Atchafalaya Basin and Louisiana marsh

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Alligator gar | *Atractosteus spatula* | Sub (stiff) | C/N | Cut bait under a float, let it run, then set hard | Bony mouth; air-breathes; legendary "The Priest" |
| 1 | Spotted gar | *Lepisosteus oculatus* | Sub (stiff) | D | Rope lures, small cut bait | |
| 2 | Longnose gar | *Lepisosteus osseus* | Sub (stiff) | D | Rope lures | |
| 1 | Blue catfish | *Ictalurus furcatus* | Sub | N/D | Fresh cut shad on the bottom in deep channels | Grows very large |
| 1 | Flathead catfish | *Pylodictis olivaris* | Sub | N | Live fish on the bottom near wood | Rarely takes dead bait |
| 1 | Largemouth bass | *Micropterus salmoides* | Sub | C | Frogs over pads, spinnerbaits, plastics in cypress | |
| 1 | White crappie | *Pomoxis annularis* | Sub | C | Small jigs and minnows on cypress and brush | "Sac-a-lait" |
| 2 | Smallmouth buffalo | *Ictiobus bubalus* | Sub | D | Dough baits on the bottom (rare) | |
| 2 | Bigmouth buffalo | *Ictiobus cyprinellus* | Sub | D | Dough baits (rare) | Filter feeder |
| 1 | Silver carp | *Hypophthalmichthys molitrix* | Sub | D | Rarely hooked (filter feeder) | **Invasive**: leaps out of the water at boat noise (real); bounty |
| 1 | Red drum (bull reds) | (see 7.4) | | T | Cut mullet or crab in marsh passes | |
| — | Bowfin, channel catfish, bluegill, redear, warmouth, black crappie, freshwater drum; marsh seatrout, black drum, southern flounder, sheepshead | | | | | |

### 7.10 Madison River and Beartooth alpine lakes (Montana)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Rainbow trout | *Oncorhynchus mykiss* | Sub | D/C | Dry flies, nymphs, streamers, small spinners in riffles and seams | Jumps |
| 1 | Brown trout | *Salmo trutta* | Sub | C/N | Nymphs and streamers; mouse flies at night for big fish | Very wary; introduced; legendary "Brass" |
| 1 | Yellowstone cutthroat trout | *Oncorhynchus clarkii bouvieri* | Sub | D | Dry flies, nymphs | Red throat slash |
| 1 | Mountain whitefish | *Prosopium williamsoni* | Sub | D | Nymphs in riffles | |
| 1 | Brook trout | *Salvelinus fontinalis* | Sub | D | Small dries, nymphs in cold tributaries | Actually a char |
| 1 | Arctic grayling | *Thymallus arcticus* | Sub | D | Small dry flies in alpine lakes and streams | Sail-like dorsal fin |
| 2 | Golden trout | *Oncorhynchus aguabonita* | Sub | D | Tiny flies and lures in high alpine lakes | Stocked in high Montana lakes; stunning colours |
| 2 | Mottled sculpin | *Cottus bairdii* | Ben | N | Mostly observed | Trout food |

The **salmonfly hatch** (giant stoneflies, *Pteronarcys californica*, early summer) is the Madison's signature event.

### 7.11 Columbia River and Oregon Coast

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | White sturgeon | *Acipenser transmontanus* | Sub | N | Smelt, shad, squid or roe on the bottom in deep holes | Can jump; ancient; legendary "Old Ironsides"; mostly release |
| 1 | Chinook salmon | *Oncorhynchus tshawytscha* | Sub | D/C | Spinners, plugs and roe in the river (fish don't feed there: strikes are aggression); trolled herring and flashers at sea | |
| 1 | Coho salmon | *Oncorhynchus kisutch* | Sub | D | Spinners, flies | Acrobatic |
| 2 | Sockeye salmon | *Oncorhynchus nerka* | Sub | D | Very rarely strikes; small lures at its level | Turns red with a green head in the river |
| 1 | Steelhead | *Oncorhynchus mykiss* (sea-run) | Sub | D | Swung flies, drifted egg patterns, spoons | Same species as rainbow, separate Ledger entry |
| 1 | American shad | *Alosa sapidissima* | Car | D | Shad darts and small spoons during the spring run | Introduced here; huge runs |
| 2 | Northern pikeminnow | *Ptychocheilus oregonensis* | Sub | D | Cut bait, small lures | A bounty fish (eats salmon smolts) |
| 2 | Walleye | (see 7.8) | | | Introduced in the Columbia; grows huge | |
| 1 | Lingcod | *Ophiodon elongatus* | Sub | D | Big swimbaits, jigs, live bait on rocky reefs | Often grabs a hooked rockfish and won't let go |
| 1 | Cabezon | *Scorpaenichthys marmoratus* | Ben | D | Jigs and crab baits on rocks | |
| 2 | Kelp greenling | *Hexagrammos decagrammus* | Sub | D | Small jigs in kelp | |
| 1 | Black rockfish | *Sebastes melanops* | Sub | D | Small swimbaits and jigs in mid-water schools | Barotrauma: descender |
| 2 | Copper rockfish | *Sebastes caurinus* | Sub | D | Jigs on rocky bottom | |
| 2 | Yelloweye rockfish | *Sebastes ruberrimus* | Sub | D | Deep jigs | Protected: descend and release |
| 1 | Pacific halibut | *Hippoglossus stenolepis* | Fla | D | Big jigs, herring on circle hooks on the bottom | Huge; dangerous on deck |
| 1 | Redtail surfperch | *Amphistichus rhodoterus* | Sub/Lab | D/T | Sand crabs or small grubs in the surf | Live-bearer |
| 2 | Striped seaperch | *Embiotoca lateralis* | Lab | D | Small baits at jetties and kelp | |
| 2 | Pile perch | *Rhacochilus vacca* | Lab | D | Mussels, crabs at pilings | |
| 2 | Starry flounder | *Platichthys stellatus* | Fla | D | Worms on sand | |
| 1 | Pacific herring | *Clupea pallasii* | Car | D | Herring jigs (sabiki-style multi-hook rigs) | Bait |
| 2 | Pacific spiny dogfish | *Squalus suckleyi* | Car | D/N | Any bait | Bait thief |
| 2 | Big skate | *Beringraja binoculata* | Raj | D | Bottom baits | |

### 7.12 Chesapeake Bay and Outer Banks

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Striped bass | *Morone saxatilis* | Car | C/N/T | Live eels, chunked menhaden, topwater and swimbaits at rips, bridges and rocks | "Rockfish"; heavily regulated |
| 1 | Bluefish | *Pomatomus saltatrix* | Car | D | Metal lures, poppers; wire leader | Razor teeth |
| 1 | Weakfish | *Cynoscion regalis* | Sub | C/N | Soft plastics, shrimp | Very soft mouth |
| 1 | Summer flounder | *Paralichthys dentatus* | Fla | D | Bucktails with strip bait, drifted minnows | "Fluke" |
| 1 | Red drum (surf) | (see 7.4) | | T/N | Cut mullet or menhaden on surf rigs at Outer Banks inlets in autumn | The surf there produced the all-tackle record |
| 1 | Cobia | (see 7.5) | | | Sight-casting in the bay in summer | |
| 2 | Spot | *Leiostomus xanthurus* | Sub | D | Bloodworms on the bottom | |
| 2 | White perch | *Morone americana* | Sub | D | Small spinners, grass shrimp | |
| 2 | Tautog | *Tautoga onitis* | Lab | D | Crabs on rocky structure and bridge pilings | |
| 2 | Hickory shad | *Alosa mediocris* | Car | D | Shad darts in the spring run | |
| 1 | Northern snakehead | *Channa argus* | Sub | D | Frogs and topwater in tidal creeks | **Invasive**: bounty, must not be released alive (real rule) |
| 1 | Blue catfish (invasive here) | (see 7.9) | | | Cut menhaden | **Invasive** in the Chesapeake: bounty |
| 2 | Atlantic menhaden | *Brevoortia tyrannus* | Car | D | Cast net | The key bait fish |
| — | Atlantic sturgeon | *Acipenser oxyrinchus oxyrinchus* | Sub | — | **Sighting only**: endangered | |
| — | Black drum, croaker, sheepshead, Spanish mackerel, black sea bass, spadefish, blue crab | | | | | |

### 7.13 Florida Keys

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Bonefish | *Albula vulpes* | Sub | T/D | Sight-cast shrimp, or shrimp/crab flies ahead of tailing fish on the rising tide | Blistering runs; spooky; release |
| 1 | Permit | *Trachinotus falcatus* | Car | T/D | Live crab or crab flies, sight-cast | Famously hard to fool; release |
| 1 | Great barracuda | *Sphyraena barracuda* | Sub | D | Tube lures retrieved very fast; wire leader | Big reef fish risk ciguatera (don't sell) |
| 1 | Lemon shark | *Negaprion brevirostris* | Car | D | Cut bait, sight fishing on flats | |
| 1 | Mutton snapper | *Lutjanus analis* | Sub | D | Bottom baits, flies on flats edges | |
| 1 | Yellowtail snapper | *Ocyurus chrysurus* | Car | D/N | Chum slick with small baits drifting back on light line | Line-shy |
| 2 | Cubera snapper | *Lutjanus cyanopterus* | Sub | N | Big live bait at night on wrecks | Largest snapper |
| 2 | Black grouper | *Mycteroperca bonaci* | Sub | D | Live bait on the reef | |
| 2 | Nassau grouper | *Epinephelus striatus* | Sub | D | Bycatch on bottom baits | Critically endangered: release |
| 2 | Queen triggerfish | *Balistes vetula* | Bal | D | Small strong hooks with squid | |
| 2 | Cero | *Scomberomorus regalis* | Thu | D | Small spoons on the reef | |
| 2 | Horse-eye jack | *Caranx latus* | Car | N | Jigs, live bait | |
| 2 | Bar jack | *Caranx ruber* | Car | D | Small jigs | |
| 2 | Houndfish | *Tylosurus crocodilus* | Sub | D | Fast-moving small lures | Leaps |
| 2 | Caribbean reef shark | *Carcharhinus perezi* | Car | D/N | Takes hooked fish; cut bait | |
| 1 | Swordfish | *Xiphias gladius* | Thu | N (D deep) | Daytime deep-drop with squid at 400-500 m (a technique developed in the Keys); drifted baits with lights at night | Follows squid up at night |
| 2 | White marlin | *Kajikia albida* | Thu | D | Trolled ballyhoo, drop-back | Release |
| — | Tarpon, snook, mahi, sailfish, wahoo, blackfin, king mackerel, gag, red grouper, goliath, hogfish, tripletail, bonnethead, blacktip, gray snapper, spiny lobster, stone crab | | | | | |

### 7.14 Kona, Hawaii

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Blue marlin | *Makaira nigricans* | Thu | D | Big trolled lures, live skipjack | Kona's famous "granders"; legendary "Grandfather"; release. (Some authorities split the Indo-Pacific blue marlin as *M. mazara*: note it in the Ledger) |
| 2 | Striped marlin | *Kajikia audax* | Thu | D | Trolled lures | Release |
| 2 | Shortbill spearfish | *Tetrapturus angustirostris* | Thu | D | Trolled lures | |
| 1 | Yellowfin tuna | *Thunnus albacares* | Thu | D | Trolling, live bait, drifting baits at night ("ika-shibi", a Hawaiian method with lights) | "Ahi"; death circles under the boat |
| 2 | Bigeye tuna | *Thunnus obesus* | Thu | N/D | Deep baits | |
| 1 | Skipjack tuna | *Katsuwonus pelamis* | Thu | D | Feathers and small lures | "Aku"; live bait for marlin |
| 1 | Wahoo (ono) | (see 7.7) | | | High-speed trolling | |
| 1 | Giant trevally | *Caranx ignobilis* | Car | N/D | Heavy shore rods with bait from lava rocks at night ("ulua" fishing), poppers from boats | Brutal; a Hawaiian tradition |
| 1 | Bluefin trevally | *Caranx melampygus* | Car | D | Small poppers, flies, jigs along the shore | "Omilu" |
| 2 | Roundjaw bonefish | *Albula glossodonta* | Sub | T | Bait or flies on sand flats | "O'io" |
| 2 | Deepwater longtail red snapper | *Etelis coruscans* | Sub | D | Deep bottom rigs | "Onaga" |
| 2 | Crimson jobfish | *Pristipomoides filamentosus* | Sub | D | Deep bottom rigs | "Opakapaka" |
| 2 | Opah | *Lampris guttatus* | Lab | D | Rare deep-water catch | The first known fully warm-blooded fish |
| 2 | Tiger shark | *Galeocerdo cuvier* | Car | N | Big baits | Release |
| — | Mahi-mahi, rainbow runner (*Elagatis bipinnulata*), swordfish | | | | | |

### 7.15 Grande Comore, Comoros (the finale)

| Tier | Species | Scientific name | Loco | Act | Real way to catch it | Notes |
|---|---|---|---|---|---|---|
| 1 | Oilfish | *Ruvettus pretiosus* | Sub | N | Deep handlines at night over steep slopes | The fish Comorian fishers targeted when coelacanths were caught as bycatch |
| 1 | West Indian Ocean coelacanth | *Latimeria chalumnae* | Lab (paired fins move in a diagonal gait) | N | A night deep drop near the caves; release with a descender (`01` §6) | Critically endangered; drifts head-down; three-lobed tail; legendary "The Old One" |
| 1 | Bluntnose sixgill shark | *Hexanchus griseus* | Ang/Sub | N | Big baits deep at night | Rises at night |
| 1 | Dogtooth tuna | *Gymnosarda unicolor* | Thu | D | Vertical jigging and live bait along drop-offs | |
| 2 | Ruby snapper | *Etelis carbunculus* | Sub | D | Deep bottom rigs | |
| — | Yellowfin tuna, giant trevally, bluefin trevally, swordfish, sailfish, wahoo | | | | | |
| — | Lanternfish (Myctophidae) | | — | N | **Sighting**: the Lantern rise | |
| — | Humpback whale | *Megaptera novaeangliae* | — | — | **Sighting** (they visit the Comoros in winter) | |

### 7.16 Post-game destinations (tier 3)

| Destination | Species (common, scientific) |
|---|---|
| **Rio Negro, Amazon** | Speckled peacock bass *Cichla temensis*, butterfly peacock bass *Cichla ocellaris*, arapaima *Arapaima gigas* (surfaces to breathe air every few minutes: a visible tell; fished in community-managed lakes), red-bellied piranha *Pygocentrus nattereri*, black piranha *Serrasalmus rhombeus*, payara *Hydrolycus scomberoides*, silver arowana *Osteoglossum bicirrhosum* (leaps to grab prey above water), redtail catfish *Phractocephalus hemioliopterus*, piraíba *Brachyplatystoma filamentosum*, tambaqui *Colossoma macropomum* (eats fruit: fruit baits), pirapitinga *Piaractus brachypomus*, trahira *Hoplias malabaricus*, jaú catfish *Zungaro zungaro*, ocellate river stingray *Potamotrygon motoro* |
| **Thailand: Mae Klong, Mekong and a stocked fishing lake** | Giant freshwater stingray *Urogymnus polylepis* (Mae Klong), giant snakehead *Channa micropeltes*, striped catfish *Pangasianodon hypophthalmus*, Siamese giant carp *Catlocarpio siamensis* (critically endangered in the wild; stocked lakes), Mekong giant catfish *Pangasianodon gigas* (critically endangered in the wild; stocked lakes), Chao Phraya catfish *Pangasius sanitwongsei*, Jullien's golden carp *Probarbus jullieni*, wallago *Wallago attu*, Thai mahseer *Tor tambroides*. Thailand's stocked fishing lakes are real and are shown honestly as stocked. |
| **Northern Norway** | Atlantic cod *Gadus morhua*, saithe *Pollachius virens* (huge in the Saltstraumen tidal current), pollack *Pollachius pollachius*, Atlantic halibut *Hippoglossus hippoglossus*, ling *Molva molva*, tusk *Brosme brosme*, Atlantic wolffish *Anarhichas lupus*, golden redfish *Sebastes norvegicus*, haddock *Melanogrammus aeglefinus*, Greenland shark *Somniosus microcephalus* (deep, cold, extremely slow), Atlantic salmon *Salmo salar* (river), sea trout *Salmo trutta* (sea-run), Arctic char *Salvelinus alpinus*, Atlantic mackerel *Scomber scombrus*, spurdog *Squalus acanthias*, red king crab *Paralithodes camtschaticus* (introduced) |
| **England and Spain** | Common carp *Cyprinus carpio* (with mirror and leather scale variants as Ledger variants), tench *Tinca tinca*, common bream *Abramis brama*, roach *Rutilus rutilus*, rudd *Scardinius erythrophthalmus*, barbel *Barbus barbus* (River Wye), chub *Squalius cephalus*, common dace *Leuciscus leuciscus*, gudgeon *Gobio gobio*, crucian carp *Carassius carassius*, European perch *Perca fluviatilis*, northern pike, zander *Sander lucioperca*, brown trout and European grayling *Thymallus thymallus* (chalk stream), European eel *Anguilla anguilla* (critically endangered: release), wels catfish *Silurus glanis* (the Ebro, where it's introduced and grows huge) |
| **Northern Australia** | Barramundi *Lates calcarifer*, Gulf saratoga *Scleropages jardinii*, mangrove jack *Lutjanus argentimaculatus*, golden snapper *Lutjanus johnii*, sooty grunter *Hephaestus fuliginosus*, queenfish *Scomberoides commersonnianus*, king threadfin *Polydactylus macrochir*, oxeye herring *Megalops cyprinoides*, giant trevally, mud crab *Scylla serrata* (pots). Inland on the Murray (a separate temperate spot): Murray cod *Maccullochella peelii*, golden perch *Macquaria ambigua*, silver perch *Bidyanus bidyanus*, redfin (European perch, introduced), common carp (**invasive** there: bounty). Saltwater crocodiles are a real hazard in the north. |

### 7.17 Crustaceans and other catches

| Catch | Scientific name | Where | How | Real rules to model |
|---|---|---|---|---|
| Blue crab | *Callinectes sapidus* | Home flats, Chesapeake, Louisiana | Crab traps, hand line with a chicken neck and dip net | Size limits; release egg-bearing females |
| Stone crab | *Menippe mercenaria* | Home flats, Keys | Traps | Take legal-size claws and release the crab to regrow them (real) |
| Caribbean spiny lobster | *Panulirus argus* | Keys | Traps | Carapace minimum, season |
| Red swamp crayfish | *Procambarus clarkii* | Atchafalaya | Crawfish traps | Bait for bass and catfish |
| Dungeness crab | *Metacarcinus magister* | Oregon Coast | Pots, ring nets | Keep males only above a size (real rule) |
| Red rock crab | *Cancer productus* | Oregon Coast | Pots | |
| Red king crab | *Paralithodes camtschaticus* | Northern Norway | Pots | |
| Mud crab | *Scylla serrata* | Northern Australia | Pots | |
| Pink shrimp | *Farfantepenaeus duorarum* | Home flats | Cast net | Live bait |
| Fiddler crab | *Uca* spp. (genus revised; verify) | Home flats, Keys | By hand at low tide | Bait for sheepshead |
| Sand crab | *Emerita analoga* | Oregon surf | Dug by hand in the swash | Bait for surfperch |
| Junk and finds | — | Everywhere | Snagged | Old lures (usable), bottles with Wren's pages, boots, a lost rod |
| Bycatch animals | Florida softshell turtle, alligator, cormorant, pelican | Various | Accidental | Released carefully; a short, respectful interaction |
