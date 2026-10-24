# Matchworks — current assets

Final art uses the existing toon candies, workshop obstacles, tools, wood/cream/gold UI and mechanical gear backdrops. Runtime resources are in `Assets/WorkshopMatch/Resources/WorkshopMatch/` and `Assets/Resources/WorkshopMatch/`; catalogs contain serialized references to original models/UI. Editor catalog builders and dynamically constructed part names are additional dependencies.

Do not remove a resource solely because its full filename is absent from a literal search. The twelve yellow/blue screw/nut rust-state textures are loaded by constructed keys and are active. Catalog-linked FBX models/icons remain as presentation dependencies/fallbacks.

The release cleanup removed the unreferenced OrbitOrdas character folder, unrelated prop/car files, temporary screenshots, Unity starter tutorial/readme, obsolete resource variants, old mockups and visual-only validation helpers. Current media lives in `Docs/ReleaseMedia/Screenshots/20261024/final-media-v2/`. Historical content was removed from the project after recoverable backups under /private/tmp; these backups are not part of the source repo.

The opening shares its wood texture with the main menu. Toolbar wells draw the full existing source crop once to avoid internal nine-slice joins. New screenshots of levels8/7/4 show that revision. The cover is a1260×1000 composition of existing opening layers; no new artwork or gameplay content was invented for screenshots.

See root CREDITS.md and Docs/AudioProvenance.json for current music/audio/typeface provenance. The raw Pixabay opening MP3 stays local and in built games; public Git ignores it under its distribution terms.
