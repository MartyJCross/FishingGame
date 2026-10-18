# 01: Vision, World and Story

## 1. The pitch

**The Ledger** is a 2D side-view fishing game where you can feel every gram on the line. You arrive at a broke,
half-flooded field station on **Florida's Nature Coast** and take the job nobody else wanted: *survey angler* for the
station's tiny fisheries trust. Your job is to catch, record and (mostly) release fish, keep the station alive, and
fight for the seagrass, oyster reefs and springs around it. You do it with the help of a vanished naturalist's field
guide, **the Ledger**, whose missing pages take you to real waters around the world. They lead from a Florida farm
pond to Lake of the Woods, Montana trout rivers, Louisiana swamps, Kona's blue water, and finally a night drop over
the volcanic caves of the **Comoros**, where an animal older than the dinosaurs' extinction still lives.

What you do minute to minute is real fishing, and it is hard to master. Load the rod with a backswing and release the
line at the right instant. Thumb a baitcaster spool so it doesn't bird's-nest. Skip a soft plastic under a dock.
Twitch a topwater into a zig-zag. Feel a bass's short inhale on the rod tip and sweep the hook home. Steer a running
fish with side pressure, ease the drag before it snaps 6 lb mono, and lower the rod as a tarpon erupts out of the
water.

What you do hour to hour is a living world: dawn bites and night predators, moon-driven tides, fronts that turn fish
on and off, salmon runs, mayfly hatches, bait balls with diving birds, and seasons with spawning and ice. You'll earn
better gear, boats, cabins on private water and crab pots. There's a story about a vanished keeper, and a mystery out
on the night sea she called **the Lantern**.

## 2. Design pillars

1. **Feel the line.** The rod, line, reel and water are physical. Every technique real anglers use should be possible
   because the simulation allows it, not because we scripted it. Information reaches the player the way it reaches an
   angler: rod-tip bend, line angle, slack, the drag's scream, a twitch in a float, rings on the surface. Meters are
   optional information assists.
2. **Real fish, real places, really behaving.** Hundreds of real species in real waters, with real habitats, diets,
   activity cycles and fight styles. Knowing fish is a skill: a player who learns that walleye feed at dusk on rocky
   drop-offs, or that bonefish tail on the rising tide, catches more. The Ledger rewards that knowledge.
3. **Hardcore and fair.** The physics is never softened. Rods break when high-sticked, lines snap, baitcasters
   backlash, slack loses fish. In exchange, the game is **consistent**: every failure has a visible, learnable cause.
4. **Mastery over grind.** Progression unlocks *where* and *what* you can fish, never makes your hands better.
5. **A coast worth restoring.** Home is damaged, and your catches, surveys and choices visibly repair it: oyster
   reefs come back, seagrass regrows, springs clear, juvenile fish return.
6. **Beauty in the details.** Stylized low-poly 2D with gorgeous light: glassy dawn springs, rain-pocked swamps, gold
   evening flats, black-water nights lit by bioluminescence. The water is the most beautiful thing on screen.

### What this game is not

- Not an arcade fishing game with a "keep the fish in the green zone" bar.
- Not a fantasy game: no invented fish, no magic lures. The only "monsters" are real animals at real record sizes.
- Not a survival or crafting grind. Light management (cooler space, bait, fuel) adds texture, not chores.

## 3. Player character and companion

The player is a **human angler** shown as a stylized, faceless low-poly figure with customizable outfit colours, hat
and vest. The rod is held in physically placed hands (2-bone arm IK to the rod butt), so the body reads the rod's
motion and leans against a pulling fish.

A **companion dog**, a scruffy station dog named **Skiff**, rides in the boat or sits on the bank, and travels with
you. It's diegetic feedback and charm: ears prick at a bite at night, it barks at a jumping fish, it sleeps when
nothing is happening. It never plays the game for you and can be turned off.

## 4. The world: real places

### 4.1 Rules for using real places

1. **Places are real; people and organizations are invented.** Real towns, rivers, lakes, reefs and islands are named
   and portrayed respectfully. Every character, business, trust and company in the story is fictional. Don't
   name or portray real businesses, guides, agencies or people. Wildlife officers are generic "state wildlife
   officers".
2. **Each spot's fish community is the real one** for that place and season (`05` §7). If a species doesn't live
   there, it isn't there.
3. **Regulations are real-style, simplified and frozen** (`06` §8): based on each place's rules as written at the
   time the data is authored, simplified for play, and not kept current. A notice in the game says so: "Regulations
   in this game are simplified and may be out of date. Always check current local rules before fishing."
4. **Geography is stylized.** A "spot" is a side-view slice inspired by a real kind of water at that place (a spring
   run, a mangrove creek, a rocky point). We don't copy real maps or real bathymetry precisely, and we don't need to.
5. **Respect local cultures.** The Comoros, the Amazon, Hawaii, Thailand and other destinations are portrayed with
   local guides as capable experts (never sidekicks or stereotypes) and real fishing traditions (for example Hawaiian
   shore fishing for giant trevally, and the Comorian deep-handline fishery for oilfish).

### 4.2 Home: Florida's Nature Coast

The station sits on the Gulf of Mexico coast of Florida between **Cedar Key and Homosassa** (the "Nature Coast").
It's a real, remarkable home base: within a short drive or boat ride there are farm ponds and lakes, crystal-clear
**spring runs**, a big blackwater river (the **Suwannee**), salt marsh and oyster bars, shallow seagrass flats, and the
open Gulf with its rock ledges, wrecks and deep water. So freshwater and saltwater are both home.

| Home spot | Water type | Character | Signature species | Unlock |
|---|---|---|---|---|
| **Station Pond** | Warm pond, 1-4 m, lily pads, a dock, a fallen live oak | The tutorial pond behind the station. Shore only. | Bluegill, Florida bass, black crappie, channel catfish | Start |
| **Spring Run** (e.g. Crystal River / Rainbow River type) | Gin-clear spring water, 22-23 °C all year, eelgrass, limestone | Sight fishing in fresh water; manatees and mullet; snook move in during cold snaps (real) | Florida bass, redear sunfish, mullet, snook (winter), sheepshead | Rowboat |
| **The Suwannee** | Tannin-dark river, limestone banks, cypress, sandbars | Wild and old. Gulf sturgeon leap out of it in summer (real, and protected). | Suwannee bass (found in almost no other river system), redbreast sunfish, flathead catfish, gar, bowfin | Jon boat |
| **Waccasassa Flats and Oyster Bars** | Shallow seagrass flats, salt marsh creeks, oyster bars | Tides rule everything. Tailing redfish at dawn. | Red drum, spotted seatrout, sheepshead, black drum, flounder, stone crab, blue crab | Saltwater licence |
| **Homosassa Tarpon Grounds** | Open, shallow Gulf flats off Homosassa | The historic giant-tarpon grounds; strings of rolling tarpon in May and June | Tarpon, cobia, sharks, Spanish mackerel | Flats skiff |
| **Nature Coast Ledges and Wrecks** | Gulf hard bottom, rock ledges, wrecks, 6-40 m | Grouper country | Gag and red grouper, red and mangrove snapper, amberjack, goliath grouper (protected), lionfish | Bay boat + reef permit |
| **The Middle Grounds and Deep Edge** | Offshore Gulf reefs and the deep shelf edge, 30-300 m | A long run out. Blue water, pelagics and deep-drop fishing | King mackerel, mahi, blackfin tuna, sailfish, snowy grouper, golden tilefish | Sportfisher |

### 4.3 Destinations (world travel)

Destinations unlock through Wren's pages, licences and money, and are reached by truck, plane or seaplane charter.
Each has 2-5 spots, a local character, and a camp or lodge you can buy.

| Destination | Water types | Signature species | Role |
|---|---|---|---|
| **Lake of the Woods** (Minnesota/Ontario) | Huge northern lake, rocky reefs, islands, weedbeds; frozen in winter | Walleye, sauger, northern pike, muskellunge, smallmouth, lake sturgeon, perch | Act 1 trip. Ice fishing in winter. |
| **Atchafalaya Basin and Louisiana marsh** | Cypress swamp, bayous, oxbows; coastal marsh | Alligator gar, bowfin, blue and flathead catfish, crappie, crawfish; bull redfish in the marsh | Act 1 trip. Big Ruth's home. |
| **Madison River** (Montana) + **Beartooth alpine lakes** | Big freestone trout river; high alpine lakes | Rainbow, brown, Yellowstone cutthroat and brook trout, mountain whitefish, Arctic grayling | Act 2. Fly fishing country; the salmonfly hatch. |
| **Columbia River and Oregon Coast** | Huge river with salmon runs; rocky kelp coast, surf beaches | White sturgeon, chinook, coho, sockeye, steelhead, American shad; lingcod, rockfish, halibut, surfperch, Dungeness crab | Act 2. |
| **Chesapeake Bay and Outer Banks** | Big estuary; Atlantic surf beaches and inlets | Striped bass, blue crab, bluefish, weakfish, croaker, big red drum in the surf, snakehead (invasive) in the Potomac | Act 2. |
| **Florida Keys** (Islamorada and the backcountry) | Flats, mangroves, coral reef, the Gulf Stream | Bonefish, permit, tarpon, barracuda; reef snappers and groupers; sailfish, mahi, wahoo; daytime deep-drop swordfish | Act 2. |
| **Kona, Hawaii** | Steep volcanic coast, deep blue water close to shore | Pacific blue marlin, yellowfin tuna (ahi), wahoo (ono), mahi, spearfish; giant trevally (ulua) from shore | Act 3. |
| **Grande Comore, Comoros** | Steep volcanic slopes dropping to great depth; caves at 150-250 m | Oilfish, deep snappers, sixgill shark, yellowfin, giant trevally, dogtooth tuna, **the coelacanth** | Act 3 finale. |
| **Rio Negro, Amazon** (Brazil) | Blackwater river, flooded forest, lagoons | Speckled peacock bass, arapaima, piranha, payara, arowana, redtail catfish, piraíba | Post-game |
| **Thailand: Mae Klong and Mekong** | Big tropical rivers | Giant freshwater stingray, giant snakehead, striped catfish, Siamese giant carp, Mekong giant catfish (release only) | Post-game |
| **Northern Norway** (Lofoten, Saltstraumen, Alta) | Fjords, the world's strongest tidal current, salmon rivers, Arctic coast | Atlantic cod, saithe, halibut, wolffish, ling, red king crab, Atlantic salmon, Greenland shark | Post-game. Aurora. |
| **England and Spain** (a carp lake, a chalk stream, the River Wye, the Ebro) | Lakes, rivers, canals | Common and mirror carp, tench, bream, barbel, chub, grayling, pike, perch, zander, wels catfish | Post-game |
| **Northern Australia** (Northern Territory rivers + the Murray) | Tropical rivers and estuaries; an inland river | Barramundi, saratoga, mangrove jack, queenfish, mud crab; Murray cod, golden perch | Post-game |

### 4.4 Home base: the field station

**Cedar Shoals Field Station** (fictional), a ramshackle research station on stilts at the edge of the marsh, with a
boatshed, a wet lab, a radio room and a pier. It's the hub: the **Trust office** (quests, surveys, restoration
projects), **Pell's Bait & Tackle** (gear, rod building, bait), the **fish house** (selling), **Teodor's boatyard**
(boats and repairs), the **map room** (travel), and the **Ledger room**, a wall of empty frames that fill with your
catches. The station upgrades visibly as the story goes on.

## 5. Characters (all fictional)

| Name | Role | Personality and function |
|---|---|---|
| **Odile Marsh** | Head of the Cedar Shoals Fisheries Trust, a retired fisheries biologist | Gruff, funny, secretly desperate to save the place. Gives survey quests and teaches by radio: "Your drag's set like you hate that line." |
| **Pell Okafor** | Runs Pell's Bait & Tackle | A craftsman who builds rods. Gear quests; unlocks rod building. |
| **Teodor Brandt** | Old boatwright | Restores the first boat with you. Taciturn. Every boat upgrade has a short scene with him. |
| **Junie Alcott** | 12-year-old who runs the bait stand | Teaches the cast net and crab traps. Knows every rumour. Running bet on the biggest bluegill. |
| **Captain Agnes Ruud** | Offshore charter skipper | Big-game mentor. Knew Wren. Later lends you her old sportfisher and flies with you to Kona. |
| **Dace Moreno** | Rival tournament angler | Flashy, sponsored, actually very good. Tournament rival with a grudging-respect arc. |
| **"Big" Ruth Delacroix** | Atchafalaya catfish legend | Runs crawfish traps and a night catfish tournament. Warm, loud, superstitious. |
| **Ingrid Halvorsen** | Lodge owner at Lake of the Woods | Taught Wren ice fishing. Holds her first pages. |
| **Kalani Akana** | Kona captain and ulua (giant trevally) shore fisher | Keeps Hawaiian shore-fishing traditions; Act 3 mentor. |
| **Mzé Abdou** | Comorian deep-handline fisher | Fishes at night from a small boat as his grandfather did. He's the one who knows the caves. The finale is his story as much as yours. |
| **The wildlife officer** | Generic state wildlife officer at each destination | Checks licences, coolers, size and bag limits. Fair, dry humour. |
| **Harlan Voss** | Regional director of **Grayline Coastal** (fictional) | The antagonist: a polite, reasonable-sounding developer who wants to dredge a deep channel through the flats for a mega-marina. Never cartoonish. |
| **Wren Halloway** | Founder of the station and the Ledger's author; missing for 12 years | Never seen, only read. Her logbook pages are the story's spine. |

## 6. The story

### Premise

**Wren Halloway** spent forty years fishing the world's waters and filling the Ledger, a field guide of every fish
she met, with drawings and notes. She founded Cedar Shoals Field Station to protect the Nature Coast. Twelve years
ago, on a trip to the Comoros, she went out alone at night following something she called *the Lantern* and never
came back. Without her the Trust lost its funding, the station rotted, and the Ledger's pages were scattered: left
with friends, in camps, in bottles, in a sunken skiff.

Now Grayline Coastal wants to dredge a channel through the seagrass and oyster bars for a marina, and the Trust has
one season to prove what the coast is worth. You answered Odile's job ad. The pay is whatever the fish are worth.

### The Ledger

**The Ledger is the game's field guide and collection.** Each species page fills in as you catch it, measure it,
observe it and complete its research tasks. Wren's handwritten notes come back as you find her pages. They're hints
written by an expert and the trail to the Lantern. Each destination holds pages that only unlock by fishing there.

### Act 1: The Nature Coast (home, Lake of the Woods, the Atchafalaya)

- Learn to fish at the Station Pond with Odile on the radio. First survey: five species, measured and released.
- Restore Wren's old rowboat, the *Teal*, with Teodor. **The boat is the first major unlock.** It opens the spring run.
- First saltwater: the flats and oyster bars, a cast net, crab traps with Junie. Meet Dace at the first bass
  tournament.
- Wren's first page points north: Ingrid's lodge on **Lake of the Woods** (walleye at dusk, musky, and ice fishing
  in winter). The next points to **Big Ruth** in the Atchafalaya.
- The first restoration project at home: an oyster-reef rebuild, funded by surveys.
- A strange entry: *"The Lantern rose again over the caves. It moves with the dark. Something old follows it up."*
- Act 1 ends when the Trust wins a delay on the dredge permit, using your catch data.

### Act 2: The Ledger's Road (Madison River, Columbia and Oregon, Chesapeake and Outer Banks, the Keys)

- Wren's pages send you west to the **Madison** for the salmonfly hatch and fly fishing. Then to the **Columbia**,
  where an ancient white sturgeon called *Old Ironsides* has broken lines for sixty years. Then to the **Chesapeake**
  for striped bass and blue crabs, and the **Outer Banks** surf for giant red drum.
- At home: seagrass replanting, a spring clean-up, mangrove planting. Each project visibly changes the water and
  fish. Gag grouper and the summer tarpon run on the Nature Coast.
- The **Keys**: bonefish, permit and tarpon on the flats. Agnes takes you offshore for sailfish, and to the deep for a
  daytime swordfish.
- Midpoint twist: Harlan Voss offers to buy the station. Wren's later pages reveal she'd found old survey data on the
  coast's seagrass nurseries, data Grayline's consultants buried.
- Act 2 ends with the permit hearing. Your survey data, the restored reefs and the returned fish win it. Agnes gives
  you her old sportfisher.

### Act 3: The Lantern (Kona, the Comoros)

- **Kona** with Agnes and Kalani: Pacific blue marlin, tuna, and night fishing for ulua from the lava rocks. Wren's
  last pages are about the Comoros and Mzé's family.
- **The Lantern is real and scientific.** It's the nightly **diel vertical migration**, when vast numbers of
  bioluminescent lanternfish, squid and plankton rise from the deep at dusk. Predators follow them up. Off Grande
  Comore's steep volcanic slopes, Wren was right: *something old follows it up*.
- **The finale:** night deep-drop fishing with Mzé over the caves, for oilfish, the way Comorian fishers have done
  for generations. Coelacanths were sometimes caught this way by accident (real history), and almost never survived:
  they live in cold water at 150-250 m and the warm surface kills them. When one takes your bait, the challenge is to
  bring it up slowly and send it straight back down with a **descender** before it overheats: the first coelacanth
  returned alive to its cave. Its photo completes Wren's Ledger.
- An in-game note at that moment: *Coelacanths are critically endangered and strictly protected. In reality they
  must never be fished for.*
- Ending: you find Wren's boat on the rim of the caves in a dive camera's light, and her last entry, written "for
  whoever comes next". The credits roll over the Ledger pages.

### Post-game

The full Ledger, legendary fish, the post-game destinations (Amazon, Thailand, Norway, England and Spain,
Australia), every property, the tournament circuit, and real "slam" challenges (the Keys' grand slam: tarpon, bonefish
and permit in one day).

## 7. Legendary fish (real species, record-class individuals)

Named individuals with history, rumours and tells. They're rare, appear under specific conditions (time, moon,
tide, weather, season, spot) and are release only. The Ledger records each encounter.

| Name | Species | Where and when | Lore |
|---|---|---|---|
| **The Duchess** | Florida bass (*Micropterus floridanus*), ~7 kg | Station Pond, the fallen oak, dawn, pre-spawn | Junie swears she's real. |
| **The Bishop** | Goliath grouper (*Epinephelus itajara*), ~300 kg | A Nature Coast wreck | Fully protected. You can only fight it and release it at the boat. |
| **Silver King** | Tarpon (*Megalops atlanticus*), ~90 kg | Homosassa, May-June, calm dawns | Jumps eight times. |
| **Lantern Jaw** | Muskellunge (*Esox masquinongy*), ~1.4 m | Lake of the Woods, autumn, overcast | "The fish of ten thousand casts." Follows, never bites, until you figure-8. |
| **The Priest** | Alligator gar (*Atractosteus spatula*), ~2.5 m | Atchafalaya oxbow, summer heat | Gulps air at dusk. |
| **Brass** | Brown trout (*Salmo trutta*), ~9 kg | The Madison's deepest run, moonless nights, streamers or mouse flies only | Hunts mice at night. |
| **Old Ironsides** | White sturgeon (*Acipenser transmontanus*), ~3.5 m | Columbia River deep hole, night, spring | Three old hooks in its mouth. |
| **Grandfather** | Blue marlin (*Makaira nigricans*), ~450 kg | Kona, late summer | Agnes's white whale. |
| **The Old One** | West Indian Ocean coelacanth (*Latimeria chalumnae*), ~1.8 m | Grande Comore caves, the rise | The finale. |

## 8. Tone

Warm, a little melancholy, quietly funny. Long stretches of calm with sudden chaos. The game respects fish: a
release where the fish kicks away from your hand is a small reward in itself. Dialogue is short, specific, and
written by people who fish. Nobody talks about "loot".
