# Cursor + Claude: natural clothing refinement passes

Owner authorization (2026-11-01): “cursor, you and claude should run passes on these... reviewing the originals to note were the cloth was supposed to look worn used. a bit dirty.. these original tetxtures are decent. they just needed a slight overhaul to make them truly ours...” and “lets have a fewe more passes done on these graphics so I can see som real keepers. talk to cursor.”

Cursor: author the next separate candidate from the finished art/sages-vault-original PNGs, using the original extracted maps as visual reference. The current look works in game but weave is too coarse/inconsistent and pants/chest/robe values do not harmonize. Preserve masks, UV islands, alpha, material roles, existing character identities and the installed baseline. Put candidate in art/sages-vault-refinement-02, never overwrite Preview 01.

Claude: independently review reference clothing wear, construction, value/tint relationships and repeated-pattern scale. Review Cursor's candidate against both original and Preview 01. Provide concrete corrections rather than a generic approval. Return findings in a dated outbox RESPONSE.md.

Codex: measured review, standalone model/pose/video tooling, cross-channel import checks and final assembled-outfit comparison. No new art pack is installed until it is reviewable and Brian explicitly requests that version's installation.

Pass A: coordinated finer cloth weave (one-third present feature size is the initial trial), compensated for each mesh's UV density; same physical scale across albedo/normal/smoothness. Match the outfit's intended dark warm cloth family across chest, sleeves, skirt and pants; do not blindly lighten pants or darken the whole robe. Preserve deliberate undyed linen accents. Use original broad folds/fade as reference; reduce tiled microdetail contrast.

Pass B: subtle localized worn/used/dirty treatment—cuffs, seams, elbows, belt friction, hems, knees. Natural leather creases/edge polish. No wholesale redesign, added faction marks, universally dirty noise, or blanket grunge. Keep the decent original's material hierarchy while making the authored artwork LWC's.

Pass C: compare Original / Preview 01 / new candidate in assembled model views and motion, fixed exposure/distance. Check shrinking weave, moire/mips, stretched UVs and robe/pants seams. Brian is recording game scenes; use that footage when available. Neutral studio and Valheim daylight/swamp/firelight are complementary; PBR studio is approximate.

Read ART-DIRECTION.md for findings, channel packing/tints, and acceptance checks. Provide side-by-side contact sheets, actual clothed-model stills/video, source PNG list and hashes. Keep installed LWC Souls Art Test 0.1.3 and all NPC/raid settings untouched during art authoring.

Local references:
- Current LWC PNGs: valheim-mods-formidable-current/art/sages-vault-original
- Extracted references: ../Extracted_SagesVault_1.1.4/Textures
- Standalone tool source: ../LWCModelSkinStudio
- Portable build folder: C:/Users/scruf/Documents/LWC Studio Builds/LWCModelSkinStudio-win32-x64

Source/profile scope: artwork and review only; no new NPC classes, role aliases, loadout replacements or distribution assumptions. Please acknowledge this request and report who is authoring each file before concurrent edits.


## New footage addendum — read before repainting pants

VIDEO-ADDENDUM.md reviews Brian's new392srecording. Crucially the shown Pillager uses ArmorIronLegs and Servant ArmorRootLegs, not SagePants. The earlier file-level SagePants luma measurement remains accurate but cannot explain those video legs. Match the new cloth to actual existing loadouts; do not change a loadout without Brian. MadHatter exact current loadout usage is six named prefabs across four NPC definitions, inventoried in madhatter-current-usage.json; asset origins/permissions were not determined.
