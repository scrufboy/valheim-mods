# Cursor project handoff — Living World C.O.R.E. / FormidableAI

Date: 2026-10-31

This file is the starting point for Cursor. It is a map to the whole project, not permission to redesign it.

## 1. Repository and local routes

- GitHub: https://github.com/scrufboy/valheim-mods
- Authoritative documentation checkout:
  `C:\Users\scruf\Documents\ChatGPT\Living World C.O.R.E. Custom NPC CREATION and AI\valheim-mods-formidable-current`
- Corrected A2 working checkout (active code repair; review branch, not installed):
  `C:\Users\scruf\Documents\ChatGPT\Living World C.O.R.E. Custom NPC CREATION and AI\valheim-mods-formidable-a2-corrected`
- Safe installed-source baseline checkout lineage: remote branch `origin/work/native-hair-attachment-1.0.32.29`, commit `1ec0f18`
- Rejected A2 candidate: remote branch `origin/work/formidable-a2`, commit `f2b8e1f`
- Corrected A2 review branch: `origin/work/formidable-a2-corrected`, commit `4cbcfff`
- Current project/exchange branch: `work/formidable-ai-current`, current authority commit `b9364fe`
- Live r2modman profile (read-only unless Brian explicitly authorizes an install):
  `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena`
- Recorded gameplay video: `Y:\OBS OUTPUT`
- Earlier user-thinking experiment, useful only as background and never as implementation authority:
  `H:\COULD BE FRIDAY`

Other worktrees under the common parent are historical or specialized. Do not assume a worktree is current merely because its name sounds relevant:

- `valheim-mods` — encounter/world work
- `valheim-mods-awareness-a1` — awareness/Tooler lineage; currently open in Cursor but not the authoritative correction checkout
- `valheim-mods-bomb-range` — bomb-range lineage
- `valheim-mods-formidable-actor` — older 1.0.32.21 Formidable observer lineage
- `valheim-mods-vikingai-audit` — comparison/audit lineage

Preserve untracked material, especially:
`valheim-mods-formidable-current\compatibility\RRRCore-3.1.5-Valheim-1.0.21\`

## 2. Read these before touching code

Read completely, in this order:

1. `AGENTS.md`
2. `CLAUDE.md`
3. `claude-exchange/SYNC.md`
4. `claude-exchange/PROTOCOL.md`
5. `claude-exchange/DECISIONS.md`
6. `docs/development/CHARACTER-IDENTITY-CONTRACT.md`
7. `REPOSITORY-LAYOUT.md`
8. `docs/CURRENT-PROJECT-STATE-2026-10-27.md` (historical orientation; newer exchange decisions override it)
9. The latest relevant handoffs in both `claude-exchange/CODEX-OUTBOX/` and `claude-exchange/CLAUDE-OUTBOX/`

The critical newest handoffs are:

- `claude-exchange/CODEX-OUTBOX/2026-10-31-discussion-is-not-execution/HANDOFF.md`
- `claude-exchange/CODEX-OUTBOX/2026-10-31-character-identity-contract/HANDOFF.md`
- `claude-exchange/CODEX-OUTBOX/2026-10-31-huntress-identity-hard-stop/HANDOFF.md`
- `claude-exchange/CODEX-OUTBOX/2026-10-31-formidable-a2-class-migration-stop/HANDOFF.md`
- `claude-exchange/CLAUDE-OUTBOX/2026-10-31-live-full-roster-tooler-2.6.6/HANDOFF.md`
- `claude-exchange/CLAUDE-OUTBOX/2026-10-31-formidable-a2-candidate/HANDOFF.md` (rejected candidate evidence only)
- `claude-exchange/CLAUDE-OUTBOX/2026-10-31-formidable-a2-installed/HANDOFF.md` (rejected installation record only)

## 3. What this project is

Living World C.O.R.E. is a persistent player-character-grade NPC foundation for Valheim. FormidableAI is its shared tactical combat brain.

The goal is not immortal or cheating NPCs. They have stakes, vulnerabilities, gear, food, armor, skills, training, biome context, relationships, jobs and mortality. They should survive by making competent, fair, timely decisions: notice, assess, communicate, form up, choose a target, select a tool, defend, evade, block, counter, reposition, protect a partner, use area control, disengage and recover.

The core principle is real arbitration using who, what, when, where, why and how—not isolated timer tricks such as “block every two seconds.” Timers may measure cadence, reaction or bounded windows; they must not substitute for tactical cause.

Valheim supplies physical tools and rules. C.O.R.E. supplies the decision architecture that can later serve friendly companions, hostile Living World people, camps and village encounters without forking the brain.

## 4. Character identity is immutable and additive

This is a hard contract:

- Guard remains Guard.
- Archer remains Archer.
- Scout remains Scout.
- Warden is a separate future authored character, not an upgraded or renamed Guard.
- Huntress is a separate future authored Plains spear character, not an Archer alias.
- Legion/heavy is a separate future authored character.
- Progression improves the same person; it never transforms identity.
- Tactical modules may be shared. Identity may not.
- Equipment is capability, not identity.
- Never infer or persist a replacement class from weapon, prefab role or current loadout.
- Never write `archer -> Huntress`, `guard -> Warden`, `scout -> Warden/Pathfinder`, or an “anything else -> Warden” fallback.

## 5. Conversation and authority contract

- Questions, brainstorming, comparisons and interrogative design conversations are non-mutating by default.
- Context can cross chats; authorization cannot.
- A named implementation task is scoped to that task. Do not turn roster discussion into roster code.
- Do not invent defaults that silently redefine existing characters.
- Before implementation, state the exact authorized sentence and the files/systems it permits changing.
- If authority is ambiguous, stop and ask one direct question.
- Never install, merge, push, migrate saved data or alter the live profile without explicit authorization for that action.

## 6. Current safe state

- The live Arena profile is restored to game build `1.0.32.29`.
- Human NPC DLL SHA-256:
  `D376D63BEAFA85714F9B0EC1912E5F98AEF5E0697219C44220EC7443BE7B6AB5`
- Telemetry DLL SHA-256:
  `F127B7F4AD53044943DBED8A067829AD1394AB16681A073CDB70664FD861BDE4`
- Rejected `1.0.33.0` was not launched after installation, so its `lw_class` migration could not write to companions.
- Rejected binaries were archived under the profile's `BepInEx\LivingWorldBackups\rejected\2026-10-31-1.0.33.0-class-alias\` folder.
- Do not launch or install the rejected candidate.

## 7. What went wrong in rejected A2

Codex produced a contradictory directive that named only Warden, Huntress, Scout and Legion/heavy. Claude then implemented:

- `archer -> Huntress`
- every other creation role -> Warden
- persistent `lw_class`
- `PairClasses = Warden,Huntress`
- default-on execution switches
- installation before the class model was approved

This would have folded Archer into Huntress and Scout into Warden. That design is rejected in full. Do not preserve it for compatibility.

## 8. What is worth salvaging from A2

The following identity-neutral work may be retained after audit:

- awareness-driven initiative and response continuity
- accurate per-member response telemetry
- strike-window defense based on an observed incoming attack and predicted impact
- one decision per strike: block, parry, evade, interrupt, counter or absorb
- learned attack windups with conservative priors
- fallback lane selection that avoids terrain, water, obstacles and enemy pass-through
- weapon reevaluation by existing authored role and actual capability
- calm hand/back stow correction
- telemetry/analyzer schema additions

These must remain behind switches that are off by default until reviewed and explicitly enabled.

## 9. Active corrected A2 worktree

Work only here for this audit/repair:
`C:\Users\scruf\Documents\ChatGPT\Living World C.O.R.E. Custom NPC CREATION and AI\valheim-mods-formidable-a2-corrected`

It is branch `work/formidable-a2-corrected`, commit `4cbcfff`, based on rejected `f2b8e1f`. It is a WIP review branch: it has passed only source grep/diff checks, not the full build. Inspect the entire diff before editing.

Already-started corrections include:

- removal of `lw_class`, `PairClasses`, pair-roster writes and class-kit rewriting
- replacement of the rejected closed `TrainedClass` model with a non-persistent tactical reading of existing `Guard`, `Archer`, `Scout`, plus an `Unknown` neutral doctrine that preserves the original role name
- A2 switches default off
- telemetry renamed from class to authored role
- older Formidable shadow profile being corrected so it stops labeling Guard as Warden, Archer as Huntress and Scout as Pathfinder

Do not trust these edits merely because they exist. Compile, test and audit them.

## 10. Source map

Primary 1.0.x runtime:

- `LivingWorldCORE/npc/src/CorePlugin.cs` — plugin/config/party creation and Harmony registration
- `LivingWorldCORE/npc/src/Kernel/NpcKernel.cs` — per-actor kernel, phase/arbitration scheduling
- `LivingWorldCORE/npc/src/Kernel/NpcBody.cs` — controlled game writes and equipment/body actions
- `LivingWorldCORE/npc/src/Kernel/AwarenessGroup.cs` — group adapter and runtime awareness
- `LivingWorldCORE/npc/src/Kernel/Awareness.Pure.cs` — pure contact/group/initiative evidence logic
- `LivingWorldCORE/npc/src/Kernel/Kernel.Pure.cs` — pure phase/arbitration rules
- `LivingWorldCORE/npc/src/Kernel/Formidable.Pure.cs` — corrected A2 pure initiative/defense/lane decisions
- `LivingWorldCORE/npc/src/Kernel/NativeApi.cs` — verified private game-member access
- `LivingWorldCORE/npc/src/Behaviors/CombatBehaviors.cs` — combat execution, bombs, defense window
- `LivingWorldCORE/npc/src/Behaviors/TargetSelection.cs` — target policy bridge
- `LivingWorldCORE/npc/src/Behaviors/FallBackBehavior.cs` — fallback execution
- `LivingWorldCORE/npc/src/Behaviors/CombatLanes.cs` — lane probes
- `LivingWorldCORE/npc/src/Behaviors/StrikeTimingHook.cs` — observed strike timing
- `LivingWorldCORE/npc/src/Telemetry/CoreTelemetry.cs` — session records
- `LivingWorldCORE/npc/tools/analyze_session.py` — session analyzer
- `LivingWorldCORE/npc/tests/RegressionLedgerTests.cs` — full ledger
- `LivingWorldCORE/npc/tests/FormidableA2Tests.cs` — A2 pure tests
- `LivingWorldCORE/npc/build.sh` — offline build and gates

Shared Formidable foundation:

- `LivingWorldCompanionSystems/FormidableAI/Kernel/`
- `LivingWorldCompanionSystems/FormidableAI/Classes/`
- `LivingWorldCompanionSystems/FormidableAI/Profile/`
- `LivingWorldCompanionSystems/FormidableAI/Integration/`
- `LivingWorldCORE/npc/src/ZFormidableShadowBehavior.cs` — read-only bridge in actual companions

Other major repository areas:

- `LivingWorldCORETooler/` — in-game development/test controls and raid launch support
- `LivingWorldEnemyFamilies/` — enemy-family content direction
- `FormidableNPCFramework/` — framework/package work
- `LivingWorldTelemetry/` — telemetry components
- `LivingVillageFoundation/`, `VillageReclaim/`, `PopVillageWorkers/` — later persistent settlement/village layer
- `compatibility/` — compatibility work, including RRR/Souls support
- `research/` — source research and comparative evidence
- `archive/` — historical material, not current authority

## 11. Current evidence that drives A2

Latest full-roster Tooler/Souls sessions showed:

- awareness envelope median about 69.46 m
- first evidence about 54.74 m
- noticed about 52.90 m
- committed about 50.94 m
- native target acquisition about 9.04 m
- native action about 7.89 m
- committed-to-native-target delay about 10.92 s median
- 10% of fighting samples had a hostile nearby but no target
- cohesion averaged 10.2 m, max 33.7 m, with 18% over 16 m
- 12 fallbacks: 6 clear, 6 caught
- defense was switched off, so no live defense verdict exists
- bomb throws were 9.2–19.8 m, median 13.6 m
- threat focus was 99–100%, no wildlife initiation, no walking away from an engaged nearby enemy
- no kernel/ownership faults

Therefore the true next work is initiative latency, target continuity, strike-aware defense, fallback quality, cohesion and existing-role weapon choices—not new-character implementation.

## 12. Cursor's assignment

Audit and repair the corrected A2 candidate. Do not redesign the roster.

1. Read the authority documents and both baseline/candidate histories.
2. Compare `1ec0f18..f2b8e1f` and then inspect every uncommitted correction in `work/formidable-a2-corrected`.
3. Find every identity alias, implicit fallback or saved-state write. This includes older shadow/profile code, not just the rejected A2 files.
4. Ensure Guard, Archer and Scout remain exactly those identities in runtime, telemetry and tests.
5. Keep future Warden, Huntress and other roles additive and uninstantiated; shared tactical functions are allowed without identity conversion.
6. Preserve useful identity-neutral A2 behavior.
7. Make all new execution switches off by default and prove switch-off behavior matches 1.0.32.29.
8. Add source-contract tests that fail on `lw_class`, `PairClasses`, `archer -> Huntress`, `guard -> Warden`, `scout -> Pathfinder/Warden`, default-on A2, or party-creation role rewriting.
9. Run the full build and every existing gate. Treat warnings as errors where the build supports it.
10. Produce an audit report listing each changed file, why it changed, exact test/gate results, remaining uncertainty and any live test that would eventually be needed.

## 13. Prohibited actions for Cursor

- Do not install anything into the r2modman profile.
- Do not launch Valheim.
- Do not alter saves, configs or companion ZDO state.
- Do not push, merge, rebase or rewrite history.
- Do not delete or move user files.
- Do not touch unrelated dirty/untracked files.
- Do not invent a new roster, class hierarchy, progression mapping or default equipment package.
- Do not ask Brian to perform manual console-command fixture sequences.
- Do not interpret discussion as implementation authority.

## 14. Definition of done for Cursor's pass

- The corrected worktree builds offline.
- Full regression, native-member, ownership, movement-writer, analyzer and combat-voice gates pass.
- No forbidden identity strings or persistence routes remain in active companion/profile execution.
- Guard, Archer and Scout identity tests pass.
- Switches are off by default and legacy-off parity is demonstrated.
- No live profile or game state changed.
- Cursor leaves a clear written report for Codex and Claude to review before any commit, push or install.

The owner wants a formidable tactical brain built carefully. The safeguard is not another clever abstraction; it is strict authority, explicit identity, evidence-driven scope and tests that make the earlier mistake impossible to repeat.
