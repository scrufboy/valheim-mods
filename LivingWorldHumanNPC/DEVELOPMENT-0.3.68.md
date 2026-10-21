# Human NPC 0.3.68 — saved facial-hair recovery boundary

## Live failure accepted

0.3.67 loaded on 2026-10-21, but both saved companions were still bald at
launch. The log reported Hair7 and Hair19 with native current hashes of zero.
The direct native attachment completion added in 0.3.67 did not execute because
it was incorrectly restricted to actors with no saved visual revision. These
actors already carried `native-human-v1` from an older readiness boundary even
though their runtime attachments were missing.

## Narrow correction

Every authoritative actor with an existing valid `HumanProfile` now receives
the same presentation verification after Valheim's ordinary visual pass. If
the selected native current hashes or renderer attachments are absent, the
actor is repaired through Valheim's own `SetHairEquipped` and
`SetBeardEquipped` methods using the immutable saved profile. Its identity is
not rerolled and its saved profile record is not rewritten.

The managed harness now reproduces the 0.3.67 launch condition: a saved visual
revision and saved native hair/beard selections, but zero runtime current
hashes and no attachment objects. Passing requires both native attachment calls
and preserves the saved identity record.

No combat, follow, formation, torch, stow, wetness, door, ambient, workstation,
cleric, raid or pathfinding implementation changed.
