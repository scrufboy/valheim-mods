# CORE 1.0.0 live combat readout

Date: 2026-10-23

Scope: read-only review of the first available Claude CORE 1.0.0 combat session. No source, DLL, profile, configuration, or gameplay asset was changed.

## Candidate and evidence

- **LIVE-PROVEN:** `LogOutput.log` loaded `Living World C.O.R.E. NPC 1.0.0` and `Living World C.O.R.E. Telemetry 1.0.0`.
- **INSTALLED:** NPC DLL SHA-256: `71D0F72DB02E5614C9BEC3BC899D49A6DB045DDA308755B8FCB44B4AB8BAB37D`.
- **INSTALLED:** Telemetry DLL SHA-256: `68A9A5E202DBF4B1CDC87004AA1241881B051EAF6E506599D9FE6E018520E90F`.
- **LIVE-PROVEN:** Telemetry file: `LWC-20261023-202843-LWC_HumanLab_v1-part00.jsonl`, schema 10, 1,001-second run, 5,083 records.
- **LIVE-PROVEN:** The party contained Gerlaug Eiriksdottir (guard) and Hallfrid Ormsdottir (archer), with successful appearance records and two-party completion.
- **SOURCE-INSPECTED:** Source read for interpretation only: `Behaviors/CombatBehaviors.cs`, `Kernel/NpcBody.cs`, `Kernel/Kernel.Pure.cs`, and `Telemetry/CoreTelemetry.cs` from `C:\Users\scruf\Documents\LivingWorld-CORE-NPC`.

## What worked and must not regress

- **LIVE-PROVEN:** Two companions existed and produced 2,876 NPC samples.
- **LIVE-PROVEN:** No domain ownership violations and no kernel behavior faults were recorded.
- **LIVE-PROVEN:** Weapons remained visible on backs in calm; the analyzer found no empty-back samples and only one calm sample with a weapon in hand.
- **LIVE-PROVEN:** No torch was held as a combat weapon. No sub-three-second torch flicker was recorded. Calm-night torch eligibility was satisfied in 90% of applicable samples.
- **LIVE-PROVEN:** Crouch matched the leader in all exercised samples.
- **LIVE-PROVEN:** Party cohesion passed the current analyzer: mean party separation 6.7 m, maximum 27.1 m, and 3% of samples above 16 m.

## Bow use: present, but not formidable or role-correct

- **LIVE-PROVEN:** Hallfrid held `BowFineWood` for 356 samples, approximately 178 seconds. Of those, 284 samples (approximately 142 seconds) were in `Combat`.
- **LIVE-PROVEN:** A target was present in 279 of 356 bow samples (78.4%). Target types included Greydwarf, Greyling, Greydwarf Shaman, Skeleton, and Draugr.
- **UNKNOWN:** Schema 10 does not record bow draw, attack start, projectile release, projectile hit, damage dealt, animation state, or ammo consumption. Therefore this file proves bow selection/holding, not arrow firing rate or successful hits.
- **FAILED:** Weapon selection churned heavily. Hallfrid produced 55 bow episodes and 57 sword episodes, with 55 bow-to-sword and 53 sword-to-bow changes. Median bow episode length was 2.01 seconds; 24 of 55 bow episodes were shorter than two seconds.
- **FAILED:** Bow use did not maintain archer spacing. Of 279 bow samples with a target, 139 were below 4 m and another 79 were between 4 and 8 m. Thus 218/279 (78.1%) occurred inside 8 m.
- **FAILED:** At the final Skeleton/Draugr encounter, Hallfrid acquired a Skeleton at 23.10 m at `t=793.13` while holding `SwordIron`. She continued closing with the sword and did not first select the bow until `t=801.70`, 8.57 seconds later, after the target changed to a Draugr at 7.18 m.
- **SOURCE-INSPECTED:** `NativeCombatBehavior` delegates weapon choice entirely to `Humanoid.EquipBestWeapon` once per new native target. There is no archer-role range commitment or class hysteresis in the inspected combat behavior. The live result shows that native best-weapon selection alone does not fulfill the archer contract.

## Defense and survival

- **FAILED:** The guard never held the shield during any of 466 `Combat` samples: every combat sample was `handL=none`, `handR=SwordIron`.
- **FAILED:** The archer's 212 sword-combat samples also had no shield in hand. Shields were visible on the back in calm and appeared in-hand only for five readiness samples per actor.
- **UNKNOWN:** Schema 10 does not record block attempts, successful blocks, parries, staggers, dodges, or attack animation edges. Weapon-only blocking cannot be measured from this file.
- **FAILED:** In the final Skeleton/Draugr encounter, Gerlaug fell from 250 HP to a last observed 60.22 HP and then disappeared from telemetry at `t=910.12`. Hallfrid fell from 250 HP to a last observed 6.82 HP and disappeared at `t=918.19` while fighting a Skeleton at 0.99 m.
- **UNKNOWN:** No death/despawn/destroy edge is recorded. Both absences occurred 83-91 seconds before `RunStop`, so shutdown is not the explanation, but the exact terminal cause cannot be certified from schema 10.

## Combat-regroup target clearing is repeated, not edge-triggered

- **FAILED:** Encounter-release counters reached 26 for Gerlaug and 28 for Hallfrid. Hallfrid's counter rose from 0 to 7 during one five-second regroup interval (`t=420.8` through `425.8`).
- **SOURCE-INSPECTED:** `CombatRegroupBehavior.Enter` calls `Body.ReleaseEncounter(this)`. Its `Tick` then calls `ReleaseEncounter` again whenever a native or static target reappears.
- **SOURCE-INSPECTED:** `NpcBody.ReleaseEncounter` clears alert, creature target, static target, attack timing, and sets `UpdateTargetTimer` before incrementing `EncounterReleases`.
- **FAILED:** The source comment says the out-of-area pursuit is dropped "once," but both source and live counters show repeated target clearing within the same regroup episode. This is a concrete combat interruption path and should be made edge/idempotence safe without altering the accepted spawn, identity, stow, torch, or calm modules.

## Phase churn: measured, with a qualification

- **FAILED:** The shipped analyzer reports 148 `Combat`/`CombatClear` flips, 26.1 per minute. Gerlaug had 44 CombatClear episodes (35 shorter than two seconds); Hallfrid had 34 (27 shorter than two seconds).
- **SOURCE-INSPECTED:** `NativeCombatBehavior.Wants` covers both `Combat` and `CombatClear`, so these label flips do not by themselves prove domain-owner loss. Treat this as noisy/fragile encounter-state evidence, not as the sole cause of bad combat.

## Telemetry and log defects

- **FAILED:** `CoreTelemetry.Start()` threw an early `NullReferenceException` before world load, then later recovered and recorded the session. The start predicate is not sufficient to guarantee every object used by `Start()` is ready.
- **UNKNOWN:** Six `Trying to add item to occupied slot -1, -1` warnings occurred before party readiness. Their owner is not identified in the log.
- **UNKNOWN:** Six BlackForest-to-Meadows `GetBiome` warnings occurred over modified terrain. No causal link to companion behavior is established.

## Specification mismatch to preserve in review

- **SOURCE-INSPECTED:** The analyzer currently passes only when torches are absent during `Readiness`.
- **FAILED:** Brian's governing requirement is different: a torch may remain through curiosity/attention/readiness and must be doused on full combat commitment. The automated gate should not silently redefine that contract.

## Required boundary

Do not alter spawning, identity/appearance, names, hair/beards, two-person party creation, back stow, calm torch stability, crouch matching, or formation behavior to address these combat findings. The evidenced owners are combat weapon selection/role policy, combat defense equipment, combat-regroup release idempotence, and missing combat telemetry.
