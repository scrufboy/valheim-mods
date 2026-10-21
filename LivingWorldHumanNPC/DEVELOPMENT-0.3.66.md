# Human NPC 0.3.66 — native facial-hair commit repair

## Live rejection of 0.3.65

The clean `lwparty` run created two new named actors. Video showed both actors
bald and inert. The log recorded independent failures for `Hair25` and `Hair7`:

`native hair hash 0 != -74603963 for Hair25`

`native hair hash 0 != 1043840417 for Hair7`

No NPC telemetry stream began because neither actor crossed appearance
initialization. The behavior installer therefore remained dormant and the
joint Follow transaction was refused. This was a complete party-creation
failure, not a leftover actor and not user error.

## Cause

The managed appearance harness did not model Valheim 1.0.20. Its fake
`SetHairItem` and `SetBeardItem` methods immediately changed the current render
hash and created attachment objects. The real methods only update the requested
selection and owner ZDO. `VisEquipment.UpdateVisuals` later reads the native ZDO
and calls the attachment methods. That false double let 0.3.65 pass offline
without proving the runtime sequence that failed for both actors.

## Correction

- Keep the verified `Hair1..Hair38` and optional `Beard1..Beard26` catalogs.
- At the fresh, locally owned, pre-revision boundary, commit the selected hashes
  to Valheim's own `ZDOVars.s_hairItem` and `ZDOVars.s_beardItem` records.
- Read both hashes back before invoking the native visual update.
- Accept the actor only after the selected current hashes and real attachment
  renderers match the immutable saved profile.
- Update the managed double to reproduce the real setter → ZDO → visual-update
  sequence instead of manufacturing an attachment inside the setter.
- Do not alter combat, follow, formation, torch, stow, wetness, doors, ambient,
  workstation, cleric, raid, resource, or pathfinding implementation methods.

## Acceptance

With no old test actors loaded, one `lwparty` command must create exactly two
new people. Both must have visible head hair; any selected male beard must be
visible. Both must complete the existing behavior install and begin following
together. Any bald, inert, single, duplicated, or non-following result rejects
the candidate.
