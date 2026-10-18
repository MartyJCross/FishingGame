# Changelog

Newest first. Each version handed to the owner gets an entry: what changed, what to try, known issues.

## v0.2.0: M1 Water surface (2026-10-18)

- The pond's water is simulated. Wind makes real-sized waves: a breeze on a small pond gives centimetre ripples, and a
  storm gives choppy whitecaps. The numbers come from the standard wind-wave formulas for that wind and that much open
  water.
- Anything that touches the water pushes it: ripples spread at the right speed, fade, and are absorbed at the edges
  instead of bouncing back. Splashes throw droplets that fall back and dimple the surface. Rain pocks the whole
  surface.
- Ripple rings are drawn exactly where the simulation was hit.
- Try: middle-click the water to drop stones; F4 toggles rain; F1 → "Debug conditions" to change wind, fetch and
  swell live. Start with `-- --weather storm` (or calm, breeze, windy, rain) from the command line.
- Known: the water, sky and bank are placeholders until the art pass (M8); there's no rod or line physics yet (M2).

## v0.1.0: M0 Foundations (2026-10-18)

- The game project exists: Godot 4.7.2 (.NET) with the simulation in its own C# library and automatic tests.
- What you can do: open it and swing a placeholder rod with the mouse or the right stick. F3 shows the debug
  overlay (including your controller's sticks and triggers), F1 opens the live tuning panel.
- Every push builds a Windows version on GitHub (artifact "TheLedger-windows-x64").
- Try: plug in the GameSir, press F3, and check it's listed and its triggers move.

## Plan revision (2026-10-18)

- Applied the owner's answers to all 15 design questions: the game is now **The Ledger**, set in **real places**
  (a Florida Nature Coast home base plus world travel) with the logbook world-tour story and a Comoros finale;
  hardcore physics with information-only assists; fish mostly visible underwater; co-op-ready architecture and a
  co-op milestone (M18); the species list regrouped by real place (about 250 species); the roadmap reordered.

## Plan (2026-10-18)

- Design and build plan written: vision and world, tech stack, fishing mechanics, physics spec, fish and ecology,
  progression and economy, art direction, roadmap, open questions, and the project constitution (`CLAUDE.md`).
