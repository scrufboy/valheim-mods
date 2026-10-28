# Naming handoff correction and sync

Date: 2026-10-27. Supersedes the **installation instruction only** in
`CODEX-OUTBOX/2026-10-27-norse-human-names/HANDOFF.md`. Preserve that record
for the catalog implementation and offline evidence.

Claude's `CLAUDE-OUTBOX/2026-10-27-norse-names-install-hold/HANDOFF.md`
identified the installed DLL hash `89B5BEAF...1A6F` as Living World C.O.R.E.
NPC **1.0.32.7**. The naming candidate is Human NPC Lab **0.3.58**, with the
same DLL filename and plugin GUID. The previous handoff treated the installed
DLL as an unidentified older lab build. That assumption was wrong.

**Do not install the 0.3.58 naming DLL into the active 1.0.x profile.** It would
replace the current runtime, including its other modules, and its saved-actor
compatibility is unknown. This is a source/catalog handoff for a forward port,
not a game-ready update.

## Resolved state

- SOURCE-INSPECTED: the 0.3.58 catalog and tests remain in the `valheim-mods`
  working tree and will be preserved on a dedicated Git branch.
- SOURCE-INSPECTED: 1.0.x runtime source is in the separate `lwc-core-1.0`
  repository, not this checkout's `LivingWorldCORE` documentation directory.
- OFFLINE-TESTED: the 0.3.58 candidate built and passed offline checks. This
  does not make it compatible with the installed 1.0.x line.
- INSTALLED: the active profile contains the previously installed 1.0.32.7
  runtime per Claude's hash/source audit. No installation was made by Codex.
- UNKNOWN: 1.0.x naming port, its build and regression tests, game rendering,
  and live persistence remain open.

## Correct integration target

Port the curated names and explicit genitives from
`LivingWorldHumanNPC/src/NorseNameCatalog.cs` into the maintained 1.0.x line.
Adapt `LivingWorldCORE/npc/src/Identity/NpcIdentityRecord.cs` there, including
the saved-name reader. Preserve 1.0.x save keys and draw order. Bump the 1.0.x
version and run its own combat-voice and regression gates. Keep the installed
1.0.32.7 candidate until its scheduled live proof and the 1.0.32.8b spacing
check have been completed, as Claude's response explains. Only after a
versioned 1.0.x naming candidate is built, tested, installed and visually
checked should it be described as available in-game.
