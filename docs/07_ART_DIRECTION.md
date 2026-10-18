# 07: Art Direction and How to Achieve It

## 1. The look in one paragraph

A **side-on "over-under" view**, like a split-level underwater photograph: the top of the screen is the world above
the water (sky, hills, trees, the angler, boats), the bottom is the world below (light shafts, weeds, rocks, fish),
and the **live water surface** cuts across the middle. Everything is built from **flat-shaded low-poly facets**:
triangles with carefully chosen colours, never textures. Depth comes from **atmospheric perspective** above water
and **colour absorption** below it. Light does the heavy lifting: gradient skies, golden-hour rim light, god rays,
caustics on the bottom, glints on the water, lanterns and bioluminescence at night. The water is the most
beautiful thing on screen.

**Visual references** (for mood and technique, never to copy): split-level underwater photography (the core
composition); *Alto's Odyssey* (gradient skies, silhouettes, weather and time of day); *Dredge* (fog, mood, night
dread); *A Short Hike* (warm low-poly charm); *Firewatch* (palettes and grading); *Ori* (light underwater).

## 2. Why procedural

The implementing AI can't paint sprites, and the game needs hundreds of fish and dozens of places. So **almost every
visual is generated from data at load time**: fish from shape and pattern specs, terrain from spot profiles, trees
and rocks from generators, skies from palettes. That gives a consistent style, tiny files, endless variety, and
lets every visual react to the sim (bending fish, swaying kelp, a surface that ripples).

## 3. Scene composition (back to front)

| Layer | Content | Technique |
|---|---|---|
| 1 Sky | Gradient, sun/moon discs, stars, aurora | Full-screen sky shader driven by the time-of-day palette |
| 2 Far clouds | Low-poly cloud banks | Generated meshes; slow parallax; tinted by palette |
| 3 Far land | Mountains, far shores | Low-poly silhouettes blended heavily toward the sky colour (atmospheric perspective) |
| 4 Mid land | Hills, tree lines, buildings | Low-poly, partial fog blend, parallax |
| 5 Far water band | The water surface receding to the horizon | Reflection + glitter shader (§6.3) |
| 6 Near land | Bank, dock, pier, boat, angler, reeds | Full-colour low-poly; wind sway in the vertex shader |
| 7 Surface | The simulated surface line and the near surface band | §6.1-6.2 |
| 8 Underwater | Back terrain → plants → far fish → lure/line → near fish → bottom → front plants | Depth fog and absorption, caustics, god rays, particles (§6.4-6.6) |
| 9 Foreground | Overhanging branches, rain streaks, front reeds, fog wisps | Dark-ish silhouettes for framing |
| 10 Post | Glow, grade, vignette, dither | ≤ 4 full-screen passes |
| 11 UI | HUD, menus | §9 |

Objects have a **pseudo-depth** `z` (distance into the screen) used only for draw order, parallax, fog and size.
Fish swimming "behind" are bluer and dimmer; fish in front are crisp. Fish never collide across z (they're separate
lanes), but the lure lane is z = 0 and only fish that come into z ≈ 0 can take it (the brain steers them there).

## 4. The low-poly system

### 4.1 Builders (`game/src/Render/LowPoly/`)

`LowPolyBuilder.Fill(outline, style, seed)` → `ArrayMesh`:
1. Sample points inside the outline (Poisson disc at `style.density`) plus points along the outline.
2. Triangulate (Delaunay via Godot's `Geometry2D`, discarding triangles whose centroid falls outside the outline).
3. Per triangle: base colour from a **palette ramp** (by height in the shape, a material band, or a gradient axis),
   plus small seeded jitter (±3-6% value), times **fake lighting**: a normal from a smooth noise height field (or from
   the shape's curvature for rounded bodies) dotted with the light direction, then quantized into 3-5 bands.
4. Output unshared vertices per triangle (flat colour) with vertex colours; `UV` carries shape coordinates for
   shaders.

Generators built on it: terrain (from spot profiles: rock, sand, mud, gravel, coral rubble, each a palette), rocks
and boulders, trees (pine, spruce, oak, willow, cypress with knees and moss, mangrove with prop roots, palm, birch),
reeds and cattails, lily pads, kelp, eelgrass, coral (branching, brain, fan, table), sponges, drowned timber, docks
and pilings (with barnacle bands at the tide line), the station, the cannery, the lighthouse, bridges, boats,
clouds, mountains (ridges from noise, snowcaps above an altitude).

**Lighting is recomputed per time of day** by passing the light direction and palette to a shared `lowpoly.gdshader`
that re-shades triangles from a stored per-triangle normal (in `CUSTOM` or a flat varying). Baked colours are albedo
only. This keeps golden hour and night correct without rebuilding meshes.

### 4.2 Palettes

`game/data/palettes/<biome>.json`: keyframes at **pre-dawn, sunrise, day, golden hour, dusk, night**, each with:
`skyTop, skyHorizon, sun, ambient, farLand, midLand, nearLand, foliage, waterSurface, waterShallow, waterDeep, fog,
rim, highlight`. The renderer interpolates by sun elevation and then applies weather (overcast desaturates and
flattens, storms darken and cool, fog lifts the blacks).

Example starting points (tune by eye; these are a direction, not law):

| Key | Station Pond golden hour | Florida Keys day | Atchafalaya night |
|---|---|---|---|
| skyTop | `#3B4F7A` | `#2E8BD8` | `#0B1020` |
| skyHorizon | `#F6B06A` | `#BFE9F7` | `#1C2440` |
| sun / moon | `#FFD9A0` | `#FFFBEA` | `#CFE0FF` |
| farLand | `#8F7E9A` | `#7FB9B3` | `#141A2A` |
| nearLand | `#4C5B32` | `#3F8F5A` | `#0E1410` |
| waterSurface | `#E8B98A` | `#9FE7E4` | `#2A3550` |
| waterShallow | `#4E7A5C` | `#38C9C1` | `#16211A` |
| waterDeep | `#14302E` | `#0B5C8E` | `#05090A` |

Rules: keep value contrast high between layers; at most one saturated accent per scene (the sunset, a mahi's
flanks, a red float); shadows lean toward the sky's complement; night is blue-black, never grey.

## 5. Fish rendering (non-negotiable: they bend and move)

### 5.1 Shape specs (`game/data/fish_shapes/*.json`)

Shared by family, overridden per species:
- **Body:** dorsal and ventral profile curves sampled at 12-16 stations from snout (u = 0) to caudal peduncle, as
  heights relative to body length; a **thickness** profile (for lighting and the turn effect).
- **Head:** length fraction, mouth position (terminal, superior, inferior, sub-terminal), mouth gape polygon, barbels
  (catfish, sturgeon, cod), snout type (gar beak, swordfish bill, billfish spear, sturgeon rostrum).
- **Eye:** position, size, iris colour, eyeshine colour (walleye glow at night: real tapetum).
- **Fins:** each a polygon template with base position, length, ray count, flexibility, and resting angle: dorsal(s),
  adipose (salmonids, catfish), caudal by type (forked, lunate, emarginate, truncate, rounded, heterocercal for
  sharks and sturgeon, the coelacanth's three-lobed diphycercal tail), anal, pectoral, pelvic, finlets (tunas,
  mackerel), gill cover outline, lateral line.

### 5.2 Mesh build (`FishMeshBuilder`)

- Build a low-poly body strip: stations along u, 3-5 rows across v (back to belly), triangulated with alternating
  diagonals, flat-coloured. Fins as separate small meshes parented to spine positions.
- **Pattern** (`pattern` in species data) applied as colours on facets and as extra geometry:
  - countershading gradient (back colour → flank → belly),
  - bars (vertical: perch, tiger musky, sheepshead), stripes (horizontal: striped bass), blotched bands
    (largemouth), spots (with optional halos: brook trout's red spots with blue halos; brown trout; pike's pale bean
    spots on green), vermiculation (brook trout's back, as facet-level colour noise), iridescent band (rainbow
    trout's pink stripe, mahi's blue-green-gold), fin edging (brook trout's white-edged fins, walleye's white tail
    tip), eye-spot (red drum's tail spot), scale facets (large-scaled carp, tarpon: scale-like quads).
  - Spots are small low-poly discs or hexes placed by seeded Poisson within pattern regions (so each fish is unique).
- **Variants:** juvenile (parr marks on young salmonids), spawning colours (sockeye red, chinook dark, brook trout
  orange belly, male bluegill), size-dependent proportions (big fish are deeper bodied), regional morphs,
  individual seed (spot layout, slight colour shift).
- Build once per species variant + seed at load or on first use; cache.

### 5.3 Skinning shader (`fish_skin.gdshader`)

- Each fish draws with a material that receives its spine (`uniform vec2 spine_pos[16]`, `uniform vec2
  spine_dir[16]`, node count) every frame from the interpolated sim state. The vertex shader maps each vertex's
  (u, v) to the spine: position = spine point at u + normal × (v × local half-height). Fins follow their base
  station and add their own flutter (sin with phase, amplitude scaled by speed and flexibility).
- Lighting per facet from the bent normal (approximate: the body normal rotated by the local spine angle) with the
  scene's light direction and palette, plus a **wet specular sheen** band along the back that slides as the body
  bends.
- Up to 24 fish agents each have their own material instance at first. If draw cost is too high, move spines to a
  shared float data texture indexed per fish.
- Mouth gape, gill flare (breathing rate rises with exertion), pectoral fin sculling and eye rotation are uniforms
  too.

### 5.4 Turning, flatfish and rays

- **Turning:** when a fish reverses direction, play a pseudo-3D turn: horizontal scale passes through
  `cos(yaw)` (clamped at the thickness profile so the fish never vanishes), its back colour shows more during the
  turn, and fins splay. Fast turns (C-starts) bend the spine into a C at the same time.
- **Flatfish** (flounder, halibut) and **rays** would be a thin line in a true side view. Render them tilted toward the
  camera (about 30-40°) so the eyed side or the disc reads, with their undulation shown on that tilted plane. This
  stylization is intentional.

### 5.5 Out of water

**Fish deaths and injuries are shown tastefully (owner decision):** gaffs and sharks taking hooked fish happen,
with at most a little blood on a gaffed fish; there are no cleaning or filleting scenes.

When landed: a brighter, wetter sheen, drips (particles), the flop physics (`04` §9.4), gills working. **Mahi-mahi's
colours fade after landing** (a real and haunting detail); salmon and trout keep theirs.

## 6. Water rendering

### 6.1 The surface line (sim)

- A triangle strip along the active surface patches and the ambient field (rendered from a 1D height texture
  updated each frame: `RFloat` 1024 × 1, cheap), from the surface line up a few pixels (the near surface band, §6.2)
  and down into the water volume.
- A bright **meniscus highlight** on the line, brighter where the slope faces the sun, and **foam** where the sim's
  foam scalar is high (shore break, around wading legs and boat hulls, whitecaps in wind).

### 6.2 The near surface band (the top of the water)

A band a few dozen pixels tall just above the surface line, showing the top of the water as if seen from slightly
above. It shows: sky reflection, the sun or moon glint, **ripple rings** as ellipses (spawned exactly where the sim
receives impacts: lure entries, rises, rain drops, fish tails, and expanding at the sim's wave speed), wakes (the
V behind a moving lure or boat), floating things (lily pads, duckweed, sargassum, foam lines, the float), and wind
texture (cat's-paws). This band is what makes the water look alive and readable. A trout's rise is a ring here.

### 6.3 The far water band

Between the near surface and the far shore: a perspective-compressed water plane with the far land and sky
reflected (a flipped, squashed, ripple-distorted sample of layers 1-4 from a back-buffer copy; fall back to a palette
gradient on Low), a **glitter path** under the sun/moon (sparkle from hashed wave slopes), and wind streaks. On glassy
mornings it's a mirror; in wind it breaks up.

### 6.4 The water volume

- Base colour from depth: `waterShallow → waterDeep` (palette), with the water's per-channel **absorption** from
  spot data (`04` §7.5): clear mountain water is blue-green and transparent, the bayou is tea-brown with ~1 m
  visibility, the reef turquoise, blue water cobalt fading to black by ~200 m.
- Objects underwater are tinted by **transmittance** over their depth and z distance (red fades first, real), and
  fogged toward the water colour with distance. Fish perception uses the water's real visibility (`05` §3.1).
- **For the player, fish are mostly visible (owner decision).** Murky water, depth and night dim and fog fish, but
  never below a readable silhouette: a minimum contrast floor (tunable, default about 35%) keeps fish near the lure
  visible. Sonar and polarized glasses still matter: sonar shows fish outside the frame and in deep water, and
  glasses cut surface glare and sharpen fish in shallow water so species can be identified.
- **Turbidity** after rain or near a muddy bottom: more scattering colour, shorter visibility.
- The underside of the surface: a bright, rippled band (total internal reflection look).

### 6.5 Light underwater

- **God rays:** additive soft quads from the surface down, angled by sun direction, intensity modulated by the surface
  heights texture (rays shimmer with the waves), fading with depth and turbidity. Off at night (moon rays very faint).
- **Caustics:** procedural animated pattern (Voronoi/cellular noise warped by time and the surface texture) projected
  on the bottom and on fish in shallow sunlit water.
- **Particulates:** drifting specks (marine snow offshore, silt in the bayou, plankton), moving with the current field.
- **Bubbles:** from splashes, breathing air-gulpers, the lure entry, the boat's prop wash.
- **Bioluminescence:** at night in the right places (the bay event, the Lantern rise, offshore): glowing particles
  spawned where the sim's water velocity is high, so the lure, line, fish and splashes leave trails of light.

### 6.6 Splashes and spray

Droplet particles from sim splash events (count and speed from impact energy), small white mist for big impacts,
spray off the bow at speed, rain drops hitting the surface (with rings on the band), and fish jumps that throw a
sheet of water.

## 7. Sky, time of day and weather

- **Sky shader:** gradient from the palette, sun disc with halo, moon with its real phase, stars (twinkle, Milky Way
  band on clear moonless nights), aurora (Northern Norway) and meteor streaks (event).
- **Time of day:** palettes interpolate continuously; low-poly re-shading follows the light direction; long rim light
  at golden hour; lanterns and boat lights glow at night (a few `PointLight2D`s at most, or a custom radial light
  term in the shared shaders, whichever is cheaper on the iGPU).
- **Weather visuals:** clouds thicken and darken, rain streaks (screen-space particles) plus surface rings and sim
  impulses, wind drives tree/reed/kelp sway and cloud speed and whitecaps, fog layers (soft noise sprites at several
  z depths; fog also lowers underwater light), storms (flash lighting that briefly relights clouds and land, thunder
  delayed by distance), snow, ice (a frozen surface becomes a low-poly ice sheet with a drilled hole; light underwater
  goes dim blue-white).
- **Seasons:** foliage palettes (spring greens, autumn ambers and reds, winter bare branches and snow), ice, low winter
  sun angles.

## 8. Tackle, angler, boats and camera

- **Rod:** a tapered polygon strip along the Catmull-Rom-smoothed rod nodes, with drawn guides, thread wraps, cork or
  EVA grip, and a low-poly reel on the seat (spool visibly turning with the sim's spool angle; the bail visible).
  A specular stripe that slides along the bend sells the flex.
- **Line:** a constant 1.5-2 px anti-aliased strip (scaled for zoom) along the line nodes, coloured by the line
  product (hi-vis yellow braid, clear mono, green, pink fluorocarbon tints, fly lines in bright orange/olive/sage),
  with a soft glint on slack curves. Underwater it dims and shifts slightly (refraction look). **Line must always be
  readable**; in Purist mode it's the main source of information.
- **Lures:** low-poly meshes from the lure data (body, lip, hooks, skirts, blades spinning, tails wagging with the
  sim's lateral/vibration state).
- **Angler:** a stylized, faceless low-poly figure. Arms by 2-bone IK to the rod handle; the body **leans against
  the line tension** (physically derived, so a big fish visibly pulls the angler); procedural walk; idle breathing;
  outfit colours, hat and vest customization. The dog Skiff: a few-bone procedural animal with simple states.
- **Boats:** generated low-poly hulls from boat data, bobbing from the sim; wakes into the water; the outboard tilts.
- **Camera:** frames the angler, the rod tip and the lure or fish. It zooms out smoothly for long casts (up to about
  150 m in view) and follows deep drops down with a depth gauge. When the lure is too far or deep to frame with the
  angler, a **picture-in-picture inset** shows the angler and rod bend (the rod's bend must always be visible during a
  fight). Gentle smoothing; optional small shakes for big impacts.

## 9. UI style

- **Field-journal aesthetic:** warm paper tones, ink-line icons, hand-ruled dividers. Menus feel like Wren's
  notebook; the Ledger looks like real field guide pages, with each fish rendered from its own mesh through an
  **ink-and-wash shader** (outline + soft watercolour fill) so it matches the in-game fish exactly.
- Fonts (OFL, from Google Fonts, licences committed): a readable modern sans for numbers and HUD (e.g. Inter), a
  characterful serif for headings (e.g. Fraunces), and a handwriting face for Wren's notes (e.g. Caveat).
- **HUD:** minimal and diegetic-leaning: a small reel icon (spool fill, drag dial, optional tension ring), a
  clock/tide/weather/moon strip, the cooler/livewell count, and toasts for Ledger updates. Everything else hides
  until needed. Purist mode removes the meters.
- Controller-first navigation everywhere; mouse works too.

## 10. Post-processing and quality presets

| Effect | Low | Medium (default) | High | Ultra |
|---|---|---|---|---|
| Glow (bloom) | off | on, half res | on | on |
| Colour grade (time/weather) | on | on | on | on |
| Far-water reflection | gradient only | on, half res | on | on |
| God rays | off | on (few) | on | on (more) |
| Caustics | off | on | on | on |
| Particle density | 25% | 60% | 100% | 150% |
| Fish agents drawn with full skinning | 12 | 24 | 24 | 32 |
| Plant sim | static sway | sim near lure | sim everywhere | sim everywhere |
| Dither (banding fix) | on | on | on | on |
| Vignette, film grain | off | subtle | subtle | subtle |

Glow and grading through Godot's `WorldEnvironment` with a Canvas background (verify on the pinned version) or one
custom final post shader; whichever is cheaper on the owner's iGPU.

## 11. Verifying visuals

- Screenshot sets per milestone: each spot at pre-dawn, day, golden hour and night, in clear, rain and fog, at default
  quality (`--screenshot` with `--time`, `--weather`, `--seed`). Look at every image before reporting.
- A fish lineup scene (`--scene fish_lineup`) renders every species in a grid, swimming in place, plus a still pose,
  for checking shapes, patterns and bending at a glance. Also renders Ledger ink versions.
- Check readability: can you see the line against every background? Is the float visible? Is the rod bend clear?

## 12. Audio direction

**Tactile audio is half the feel.** It responds continuously to the sim:

| Sound | Driven by | Technique |
|---|---|---|
| Reel crank | Crank rate, gear ratio | Synthesized gear whir + recorded mechanism layers; pitch and rate follow the handle |
| Drag | Slip speed, drag quality | A click train at (spool revs × clicker teeth) blended into a filtered "zzz" at high speed; stick-slip drags stutter |
| Clicker / bait alarm | Spool turning in free spool | Ratchet clicks at the real rate |
| Line through guides on a cast | Pay-out speed | Filtered noise "hiss" rising with speed |
| Line hum in wind or under tension | Tension and line length | A string tone: `f = (1/2L)·√(T/μ)` (the physics of a vibrating string), mixed low, mostly audible on tight line in wind |
| Rod swish | Tip speed | Noise burst whose loudness and pitch follow tip speed |
| Splashes | Entry energy, size, skip | Layered recordings by energy band; skip "taps" |
| Popper bloop, prop chatter | Lure events | Recordings or synthesis |
| Fish jumps, tail slaps, rises | Sim events | Recordings by size |
| Boat motor | RPM, load | Looped recordings pitch-shifted, or simple engine synthesis |
| Underwater mix | Camera below the surface | Low-pass bus, muffled world, bubbles, the sonar ping |
| Ambience | Biome × time × weather × season | Layered beds: dawn chorus, loons on the reservoir, cicadas and frogs in the bayou, gulls and surf on the coast, wind in pines, rain on water, distant thunder (delay = distance / 343 m/s) |
| Dog, NPC voices | Events | Short barks; NPCs use text with small vocal "murmurs" (no voice acting at first) |
| Music | Calm vs fight | Sparse acoustic/ambient cues; a subtle tension layer during fights; silence is allowed |

**Sources:** CC0 libraries (Freesound with the CC0 filter, Kenney audio), other royalty-free packs only if their
licence clearly allows use in a commercial game, plus synthesis. Every file is credited in `CREDITS.md` with source
and licence (CLAUDE.md rule 10). Missing files fall back to synthesized versions.
