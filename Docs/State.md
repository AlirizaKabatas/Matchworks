# Matchworks — current state ·25 October 2026

The final game name is Matchworks; the source project folder keeps its original MechanicalCrush name. One gameplay scene loads20 JSON levels. Ali’s seeds, moves and obstacle positions were not changed by release cleanup.

Latest finished work: developer input routes and prototype stock grants removed; no runtime Keyboard/KeyCode/GetKey handlers remain. Toolbar nine-patch joins replaced by a single texture draw. New level8,7and4 screenshots and a1260×1000 cover are in the v2media package. Ali is arranging the itch.io page.

Assets went from~177 MiB to~155 MiB after reference-audited pruning. OrbitOrdas, unrelated props/car sources, temporary screenshots, obsolete resources and visual-only validation helpers were removed. Twelve dynamically constructed rust textures and catalog/model references were deliberately retained. Historical Docs/concepts/raw screenshot revisions were backed up outside the project before removal. Current Docs is~14 MiB including final media.

Real verification:1,345 offline checks PASS; Editor/release source compilations: 0 errors, 0 warnings. Unity production WebGL build Succeeded,0 errors, 346 warnings (unused AI-inference shader variants/toolchain). Result `Build/Matchworks-WebGL-1.0.0.zip`,~134.5 MiB,17 files with index.html at root. Development/threads=false, gzip/fallback, responsive shell. ZIP and gzip signatures/integrity verified. No agent live gameplay test was performed at Ali’s request; browser/itch.io save/audio/runtime acceptance remains pending.

Music source recovered from original download metadata and confirmed by Ali: Stable Audio. Opening: ZoEdit Slow mouse wheel movement via Pixabay. Font: Cute Dino/Khurasan, with supporting Nunito/OFL source. CREDITS.md and ItchPageText.md contain actual attribution; no new credits screen was added.

GitHub: authenticated connector identifies AlirizaKabatas, but offers no repository-creation tool. Terminal has no existing GitHub credential; Chrome new-repository page shows Sign in. User approved a public repository. Do not claim repository creation/upload complete before a real URL/commit exists.

## Remaining decisions/actions

Ali prioritizes showing the browser-playable page now. Upload new WebGL ZIP, finish explanatory copy and compact screenshots, then final browser test and CSS polish. GitHub source upload follows once sign-in is available. Do not resume cancelled Ğ/edge-flow experiments or developer keys. Do not silently alter saved player data for screenshots/tests.

Local Git initial source commit is now created. No remote GitHub repository exists yet; browser sign-in is still required. Extra unreferenced ButtonConcepts/FrameConcepts folders were removed after confirming zero GUID/path references; these were outside Resources and did not enter the completed WebGL build. Old generated reports and previous WebGL builds were removed; current Release/ZIP retained. CSS is an optional un-applied draft, not a verified live-site change.


## 27 October 2026 — GitHub complete, page layout handoff

GitHub CLI was installed, the user personally authorized its device login, and the account was verified as AlirizaKabatas. Public repository https://github.com/AlirizaKabatas/Matchworks was created and the prepared main branch pushed successfully. The public source excludes the standalone Pixabay MP3, caches, logs and build ZIPs as documented in README. The earlier “GitHub sign-in pending” notes are superseded.

Ali uploaded Matchworks-WebGL-1.0.0.zip to itch.io and published the page. No new agent gameplay/browser save/audio acceptance was performed. Ali liked the existing portfolio cover and asked to keep it. Read-only DOM inspection found a405×720game frame with background-size:auto, and six portrait screenshots stacked at500px each (~3050px gallery).

Ali explicitly prefers to apply page changes himself. The theme editor was only opened; no theme/text inputs were changed or saved by the agent. Docs/ItchPageKit contains a1080×1920portrait embed poster using existing opening layers, unchanged approved cover, CSS scoped to the actual observed page classes (contain launch poster, two-column screenshots, matching cyan/cream/gold colors), balanced HTML description and exact field instructions. ZIP:Docs/Matchworks-Itch-Sayfa-Paketi.zip. Poster visually inspected; CSS not applied/tested on the live page. User preview and final browser smoke test remain. No new game build is needed for page-only changes.
