# Current implementation map

## Approved local runtime evidence

- Combat voice baseline: **1.0.32.6**, approved and locked.
- Saved tag in the v3 bundle: `combat-voice-approved-1.0.32.6`.
- Approved DLL hash: `4dee75d49c01a32f0250a09595b166e2deac5ad43d8b46aabeb3257b2667cc6e`.
- Snapshot: `Documents/LivingWorld-CORE-NPC/20261027-APPROVED-COMBAT-VOICE-1.0.32.6/`.
- 1.0.32.7 is a module/name boundary change over the locked behavior.
- 1.0.32.8 party spacing exists as a review build/diff, not an approved installed baseline.

## Proven combat-voice behavior

Semantic male/female pools cover alert, exertion, ordinary pain, devastating pain,
gloat, war cry and death. Timing, priority and gender routing are locked. Death
voice playback was corrected to survive actor destruction. Later non-combat dialogue
must use separate modules/pools and may not reopen combat-voice behavior casually.

## Spacing review status

The review build adds soft standing regions, player back-off, sidestep corridors,
and a wait state while retaining native Follow as the movement writer. A known
acceptance concern is that a blocked wait must never expire back into a straight
collision path while the same obstacle remains. Persistent-blocker, lane-clear,
right-of-way, oscillation and live door/slope tests remain mandatory.

## GitHub `main`

GitHub contains the broader design/framework and an older Human NPC implementation
line. It does **not** by itself prove that the approved local 1.0.32.x runtime is
merged. Claude must inspect both histories and report provenance before moving code.

Status vocabulary: `SOURCE-INSPECTED`, `OFFLINE-TESTED`, `BUILT`, `INSTALLED`,
`LIVE-PROVEN`, `DESIGN-AUTHORITY`, `OPEN`.

