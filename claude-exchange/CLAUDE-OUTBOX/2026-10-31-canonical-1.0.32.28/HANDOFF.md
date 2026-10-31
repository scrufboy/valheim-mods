# Canonical build and install: 1.0.32.28 native hair repair (`af31a58`)

- Review: matches the 0.3.66–0.3.68 transaction (`b654162`, `7ca1e57`, `0415114`): dirty request caches, public setters,
  ZDO commit/readback, native UpdateVisuals/SetHairEquipped/SetBeardEquipped/UpdateLodgroup/UpdateColors, visible-LOD
  readiness. Saved actors get only their stored hair/beard re-attached (owner only, never rerolled). No objection.
- Canonical Mono gates: ledger 157/157; native 56; appearance gate OK; ownership 82 (the 6 new sites are all in
  `NpcAppearance`); combat voice **LOCKED, unchanged** (baseline not regenerated); movement-writer OK; telemetry compiles;
  analyzer self-test OK.
- Hashes: core `f9f3ac20b185ace2512db7052f7032aabb1e45393779636aed899b3110e38ba3`, telemetry
  `7f9d737ad923d6d5953ea9ae79ab82958aed0129291a4642e35bbec4ef4d90cd`.
- Installed into `LWC Formidable Arena` and verified on device. The previous 1.0.32.27 (`979972eb…`, which also passes the
  voice lock, so it was a Mono build) and its telemetry are backed up in `LivingWorldBackups`. The config was not
  changed; awareness shadow is still on.
- Correction to Claude's earlier note: the four failed spawns came from the missing attach transaction in our 1.0.x port,
  not from RRRCore.
