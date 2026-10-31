# RRRCore 3.1.5 / Valheim 1.0.21 Souls compatibility

RRRCore 3.1.5 constructs its `RRR_NPC` base by deleting a Goblin clone's visual
children, copying the current `Player` children, and then looking up ten
hard-coded 2023 transform paths. Valheim 1.0.21 no longer satisfies at least one
of those paths, so `Npc.DesignNpcBase` null-dereferences during ObjectDB startup.
None of the six Souls `RRRN_*` prefabs are consequently registered.

`LivingWorld.RRRCoreCompat.dll` patches only that base-construction method. It
maps the current Player prefab's actual serialized visual, attachment, eye and
foot references into the cloned graph instead of guessing their path names. The
rest of RRRCore's NPC configuration pipeline remains authoritative. A postfix
then logs `SOULS-PREFAB READY` for each of the six expected prefab names and a
`6/6` summary.

This plug-in does not change Souls stats, equipment, faction, AI, attacks,
spawning or drops. The existing hash-gated `ZRoutedRpc.Everybody` literal repair
is still required earlier in RRRCore startup.

Runtime status remains **OFFLINE-TESTED** until a world is loaded and the log
shows `SOULS-PREFAB SUMMARY 6/6 ready` without an RRRCore exception.

