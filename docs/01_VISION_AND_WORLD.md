# 01: Vision, World and Story

## 1. The pitch

**Longwater** is a 2D side-view fishing game where you can feel every gram on the line. You arrive at a broke,
half-flooded field station at the mouth of a great river and take the job nobody else wanted: *survey angler* for the
Longwater Basin Trust. Your job is to catch, record and (mostly) release every fish in a river system that runs from
a glacier to the open ocean, and use what you earn to bring the river back to life.

What you do in the game, minute to minute, is real fishing, and it is hard to master. Load the rod with a backswing
and release the line at the right instant. Thumb a baitcaster spool so it doesn't bird's-nest. Skip a soft plastic
under a dock. Twitch a topwater into a zig-zag. Feel a bass's short inhale on the rod tip and sweep the hook home.
Steer a running fish with side pressure, ease the drag before it snaps 6 lb mono, and lower the rod as a tarpon
erupts out of the water.

What you do hour to hour is a living world. Dawn bites and night predators, moon-driven tides, fronts that turn fish
on and off, salmon runs, mayfly hatches, bait balls with diving birds. You'll find better gear, boats, cabins on
private water, crab pots, a story about a vanished keeper, and a mystery out on the night sea called **the Lantern**.

## 2. Design pillars

1. **Feel the line.** The rod, line, reel and water are physical. Every technique real anglers use should be possible
   because the simulation allows it, not because we scripted it. Information reaches the player the way it reaches an
   angler: rod-tip bend, line angle, slack, the drag's scream, a twitch in a float, rings on the surface. Meters are
   an optional assist.
2. **Real fish, really behaving.** Hundreds of real species with real habitats, diets, activity cycles and fight
   styles. Knowing fish is a skill: a player who learns that walleye feed at dusk on rocky drop-offs, or that bonefish
   tail on the rising tide, catches more. The Ledger rewards that knowledge.
3. **Mastery over grind.** Progression unlocks *where* and *what* you can fish, never makes your hands better. A
   skilled player with starter gear should out-fish a clumsy one with the best gear.
4. **A world worth restoring.** The basin is damaged, and your catches, surveys and choices visibly repair it:
   weirs come down, salmon return, mangroves regrow, the reef recovers. The world changes because of you.
5. **Beauty in the details.** Stylized low-poly 2D with gorgeous light: glassy dawn ponds, rain-pocked bayous, gold
   evening flats, black-water nights lit by bioluminescence. The water should be the most beautiful thing on screen.

### What this game is not

- Not an arcade fishing game with a "keep the fish in the green zone" bar.
- Not a fantasy game: no invented fish, no magic lures. The only "monsters" are real animals at real record sizes.
- Not a survival or crafting grind. Light management (cooler space, bait, fuel) exists for texture, not chores.

## 3. Player character and companion

**Assumption (see OPEN_QUESTIONS Q1):** the player is a human angler shown as a stylized, faceless low-poly figure
with customizable outfit colours, hat and vest. The rod is held in physically placed hands (simple 2-bone arm IK to
the rod butt), so the body reads the rod's motion.

A **companion dog** (a scruffy station dog named **Skiff**) rides in the boat or sits on the bank. It is purely
diegetic feedback and charm: its ears prick at a bite at night, it barks at a jumping fish, it sleeps when nothing is
happening. It never plays the game for you and can be turned off.

## 4. The world: the Longwater Basin

A fictional coastal nation's great river basin, modelled as a composite of North American temperate waters. The
basin has one unique geographic fact that explains its astonishing fish diversity and is the backbone of the world:

> **Harrow Sound sits where two ocean currents meet.** The cold, nutrient-rich **Kesh Upwelling** sweeps down the
> northern shore (kelp forests, rockfish, lingcod, salmon), and the warm **Calder Current** curls up from the south
> (mangroves, bonefish, tarpon, coral, billfish). Real convergence zones like this are some of the richest fisheries
> on Earth. This one sits at the mouth of a river that runs from a glacier, so freshwater, brackish and both cold and
> warm saltwater are all within one journey.

**The biogeography rule:** the basin as a whole is a composite, but **each water body's fish community must be one
that could really live together** there: same temperature range, salinity, habitat and season. `docs/05` lists who
lives where.

### 4.1 The regions (source to sea)

Each region is a set of **spots**. A spot is one side-view cross-section scene, for example a pond 60 m wide, or a
river pool with a riffle, or a strip of open ocean 2 km long. A region map connects spots, and some spots need a boat
or a property. Regions unlock through story, licences and boats (see `docs/06`).

| # | Region | Water type | Character | Signature species | Unlock |
|---|---|---|---|---|---|
| 1 | **Millbrook Pond** | Warm farm pond, 1-5 m, lily pads, dock, fallen oak | The tutorial pond behind the station. Shore only. | Bluegill, largemouth bass, channel catfish, common carp | Start |
| 2 | **Harrow Reservoir** | Big dammed lake, 35 m, drowned forest, rocky points, summer thermocline | The old Harrow Dam's lake. First boat water. Standing dead timber poking out of the water. | Walleye, smallmouth, pike, muskellunge, lake trout | Rowboat |
| 3 | **Glass Creek (Upper Longwater)** | Cold mountain river: riffles, runs, pools, pocket water | Clear, fast, loud. Fly fishing country. Changes once the weir is removed. | Rainbow, brown, brook and cutthroat trout, grayling; salmon runs after restoration | Freshwater licence + trout stamp |
| 4 | **Kesh Tarn** | Glacial alpine lake, very cold, 60 m | Turquoise glacial-flour water, scree slopes, a glacier face. Frozen in winter: ice fishing. | Arctic char, lake trout, burbot, kokanee | Act 1 story |
| 5 | **Blackwater Bayou** | Slow, tannin-dark swamp, cypress knees, duckweed, oxbows | Hot, humid, loud at night with frogs and insects. Visibility under a metre. | Alligator gar, bowfin, flathead and blue catfish, chain pickerel | Jon boat |
| 6 | **Harrow Sound Estuary** | Brackish delta: salt marsh, oyster bars, channels, bridge pilings, the old cannery harbour | Where the river meets the sea. Tides rule everything. | Striped bass, red drum, seatrout, flounder, sheepshead, tarpon, blue crab | Saltwater licence |
| 7 | **Gull Rock Coast** | Cold rocky shore, kelp forest, surf beach, lighthouse | The Kesh Upwelling side: fog, swell, kelp swaying in the surge. | Lingcod, rockfish, cabezon, halibut, surfperch, Dungeness crab | Act 2 story |
| 8 | **Calder Keys** | Warm mangrove islands and sand/grass flats, 0.3-2 m | Sight fishing: you see tails and shadows; the skiff's poling platform matters. | Bonefish, permit, tarpon, snook, barracuda, lemon shark | Flats skiff |
| 9 | **Calder Reef** | Coral reef, wrecks, drop-offs, 5-60 m | Colour and life; grouper bolting for holes, snapper schools. | Groupers, snappers, amberjack, cobia, hogfish, lionfish (invasive) | Bay boat + reef permit |
| 10 | **Blue Water** | Open ocean on the Calder Current edge, weedlines, floating debris, 200-1500 m | Huge, empty, blue, then sudden chaos: bait balls, birds, tuna crashing. | Mahi-mahi, wahoo, tunas, sailfish, marlin, swordfish, makos | Sportfisher + HMS permit |
| 11 | **The Lantern Trench** | A volcanic trench off Outer Calder, steep slopes, caves at 150-300 m | The endgame. Night, deep-drop electric reels, the great bioluminescent rise. | Deep groupers, tilefish, wreckfish, sixgill shark, **the coelacanth** | Act 3 story |

**Expeditions** (post-Act 2, by seaplane, see `docs/06` §7) add far waters with their own real communities: the
**Verdant Basin** (Amazon-type flooded forest), the **Mekong Reach** (great Asian river), **Kaldfjord** (Norwegian-type
fjord and Arctic coast), the **Old Country Canals** (European coarse fishing), and the **Northern Billabong**
(Australian tropical rivers).

### 4.2 Home base: Longwater Field Station

A ramshackle, half-flooded research station on stilts where the estuary begins, with a boatshed, a wet lab, a
radio room and a pier. It's the hub: the **Trust office** (quests, surveys, restoration projects), **Pell's Bait &
Tackle** (gear, rod building, bait), the **Fish Market** shed (selling), **Teodor's boatyard** (boats and repairs),
and the **Ledger room**, a wall of empty frames that fill with your catches. The station upgrades visibly as the
story goes on: patched roof, new pier lights, a working aquarium tank, a trophy wall.

## 5. Characters (all original)

| Name | Role | Personality and function |
|---|---|---|
| **Odile Marsh** | Head of the Longwater Basin Trust, a retired fisheries biologist | Gruff, funny, and secretly desperate to save the place. Gives the survey quests and teaches by radio. The game's teacher voice: "Your drag's set like you hate that line." |
| **Pell Okafor** | Runs Pell's Bait & Tackle | A craftsman who builds rods. Gives gear quests ("bring me a 5 lb smallmouth on the blank I built you"). Unlocks rod building. |
| **Teodor Brandt** | Old boatwright | Restores the first boat with you. Taciturn. Every boat upgrade has a short scene with him. |
| **Junie Alcott** | 12-year-old who runs the bait stand | Teaches the cast net and trap lines. Knows every local rumour. Has a running bet with you on the biggest bluegill. |
| **Captain Agnes Ruud** | Charter skipper at the old cannery | Big-game mentor for offshore. Knew the missing keeper. |
| **Dace Moreno** | Rival tournament angler | Flashy, sponsored, actually very good. Tournament rival; grudging respect arc. |
| **"Big" Ruth Delacroix** | Bayou catfish legend | Runs the bayou trap lines and the night catfish tournament. Warm, loud, superstitious. |
| **Officer Ken Sato** | Fish and wildlife warden | Checks licences, coolers, size and bag limits. Fair, dry humour. The face of regulations. |
| **Harlan Voss** | Regional director of **Grayline Fisheries** | The antagonist: a polite, reasonable-sounding executive pushing to rebuild the dam and run trawlers in the Sound. Never cartoonish. |
| **Wren Halloway** | The station's previous keeper, missing for 12 years | Never seen, only read. Her logbook pages are the story's spine. |

## 6. The story

### Premise

Twelve years ago the station's keeper, **Wren Halloway**, went out alone on the night sea beyond Outer Calder chasing
something she called *the Lantern*, and never came back. Without her the Trust lost its funding, the station rotted,
and Grayline Fisheries moved into the Sound. The Harrow Dam was decommissioned but its weir still blocks Glass Creek,
so the salmon have not run in a generation.

You answered Odile's job ad. The pay is whatever the fish are worth.

### The Ledger

Wren kept the **Ledger**, a field guide to every fish in the basin with her drawings and notes. It was scattered:
pages are in the station, at old camps, in bottles, with old-timers, and in a sunken skiff. **The Ledger is the game's
field guide and collection.** Each species entry fills in as you catch it, measure it, observe it, and complete its
research tasks. Wren's handwritten notes, which come back as you find her pages, are hints written by an expert and
the trail of clues to the Lantern.

### Act 1: The Survey (Millbrook Pond, Harrow Reservoir, Kesh Tarn)

- Learn to fish at Millbrook Pond with Odile on the radio. First survey: catch and release five species.
- Restore Wren's old rowboat, the *Teal*, with Teodor. **The boat is the first major unlock.**
- Survey Harrow Reservoir: deep structure, the drowned forest, night walleye. Meet Dace at the first bass
  tournament.
- The Trust's first restoration project: fund the fish ladder study for the Harrow weir by completing surveys.
- Find Wren's first logbook pages and a strange entry: *"The Lantern rose again over the Trench. It moves with the
  dark. Something old follows it up."*
- Act 1 ends when the Trust wins the hearing to **remove the Glass Creek weir**, against Grayline's lawyers.

### Act 2: The River Returns (Glass Creek, Blackwater Bayou, Harrow Sound, Gull Rock, Calder Keys)

- The weir comes down in a scripted, physical event (the water level and the river actually change). That autumn,
  **the first salmon run in 20 years** comes up Glass Creek. Your surveys of the run decide the Trust's next grant.
- The bayou with Big Ruth: trap lines, night catfish, a war on invasive snakeheads.
- The estuary: saltwater licence, tides, Agnes Ruud and the cannery pier. Grayline's trawlers are scraping the Sound.
  You gather catch data (tagging striped bass and red drum) for the Trust's case for a protected zone.
- Gull Rock and Calder Keys open up. Mangrove replanting and oyster-reef restoration projects visibly bring back
  snook nurseries and clearer water.
- Midpoint twist: Harlan Voss offers to buy the station. In Wren's later pages you learn she was compiling night
  catch data that Grayline buried, data showing a deep-water ecosystem off Outer Calder that would block their
  offshore leases.
- Act 2 ends with the Sound protection vote, which turns on your data. The protected zone passes, and Agnes offers
  you her old sportfisher.

### Act 3: The Lantern (Calder Reef, Blue Water, the Lantern Trench)

- Offshore fishing: reef, weedlines, the bluewater giants. Wren's final pages are coordinates and notes about the
  Lantern.
- **The Lantern is real and scientific:** it's the nightly **diel vertical migration** over the trench, when billions
  of bioluminescent lanternfish, squid and plankton rise from the deep at dusk. Predators follow them up:
  swordfish, sixgill sharks, deep groupers. Wren was right: *something old follows it up*.
- The finale is a night deep-drop over the trench's volcanic caves at the peak of the rise, using everything you have
  mastered. The target is a **West Indian Ocean coelacanth** (*Latimeria chalumnae*), the "living fossil", a real
  species once thought extinct for 66 million years. It's protected: the only right ending is to **bring it up
  gently, photograph it, and release it**, which completes Wren's work. (Its real habitat is deep volcanic caves on
  steep slopes at 150-250 m, and it's active at night; the game keeps that.)
- Ending: the Trust is secure and the station restored. You find out what happened to Wren: her boat's wreck sits
  at the trench rim, and her last log entry is a note to "whoever comes next." The credits roll over the Ledger
  pages.

### Post-game

The full Ledger, legendary individuals, expeditions, every property, tournaments circuit, and a "Grand Slam" set of
challenges (e.g. the real inshore slam: tarpon, bonefish and permit in one day).

## 7. Legendary fish (real species, record-class individuals)

Named individuals with history, rumours and tells. They are rare, they're found at specific conditions (time, moon,
tide, weather, season, spot), and they are protected by default (catch, photograph, release; the Ledger records it).

| Name | Species | Where and when | Lore |
|---|---|---|---|
| **Old Ironsides** | White sturgeon (*Acipenser transmontanus*), ~3.5 m | Lower Longwater deep hole, night, spring | Has broken lines for 60 years; three old hooks in its mouth. |
| **The Duchess** | Largemouth bass, ~6.5 kg | Millbrook Pond, the fallen oak, dawn, pre-spawn | Junie swears she's real. |
| **Lantern Jaw** | Muskellunge, ~1.4 m | Harrow Reservoir drowned forest, autumn, overcast | "The fish of ten thousand casts." Follows, never bites, until you figure-8. |
| **Brass** | Brown trout, ~9 kg | Glass Creek's deepest pool, moonless nights, streamers only | Hunts mice at night. |
| **The Priest** | Alligator gar, ~2.5 m | Blackwater Bayou oxbow, summer heat | Air-gulps at dusk. |
| **Mangrove King** | Tarpon, ~90 kg | Calder Keys channel bridge, full moon, May-June | Jumps eight times. |
| **Grandfather** | Atlantic blue marlin, ~450 kg | Blue Water, the Calder edge, late summer | Agnes's white whale. |
| **The Bishop** | Goliath grouper, ~300 kg | Calder Reef wreck | Fully protected. Can't be landed, only fought and released at the boat. |
| **The Old One** | West Indian Ocean coelacanth, ~1.8 m | The Lantern Trench caves, the rise | The finale. |

## 8. Tone

Warm, a little melancholy, quietly funny. Long stretches of calm with sudden chaos. The game respects fish: a
release animation where the fish kicks away from your hand is a small reward in itself. Dialogue is short, specific,
and written by people who fish. Nobody talks about "loot".
