# Owner Decisions and Open Questions

**Opus:** when a new design question comes up that only the owner can answer, add it under "Open" with the
assumption you'll build on until they answer. When they answer, move it to "Answered" with the date, and update the
affected docs.

## Open

(none)

## Answered (2026-10-18)

| # | Question | Owner's answer | Where it's applied |
|---|---|---|---|
| Q1 | Who is the player? | A stylized faceless **human angler** with a companion dog, Skiff | `01` §3, `07` §8 |
| Q2 | What does "low-poly" mean in 2D? | **Flat 2D shapes built from shaded triangles** (not 3D models) | `07` §4 |
| Q3 | Engine? | **Godot 4 + C#** | `02` |
| Q4 | How harsh should realism be? | **Hardcore only.** No preset softens the physics: rods break, lines snap, baitcasters backlash, slack loses fish. Information assists (meters, arc preview) can still be toggled | CLAUDE.md rule 12, `03` §11, `08` M7 |
| Q5 | Seasons? | **Full seasons**, 7 game days each by default, with spawning, runs, hatches and ice fishing | `06` §9 |
| Q6 | Keep and sell fish, or release only? | **Both**: selling earns money; releasing, tagging and surveys earn reputation | `06` §1, §11 |
| Q7 | Can the player see every fish underwater? | **Mostly visible**: fish are dimmer and foggier in murky water, deep water and at night, but never fully hidden. Fish perception still uses real visibility | `07` §6.4, `03` §11 |
| Q8 | Personal project or release? | **Keep a Steam release possible**: Windows first, commercial-safe assets, controller-first UI, Steam Deck checks in M17 | CLAUDE.md rule 10, `08` M17 |
| Q9 | Which controllers? | The owner uses a **GameSir controller (Xbox layout)** plus mouse and keyboard. Treat it as an Xbox-layout controller; test everything on both | `03` §2.2, `08` |
| Q10 | Music? | **Mostly ambient**, with a few sparse CC0 cues and a subtle fight layer | `07` §12 |
| Q11 | Fictional world or real places? | **Real-world places** | `01` §4, `05` §7 |
| Q11a | How is the real world structured? | **A home base plus world travel** | `01` §4, `06` §2 |
| Q11b | Where is home? | **Florida's Gulf Coast** (the Nature Coast, Cedar Key to Homosassa) | `01` §4.2 |
| Q11c | How closely do regulations follow real places? | **Real-style, simplified and frozen**, with an in-game notice that they may be out of date | `06` §8, CLAUDE.md rule 14 |
| Q11d | What happens to the story? | **The logbook world tour**: a fictional Florida field station; Wren's pages lead to real waters worldwide; home restoration; the coelacanth finale in the Comoros | `01` §6 |
| Q12 | Multiplayer? | **Plan for co-op later**: single player first, built so 2-player online co-op can be added (M18) | `02` §10, CLAUDE.md rule 15, `08` M18 |
| Q13 | Name? | **The Ledger** | Everywhere |
| Q14 | How are fish deaths shown? | **Tasteful**: gaffs, sharks taking hooked fish, a little blood on a gaffed fish; no cleaning or filleting scenes | `07` |
| Q15 | How deep is fly fishing? | **The full system**: false casts, roll casts, shooting line, mending, stripping, fly tying | `03` §4.4, `08` M13 |
