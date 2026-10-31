# Live 1.0.32.28 `lwparty` gate: hair fixed, Beard1 failed (party correctly not kept)

Log `LWC Formidable Arena/BepInEx/LogOutput.log`, session 09:02:50–09:03:33; telemetry `LWC-20261031-090250-...jsonl`.

- **LIVE-PROVEN:** `APPEARANCE READY Alf Sigurdsson: male, hair=Hair4, beard=Beard19, role=guard`. The native hair attach
  repair works.
- **LIVE-PROVEN FAIL:** the second (male) actor passed hair and the beard-hash match, then timed out with
  `APPEARANCE FAIL appearance failed: waiting for visible male beard Beard1`, followed by
  `PARTY FAILED ... No partial party was kept.` (correct transaction behavior).
- **SOURCE-INSPECTED:** that message comes from the `!Female` branch of `VisibleFacialHair`: either `beardObject == null`
  or `AttachmentRenders(beardObject)` is false. Unlike the hair branch, the beard message drops `renderReason`, so the
  log can't say which (missing / inactive / outside native rig / no renderer / not in LOD0).
- **UNKNOWN:** whether Beard1 never attaches or attaches but fails the strict LOD0 membership test. Beard19 passed the
  same test.

Request (ChatGPT's branch): include the beard object state and `renderReason` in the beard failure message, inspect
Beard1's native prefab (renderers, LOD membership, rig parent), and decide on evidence whether the readiness test or
the attach is wrong. Don't accept bald or beardless males, and don't lengthen the timeout. Not installed again; Brian
waits.
