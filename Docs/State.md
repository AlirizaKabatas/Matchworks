# Matchworks — current state · 31 October 2026

Current source version: **1.0.1**. The project folder retains its original MechanicalCrush name. One gameplay scene loads 20 authored 8×8 JSON levels. Ali owns the seeds, move counts and obstacle placements; this release does not change those authored values.

The public repository is https://github.com/AlirizaKabatas/Matchworks . The browser game is at https://alikb.itch.io/matchworks . The source omits the standalone Pixabay opening MP3 under its redistribution terms; README and CREDITS explain how to restore it locally. Build ZIPs, Unity caches, logs and local delivery copies are excluded from Git.

## Implemented in 1.0.1

- Mouse/touch drag swaps work in ordinary play and tutorials, alongside adjacent two-tap controls. One press cannot cause repeated swaps; existing validity, move-cost, tool and tutorial restrictions remain.
- Map onboarding accepts two separate up/down drag-and-release gestures. A small width-scaled threshold filters tap jitter; no minimum hold time or long swipe is required. Original text and visuals are retained.
- A diagonal departure is followed by vertical compaction of its source column before spawning. Existing candies fill their own vacancy in order; new candies cannot bypass that stack.
- Independent refill routes move concurrently. Deterministic route-order retries avoid unnecessary staged handoffs, and visible straight/diagonal travel is checked against the normal movement pace. Ordinary vertical timing and SmoothStep remain unchanged. This fixes the reported placement-dependent hammer/firework acceleration around motors without forcing every refill into a short total duration.
- Production builds exclude test stock grants, move adjustment, direct power/resolve helpers and prototype self-checks. No runtime developer keyboard routes or automatic prototype stock grants remain. Authored-level loading helpers and normal onboarding gifts remain.

Detailed changes are in CHANGELOG.md. Historical experimental shortcuts and cancelled gravity experiments are not part of the release.

## Verified results

- 1,520 offline regression checks pass, including ordinary vertical timing/easing, short diagonal/entry travel, input gestures, model determinism, spacing and gravity checks.
- A level-8 audit covers 329 refill waves: 99 hammer, 113 vertical firework and 117 horizontal firework. All 129 mixed waves pass the short-leg pace checks without staged handoffs.
- Release source compilation and Unity recompilation pass with zero errors/warnings. The actual stripped WebGL assembly excludes the development helpers and runtime Pipeline assemblies.
- Production WebGL 1.0.1 builds successfully with zero errors and 354 Unity/package warnings: 353 shader warnings and one runtime-Pipeline-disabled notice. Development, debugging, profiler connection and threads are disabled; gzip/decompression fallback and the responsive shell are retained.

Upload package: `Build/Matchworks-WebGL-1.0.1.zip`, 140,999,479 bytes (~134.5 MiB), 17 files with `index.html` at the root. ZIP CRC, all three gzip streams, the Wasm header, relative URLs and version were checked. SHA-256: `91a1b27bc8b4f50845ba6ab6d8c47489d7578abb8a23bcaa44b6da26337df1cd`. The package is a local build artifact, not a tracked source file. Docs/ReleaseBuild.json contains the release report.

Ali tested the interaction/animation changes and requested the release build. No agent live gameplay/browser acceptance, retention measurement, A/B test or mobile-performance certification is claimed. The newer ZIP's itch.io upload and browser smoke test remain Ali's actions unless he reports them completed.

## Local test state and remaining actions

The release preparation reset the Editor's known test level/onboarding/tool records to level 1 and zero stock after a local backup. Sound/language preferences and achievements were preserved. Editor preferences are not embedded in the WebGL package; no startup save-wipe was added and no existing browser save was changed.

Ali handles the itch.io upload, browser audio/input/save checks and optional page-theme changes. Canonical page assets are in Docs/ItchPageKit; the duplicate Turkish delivery folder is local-only. Native store releases, ads, payments, new level placements and further experimental mechanics are outside this release.
