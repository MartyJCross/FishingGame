# 03: Fishing Mechanics

This is what the player does and feels. `04_PHYSICS_SPEC.md` is how the sim makes it true. Rule: **every mechanic
here must come out of the physics.** Where this doc says "the lure skips", it means the lure-water contact model
produces a skip. Nothing below is a scripted animation.

## 1. The control philosophy

- **The player's hand is the rod hand.** Mouse movement or the right stick moves the rod. Speed matters: a slow
  sweep and a sharp flick are different inputs, and the rod's mass and flex respond to them.
- **The other hand is the reel hand.** It controls the line: holding it (finger on the spool or thumb on a
  baitcaster), letting it go, cranking, and setting the drag.
- **Hooksets, twitches, pops, jigs, side pressure, pumping and casting are all rod motions.** None of them is a
  button press that plays an animation.
- Inputs map through Godot's InputMap and are fully rebindable. Both schemes are first-class from M2 on.

## 2. Control schemes

### 2.1 Mouse + keyboard (default)

| Input | Action |
|---|---|
| **Mouse movement** | Moves the rod. The cursor sets the **rod-tip target direction** from the angler's hands. The hands follow with a strong but finite-torque spring, so flick speed is limited by a believable arm. Optional "relative mode" (captured cursor, mouse deltas rotate the rod) in settings. |
| **Left mouse button (hold)** | **Line hold**: finger on the line or thumb on the spool. Held = no line leaves the reel (bail closed, or thumb pressing). Released during a cast = line flows. Tap during flight = brief feathering. Holding during flight ramps thumb pressure up over ~0.25 s (feathering to slow and stop the lure). |
| **Right mouse button (hold)** | **Crank at the set retrieve speed** (smooth, steady retrieve). |
| **Mouse wheel** | **Crank by hand**: each wheel notch = 1/8 handle turn (tunable). Spinning the wheel fast cranks fast, and stopping stops. Precise, tactile retrieves: stop-and-go, slow-roll, burning. |
| **Shift + wheel** or **Q / E** | Drag tighter / looser (in clicks: the reel's drag has 12-20 detents). |
| **Ctrl + wheel** or **Z / C** | Set the right-button crank speed. |
| **Space** | Open/close the bail (spinning) or press the free-spool button (baitcaster). Usually automatic: pressing line hold before a cast opens the bail. |
| **A / D** | Walk along the bank, dock or boat deck. With a boat: see §9. |
| **W / S** | Lift/lower the hands (reach), e.g. lift the rod high over weeds, or plunge the tip under water to keep a fish off the surface. |
| **F** | Net / grip / gaff / tail-rope (the right tool for the fish and gear). Timing and position matter (§7). |
| **R** | Rig menu: change lure, bait, float depth, leader. |
| **Tab** | Ledger / journal. **M**: map. **Esc**: pause. **F1**: dev tuning panel (dev builds). |

### 2.2 Gamepad (Xbox layout; PlayStation equivalents via Godot's mapping)

| Input | Action |
|---|---|
| **Right stick** | Rod. The stick's angle maps to the rod angle across a ~200° arc, and stick deflection sets reach. Flicking the stick produces a fast rod swing, limited by the same arm model as the mouse. |
| **Left trigger (analog)** | **Line hold / thumb pressure**, fully analog. Half-pressure during a baitcaster cast is feathering. The best feathering control in the game. |
| **Right trigger (analog)** | **Crank speed**, fully analog. Retrieve speed is exactly what the trigger says. |
| **LB / RB** | Drag looser / tighter (one detent per press; hold to repeat). |
| **Left stick** | Walk / move along the deck; with a boat: motor or oars (§9). |
| **A** | Net / grip / gaff. **B** | Bail / free-spool. **X** | Rig menu. **Y** | Camera toggle (angler view / lure view). |
| **D-pad** | Quick-select 4 rigs. **View** | Ledger. **Menu** | Pause. |
| **Rumble** | The line talks through the triggers and motors (Xbox impulse triggers where Godot supports them, otherwise both motors): a tap for each bite knock, a continuous buzz for drag slip that scales with slip speed, a pulse for each head shake, a heavy thud on hookset. Strength in settings. |

### 2.3 Accessibility

- Toggle-hold for line hold and crank. One-handed layouts. Adjustable rod-follow stiffness (makes fast flicks
  easier). All audio cues have optional visual equivalents. Colour-blind safe line and HUD colours.

## 3. The rod in hand

- The rod is held at the butt by two hands. The **hands are kinematic** and follow input through a spring-damper
  with a torque limit and angular speed limit (arm model). The rod is a flexible XPBD beam, so a fast hand motion
  bends it and the tip lags behind, then whips through.
- The player sees the rod bend and the line angle, and feels them through rumble and audio. That is the core
  feedback, so the rod must be drawn big and clear: the camera frames the angler and the rod tip with room for the
  bend.
- **Rod angle is physics.** Holding the rod high under a heavy load overloads the tip section (high-sticking) and can
  break it. Holding it low and to the side applies side pressure that turns the fish. (In the 2D view, "side
  pressure" means changing the angle of the pull across the fish's body: see `04_PHYSICS_SPEC.md` §1.)

## 4. Casting

A cast works like this: the line is held, the rod is swung so the lure's weight **loads** the rod (it bends), and
the line is let go at the right moment so the rod **unloads** and throws the lure. The release angle and timing set
the trajectory. Line then flows off the reel until the lure lands or the line is stopped.

### 4.1 Cast types (all emergent)

| Cast | How the player does it | What makes it work in the sim |
|---|---|---|
| **Overhead** | Hold line, rod back past vertical, sharp forward stroke, release at ~10-11 o'clock | Rod load + tip velocity at release; the release angle sets the launch angle. Most distance. |
| **Sidearm** | Same, but the rod swings low and horizontal, release early | Low trajectory, goes under branches. |
| **Pitch** | Short drop, underhand lift-and-swing of the lure, release low | Small, quiet, accurate. A small splash (entry energy) doesn't spook fish. |
| **Flip** | Line stripped in short hand-length loops (hold W, sweep); lure swung pendulum-style to a target meters away | Line out fixed; pendulum physics; very quiet entry. |
| **Skip cast** | Hard, low sidearm, release so the lure meets the water at a shallow angle | Water-contact model: shallow angle + high speed + flat lure profile = bounce (planing) with energy loss per skip. Skirted jigs and soft plastics skip well, crankbaits don't. Under docks and overhanging trees, where the big fish are. |
| **Pendulum (surf)** | Long drop, swing the sinker out behind and around, then a full overhead power stroke | The long drop loads a long surf rod hugely; up to 100-150 m with heavy sinkers. |
| **Lob (bait)** | Soft, slow swing with soft live/cut bait | **Bait can tear off** if the cast's peak acceleration exceeds its hook-hold (real: hard casts throw off soft bait). |
| **Fly casting** | Different system (§4.4) | The line's weight is cast, not the fly. |
| **Drop (vertical)** | From a boat or ice hole: open the bail, let the rig fall | Sinker mass, line drag in water, current. Count-down to a depth. |

### 4.2 Reel types and how they cast

- **Spincast** (starter, closed face): button = line hold. Forgiving, short range, no backlash. Very low friction
  variation. Kids' reel.
- **Spinning** (open face): finger hold, bail. Line comes off the spool's end in coils with friction that grows as
  the spool empties (real). No backlash; **wind knots** when slack line is cranked onto the spool (§8).
- **Baitcaster**: the spool turns during the cast. **The spool has inertia** and keeps spinning when the lure slows
  (air drag, entry). If the spool feeds line faster than the lure takes it, the line overruns into a **backlash**
  (bird's nest), and the cast stops dead (and the lure may snap off). Prevention: the brake setting
  (centrifugal/magnetic, 0-10 on the reel; a customization) and the **thumb** (LMB/LT feathering), especially just
  before the lure lands. Mastered baitcasters cast farther and much more accurately than spinning. Clearing a backlash
  takes a short line-picking interaction (a few rod-hand motions), with a chance to have to cut line.
- **Conventional / lever drag** (offshore): like a baitcaster for casting (rarely cast), with a lever drag and a
  strike position.
- **Fly reel**: storage and drag only; casting is done with the line.
- **Electric reel** (deep drop): motor retrieve with speed setting; casting is just a drop.

### 4.3 In-flight control

- **Feathering** (line hold pressure during flight) slows the lure, straightens the line and improves accuracy.
  Stopping it fully just before entry ("closing the bail in the air") makes a soft entry and has the line tight
  for an immediate strike.
- **Wind** pushes the lure and the line bows downwind (the sim's line has air drag); casting into wind loses distance,
  and casting across it curves the lure.
- **Trees, overhangs and structure** can catch the lure or line in flight (snag, §8).

### 4.4 Fly fishing

- The fly line is a heavy, thick rope (3-12 weight classes by mass per length). The fly is nearly weightless.
- **False casting:** the rod moves between ~10 o'clock and ~2 o'clock with a stop at each end. A crisp stop forms a
  loop that unrolls along the line; a sloppy arc makes a big, weak loop or a tailing loop (knot). The sim's rope
  produces this when the rod-tip path is straight and the stop is sharp. **This is the most technical skill in the
  game, and the most beautiful.**
- **Shooting line:** releasing the line hold on the forward cast lets extra line shoot.
- **Roll cast:** with line on the water, slowly lift the rod back, then a sharp forward stroke rolls the line out
  (water tension anchors it; the sim's surface contact model with line drag on water makes it happen).
- **Mending:** flipping a belly of line upstream on the water (a rod flick) so the current doesn't drag the fly
  unnaturally. **Drag-free drift** matters to trout (they refuse a fly moving faster than the current). The sim knows
  the fly's speed relative to the local current.
- **Stripping:** with fly gear, line hold + crank becomes "strip": RMB/RT pulls line in hand-length strips (the line
  piles at the angler's feet as slack).
- Flies: dry (floats in the surface film), emerger, nymph (sinks, drifts), streamer (stripped), wet fly (swung
  across current), poppers. **Fly tying** (`06` §3.5) builds them.

## 5. Retrieves and presentations

All retrieves come from rod motion + crank rate + the lure's hydrodynamics (`04` §6).

| Retrieve | Input | What the lure does | For |
|---|---|---|---|
| **Steady** | constant crank | Crankbait dives to its depth and wobbles; spinner blade turns | Most predators |
| **Burn** | fast crank | Lure near its speed limit; spinners/spoons roll and flash | Aggressive fish, warm water, schools |
| **Slow-roll** | very slow crank | Spinnerbait just ticking along | Cold water, lethargic fish |
| **Stop-and-go** | crank, pause | Floating cranks rise on the pause (suspending ones hang) | Strikes on the pause |
| **Twitch / jerk** | sharp short rod twitches with slack | Jerkbait darts sideways, then suspends | Cold-water bass, pike, trout |
| **Walk-the-dog** | rhythmic tip twitches on slack line | Topwater stick bait zig-zags on the surface | Bass, stripers, seatrout, snook |
| **Pop** | short sharp twitch | Popper cup throws water and makes a "bloop" | Topwater, bluegill to tuna |
| **Hop / jig** | lift the rod, let the lure fall on semi-slack line, reel the slack | Jig hops along the bottom, kicking up sand puffs | Bass, walleye, flounder, drum |
| **Drag** | slow sweep along the bottom | Carolina rig or tube crawls | Bass, drum, catfish |
| **Yo-yo / vertical jig** | sharp lift then free-fall straight down | Metal jig flutters on the fall | Tuna, amberjack, cod, lake trout, ice fishing |
| **Dead-stick** | do nothing | Bait/lure sits, current moves it | Bottom fishing, floats, finicky fish |
| **Drift** | boat drifts with wind/current | Rig bounces bottom | Flounder, halibut, walleye |
| **Troll** | boat motor moves the lures | Lures run at their speed-dependent depth | Walleye, lake trout, salmon, offshore |
| **Figure-8** | at the boat, sweep the rod tip in a big figure-8 with the lure in the water | Lure keeps moving and changes direction | Muskellunge following to the boat (real) |

### Float (bobber) fishing

- Fixed float or **slip float** with a bobber stop to set depth (any depth, set in the rig menu, physically enforced:
  the line slides through the float until the stop).
- The float's buoyancy and weighting decide how it shows bites: a well-weighted float with only a tip showing reveals
  tiny nibbles; an over-buoyant one hides them. **Float dips, lies flat (lift bite), bobs, or runs**: each is a real
  bite signature from a species.

### Bottom rigs

- **Rigs are physical:** the line, swivel, leader, sinker and hook are separate sim bodies on the rope. A Carolina
  rig's sliding sinker really slides; a drop shot's hook really sits above its weight; a fish-finder rig lets a
  catfish run without feeling weight. A hair rig (carp) exposes the hook.
- **Bite detection on the bottom:** rod-tip taps, line twitches, line going slack (fish swam towards you), or the
  reel's **bite alarm/clicker** (bait-runner / clicker reels, an upgrade) screaming as a carp or catfish runs.

## 6. Bites and the hookset

### 6.1 How fish bite (species-specific, from `05`)

| Bite style | What the player sees and feels | Examples |
|---|---|---|
| **Inhale** | A tick on the line, slack, or "mushy" weight; the fish sucks the lure in and can blow it out in under a second | Largemouth, crappie, bluegill on jigs |
| **Nibble / peck** | Several small taps, bait slowly disappearing | Panfish, sheepshead (the bait thief), pinfish |
| **Slam / reaction** | A sudden hard pull, often hooks itself | Pike, bluefish, barracuda, wahoo, trout on spinners |
| **Run** | Line pours out steadily; let it go, then set | Catfish, carp, striped bass on bait, sharks |
| **Lift** | Float lies flat or rises (the fish lifted the weight) | Bream, tench, carp |
| **Short strike** | Hits but misses the hooks | Pike/musky on big lures, tired fish, cold water |
| **Follow** | A shadow trailing the lure, sometimes to the boat | Musky, pike, trout, bonefish inspecting a fly |
| **Surface sip / rise** | Rings on the water, the dry fly gone | Trout, grayling |
| **Crash** | Explosion on a topwater | Bass, snook, tarpon, tuna, GTs |
| **Pick-up and drop** | A tap, then nothing; the fish felt resistance | Walleye, carp, finicky fish |

### 6.2 The hookset

- The hookset is a **rod motion**: a sharp sweep that pulls the hook point into the jaw. The sim computes the force at
  the hook from the rod's flex, line stretch, slack and line length.
- **Line matters**: with 40 m of stretchy mono out, a sweep barely moves the hook; with braid it's instant. A long
  sweep (or reeling down to the fish first) is needed with mono at distance. Real.
- **Hook type matters**:
  - **J-hooks** need a firm hookset.
  - **Circle hooks** must **not** be swept: the fish turns and the hook rolls into the jaw corner with steady
    tension. A hard sweep pulls a circle hook out. Used for catfish, tarpon, sharks and offshore live bait (and
    much kinder to fish: corner hooks).
  - **Trebles** hook on contact but hold less well under heavy pressure and jumps.
- **Timing**: set too early (before the fish closes its mouth or turns) and the lure comes away; too late and the fish
  spits it, or with bait swallows it (**deep hooking**, which harms the fish and lowers release survival).
- **Hook penetration** depends on force, hook sharpness, wire gauge, and the species' mouth hardness (tarpon have
  famously hard, bony mouths; crappie have papery mouths that tear). Hook-hold quality comes out of the hookset and
  sets how likely the hook is to pull later.

## 7. The fight

### 7.1 The physical loop

The fish pulls (its thrust, direction and behaviour state from its fight model) → the line stretches and the rod bends
→ the tension reaches the reel → **if tension exceeds the drag setting, the drag slips** and line goes out with a
scream whose pitch follows slip speed. The player:

- **Sets the drag.** The real rule of thumb is about 25-33% of the line's breaking strength, but a jumpy fish near
  the boat needs less. Too tight and the line snaps on a surge; too loose and the fish reaches the snags or
  **spools you** (runs all the line off the reel).
- **Uses the rod angle.** The rod is a shock absorber and lever. Rod up and back = pressure; rod low and to the side =
  **side pressure** that turns the fish's head. Steering a fish away from a snag is a core skill.
- **Pumps and reels.** On a heavy fish, lift the rod slowly (gain line from the fish), then reel down quickly while
  lowering it. Cranking against a slipping drag only twists line (§8).
- **Bows to the king.** When a fish jumps (tarpon, mahi, trout, smallmouth, marlin), lower the rod and push it
  toward the fish. A tight line on a jumping fish is the main way hooks are thrown and lines are broken (shake + the
  fish's falling weight). The sim models that: head shakes in the air against a taut line strongly degrade the hook
  hold.
- **Keeps tension.** Slack lets barbless hooks fall out and lets shaking fish work the hook loose.

### 7.2 What can go wrong (all real, all simulated)

| Failure | Cause |
|---|---|
| **Line breaks** | Tension > line strength × knot factor × wear. Wear comes from rock/coral/pilings abrasion, sharp teeth (pike, bluefish, wahoo, barracuda, sharks), gill plates (snook), and old line. |
| **Bite-off** | Toothy fish against mono or fluoro without a wire or heavy leader. |
| **Rod breaks** | Bending beyond a section's limit, typically high-sticking with a heavy fish near the boat, or a jammed drag. |
| **Hook pulls** | Hook-hold degraded by slack, shakes, jumps, soft mouths, poor hookset, trebles under heavy load. |
| **Hook straightens** | Light-wire hooks under heavy drag (real, intentional: lets you free snags). |
| **Spooled** | Line capacity exhausted on a long run. |
| **Wrapped / cut off** | The fish reaches structure (weeds, timber, pilings, coral, a crab trap line) and the line wraps or is cut. Grouper and snook head for holes the moment they're hooked: the first seconds decide it. |
| **Tail-wrapped / foul hooked** | A fish hooked outside the mouth fights sideways and harder; regulations may require release. |
| **Taken by a predator** | Sharks, barracuda, big bass, ospreys or alligators may grab a hooked fish (realistic, rare, a few species and places). |

### 7.3 Fish fight behaviours (from species data)

**Run** (fast, long, straight), **Dive** (straight down, deep), **Bulldog** (heavy, short, head-down surges), **Circle**
(tuna death circles under the boat), **Jump** (with head shakes in the air), **Headshake**, **Roll** (death roll
twisting the line: gar, eels, sturgeon), **Structure dash**, **Sulk** (sits on the bottom like a rock: halibut,
sturgeon, big catfish), **Tow** (big fish pull the boat: sharks, tuna, a "sleigh ride"), **Boat run** (a sudden run at
you, slack!), **Surface skitter** (smaller fish skating across the top). Each species has weighted state transitions,
stamina, and power from `05`.

### 7.4 Stamina and the end of the fight

- A fish has aerobic and anaerobic energy (two pools). Hard runs burn the anaerobic pool fast; it recovers slowly
  while the fish rests or the line is slack. Fights end when the fish is **exhausted** (it rolls on its side, its
  struggle weakens).
- **Fight duration and water temperature affect survival** (lactate build-up; warm water holds less oxygen).
  Long, sloppy fights lower the fish's condition and release survival. Good anglers fight fish fast with strong
  enough gear: the game rewards that with better "release quality" in the Ledger and Trust reputation.

## 8. Line management and mishaps

- **Snags:** lures and hooks catch on simulated snag geometry (weeds, timber, rocks, coral, oyster bars, pilings,
  branches above water). Weedless rigs (Texas-rigged soft plastics, weedless frogs, weed guards) and snagless sinkers
  reduce snag chance. To free a snag: change the pull angle (walk or move the boat past it), **pop** it (snap a slack
  line taut), or pull until something gives (light-wire hooks straighten; otherwise the line breaks at the weakest
  point, usually the knot). Lost lures are lost.
- **Wind knots** (spinning): cranking slack line in loose coils onto the spool can make a knot that jams the next
  cast (and weakens the line). Prevent by keeping tension when cranking after a cast.
- **Line twist** (spinning): cranking while the drag slips, or a spinning lure without a swivel, adds twist that
  shows as loops in slack line. Light model only: a twist counter that raises wind-knot odds; fixed by trailing line
  behind the moving boat or changing the line.
- **Tangles:** the line wrapped around the rod tip (on a sloppy cast) prevents casting until cleared with a flick.
- **Line wear:** each segment has wear; the whole line has age. Re-spooling is a shop action. Re-tying the knot after
  a big fight resets the knot (real advice).

## 9. Boats and moving

- **Rowboat:** oars are physical; A/D or the left stick rows left/right oar (both together for straight). Quiet.
- **Outboard / tiller:** throttle and steer; loud (spooks shallow fish), fast.
- **Trolling motor:** quiet slow positioning while fishing (bass boat, bay boat). Hands-free with the left stick
  while the right stick holds the rod.
- **Push pole** (flats skiff): silent, the best way to stalk flats; standing on the poling platform raises the
  angler's eye height, so you see farther into the water (the camera shows more).
- **Anchor / drift sock / spot-lock** (late-game GPS trolling motor).
- **Drifting** with wind and current is a fishing technique (drift for flounder, halibut, walleye).
- **Big fish can tow the boat.**
- In a boat you fight a fish **around the boat**: the fish can run under it (line on the hull: fail), and the angler
  walks the deck or passes the rod around the bow/motor. Big game fights are in the **fighting chair** with harness.

## 10. Landing and after the catch

| Tool | Use | Skill |
|---|---|---|
| **Hand / lip grip** | Small fish, bass (lip), panfish | Lift at the right moment; a fresh fish shakes off |
| **Landing net** | Most fish | Lead the fish's head into the net with the rod; don't stab at its tail (scares it into a last run) |
| **Lip gripper tool** | Toothy or slimy fish | Precise timing |
| **Gaff** | Big saltwater fish **to be kept** (kills or injures; never on fish to be released, and illegal on some) | Strike behind the head as it comes alongside |
| **Tail rope / release at the boat** | Billfish, big sharks, protected giants | Leader touch counts as a catch; release boat-side |
| **Beach / slide** | Surf fishing; big fish ashore on a wave | Time the wave surge |

Then:

1. **Measure and weigh** on a measuring board and scale (the species' real length-weight relationship decides the
   weight; individual condition varies ±15%).
2. **Photo** (a photo-mode pose; adds to the Ledger; required for some quests and for every protected fish).
3. **Unhook** (pliers, a dehooker; deep hooks can be cut; this is a short interaction, gloves for spiny fish).
4. **Release**, holding the fish facing into the current until it kicks away (reviving matters for exhausted fish),
   or **venting/descending** deep-water fish suffering barotrauma (a descender device, real and essential for
   rockfish and snapper brought up from depth), or **keep** (livewell for live tournament fish, cooler on ice to
   sell, bait well for live bait).
5. **Regulations** apply on keep: species protections, minimum/maximum sizes ("slot limits"), bag limits per day,
   seasons. See `06` §8.

## 11. Information and assists

The game prefers diegetic information. Assists are optional, labelled, and all off in **Purist mode**.

| Information | Diegetic source | Optional assist |
|---|---|---|
| Line tension | Rod bend, line hum pitch, rumble, the drag sound | Tension meter around the reel icon |
| Drag setting | Click count heard when adjusting, reel dial on the HUD reel | Number in kg/lb |
| Line out | Spool fill shown on the reel icon | Metres/feet readout |
| Lure depth | Count-down timing, line angle, sonar | Depth readout |
| Cast aim | Practice | Trajectory arc preview |
| Bites | Rod tip, line, float, clicker, splash, the dog | Bite flash icon |
| What fish is near | Visual (limited by water clarity, see `07` §6), sonar | Species silhouette hints |
| Hookset timing | Practice | "Set!" flash (training levels only) |

**Training:** the station has a **casting lawn** (accuracy hoops on grass, a skip-cast dock, a target pool) and
Odile's radio lessons. Short drills for each technique: overhead, sidearm, pitch, skip, baitcaster thumbing, walk-the-dog,
roll cast, false cast, mending.
