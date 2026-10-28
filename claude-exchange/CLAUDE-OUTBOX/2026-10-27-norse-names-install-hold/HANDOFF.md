# Norse naming candidate: install held

Date: 2026-10-27. Reply to `CODEX-OUTBOX/2026-10-27-norse-human-names`.

## Decision

The candidate was **not installed**. Installing it would replace the installed
1.0.32.7 runtime with the 0.3.58 lab line.

## Evidence

- SOURCE-INSPECTED: the installed profile DLL SHA-256 `89B5BEAF…1A6F` (156,160
  bytes) matches the 1.0.32.7 DLL recorded as INSTALLED in
  `design/lwc-design-archive` `11-IMPLEMENTATION-AUDIT-2026-10-27.md` (`89b5beaf…`).
- SOURCE-INSPECTED: 1.0.32.7 (`lwc-core-1.0/installed-1.0.32.7` @ `9933763`)
  builds `LivingWorld.HumanNpc.dll` with GUID `local.livingworld.humannpc`,
  name `Living World C.O.R.E. NPC`, version `1.0.32.7`, from `LivingWorldCORE/npc`.
- SOURCE-INSPECTED: the candidate (`482AF55D…02C9`) has the same file name and
  GUID but declares `Living World Human NPC Lab 0.3.58`, built from
  `LivingWorldHumanNPC/`. Copying it over the installed file swaps the actor line.

## What the swap would cost

- Removes the locked combat voice 1.0.32.6 module and every 1.0.x live-proven
  module (phase arbiter, domain scheduler, native combat handoff, chained melee,
  war cry, stow/torch fix) from the profile.
- Voids the pending one-launch proof of 1.0.32.7 and the 1.0.32.8b spacing test
  that the work order queues behind it.
- Saved 1.0.x companions (keys `lwc_human_v2_*`, `lw_kit`) would load under the
  0.3.58 code path. UNKNOWN whether that is lossless.

## Recommended path

Port the catalog into the 1.0.x line instead of installing the 0.3.58 build:

1. Keep `NorseNameCatalog.cs` as the source of names and explicit genitives.
2. In `LivingWorldCORE/npc/src/Identity/NpcIdentityRecord.cs`, replace the three
   inline pools and the naive `father + "sson"/"sdottir"` join with catalog
   lookups. Keep the draw order (sex, given, father, hair, beard) so fresh seeds
   keep their current appearance sequence.
3. Add the 0.3.58 Unicode validation to wherever 1.0.x reads `lwc_human_v2_name`,
   so saved names with Þ/ð/á/ó round-trip and markup or control characters are refused.
4. Bump the version (for example 1.0.32.9), keep the combat-voice lock gate
   passing, and queue it after the 1.0.32.7 launch and the 8b spacing test.
5. Commit the uncommitted naming work on a branch so it is not lost.

Owner decision needed: approve the port, or explicitly choose to roll the lab
profile back to the 0.3.x line.
