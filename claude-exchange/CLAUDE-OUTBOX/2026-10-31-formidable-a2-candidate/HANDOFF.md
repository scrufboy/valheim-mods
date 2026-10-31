# FormidableAI A2 candidate — initiative, explicit classes, strike-window defense, lanes (1.0.33.0)

Directive: `claude-exchange/CODEX-OUTBOX/2026-10-31-formidable-a2-true-build-phase/HANDOFF.md` (3e5ac3c).
Base: canonical 1.0.32.29 (`work/native-hair-attachment-1.0.32.29`, 1ec0f18). It includes A1 awareness rev 5, bombs, the voice lock, heavies,
partner defense, the hair fix and telemetry. Branch: `work/formidable-a2`.

Evidence labels: everything below is **SOURCE-INSPECTED / OFFLINE-TESTED**. Nothing is LIVE-PROVEN. The candidate is not installed.

## 1. What now controls the companion, and what is still native

| Concern | 1.0.32.29 | A2 candidate (switch, default) |
|---|---|---|
| When a fight starts | Native MonsterAI finds a target (FindEnemy every 2 s, own senses). Median 10.9 s after awareness commitment, at 9 m | **Initiative** (`[FormidableAI] Initiative`, on; needs `AwarenessShadow=true`). The awareness transition is acted on at the next 0.1 s kernel tick: alerted = Readiness (draw, face the contact); committed = the class response — engage inside class reach, readiness in formation beyond it. `NpcBody.Engage` sets the native target directly |
| Gaps between targets | Drop the slot, wait up to 2 s for the native re-pick | The initiative contact, else TargetPolicy's own pick, is engaged at once (**continuity**) |
| Class | Inferred: bow carrier vs not | **Explicit**, stored on the person (`lw_class`): Warden, Huntress, Scout, Legion (`[FormidableAI] Classes`, on) |
| Weapon choice | One band for all (bow ≥ 10 m) at the native ~1 s ask | Class bands, re-checked every 0.25 s in combat. No swap under a charge that arrives within 1.2 s |
| Chasing | Any engaged target ≤ 30 m from me | Class **leash**: a target farther than leash from the leader is not taken or kept (self/partner defence excepted) |
| Shield | Off (`ShieldBlocking=false`) | **Strike-window defense** (`[FormidableAI] Defense`, on): one decision per incoming strike, timed to its predicted impact |
| Fall-back | Run away + toward leader, blind | **Lane** chosen first (terrain, water, obstacles, never past an enemy; toward the partner, else the leader); no lane = stand (`[FormidableAI] FallbackLanes`, on) |
| Calm back | Whatever was last in hand (bow only in 45–50% of calm samples) | Melee weapon (+ shield beside a one-hander) re-equipped before stowing |

Still native: pathing and pursuit, the attack animation and its hit, chains/heavies (1.0.17/1.0.21 rules, unchanged), bombs (1.0.32.12 planner, unchanged),
facing while attacking, block mechanics after the raise (Valheim's own perfect-block stagger, stamina, damage), damage itself, voice (locked; gate unchanged).

**Not available: Dodge.** In Valheim, dodge with invulnerability frames exists only on `Player` (verified in metadata: `Player.Dodge`, `Player.UpdateDodge`).
Companions are Humanoids. Triggering the animation alone would be a fake dodge, so the defensive movement option is **Evade**: a planned side-step on the fall-back mover.

## 2. Arbitration priority (as implemented)

1. Imminent personal defense: defense window (block/parry/interrupt/evade) inside NativeCombat; fall-back/evade mover outranks combat
2. Save/cover a partner: TargetPolicy partner defence; initiative intercept (a contact on the leader or partner)
3. Safe opening area control: bombs (unchanged planner); Huntress/Scout ranged opening on committed contacts
4. Class attack/interrupt
5. Reposition: leash, readiness in formation, cohesion latch (unchanged)
6. Observe/readiness/calm

Re-evaluation: awareness at 10 Hz plus the 0.1 s kernel heartbeat. Timers remain only for physical windows (block hold, evade 0.7 s, lane re-check 0.5 s, weapon re-check 0.25 s).

## 3. Classes (A2.2)

| | Warden | Huntress | Scout | Legion |
|---|---|---|---|---|
| Created from | role `guard`; roster slot 1 | role `archer`; roster slot 2 | roster | roster |
| Iron kit weapons | SwordIron + ShieldBanded + BowFineWood | same as today | SpearBronze + BowFineWood | AtgeirIron + BowFineWood |
| Plains kit weapons | SwordBlackmetal + ShieldBlackmetal + BowDraugrFang | same as today | SpearWolfFang + BowDraugrFang | AtgeirBlackmetal + BowDraugrFang |
| Bow from / melee under | 12 / 7 m | 6 / 4 m | 10 / 5 m | 14 / 8 m |
| Own bow minimum (native AI fires from) | 8 m | 4 m | 5 m | 8 m |
| Engage reach / leash | 14 / 14 m | 28 / 28 m | 24 / 22 m | 14 / 14 m |
| Defense weights | parry .7, interrupt .3 | evade .8 | interrupt .7, evade .9 | absorb .8, interrupt .6 |

- Armor tiers are the existing ones for every class. Warden and Huntress keep exactly today's kits (ledger-tested), so existing companions' gear does not change.
- The class is written once per person. Companions created before this build get the class of their creation role (guard → Warden, archer → Huntress), logged once.
- New pairs take `[FormidableAI] PairClasses` (default `Warden,Huntress`). This is Brian's single control: e.g. `Scout,Legion`.
- Scout/Legion pairs are outfitted at the iron tier on creation, and their kit is re-applied on every load (Valheim re-gives prefab items).
- Fallback: `Classes=false` gives every companion the Warden doctrine and the 1.0.32 weapon rule (`WeaponChoice`); no class kits.
- The doctrine reads the actual loadout: no shield in hand = no block, whatever the class.

## 4. Strike-window defense (A2.3)

Per enemy Humanoid in an attack: the native `m_currentAttack` and its elapsed time `Attack.m_time` (added to NativeApi; native gate 58/58).
The decision uses:
- predicted impact = windup − elapsed, plus flight time for projectiles;
- aimed at me = its target is me, or it faces me within reach.

The windup is learned by watching strikes land (`StrikeTimingHook`, a separate postfix on `Attack.OnAttackTrigger`, within 30 m of a companion) with a 0.55 s / 0.85 s prior.
One `DefenseWindow` per strike (attacker + strike start). Its choices:
- Interrupt: own swing lands first, in reach, facing, class weight ≥ .5, not against a heavy.
- Parry: learned windup only. Raise 0.20 s before impact (the 0.1 s tick makes it 0.10–0.20 s), inside Valheim's 0.25 s perfect-block interval.
- Block: raise 0.35 s before impact (never before reaction time), hold to impact + 0.25 s, release. An unlearned (prior-timed) guard stays up while that attacker is still swinging, up to 1.5 s.
- Evade: free side lane, impact ≥ reaction + 0.1 s tick + 0.35 s, class weight or heavy or low health. Known cost: the native target must be released for the step (MonsterAI walks to its target and ignores the anchor), so the companion re-engages after ~0.7 s.
- Absorb: everything else, including impact sooner than this person's reaction time (0.12–0.30 s).
- Counter: one swing after a defended strike, if the attacker is in reach and in front.

No window, no raise (ledger: 10 quiet seconds never raise).

## 5. Fallback lanes (A2.4)

Eight 6 m lanes. Each is checked for standable ground at the end, no drop or climb over 1.2 m, not under water, and no obstacle at chest height (`CombatLanes`, read-only).
A lane is excluded if it passes within 2.5 m of a threat. Scoring: distance from threats (with 0.8 s of their motion) + class cover weight × progress toward the partner (else the leader).
The lane is re-checked every 0.5 s and kept unless a new one is clearly better. No lane: the fall-back ends "no lane: stand and fight".

## 6. Ownership map (every new write)

| Write | Domain | Writer (NpcBody) | Caller |
|---|---|---|---|
| Native target (initiative/continuity) | Movement | `Engage` | NativeCombatBehavior.Tick |
| Weapon re-check | Hands | `RefreshWeapon` → `ChooseWeapon` | NativeCombatBehavior.Tick |
| Shield raise/lower | Hands | `SetBlocking` (existing) | NativeCombatBehavior.StepDefense |
| Interrupt / counter swing | Hands | `StrikeNow` | NativeCombatBehavior |
| Face attacker while guarding; face focus contact | Look | `LookAt` (existing) | NativeCombatBehavior.LateFrame; AwarenessLookBehavior |
| Lane / evade movement | Movement | `Follow`, `SetPace`, `SetRun` (existing); anchor transform (existing FallBackBehavior site) | FallBackBehavior |
| Calm back loadout | Hands | `Stow` → `StowCanonical` | HandsBehavior |
| Class identity (ZDO) | none (identity, owner only) | `SetClass` / `EnsureClass` | party creation; kernel on load |
| Class kit | none (existing Outfit path) | `Outfit` | party creation; reload |

Gates:
- Ownership gate: 89 write sites, all inside the allowed classes.
- Movement-writer gate: unchanged lock, 13 sites, OK.
- Voice lock: 71 methods unchanged; `OnNewTarget` is fed by initiative engages too (same call-out law).
- Native gate: 58/58.

## 7. Telemetry (schema 14) and analyzer

New `Npc` fields:
- class
- initiative, initiativeWhy, initiativeEngages, continuityEngages
- focus, targetTtc, throwing, evading
- evades, interrupts, counters, defenseWindows, defense
- lane, backStows

New rows: `Defense` (one per window: attacker, attack key, choice, why, impactIn, actLead, learned, outcome, damage, attacker staggered, stamina, hp) and `DefenseGap`.
New Initiative milestone: **MemberResponse**. RunStart records the four switches and the roster.

Analyzer `A2:` section:
- committed → response;
- awareness-late cases;
- melee vs class-bow-range;
- fall-back clear share;
- calm back;
- defense windows, outcomes, timing, raises-vs-windows;
- engages.

## 8. Directive metrics that needed correcting (sources conflict — surfaced, not silently picked)

1. **"Response-ready 3.9 s → ≤ 0.75 s."** In A1, `ResponseReady` is marked when a *committed contact comes within 30 m* (`AwarenessTuning.ResponseRange`). It measures how fast enemies close, not how fast companions respond.
   The companion's actual latency in that session was **Committed → native target 10.92 s median**. A2 adds `MemberResponse` (first readiness-facing or engage by any member). The ≤ 0.75 s target is graded on Committed → MemberResponse; `ResponseReady` is unchanged.
2. **"Shield held during calm 55% → ≤ 5%."** The 55% is the share of calm samples where the shield is *on the back or in hand*. The failure is the other ~45%: only the **bow** on the back, sword and shield nowhere (telemetry: 161 of 361 calm samples `backL=BowFineWood, backR=none`).
   A2 fixes the stow. The analyzer grades "only the bow on the back" ≤ 5%. Re-analysing the same session with that definition gives 50% today.
3. **"Melee against distant targets 20%."** In that session 82 of the 155 flagged samples were **bomb throws in hand** (`LWC_BombThrow`), counted as melee by the 1.0.12 check.
   Excluding throws gives 10% at ≥ 10 m; by class bands and without < 1.2 s charges, 8% (same session, re-analysed). The A2 check grades class bands; the legacy figure is printed beside it.
4. **Classes.** The shadow library `LivingWorldCompanionSystems/FormidableAI` (1.0.32.22, shadow-only) defines Warden, Huntress, Mystic, Skirmisher, Commander, Pathfinder and its own shadow `DefensePolicy` (0.8 s reaction cooldown, Dodge).
   The directive names Warden, Huntress, Scout, Legion. The execution path follows the directive (type `TrainedClass`, to avoid a name clash). The shadow library is untouched and still shadow-only.
   **Decision needed (Brian/Codex):** retire the shadow class set and shadow defense, or map them onto these four, so there is one implementation.

## 9. Test parameters (all exposed in telemetry; chosen conservatively)

- Engage reach and leash per class
- Weapon bands
- Swap-commit 1.2 s
- Windup priors 0.55 / 0.85 s, learned after 3 strikes
- Block lead 0.35 s, parry lead 0.20 s, hold after impact 0.25 s, unlearned hold ceiling 1.5 s
- Evade 0.7 s side-step 3 m
- Lane 6 m, 2.5 m pass clearance, 0.5 s re-check
- Weapon re-check 0.25 s

## 10. Independent review (before push)

A separate review agent read the diff cold. It found 13 issues; all were fixed before this push (ledger 186/186):
- evade anchor too close and too late;
- lanes ignored a hazard cloud (the hazard flight now keeps the 1.0.32.15 path);
- creation-roster class race (classes are now written at spawn and applied at once);
- the leash did not let go of a held target (one shared rule now governs both taking and keeping);
- an intercept could reach past 30 m and loop drop/re-take with call-outs;
- bows held inside the native fire minimum;
- prior-timed guards dropping before slow hits;
- parry lead lopsided by the 0.1 s tick;
- weapon swaps removing a planned shield;
- stale lanes passing an enemy;
- an evade window surviving into a later fight;
- per-tick strings/rays/boxing in defense;
- multi-hit attacks skewing the learned windup.

**Kept by design (flagged):** a contact committed by sustained closing breaks the leader's stealth (A1 doctrine: commitment means stealth has failed).

## 11. Not done in this candidate

- Bombs' "prefer the outer envelope for committed groups" is not changed (1.0.32.12 planner as is).
- Cohesion latch thresholds are not changed (the leash is the cohesion change).
- No Tooler control added: the roster is in the installed config.
- `IFormidableProfileSource` is the only Hostile Peoples boundary: a pack registers doctrine by family name; no second brain.
