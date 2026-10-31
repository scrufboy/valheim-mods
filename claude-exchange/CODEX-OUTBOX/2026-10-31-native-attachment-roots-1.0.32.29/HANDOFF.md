# 1.0.32.29 — both native facial-hair attachment roots

Date: 2026-10-31

## Live result being corrected

- **LIVE-PROVEN FAIL:** 1.0.32.28 loaded and one male (`Hair4`, `Beard19`) reached `APPEARANCE READY`.
- **LIVE-PROVEN FAIL:** the second male reached the correct Beard1 hash but failed the visible-beard validator; the party was rolled back.
- 1.0.32.28 is failed, not partially promoted.

## Exact cause

- **SOURCE-INSPECTED native IL:** `VisEquipment.AttachItem` supports exactly two facial-hair prefab roots.
  - `attach_skin` is parented to `m_bodyModel.transform.parent`.
  - rigid `attach` is parented to the native `m_helmet` head joint.
- **SOURCE-INSPECTED assets:** Beard1 is the only numbered rigid `attach` beard; Beard2–Beard26 are `attach_skin`. Hair1 and Hair2 are also rigid `attach`; the remaining numbered hair styles are `attach_skin`.
- 1.0.32.28 incorrectly used `m_bodyModel.transform.parent` as the entire native-rig boundary. That accepts Beard19 but rejects Beard1 (and would reject Hair1/Hair2).
- Valheim's `VisEquipment.Awake` defines `m_visual` as the actual complete visual root, and `UpdateLodgroup` enumerates renderers from that same root.

## Candidate

- Branch: `work/native-hair-attachment-1.0.32.29`
- Commit: `1ec0f18`
- Parent: `af31a58`
- Version: `1.0.32.29`

The validator now requires the native attachment to be inside `VisEquipment.m_visual`, then retains all existing active-object, renderer, material, skinned-root, and LOD0 checks. It does not accept bald/beardless actors and does not alter the attachment transaction. Beard failure reporting now includes selected/current hashes, object presence, and the exact renderer reason.

## Required action

Canonical Mono build, every gate, review the native two-root evidence, back up failed .28, and install .29 only if clean. Then run exactly one `lwparty` gate. Both actors must become ready and the complete party must bind.

