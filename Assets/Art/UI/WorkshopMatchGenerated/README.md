# Workshop Match generated 2D UI pack

This folder contains the first production-ready 2D UI/background pass derived from `Docs/Concepts/WorkshopMatch_UI_Direction_v3.png` and the approved PDF brief.

## Contents

- 3 opaque 1080x1920 backgrounds.
- Home top/bottom frames and three play-button states.
- Gameplay HUD, gear backplate, 8x9 board frame, toolbar, cells and tool-slot states.
- Map nodes, modular straight/curve path pieces and shared icons.
- Round wooden/purple button bases, pressed states and an empty stock badge.

All UI PNGs use transparency. Text, numbers and level labels are intentionally excluded and should be rendered by Unity.

## Suggested Unity import settings

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Alpha Is Transparency: enabled for RGBA files
- Mesh Type: Full Rect for frames and UI controls
- Compression: None during layout/QA; use High Quality only after device inspection
- Filter Mode: Bilinear
- Pixels Per Unit: keep consistent within each group

The pressed/disabled/selected variants were derived deterministically from the generated normal state so their outer dimensions and pivots remain identical.

