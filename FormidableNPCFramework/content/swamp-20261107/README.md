# Formidable roster evidence — November 7, 2026

`roster-manifest.json` is an authoring inventory, **not an installable content pack**. It records inspected definitions, exact existing identities, original bundle hashes, requested additions and unresolved integration work. A null replacement prefab means that no new native actor is established here. It is not permission to allocate a conflicting identity.

| Role | Existing exact runtime identity | Current evidence |
|---|---|---|
| Pillager | `RRRN_Pillager` | Souls definition inventoried; native Formidable port pending |
| Green Forest Guardian | `RRRN_WoodsSoul` | Souls definition inventoried; native Formidable port pending |
| Neckman / Fen Draugr | `RRRN_Neckman` | Souls definition inventoried; retained identity |
| Blue Forest Guardian | `RRRN_NinoBosque` | Souls definition inventoried; native Formidable port pending |
| Eitrherjar Legionary | `RRRN_OrcBrute` | Souls definition inventoried; retained established name |
| Corrupted Servant / Myrkvidr Thrall | `RRRN_Servant` | Souls definition inventoried; native Formidable port pending |
| Neck Marauder | `LWC_MarshMarauder_Prototype` | Existing installed 0.3.4 package and matching asset bundle; owner acceptance pending |
| Draugr Behemoth | `LWC_UndeadColossus_Test` | Existing installed 0.1.41 package and matching asset bundle; preserve accepted appearance and lore |
| Neckman Shaman | Not established by this inventory | Red appearance recipe exists; native integration being developed separately |
| Tiny adult blowgun goblin | None | Purchased Scout source awaits local acquisition; no playable prefab |
| Random Neck variants | None | Optional, unauthored |
| Shoulder composite | None | Later; singles first |

The six Souls identifiers are preserved, including their equipment-reference inventory. The manifest does not duplicate the full legacy definitions or licensed assets. Item names show references rather than proof that every item resolves in the current game. Original configuration hashes let another owner compare inputs without rewriting them. The inventory does not declare a finished RRR-free port.

The Shaman must shield self, Neckmen, Marauders and tiny allies, never the Behemoth. Healing has not been specified. It fights at range, replenishes minions under bounds, attempts beast summoning and uses an orange staff-tip control tether. Accepted health damage breaks control; the beast becomes hostile to every side. The Shaman retreats and attempts a fresh safe rebind. Policy tests alone do not prove any shield, summon, visual effect or faction change in game.

The tiny goblin comes first as an individual adult: squat proportions, relatively enlarged head/limbs/feet, high speed, low durability, blowgun poison-dart harassment and simple close attacks. The dart source policy is not yet a native attack executor. Local Scout and spear discovery evidence is recorded in `LWCModelSkinStudio/exchange/goblin-preparation/2026-11-07/SOURCE-STATUS.json` and `HANDOFF.md`.

The mounted design is **one composite game entity**. A top-region death leaves the bottom individual without the headdress; a bottom-region death leaves the upper individual with the headdress. Simultaneous death leaves neither. Implement exactly-once survivor replacement, health/equipment transfer, loot and multiplayer ownership before claiming this behavior works. This replaces the earlier two-independent-actor stacking proposal.

Raid roles can be tested as a faction without moving Bonemass. Keep native undead and Bonemass intact. Preserve the established Behemoth fallen-hero/hoard/binding story; stitched multi-body, faces-in-skin and Neck-built skins remain proposals. Froglike Marauder fit remains conditional. Small Forest Guardians and the tiny goblins are not human child variants.

Run `python validate_roster.py` from this directory to check roster identity coverage, evidence presence and recorded hashes against this machine. Validation reads originals and writes nothing. It does not load the game or prove gameplay acceptance. Evidence can legitimately become stale when another owner builds a new version; reconcile the specific receipt instead of silently rewriting history.
