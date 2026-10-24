# Matchworks — technical overview

Unity6000.5.5f1, Universal Render Pipeline, Input System and uGUI/IMGUI presentation. `Assets/Scenes/SampleScene.unity` is the single gameplay/menus scene. `Match3BoardModel` owns deterministic board state; the partial `BoardBlockoutController` owns animation and UI. JSON levels live under `Assets/WorkshopMatch/Resources/WorkshopMatch/Levels/`.

Normal gravity, special-tool resolution and objective logic remain separate from fall-animation scheduling. A seed/replay consumes a deterministic sequence; presentation must not add random draws. Fixed motor/frozen cells and floor oil have separate state. Level placement edits are performed by Ali through the visual editor.

Opening uses native overlay Canvas layers, three independently rotating gears and a minimum3 seconds from first complete presentation, then the readiness gate. Textures load asynchronously and gameplay is prepared in batches. Main UI uses common portrait/safe-area layout helpers; it is not hardcoded to one iPhone model.

PlayerPrefs persist local progress/settings. WebGL stores browser-local data; Editor saves do not transfer to a browser. Language defaults to English when no selection exists. Music and SFX preferences are independent.

Runtime developer keyboard shortcuts and automatic prototype tool stock were removed for release preparation. Editor authoring/build tools remain. The production build helper sets Development=false, threads=false, gzip compression with fallback and a responsive HTML shell.

Offline revision checks use Unity DLLs and .NET; csproj paths currently target the author’s local Unity installation. Unity Editor compilation is the portable source-of-truth build. Current final build is `Build/Matchworks-WebGL-1.0.0.zip`; build outputs are not committed.
