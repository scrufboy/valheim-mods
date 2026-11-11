# Neckman Shaman and swamp faction trial
Owner directive: 7 November 2026. Ground creatures and repeatable raid first; Bonemass unchanged.

## Required combat role
The Neckman Shaman is a faster ranged support caster. It does not close to melee, use a poke or equip an unarmed combat fallback. It keeps distance, seeks a reachable escape when threatened, protects itself and eligible Neckman/Marauder/tiny allies with shield bubbles, and attacks enemies with a differently colored ranged area spell. Healing is not specified and is not added.

Shield selection explicitly excludes every authored Behemoth/homunculus variant, even when allied. Do not copy a vanilla all-friend shield AoE that bypasses this exclusion. A filtered native status-effect request must be tied to actual spell completion/cooldown and confirmed ownership.

Minions replenish rather than accumulate without limit. Spawn only a real registered tiny actor at a valid position, count live and pending actors against per-caster and encounter caps, and commit spawn reservations atomically. A typical tiny pack target is 7-10, subject to gameplay balance. Ground emergence is an art/animation choice, not delivered here.

## Beast control
| State/event | Required result |
| --- | --- |
| No beast arrives | Continue protection, minion support and ranged attacks. |
| Accepted encounter summon | One stable encounter record reserves the beast chance/attempt; no second raid spawner competes. |
| Binding completes | Staff-tip control tether and larger control effect surround the Behemoth; orange controlled eyes; explicit neck-family allegiance. |
| Shaman takes accepted positive health damage | Interrupt link immediately. Blocked/zero damage does not invent a health-damage event. |
| Link broken | Beast targets all sides, including the shaman and neck faction; no native Undead/Plains blanket alliance. Orange control eyes/tether stop. |
| Shaman recovering | Retreat toward real reachable safety. Further injury delays the next attempt. |
| Safe fresh binding completes | Reestablish scoped control, allegiance and control effects. Old interrupted-cast receipts must not restore it. |
| Shaman dies | Release its beast once. Do not spawn a duplicate, resurrect the caster or affect another controller's beast. |

The beast retains its existing attacks, appearance, death/loot and established treasure-keeper/fallen-hero/binding lore. Stitched bodies, tiny faces in the skin, new-made and Neck-built corpse variations remain optional art proposals, not applied changes or replacement origins.

## Test encounter
Required roles are Neckman, Marauder, tiny blowgun individual, Shaman and the possible Behemoth. Optional random Neck variants are additions. Build a manually repeatable event before enabling random raids or world replacement.
The full raid cannot be declared runnable until actual actor registration, family relations, native caster/summon executor and faction-preserving raid integration are verified. Do not use Fuling Shaman or a uniformly scaled placeholder as the missing characters.
Custom Raids must not impose a global raid faction. The installed version's omitted setting still forces Boss; source now includes an exact-event-only compatibility adapter. This adapter is not installed or game-verified.
A beast's encounter chance belongs to the single native shaman executor. It is not a per-10-second Custom Raids chance and is not an independently replenished raid slot.

## Tiny fighter
Purchased Scout-derived adult monster: reduce stature and make a squat silhouette while enlarging head, hands/limbs and feet relative to the body. Preserve the original mesh/rig/source and author a derived version. Do not represent uniform miniature scale as the requested body redesign.
Use its actual blowgun animation and a real weapon/projectile/ammunition path adapted from native bow/arrow mechanics, with poison darts and appropriate native material tiers. Fast evasive harassment alternates with approach and authored close club opportunities; fragility compensates for speed and numbers. Custom dart events, muzzle alignment, hit damage, ammunition, terrain, melee transitions and corpse must be checked in-game.
Masks/face/ear/eye modifications are separate visual work and must not silently break weights or source animation. The purchased Scout/spear source files have not yet been found locally; no miniature goblin prefab is claimed.

## Implemented source and evidence
ShamanControlPolicy.cs and its existing FormidableAI proposal adapter implement source-level control/recovery/shield eligibility/minion bounds. Tests reproduce damage interruption, recovery, shield exclusions, native-domain intent, interrupted bind receipts, removed-beast identity cleanup, per-encounter accepted summon latch and dead/uncommitted frame refusal.
Native spell source inspection confirms the Player-rig StaffFireball/StaffShield route; GoblinShaman's own attack triggers do not match this rig and its loadout includes a forbidden melee poke. Native shield and spell-item helpers compile against the current game but do not establish actor or executor integration.
Root saved the appearance separately through existing recipe validation/save and project loading as Formidable Neckman Shaman; the original Red Neck Shaman remains intact. Saved authoring project readiness is not gameplay readiness.
Raid compiler/routing tests and roster evidence validator are independent source checks, not a raid playtest. Human-constructor and native prefab registration remain a bounded implementation workstream. Native animation-state mapping, damage events, owner changes, late join, saved state, telemetry and live balance are outstanding.

## App authoring
This should ultimately be one authored shaman behavior profile with protection recipients/exclusions, ranged spell, reinforcement limits and beast-control settings, consumed by one authoritative executor. The present Workbench has no supported complete shaman-behavior export/consumer. Do not add inert UI controls or silently treat this source contract as an installed feature.

## Ground staff lightning — accepted additional attack
Brian selected the actual Ground Staff Attack preview on November 7. Add a staff-ground discharge that travels along valid ground toward the authorized player, then arcs to other hostile players and staggers those hit. Keep the existing ranged blast. Protection, retreat and beast control retain their priorities; this is another action of the same FormidableAI owner.

The installed Player controller maps Ground Staff Attack to `staff_shield`, Base Layer state 91, clip index 575. The native `Hit` event is at 1.078104138 seconds of clip time, not a guessed wall-clock timer. A cast receipt must distinguish this offensive action from a shield cast sharing that gesture. `staff_thunder` maps a different clip; StaffLightning's 12-projectile shot is also a different attack. Native chain-lightning effect prefabs supply presentation, not a verified targeting/chain implementation.

Source policy now selects `GroundLightning` only with cooldown readiness, a validated ground origin/path and the authorized combat target. It uses the existing attack/facing arbitration and preserves ranged-blast fallback. Terrain sampling, visible ground propagation, unique hostile-player hop selection, finite hop/range caps, per-cast hit/stagger receipts, cooldown consumption and owner migration belong to the native executor. No arcs through walls, repeated hits on the same player, friendly hits, recursive chains or instant repeated staggers. Damage, chain distance/count and recovery remain explicit tuning inputs for playtesting.

Source checks pass: 66 shaman checks, 19 dart checks and seven roster importer tests. This addition is not installed combat yet; preview animation is not proof of electrical effects, stagger or multiplayer execution.

## Habitat scope
Add inhabited swamp ruins/camps around authored populations first. The cached KumaBeer Stylized ancient modular ruins package is an available source to inspect. Alternative crypt rooms/locations are a separate later prototype; no replacement of existing worlds or Bonemass locations has run.
Published Jotunn data generated for Valheim1.0.7 lists GoblinHut03 with Goblin, GoblinArcher, GoblinBrute and GoblinShaman. This is a version-qualified role benchmark, not a verified count for every local camp.
