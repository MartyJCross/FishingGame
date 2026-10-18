# Ideas backlog

Owner ideas and requests to fold into the roadmap (`08_ROADMAP.md`) when their milestone comes up. Newest first.
Mark an idea **done** (with the version) or **planned (Mx)** rather than deleting it.

## 2026-10-18 (after playing v0.4.1)

1. **Water temperature drives behaviour.** Warm and cool water should change what fish do: activity and metabolism
   (hunger rises roughly 2x per 10 °C toward the species' optimum, then falls off), where they sit (deeper/shaded in
   summer heat, sunny shallows on a cool spring day), how hard they fight (`04` §9.3 temperature performance curve),
   oxygen stress in hot still water, and release survival. Species files already carry `water.temperatureC`
   (lethal min/max, optimum). Needs a water temperature per spot and depth (season, time of day, sun, wind, springs).
   **Planned (M8 weather/seasons, with a simple version earlier if fishing feels samey).**
2. **Line colour gradient that warns before it snaps.** **Done (v0.4.2):** the line keeps its colour up to 50% of its
   breaking strength (with knot), warms to amber by 80%, and goes red near the break. F3 still shows full load colours.
3. **Underhand casting and other cast variations.** Underhand / pitch (rod low, sweep up and release), sidearm (low
   trajectory under branches), roll and flip casts, a short "pendulum" pitch for docks, and later fly casts. The rod
   and line physics already allow any motion; what's missing is the arm motion for them (a lower rod angle range and a
   wrist flick), practice targets (the casting lawn, M3.6), and feedback (distance and accuracy). **Planned (M3.6).**
