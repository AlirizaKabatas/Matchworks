# Matchworks — remaining release work ·25 October 2026

Completed:20 level demo, latest UI fixes, cover/screenshots, all runtime developer hotkeys removed, proven-unused asset/doc cleanup, credit provenance,1,345checks, source compilation and new compressed production WebGL ZIP.

1. Upload `Build/Matchworks-WebGL-1.0.0.zip` in itch.io. Kind=HTML, mark the upload “This file will be played in the browser”. Do not upload a localhost URL or the source project as the browser game.
2. Use Docs/ItchPageText.md for page copy. Theme: BG#163B43,content#FFF7E6,text#4B321D,links#087F8C,Lato Large. Use4 SSfor a compact gallery; Auto may hide screenshots on embedded HTML pages, Sidebar creates a tall vertical column. Final CSS/layout is still pending user preview.
3. Browser smoke test: start/audio, level1→2tutorial/map interaction, level gameplay, progress/achievement/inventory/settings persisted after normal-tab reopen, Developer URL. Ali’s Editor save/appearance acceptance does not replace this new-browser test.
4. Publish the finished page after the smoke test. Current build has0 errors and346package/toolchain warnings; no claim of warning-free build or real-phone performance certification.
5. Public GitHub source repository. User approved public; GitHub sign-in is required. Local Git/source preparation is independent of the already delivered playable ZIP. Third-party Pixabay MP3 is excluded from public source; its original download/setup is documented in README.md.

No additional screenshots required. Credits can be pasted into itch.io, not a new in-game panel. Mobile store releases are not required. Source cleanup/audit detail remains in Cleanup_20261024.json; recoverable backups are outside the project in /private/tmp.
