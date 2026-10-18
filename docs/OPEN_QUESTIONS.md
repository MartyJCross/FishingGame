# Open Questions for the Owner

The plan was written with an assumption for each question. **Opus: build on the assumption until the owner
answers, then update this file (move the question to "Answered" with the date and answer) and the affected docs.**

## Open

| # | Question | Assumption used until answered | What changes if different |
|---|---|---|---|
| Q1 | **Who is the player?** A human angler (customizable), or an animal like the cat in *Cat Goes Fishing*? | A stylized faceless human angler plus a companion dog, Skiff | Angler model and IK, story voice, marketing |
| Q2 | **What does "low-poly" mean to you in a 2D game?** (a) flat 2D shapes built from shaded triangles, or (b) real 3D low-poly models shown from the side (2.5D) | (a) Flat-shaded 2D triangle art | The rendering layer only (the sim stays 2D). 2.5D costs more GPU on the laptop and needs 3D lighting work |
| Q3 | **Engine:** OK with Godot 4 + C#? | Yes | An engine change after M0 is very expensive |
| Q4 | **How harsh should realism be by default?** Rods that break from high-sticking, lines that snap, baitcaster bird's nests, hooks pulled by slack, fines from the warden | Realistic by default, with assists and a "Relaxed" preset (no rod breaks, fewer backlashes, warnings before line breaks) | Default settings and tuning |
| Q5 | **Seasons?** A full year with spawning, ice fishing and runs (each season 7 game days) | Yes, seasons are in | Without seasons, runs, ice fishing and spawning become scheduled events |
| Q6 | **Keeping and killing fish:** selling fish, gaffs and coolers are realistic. Some players prefer catch-and-release only | Both: selling is allowed and realistic, release earns reputation | Economy design |
| Q7 | **Can the player see every fish underwater** (like *Cat Goes Fishing*), or only as far as the water's real visibility (so murky water and night hide fish, and sonar and polarized glasses matter)? | Realistic visibility, with an assist toggle that shows faint silhouettes | Big effect on the feel of murky water, night and sonar |
| Q8 | **Platform and release:** personal project, or a Steam release one day? Windows only? Steam Deck? | Windows first, built so Steam and Steam Deck are possible later | Asset licences (commercial-safe), controller-first UI, store build |
| Q9 | **Controllers:** Xbox only, or also PlayStation (DualSense trigger effects)? | Xbox layout first; PlayStation buttons through Godot's mapping; no adaptive triggers | Haptics design |
| Q10 | **Music:** CC0/royalty-free tracks, ambient-only, or commissioned later? | Mostly ambient sound with a few sparse CC0 music cues | Audio plan |
| Q11 | **World:** a fictional basin with real fish (as planned), or real-world places (e.g. "Florida Keys", "Lake Erie")? | Fictional world, real fish, real-world-style regulations | Story, map, naming |
| Q12 | **Multiplayer or co-op** ever? | No, single player | Architecture (the sim is deterministic, which would help later) |
| Q13 | **Name:** "Longwater" is a working title | Longwater | Branding only |
| Q14 | **Gore and realism around fish deaths:** gaffs, cleaning fish, sharks biting hooked fish | Shown tastefully, no blood beyond a little on a gaffed fish; no cleaning scenes | Art and tone |
| Q15 | **How much fly fishing?** It's the hardest system to build well | A full fly-fishing system (false casts, roll casts, mending, fly tying) in M12 | Scope of M12 |

## Answered

(none yet)
