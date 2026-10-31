# Changelog

## 1.0.1 — 2026-10-31

- Add mouse/touch drag swaps throughout gameplay and tutorials; preserve adjacent two-tap swaps and one swap per press.
- Make the map tutorial accept two separate up/down drag-and-release gestures without requiring a long swipe or minimum hold time. Keep its existing text and visuals.
- Drop the existing source-column stack after a diagonal departure before spawning its replacement, preserving candy order.
- Play independent vertical/diagonal refills concurrently. Correct placement-dependent hammer/firework speed spikes around motors while retaining ordinary vertical fall timing and easing.
- Exclude test stock grants, move adjustment, direct power/resolve helpers and prototype self-checks from production builds. No runtime developer keyboard cheats or automatic test inventory grants.
- Update the production WebGL build to 1.0.1 with development/debugging/profiler/threads disabled and the existing responsive HTML shell.

Validation: 1,520 offline regression checks, Unity/release C# compilation and a successful production WebGL build with zero errors. The build emits 354 Unity/package warnings; see `Docs/ReleaseBuild.json` for the report. A separate level-8 audit covers 329 refill waves from hammer and both firework directions. Browser acceptance is performed separately on the uploaded build.

## 1.0.0 — 2026-10-27

- Publish the initial portfolio source and itch.io page materials for the 20-level Matchworks demo.
- Include the visual level editor, tutorials, tools/combinations, local progress, achievements, map, reward wheel and English/Turkish localization.
