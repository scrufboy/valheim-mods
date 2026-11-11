# Formidable full-roster development - 7 November 2026

Brian explicitly requests development and visible results using LWC tools and FormidableAI. Full Souls roster first, with authorized Sages Vault clothing incorporated into the content pack with Fantu's credit. Preserve the six source identities and their authored lore; rebuild native implementation rather than rename/repackage RRRCore.

## Work actually done
- Added a lossless roster-input reader under migration/. It inspected all six installed Souls definitions and 40 equipment references. Source hashes, every category/unknown field, weighted equipment lists, special tokens and advanced attacks are retained. Duplicate keys/identities, nonfinite values and unsafe output destinations are refused.
- Added DartSkirmisherModule.cs against the existing FormidableAI CombatFrame/IFormidableCombatModule contract and shared arbitrator. It proposes reachable close evasion, regrouping before pack commitment and authorized visible-target firing with clear lane, weapon readiness, recovery and a pack fire permit. No module directly moves a native actor, deals damage or creates a second brain.
- Seven importer tests and 19 C# policy checks passed using the actual existing kernel source from the formidable-actor worktree. Reproduce with Verify-Source.ps1 -KernelDirectory <the actual FormidableAI Kernel directory>.
- Opened the existing portable Workbench 0.5.28 suite02 on Brian's Windows desktop; connected Human Lab asset sources and visibly loaded Red Neck Shaman. The actual preview shows neckhead, red Sage robe and fire staff, with IdleTweaked reporting 54 matched tracks, 12 meshes and 53 bones. This is appearance/animation evidence, not a registered hostile shaman or gameplay acceptance.
- Only root was active in this conversation's worker inventory. Separate chat activity cannot be established from that inventory. No claim of notifying separate chats.

## Source roster preserved
| Technical identity | Installed source name | Established LWC identity/context |
| --- | --- | --- |
| WoodsSoul | Forest Child | Forest Guardian; not a human child |
| NinoBosque | Black Forest Child | Distinct Forest Guardian; not a human child |
| Servant | Corrupted Servant | Existing Corrupted Servant / Myrkvidr Thrall lore retained |
| Pillager | Common Folk Pillager | Living Pillager people; never blanket Undead allegiance |
| OrcBrute | Krauguk Legions | Eitrherjar Legionary, retained authored name |
| Neckman | Swamp Neckman | Existing neck lineage retained; add a separate Neckman Shaman |

No saved identity is migrated by the reader. Display-name correction is distinct from prefab/save migration. The Behemoth remains an additive existing creature with all its accepted lore and presentation.

## Latest mounted design replaces the earlier two-actor proposal
Brian now specifies ONE composite creature/entity while stacked. The mounted spear/headdress figure and lower figure need authored hit regions and coherent two-body animation. A lethal upper-region outcome replaces the pair with the lower bareheaded single; a lethal lower-region outcome leaves the upper headdress single. A defined lethal-both outcome leaves no survivor. This supersedes the earlier simultaneous-two-network-actor approach.

Use one owner-authoritative death transition and an idempotent event identity so ownership transfer, delayed hits and ragdoll callbacks cannot spawn duplicate survivors or duplicate loot. Define damage/area-hit routing and survivor health/equipment explicitly during implementation; do not infer region solely from height. Pair formation by two singles is still requested design exploration and must consume both singles exactly once if implemented. Individual blowgun characters and their native testing remain first.

## Creature feel
Tiny adult monstrous goblin-derived individuals: very fast, evasive and fragile. Typical authored encounter target: roughly 7-10. Avoid unfair simultaneous dart volleys; the fact source/executor must implement actual pack firing permits. Packs should have idle activity, interactions with creatures, regrouping and attack transitions. Dart balance, speed, health, orange eye glow, skin adaptation, masks and source-clip event timing require actual actor/asset inspection. No unsupported numerical tuning is silently installed.

## Remaining blockers
The standalone FormidableNPCFramework directory was a scaffold; this source increment is not a completed RRR-free runtime. Native NPC composition, appearance/gear/effects/attack conversion, per-actor FormidableAI execution and packaged Sages resource registration remain to be built and game-tested. The new dart module has no native executor yet.
Purchased Goblin Scout/spear bytes have not been located in the checked Unity cache/download paths. Ownership does not establish an imported model or clip set. A valid package/project path is needed for the actual tiny model.
The shaman is visibly opened in the tool, not spawned in-game. No game/save/profile was altered by this pass. No new portable build/shortcut or installer was created; the existing suite02 portable was launched.

Voice candidates found for later audition: Monster Series Goblin Voice Pack (Unity 64494), Goblin Voice Clips Vol. I (Stormwave Audio), Goblin Vocalizations (Magic Sound Effects). These are researched candidates, not purchased or approved audio. Existing owned voice libraries should be checked before any purchase. Separate idle/chatter/alert/blow/attack/hurt/death semantics, bounded group overlap, and no cloned Blizzard voices.
