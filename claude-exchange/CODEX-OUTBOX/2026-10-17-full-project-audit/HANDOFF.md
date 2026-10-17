# Living World C.O.R.E. — full project handoff to Claude

Date: 2026-10-17

## Authority and purpose

The user's direct intent governs this handoff. The user is frustrated by repeated regressions, by working behavior being removed while unrelated behavior is changed, and by requirements being converted into their opposites. Claude is asked to take ownership of the next implementation from the evidence in this package, not to produce another speculative rewrite.

The immediate objective is narrow: stabilize and prove the active human companion before any postponed feature work. Do not begin Outsiders, Timed Item Destruction, settlement expansion, jobs, food, beds, or other new features until the companion gates below pass live.

## Non-negotiable operating rules

1. Preserve working behavior. Never delete, bypass, weaken, or replace an accepted behavior merely to make a new feature or test pass.
2. Change one owning boundary at a time. A combat repair stays inside combat arbitration unless evidence proves another owner is faulty.
3. Keep movement, facing, hands, target selection, utility, doors, and social posture under explicit exclusive ownership. Lower-priority systems yield to higher-priority systems.
4. Treat awareness, target authorization, attack commitment, encounter ownership, and attack execution as different states.
5. Use native Valheim systems for movement, attacks, ammunition, projectiles, damage, blocking, animation events, inventory, status effects, and persistence. Adapter code requests and verifies; it does not fake outcomes.
6. Compilation and unit tests are not live proof. Label every claim as source-inspected, offline-tested, installed, or live-proven.
7. Never report success from selection or intent alone. For example, a bow is proven only by native attack release, ammunition loss, and projectile evidence.
8. Do not expose internal retries or incomplete transactions as user steps.
9. Do not reinterpret future/postponed ideas as permission to interrupt current companion work.
10. Before editing, make a protected baseline copy and a file/hash manifest. After editing, diff the complete accepted-behavior surface and rerun the complete gate, not only the newest test.

## Current candidate

Current installed candidate: `Living World Human NPC Lab 0.3.31`.

Installed/build DLL SHA-256 reported by the completed Xbox-compatible server-mod chat:

`7443EA847975413F71270A71ED17777660E1CE66006502F3D7B12576638775A0`

Source/build directory:

`work/LivingVillage/human-npc-0.3.31-combat-arbitration`

Status: offline gates passed and installed; **not live-proven**.

### Packaging/verification inconsistency that must be fixed before trust

The 0.3.31 candidate contains stale and contradictory release metadata:

- its root `START-HERE.txt` still identifies the archive as **0.3.12 — VISIBLE STOW CORRECTION LAB** and describes an older correction set;
- `build/build-verification.json` correctly identifies version 0.3.31 and the DLL hash, but records `ProfileInstalled: false` and `GameLaunched: false`;
- the completed project chat separately reports that 0.3.31 was installed.

Therefore the binary hash is currently stronger evidence than the packaging prose, and the installation claim must be independently verified against the actual profile DLL before testing. Do not ship or hand the user an archive with stale version/readme text. Make package metadata, build metadata, installed hash, and runtime startup version agree.

0.3.31 states that it:

- uses the Souls of the Land 30-meter sensing/acquisition envelope;
- treats a sensed, alerted enemy as committed without waiting for a hit or completed victim choice;
- separates acquisition distance from bounded physical pursuit;
- drops stale targets when native enemy state says the threat is inactive;
- requires repeated clear scans before regrouping;
- preserves the bow-facing gate before arrow release;
- bases torch use on rendered ambient light with hysteresis instead of time-of-day;
- leaves the established weapon-stowing implementation byte-for-byte unchanged.

Those are implementation claims pending live validation.

## Regression history and root pattern

The central failure pattern is not one bad line. It is cross-layer rewriting without preservation gates.

### 0.1.6 to 0.1.8

Early foundation versions established the pattern: later candidates changed previously working placement/area behavior while pursuing new work. The project already contains `REGRESSION-AUDIT-0.1.6-to-0.1.8.txt` and exact rollback packages. Do not repeat the patch-forward cycle; preserve known-good artifacts and compare behavior contracts before promotion.

### 0.3.17 to 0.3.19

The live regression audit identifies these confirmed or structurally proven failures:

- Active companions stopped readying when their bound leader drew a combat weapon because `LeaderReady` was removed from the readiness decision. A settlement etiquette rule was wrongly applied to active companions.
- Utility torch equip destroyed the visible back-stow transaction. Native equip/unequip transitions cleared hidden combat slots, and torch release did not restore the legal combat set.
- Torch decisions treated nearby light components as useful illumination and used generic refusal logging, obscuring the real gate.
- Non-Player human NPCs remained dry while the player became Wet because rain/shelter status was never authoritatively adapted to the native Wet status path.
- Companions used identical polling and delays, producing synchronized crouch, sit, draw/stow, and torch behavior.
- Door discovery used physics/pathfinding knowledge as character knowledge, allowing unseen alternative entrances to influence intent.
- Follow/party continuity failed at extreme distances; measured separation reached roughly 185 m for one companion and 83 m for another.
- Encounter state oscillated across target changes and missed scans, letting lower-priority systems reclaim movement and hands mid-fight.
- Equipment arbitration allowed bow, sword/shield, shield alone, torch/shield, torch alone, and empty hands during the same encounter.
- Attack execution was mostly refused by native start gates.
- Bow selection occurred without a single proven arrow release or ammunition loss.
- Low-health disengage, guarded recovery, and coherent re-entry were absent.

Root causes:

- rules for different contexts were collapsed into global rules;
- latest-feature tests were stronger than retained-behavior tests;
- intent/selection was counted as success instead of native outcome;
- multiple controllers independently mutated hands, movement, facing, or target state;
- path reachability was confused with actor perception/knowledge;
- live failure evidence was patched locally instead of repairing ownership and lifecycle boundaries.

### 0.3.20 to 0.3.31

This sequence attempted foundation repair, bow release, transactional stow, bow stow, party behavior, native combat, priority, awareness, aiming, console discovery, and combat arbitration. The number of successive candidates is itself a warning: do not assume the newest folder is a trustworthy champion simply because it contains all later changes.

Before modifying 0.3.31, compare it against the last version that live-proved each individual behavior. Preserve those behaviors with explicit tests and source/hash checks.

## Protected behavior matrix

These behaviors may be suspended only by an explicitly higher-priority owner and must resume afterward:

| Contract | Required observable proof |
|---|---|
| Identity/persistence | Same actor identity, role, inventory, orders, and relationships survive save/load and unload/reload. |
| Spawn/party transaction | One command yields exactly the promised ready party; retries do not duplicate, refill, resurrect, or silently replace valid actors. |
| Follow/formation | Stable identity-derived lanes; no collapse onto the leader; bounded separation and coherent recovery. |
| Peaceful life | Existing believable guard, observe, and short-meander behavior remains staggered and intact. |
| Leader readiness | Bound companions may visibly ready with their armed leader, but this never manufactures a target or authorizes an attack. |
| Threat lifecycle | Encounter ownership survives target switches and isolated missed scans; release occurs only after repeated legal clear scans. |
| Hands/equipment | Exactly one transactional owner; no impossible sets, empty-state loss, duplication, or identity loss. |
| Visible stow | Peaceful combat gear is visibly and verifiably attached to native back slots. |
| Bow | Body faces inside the firing cone; native attack starts; matching ammunition decreases; a physical projectile/hit or visible miss exists. |
| Melee/block | Native legal attack cadence, shield block, stagger, counterattack, stamina, damage, and animation gates remain authoritative. |
| Torch | Utility light never enters combat; darkness is based on useful ambient light with hysteresis; torch round-trip restores the same combat loadout and stow state. |
| Wet/weather | Exposed non-Player humans receive native Wet under equivalent rain/shelter conditions; liquid Wet remains distinct; native recovery modifiers apply. |
| Human timing | Narrow deterministic identity offsets prevent hive-mind social actions; combat interruption is immediate. |
| Doors | Directly perceived blocking doors outrank unseen alternatives; peaceful transit closes after party clearance; combat transit leaves the passage available. |
| Commands/telemetry | Help, autocomplete, metadata, runtime state, and success messages agree; telemetry explains state transitions and refusal reasons. |

No gate may be made green by deleting the behavior the gate was meant to protect.

## Immediate live validation for 0.3.31

Run one controlled session and capture startup log, structured telemetry, and video. Stop at the first failed boundary rather than continuing into unrelated scenarios.

1. Confirm startup reports exactly version 0.3.31 and the installed DLL hash matches the value above.
2. Spawn/restore the intended party once. Verify no duplicate actors, refills, or replacements.
3. In peace, verify sword, shield, and bow are visibly stowed in native back slots.
4. Draw/stow the leader's weapon. Verify companion readiness follows with bounded per-identity delays and creates no target by itself.
5. Enter a real hostile encounter at the 30 m sensing boundary. Verify sensing and commitment without waiting for first damage.
6. Verify physical pursuit remains bounded and does not inherit the 30 m acquisition radius as an unlimited chase rule.
7. Force target death/switch/temporary occlusion. Verify encounter ownership persists and lower-priority follow/torch/social controllers do not reclaim movement or hands.
8. For the archer, capture weapon intent, facing readiness, native `StartAttack`, bow attack trigger, arrow count decrement, and projectile/hit-or-miss evidence.
9. For melee, capture native movement, legal attack cadence, block/stagger, and recovery without constant circling or attack-gate spam.
10. After repeated clear scans, verify bounded regroup and visible restow.
11. At practical darkness, verify torch acquisition from ambient light; cross a dawn or marginal-light transition and confirm hysteresis prevents chattering or synchronized extinction.
12. Trigger threat while torch is out. Verify immediate legal torch-to-combat transaction and no empty/impossible hand state.
13. Test a directly blocking closed door in peace and in combat. Peaceful passage closes after party clearance; combat passage remains open; unseen entrances do not become intent sources.
14. Expose player and companions to rain and shelter transitions. Verify native Wet parity. This remained open in the earlier audits and must not be silently omitted.

## Promotion rule

0.3.31 becomes a baseline only if the complete applicable matrix passes live. If a gate fails:

1. classify the failure by owner;
2. identify the last live-proven version/artifact for that behavior;
3. write a failing preservation test before the repair when technically possible;
4. modify only the owning layer;
5. rerun all offline gates and the full live non-regression subset;
6. retain the failed build and evidence; do not overwrite history;
7. state exactly what is proven and what remains unproven.

Do not create 0.3.32 as a broad rewrite. A successor needs a named failing boundary, minimal scoped repair, explicit preservation diff, full gate results, installed hash, and live evidence.

## Postponed work

The following is explicitly later work and must not interrupt companion stabilization:

- UnJohnny's Outsiders;
- Smoothbrain's Timed Item Destruction;
- broader Xbox/server-side vanilla-effect research beyond the completed feasibility findings;
- food, beds, jobs, production, settlement expansion, role totems, arrivals, civic behavior, and advanced hostile door tactics.

The server-side/Xbox research may inform later architecture, but it is not permission to inject unvalidated effects, bypass native rules, or claim Xbox compatibility without a real crossplay test.

## Claude's required first response and work product

Do not answer with a generic plan. First:

1. Restate the user's non-regression requirement in concrete engineering terms.
2. Inventory the supplied evidence and identify which claims are live-proven versus offline-only.
3. Produce a pass/fail/unknown table for the protected behavior matrix.
4. Name the exact first live validation sequence for 0.3.31.
5. Make no code change until the failing boundary is evidenced.

After evidence exists, return:

- the failure owner and causal chain;
- the minimal files changed and why;
- proof that protected files/behaviors were preserved;
- complete offline gate results;
- build and installed hashes;
- live proof for the repaired behavior and all affected regression gates;
- an explicit list of remaining unknowns.

The user is not asking for another optimistic candidate. The user is asking for continuity, correct interpretation, working combat, visible stow, correct torches, correct facing and bow release, bounded pursuit, coherent regrouping, and proof that working systems were not erased.

