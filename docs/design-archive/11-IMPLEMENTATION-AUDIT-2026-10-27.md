# Audit: design archive implementation map and work order

**Audited:** `design/lwc-design-archive` @ `38a6103`, files `02-CURRENT-IMPLEMENTATION-MAP.md` and `10-CLAUDE-WORK-ORDER.md`.
**Against:** the 1.0.x source (`LivingWorldCORE/npc`, branch `claude/voice-1.0.32.1` @ `33addad`), the Lab install and GitHub `main` @ `54f8d7a`.
**Scope:** this audits the archive; it does not replace it. No source, install or config was changed.

Status words used below: SOURCE-INSPECTED, OFFLINE-TESTED, BUILT, INSTALLED, LIVE-PROVEN, DESIGN-AUTHORITY, OPEN.

## 1. Corrections to `02-CURRENT-IMPLEMENTATION-MAP.md`

| Archive statement | Fact (status) | Correction |
|---|---|---|
| "Saved tag in the v3 bundle" | v3 contains the 1.0.x history up to `9933763` (1.0.32.7 plus the lock). It does **not** contain 1.0.32.8 (`1502c0b`) or 8b (`33addad`). (SOURCE-INSPECTED) | The complete record is the **v4 bundle**: `Documents/LivingWorld-CORE-NPC/LWC-valheim-mods-1.0x-v4.bundle`, SHA-256 `0b5fd617e437eccf01eaf1b12f799cb35d30763dc0dbd0ee79e1bd5dc1cd7912`. It was read back from the PC and verified: clean clone, strict integrity check passes, the tag rebuilds the approved DLL `4dee75d4…`, and the 8b head rebuilds `7cc46473…` with 96 of 96 tests passing. |
| Installed state is not stated | **1.0.32.7 is INSTALLED** (DLL `89b5beaf…`, config `95624ae1…`, all 80 voice files unchanged). It has **not been launched**: the last log line is 12:21, the end of the 1.0.32.6 session. | Add the installed row. 1.0.32.7 is not yet LIVE-PROVEN. |
| "a blocked wait must never expire back into a straight collision path … remain mandatory" | Fixed in **1.0.32.8b**. A blocked companion waits, re-checks both lanes after 1.5 s, otherwise stays stopped, and is never handed back into the blocker. Deterministic right of way; the yielding companion backs out of the way. 16 spacing tests. (OFFLINE-TESTED, BUILT; not INSTALLED) | Mark as fixed in 8b. **Still open:** live door, slope and physical-contact proof. Also open: whether travel looks too tied to rear/flank spots (judged from the live test with `DevSpacingLog = true`). |
| "1.0.32.8 party spacing exists as a review build" | 1.0.32.8b is the current review build: `7cc46473…`. Brian approved it for a live test, not as final. | Update the version and hash. |
| "Those commits are not all present on GitHub `main`" | **None** of the 1.0.x runtime is on GitHub: `LivingWorldCORE/npc/` doesn't exist on any GitHub branch. The 1.0 kernel was added in `26dc9b5` (2026-10-23), branched from `main` @ `91f80f2`, plus 92 commits since. | Sharper statement: 1.0.x exists only locally and in the v4 bundle. See section 4 for getting it onto GitHub. |

## 2. The real module map (1.0.32.8b source)

Status is per the 1.0.x source. "None" means no code exists for it.

| Archive module | 1.0.x source | What exists | Status |
|---|---|---|---|
| Identity | `Identity/NpcIdentityRecord.cs`, `NpcAppearance.cs`, `NpcFactory.cs` | Name (patronymic), sex, hair, beard and role. Saved under the keys `lwc_human_v2_name/female/hair/beard/role/marker`. | LIVE-PROVEN for these fields only. No origin, body recipe, biome, class or history. |
| Persistence | Saved per-NPC keys: `lwc_human_v2_*` (identity), `lwc_human_v2_leader/party` (`NpcLeaderBinding`), `lw_kit` and `lw_kit_quality` (`NpcBody`) | Identity, leader binding and kit tier are saved on the NPC. | LIVE-PROVEN for those keys. **Not saved:** inventory contents, durability, skills, relationship, home, order or history. No versioning or migration. |
| Traversal | `Behaviors/TraversalBehavior.cs` (formation leases, moving anchor, catch-up), `SettledGuardBehavior.cs` | Follow, regroup and settled guard. Native `Follow` is the only executor. | LIVE-PROVEN (pre-spacing). **No portals or instanced interiors** — gate 03 has no code. |
| Party spacing | `Kernel/PartySpacing.Pure.cs`, `Behaviors/PartySpacingProbe.cs`, `tools/movement_writer_gate.py` | Soft standing areas, back-off from the leader (wider in a menu), sidestep, blocked wait, right of way. | OFFLINE-TESTED, BUILT (8b). |
| Awareness / reaction | `Kernel/Kernel.Pure.cs` (`PhaseArbiter`: Calm, Readiness, Combat, Regroup, cohesion), `Behaviors/TargetSelection.cs`, `NpcKernel` senses | Phase rules with clear and readiness timers; committed-threat target choice; no wallhack. | LIVE-PROVEN. |
| Investigation | — | **None.** No bounded-interest module. | OPEN |
| Combat | `Behaviors/CombatBehaviors.cs`, `FallBackBehavior.cs`, `Embodiment/MeleeChain.cs`, `ArmorParity.cs`, `WarCry.cs`; native `MonsterAI` owns committed combat | Target authorization, weapon choice by range, chained melee, heavy attack only at an opening, armor parity, fall-back, war cry. | LIVE-PROVEN. **No bombs, thrown weapons, lunge, magic or predictive aim.** |
| Needs | `CorePlugin` config `CompanionFoodStamina`; `Kernel.Pure` stamina rules | Extra heavy-attack stamina from a flat config value only. | **No food use, meads, rest, retreat-to-heal or environment needs.** OPEN |
| Gear | `NpcBody` kits (`lwgear iron|plains`), `HandsBehavior`, shield pairing | Fixed biome kits, ammunition top-up, stowing. | LIVE-PROVEN for kits. **No owned or loaned inventory, durability, repair, or equip-best from inventory.** |
| Progression | — | **None** (beyond the kit tier). | OPEN |
| Roles / classes | `NpcAppearance` role key; companion and settled guard behaviors | Companion, plus guard while settled. | **No worker, resident, trader or hunter; no class profiles.** OPEN |
| Voice / presentation | `Companion/Combat/CompanionCombatVoice.cs` + `CombatVoicePolicy` | Combat voice. | LIVE-PROVEN and LOCKED (1.0.32.6; gate in every build). |
| Mortality | — | Vanilla death and removal; death voice. | No downed state, memorial or gear recovery. Gated OFF by design. |
| Telemetry | `Telemetry/CoreTelemetry.cs` (the separate DLL); installed recorder is the 1.0.23 build | Session recorder. | LIVE-PROVEN. |

**Misplaced ownership and duplication (SOURCE-INSPECTED):**
1. `NpcBody.cs` (848 lines) mixes engine-level body services (native writes, pace, follow, placement) with Living World content (biome kits). Kits belong in a gear or content module.
2. `Kernel.Pure.cs` (913 lines) holds the phase rules plus every policy: voice, war cry, chain, stamina, block, fall-back, weapon choice. Each policy should become its own module, as was done for `PartySpacing`.
3. The settled-guard and spacing terrain checks (ground, segment, water) are two near-copies. They should be one shared terrain service.
4. Identity lives in the `lwc_human_v2_*` keys spread across three classes. There is no single character record of the kind the archive's Identity and Persistence rows require.

## 3. Conflicts between the documents (need a decision)

1. **Where the implementation lives.** `PROJECT-BOUNDARIES.md` names `LivingWorldHumanNPC/` (the 0.3.x line) as the actor package and `FormidableNPCFramework/` as the framework. The running runtime is `LivingWorldCORE/npc` (1.0.x), an independent kernel that is neither of those directories. The docs need to name 1.0.x as the actor line and decide which of its services move into `FormidableNPCFramework/`.
2. **The combat plan's base.** `SPECIALIST-COMBAT-CAPABILITY-PLAN.md` says it builds on the "0.3.58 moving-anchor defense line". It must be re-based on the 1.0.32 kernel before any bomb code.
3. **Work order.** Archive files `08` and `10` put **portals and instances before bombs**. ChatGPT's earlier message ordered: validate .7 → spacing → solidify the core → combat (bombs…), with no portals step. **Brian to confirm** where the portal gate sits.

## 4. Work order audit (`10-CLAUDE-WORK-ORDER.md`)

| Step | State |
|---|---|
| 1. Read the archive and binding docs | Done (this audit). |
| 2. Inspect both histories; report provenance | Done (sections 1–3). **Push blocked:** GitHub refused Claude's push with "Claude doesn't have GitHub access to scrufboy/valheim-mods" (HTTP 403). The Claude GitHub App needs installing with write access, or Codex pushes from the v4 bundle (below). |
| 3. Keep the 1.0.32.6 combat-voice lock | Holding: the gate passes on 1.0.32.7 and 8b. |
| 4. Finish spacing; install after prior-build validation | 8b built and accepted for a live test. **Waiting on:** Brian launching 1.0.32.7 once. Then install 8b with `DevSpacingLog = true`. |
| 5. Portal / instance gate design | Not started. No code exists for it. |
| 6–9 | Not started. |

**For Codex, to put the 1.0.x line on GitHub without touching `main`:**
```
git fetch "<Documents>/LivingWorld-CORE-NPC/LWC-valheim-mods-1.0x-v4.bundle" \
  claude/voice-1.0.32.1:refs/heads/lwc-core-1.0/1.0.32.8b-party-spacing-test \
  baseline/combat-voice-1.0.32.6:refs/heads/lwc-core-1.0/baseline-combat-voice-1.0.32.6 \
  claude/lwcore-npc-1.0.0:refs/heads/lwc-core-1.0/parked-1.0.33 \
  refs/tags/combat-voice-approved-1.0.32.6:refs/tags/combat-voice-approved-1.0.32.6
git push origin lwc-core-1.0/1.0.32.8b-party-spacing-test lwc-core-1.0/baseline-combat-voice-1.0.32.6 \
  lwc-core-1.0/parked-1.0.33 combat-voice-approved-1.0.32.6
```
**Expected heads:**

| Ref | Commit |
|---|---|
| `lwc-core-1.0/1.0.32.8b-party-spacing-test` | `33addad57523d5d95df7eb2572403e4f76d1ea36` |
| `lwc-core-1.0/baseline-combat-voice-1.0.32.6` | `26fd295b87d0c1057f7503b64012e165621b3d2c` |
| `lwc-core-1.0/parked-1.0.33` | `965a2a045436ed5a2128ea92b489f4d48d795831` |
| tag `combat-voice-approved-1.0.32.6` | `b577288` (points to `26fd295`) |

**Contents check (SOURCE-INSPECTED):** across the 92 commits not on `main`, no DLL, EXE, WAV, MP3, OGG, video, config, log, zip, bundle, image or backup file is added. `LivingWorldCORE/npc` contains no local paths. The older Tooler and HumanNPC build scripts contain `C:\Users\scruf\…` defaults that are already public on `main`. Authors: Brian (38 commits) and Claude (53).

