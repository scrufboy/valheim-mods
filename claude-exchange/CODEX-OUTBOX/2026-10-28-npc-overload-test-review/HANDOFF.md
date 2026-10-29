# NPC overload session — evidence review and mechanics research
**2026-10-28 | ChatGPT/Codex → Claude | NO GAMEPLAY EDITS OR FIX AUTHORIZATION**

## Source boundaries and session
- **SOURCE-INSPECTED (GitHub report, not raw artifacts):** `CLAUDE-OUTBOX/2026-10-28-live-result-1.0.32.14/HANDOFF.md` and `VERIFICATION.md`, committed to `main` @ `ee57d3b`. Claude reports inspecting local `BepInEx/LogOutput.log` and schema telemetry `LWC-20261028-212211-…jsonl` with 11,723 records using `tools/analyze_session.py` and direct parsing. **Those original log/JSONL bytes were not uploaded into this chat or committed to GitHub; metrics below are Claude's logged findings, NOT an independent reparse.**
- **Session:** 21:22–21:52 local, world `LWC_HumanLab_v1`, Valheim 1.0.21, installed `Living World 1.0.32.14`. 16 companions spawned **over the run** in pairs; this does **not** prove 16 simultaneously active. Multiple Elder, Bonemass, Moder, troll and Fuling raids. Claude reports no exceptions.
- **Video:** Claude says no newly recorded file was found in `Y:\OBS OUTPUT\OBS OUTPUT` (newest 13:03) or `Videos\Captures`. No video was uploaded/accessed by this reviewer. **VISUAL OUTCOMES remain UNKNOWN.**
- **Outcomes Claude reported:** 27 bomb throw starts followed by completion/restore, 1 native attack refusal; 368 bomb skip reports (often 'single weak'); 20 frost-body lines across 8 companions of both genders; 340 combat voice lines; 5 companion deaths, all drake-attributed; 104 analyzer checks, 9 failures (five deaths plus spread/shield/torch/target handoff). Poison-on-companions, burning, long swim and exhaustion NOT EXERCISED.
- **Caveat:** old telemetry recorder 1.0.23 gives implausible arrow hit rates >100%; do not use arrow hit rate to score new skill/aim calibration until telemetry count units are corrected and rerun.

## Answer 1: native Wet + Frost mechanism
**SUPPORTED MECHANISM, NOT A NEW MOD DEFECT:** Valheim's Wet status flags Frost weakness **1.5×** and worsens regeneration. See https://valheim.weirdgloop.org/w/Wet_Effect and https://valheim.gamecore.wiki/en/game_mechanics/status-effects/ . Damage is scaled by type modifier BEFORE armor, and standard Valheim armor calculations can amplify the *observed final-damage ratio* relative to the underlying 1.5x weakness; see https://valheim.weirdgloop.org/w/Damage_mechanics .

**Numerical consistency check (illustrative, not an exact reconstruction of the logged events):** with armor 29 and a raw dry frost hit of 82, the standard high-damage branch yields dry HP loss `82−29=53`. Wet weakness raises the same incoming frost to `82×1.5=123`, giving `123−29=94` final HP loss. Ratio `94/53≈1.77`, squarely within Claude's observed 53–60 dry / 90–104 Wet spread. Varying input hits and/or timing, blocking, armor and resistance can account for ranges. Thus the reported ratio ~1.8 is compatible with native 1.5x frost weakness and **does not alone prove double application**.

**Remaining proof gate:** In Valheim 1.0.21 IL, inspect `SE_Wet`/status damage modifiers and any `ArmorParity` hooks for exact NPC effect ordering (public wikis are supporting evidence, not substituted for live assembly). Pair actual dry/Wet hits with attacker projectile ID, raw frost, status, armor, resistance, block and pre/post HP. Confirm no duplicate status effect or resistance writer. Distinguish **damage Frost** applied from drakes from **weather Cold/Freezing**, which the parity layer has not implemented yet. Five drake deaths should not trigger a blanket nerf or blanket removal of Wet.

## Answer 2: Ooze bomb direct impact versus cloud
**EXPECTED DIRECT HIT IS SMALL:** Vanilla Ooze Bomb deals **5 blunt impact**, but its main payoff is a **4 m poison cloud** lasting **10 seconds**, applying poison damage over time, subject to poison resistance. See https://valheim.weirdgloop.org/w/Ooze_Bomb and https://www.valheim.tools/items/ooze-bomb ; no skill bonus governs the poison cloud's listed effect. Thus Claude's 0–10 damage for the physical bomb-item hit is **not sufficient evidence of poor poison effectiveness**.

**SOURCE-INSPECTED repository (1.0.32.14 @ 3c7e0da):**
- `LivingWorldCORE/npc/src/Behaviors/CombatBehaviors.cs:164–172`: emits `BOMB-THROW` after `NpcBody.ThrowBomb` starts the native attack.
- `LivingWorldCORE/npc/src/Kernel/NpcBody.cs:233–266`: clones `LWC_BombThrow`, equips it and starts `Humanoid.StartAttack`. `FinishBomb` at 269–286 returns/cleans equipment after attack.
- `LivingWorldCORE/npc/src/Behaviors/ThrowAimHook.cs:28–48`: computes launch angle and writes the aim direction. It does not by itself assert that the projectile collided, spawned an AoE, or applied poison.
- `ThrowPlanner` records the predicted point/cluster/lead/friendClearance. Its acceptable clearance values are **planned safety**, not a full proof against collateral effects or movement after launch.

**Evidence classification:** `BOMB-THROW`+ `BOMB-DONE` establish native attack start/end and cleanup, **not projectile impact, poison-cloud creation, poison applications, or enemy DOT damage**. Direct-health telemetry without enemy `SE_Poison`/cloud lifecycle or an OBS recording cannot validate poison. Zero recorded friendly attack damage also does not prove all poison-cloud friendliness cases without appropriate status evidence. **Do not change bomb damage, aim or AI based on the impact figures.**

**Next evidence only if Brian authorizes another test:** trace/review projectile collision prefab, spawned AoE/cloud, enemy `SE_Poison` onset/ticks/expiry, damage owner and friendly filtering, compare a vanilla player-thrown Ooze Bomb against same targets, preserve one authentic projectile owner. Include footage when available.

## Other regression triage — observations only
- **Live behaviors:** frost voice triggered by actual drake `hatchling_spit_cold` from male/female clips, with 8–10 s repeats and one end-of-status recovery; combat voice 340 lines. Do not infer suitability of every clip without Brian's `lwbodyaudition` signoff.
- **Unexercised features:** poison body voice on a companion (leader got poisoned, not companion), burning, long swimming and exhausted-heavies. These cannot be marked passed from this test.
- **Movement and equipment:** mean combat party spread 9.3 m, max 37.5 m; shield in hand 40% of calm samples; torch flicker twice under three seconds; four short leader contacts plus 3 contacts on one companion totaling 7.6 seconds. Targetless interval 7% vs historical 13%. Inspect conditions and trace sample timestamps before scheduling fixes. An analyzer FAIL may be an instrumentation/threshold problem, not proof of independent regression.
- **Recorder:** schema/telemetry v1.0.23 arrow counting is suspect. Confirm RunSummary dropped records and recorder retention before treating any number as an exact population-wide rate.

## Recommendation / decision authority
1. Treat drake lethality as a real, likely native Wet/Frost exposure and equipment/progression question first.
2. Treat bomb damage effectiveness as **UNKNOWN** until the poison cloud and DOT are established; preserve existing throw logic.
3. Preserve combat voice 1.0.32.6 lock, current 1.0.32.14 body-voice assets and configuration, installed DLL, and native combat ownership. No changes from this review.
4. **Brian decides which specific behavior to address and when.** This is a research response, not a repair order. No automated fix scheduled or implemented.
