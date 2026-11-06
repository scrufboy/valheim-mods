# Souls faction correction candidate — 2026-11-06

SOURCE-INSPECTED, candidate only. Not installed. No game launched.

Owner direction relayed by the active Workshop chat: Pillagers cannot be Undead;
draugr must fight Pillagers; Krauguk must not inherit Fuling allegiance. Existing
`LivingWorldCORE/FACTION-AND-PEOPLES-BIBLE.md` independently specifies distinct
Pillager and Eitrherjar identities, with Eitrherjar hostile to draugr and Fulings.
The existing allegiance chat owns runtime implementation. This folder owns only
the narrowly scoped configuration preparation and evidence; no competing runtime.

## Exact identities and observed causes

| Stable prefab | Installed display | Installed JSON/native spawn | Required managed identity |
| --- | --- | --- | --- |
| `RRRN_Pillager` | Common Folk Pillager | `Undead` | `LWC.Pillagers` |
| `RRRN_OrcBrute` | Krauguk Legions | `PlainsMonsters` | `LWC.Eitrherjar` |

Both names are preserved. Neither prefab is renamed or migrated. JSON definitions
are `rrrnpcs.custom.CommonFolk.json` and `rrrnpcs.custom.Orcs.json` in the existing
Souls config directory. Natural spawn sections `WorldSpawner.9005` and `.9004`
respectively repeat those native assignments. Both current mixed Souls raid roots
force `PlainsMonsters`, overriding their separate identities again.

## Candidate behavior

`prepare_candidate.py INPUT_CONFIG OUTPUT` reads the existing config tree and
writes two candidate files plus a hash report to a new, separate directory. It
rejects output beneath an installed profile/BepInEx and never overwrites files.

- Remove `SetFaction` only from natural spawners 9004 and 9005.
- Remove event-wide forced faction from the two existing mixed Souls raids.
- Keep the previous effective `PlainsMonsters` explicitly on unrelated raid slots.
- Preserve explicit Colossus `Undead`, every roster member, spawn count, timing,
  biome, `HuntPlayer`, equipment and appearance setting.

**This candidate requires the managed faction runtime. It is not a standalone
fix.** Original RRR JSON faction fields remain untouched until that runtime's
assignment contract is available. Arbitrary strings must not be inserted into
RRR's enum parser. Removing spawn overrides alone restores the old JSON values;
it cannot create the required distinct allegiances.

Runtime handoff: resolve stable prefab IDs, not display names or body/species;
give both actors distinct managed identities; correct stale persisted native
faction only for those managed actors; resolve relations in both argument orders
before native same-faction rejection; preserve self, explicit same-group and
tame/player protections. Unknown actors/pairs yield the existing rules. Never
rewrite all Undead, PlainsMonsters, companions or other mod groups.

## Upstream research

[ASharpPen Spawn That](https://github.com/ASharpPen/Valheim.SpawnThat) and
[Custom Raids](https://github.com/ASharpPen/Valheim.CustomRaids) provide native
faction assignment. They do not provide the required arbitrary allegiance matrix.

[CreatureManager faction source](https://github.com/sighsorry1029/CreatureManager/blob/main/CreatureFactionManager.cs)
and [default faction file](https://github.com/sighsorry1029/CreatureManager/blob/main/docs/defaults/factions.yml)
implement custom numeric IDs and relations. Its repository includes GPLv3 license
text. No upstream code/assets were copied. Its broader creature/AI/modifier
ownership and active global native faction definitions increase compatibility
surface; the owned LWC runtime is the narrower choice for this correction.
The upstream sample's native faction list also omits current `DeepNorth`, so do
not treat its full global defaults as a drop-in replacement for the current game.

Existing chat: **Build encounter allegiance and uncertain…**,
`01a182f2-4a63-7d63-9fad-d893de8505ae` (local host). It already owns the engine-free
matrix and now receives runtime coordination through the parent chat.

## Verification boundary

OFFLINE-TESTED: preparation against the actual installed config succeeds and
records before/after SHA-256. Field-level comparison checks that only the named
faction fields change; all six original natural spawners and all nine mixed-raid
slots remain. No DLL, asset, profile or game execution is part of this work.
Runtime hostility, ownership transfer and multiplayer remain UNKNOWN until tested.
