# Human NPC 0.3.62 — visible hair and beard repair

## Evidence from the failed 0.3.61 run

The video `2026-10-20 17-25-31.mp4` visibly shows both companions bald. Stig
Anundsson also has no visible beard. The same run's log reports Stig as
`hair=Hair14; beard=Beard22` and Sigrid Ragnarsdottir as
`hair=Hair34; beard=clean-shaven`.

That contradiction identified a false readiness check. Version 0.3.61 proved
only that Valheim had stored the selected hashes and allocated attachment
GameObjects. It did not prove that those objects had a live renderer on the
copied human rig.

## Correction

- Keep the verified contiguous native `Hair1..N` and `Beard1..N` catalog.
- After the native setters, invoke Valheim's existing `VisEquipment.UpdateVisuals`
  twice. The first pass constructs the selected attachment. The second pass
  applies the native hair material/property block after that renderer exists.
- Reject initialization unless each required attachment is active, has an
  enabled renderer and material, remains bound to the local human visual rig,
  and participates in the rig's first LOD.
- Preserve the existing `native-human-v1` save contract; this changes no saved
  identity format and does not reroll an existing identity.
- No combat, targeting, follow, formation, torch, stowing, door, wetness,
  ambient, workstation, equipment, resource, or stat logic changed.

## Live acceptance

Run `lwparty` once with no old companions loaded. Both new companions must have
visible head hair. Any male profile whose log names a beard must visibly show
that beard. The initialization line is now allowed only after the corresponding
renderers pass the stronger live-rig check.

## Installed candidate

- Installed in `LWC Human Lab v1` after confirming Valheim was closed.
- DLL SHA-256:
  `2E81223169C39F04F6B3D0D13D3BA6BF47D419A647A76481FBFE696A38483AB2`.
- The failed 0.3.61 DLL and start document are preserved under
  `BepInEx\LivingWorldBackups\HumanNpc\20261020-failed-0.3.61-bald-render`.
- Package SHA-256:
  `D81A2548C8B38F67E1D922547A4E13DC590374ADE61B9A1B6CCD89744B3CA36A`.
- Offline verification passed. Visible hair/beard in Unity remains the required
  acceptance test.
