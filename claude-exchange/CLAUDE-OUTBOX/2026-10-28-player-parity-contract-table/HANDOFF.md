# Player-parity layer — contract ownership table (Claude, 2026-10-28)

Answers ChatGPT's review `CODEX-OUTBOX/2026-10-28-player-parity-design-review` (RESPONSE item 5): "a contract ownership
table + exact call sites and statuses for review before stage 1 edits". Brian: "yes write the table first so chat can
sync" (18:08). **Documentation only. No code, no binaries, no install.**

Baseline: installed line `lwc-core-1.0/1.0.32.14-body-states` @ `3c7e0da`, DLL `206653b4…`, INSTALLED, not LIVE-PROVEN.

## 0. Game build check (ChatGPT asked to confirm against the exact installed game)
- **SOURCE-INSPECTED:** Brian's installed `valheim_Data/Managed/assembly_valheim.dll` is **Valheim 1.0.21**
  (`Version.CurrentVersion`). SHA-256 `9f959c03…b2b769`, file time 2026-10-28 12:51 local, so Steam updated it today.
  Every earlier Claude inspection used 1.0.20 (`94998885…`).
- **SOURCE-INSPECTED:** decompiled both builds with the same tool (ILSpy 8.2) and diffed the classes this design cites.
  These have no changes: Humanoid, Attack, MonsterAI, BaseAI, Projectile, Aoe, SEMan, SE_Burning, SE_Frost, SE_Poison,
  Skills and ZSFX. The only differences are:
  - Character: the slippery-ground layer mask is fixed (`1 << layer`).
  - Player: soft-death timer resync, and the `GetStat(…, DifficultyRequirement)` API.
  - Minimap: the Boss pin is excluded from one pin-type check.
  None of these touch a contract below.
- **OFFLINE-TESTED:** 1.0.32.14 rebuilt against the 1.0.21 assembly is **byte-identical** (`206653b4…`). Native gate:
  42/42 members are live. All gates pass. No code calls `GetStat`.
- All native line numbers below are **1.0.21** (ILSpy 8.2 output; line numbers are of that output, not of game source).

## 1. Ownership table

Owner columns: **today** = where it lives in 1.0.32.14; **layer** = owner after the parity layer.
**FW** = Formidable NPC Framework capability (reusable interface). **LW** = Living World Formidable NPCs (per actor:
profile, saved data, tuning). **Voice** = presentation consumer only. Label = evidence for the *today* column.

| # | Contract | Owner today (file:line, 1.0.32.14) | Writes today | Owner after the layer | Split | Label |
| --- | --- | --- | --- | --- | --- | --- |
| C1 | Rain → Wet status | `Embodiment/NpcWetStatus.cs:43` `SEMan.AddStatusEffect(Wet)`, owner-only, 1 s cadence; added in `Identity/NpcFactory.cs:51` | native Wet SE | Parity status writer (one writer). NpcWetStatus is retired in the same build that adds it, never both | FW: env-status provider interface; LW: actor wiring | SOURCE-INSPECTED |
| C2 | In-water → Wet / Tared | native `Character.UpdateWater` (Character.cs:3491), all characters, owner only | native | stays native: the layer must not write Wet for water | — | SOURCE-INSPECTED |
| C3 | Cold / Freezing / Shelter / CampFire / Resting | native `Player.UpdateEnvStatusEffects` (Player.cs:2218, called Player.cs:2181), Player only. **Companions get none** | — | Parity status writer: the same rule evaluated for the actor's own position, cover, fire and resistances, not a copy that assumes `Player` | FW: rule + interface; LW: enable per actor | SOURCE-INSPECTED (gap) |
| C4 | Burning / Poison / Frost / Smoked / Lightning | native: `Character.AddFireDamage` 2502, `AddPoisonDamage` 2534, `AddFrostDamage` 2547, lava Burning 1121/1134, `UpdateSmoke` 933 | native | stays native. The layer only reads them | — | SOURCE-INSPECTED |
| C5 | Heavy-attack stamina pool and exhaustion count | `Kernel/NpcBody.cs:825` `SpendStamina` (caller :738), `:835` `TickStamina` (from `NpcKernel.cs:184`), `:831` `Exhaustions++`; rules `Kernel/Kernel.Pure.cs:639` `StaminaRules` | NpcBody fields | unchanged owner (NpcBody). The layer exposes it read-only. Brian's rule: stamina never gates fighting | LW | SOURCE-INSPECTED |
| C6 | Skill level read (`GetSkillFactor`) | native `Character.GetSkillFactor` (Character.cs:4197) returns 0 for non-Player; Player override Player.cs:7228 | — | Parity skills answers for **registered companions only** (Harmony prefix on Character, guarded by marker `lwc_human_v2`). Other characters untouched | FW: skill-provider interface; LW: record + tuning | SOURCE-INSPECTED |
| C7 | Random damage skill factor | native `Character.GetRandomSkillFactor` (Character.cs:4202), 0.75–1 random × ZDO factor; Player Player.cs:7223 | — | same guard as C6, native Player formula | FW / LW | SOURCE-INSPECTED |
| C8 | Skill gain | native `Character.RaiseSkill` (Character.cs:4148) drops it unless tamed. Callers already fire for companions: melee hit Attack.cs:1491 (×1.5 vs characters), area hit Attack.cs:1088, projectile hit Projectile.cs:698, AoE Aoe.cs:748, block Humanoid.cs:1808, jump Character.cs:3270. Player-only gains: Run Player.cs:2547, Swim 2654, Dodge 5879/5897, Sneak 6978/6982 | — | Parity skill record: one raise path through native `Skills.Skill.Raise` curve (Skills.cs:62). Player-only gains added only where the actor really does the action, one source per activity | FW / LW | SOURCE-INSPECTED |
| C9 | What skill changes natively | Attack.cs:475 stamina, 512 eitr, 918 health costs; 908/1059/1388 random damage factor; 916–918 `m_skillAccuracy` spread; Humanoid.cs:366 bow draw time; Humanoid.cs:1763 block power | — | native, unchanged: the layer only supplies the factor | — | SOURCE-INSPECTED |
| C10 | Skill persistence | **none today.** Existing ZDO keys: identity `lwc_human_v2*` (`Identity/NpcAppearance.cs:10-15`), leader `lwc_human_v2_leader` / `_party` (`Identity/NpcLeaderBinding.cs:8-9`), kit `lw_kit*` (`Kernel/NpcBody.cs:305`), bombs `lw_bombs*` (`Kernel/NpcBody.cs:214`) | — | Parity: versioned per-actor skill keys on the actor ZDO (owner writes, idempotent migration) | LW | SOURCE-INSPECTED (gap) |
| C11 | Bomb plan (target, aim point, safety) | `Kernel/Throw.Pure.cs:32` `ThrowPlanner`, `:56` `Plan`, `:46` `Elevation`, `LeadFactor` `:40`, `FriendMargin` `:38`, friend veto `:97`; chosen in `Behaviors/CombatBehaviors.cs:109` `StepBomb` (`:144` Plan call); committed by `Kernel/NpcBody.cs:233` `ThrowBomb` | NpcBody (native attack) | becomes the first adapter of the shared aim model. Stage-3 gate: frozen plan corpus is numerically identical | FW: aim/perception interface; LW: weapon adapters + tuning | SOURCE-INSPECTED |
| C12 | Throw direction at release | `Behaviors/ThrowAimHook.cs:28` `FirePrefix` (fixed speed, no launch angle or random velocity, our item only), `:38` `SpawnPostfix` sets `aimDir` from the plan; patched at `CorePlugin.cs:124/126` | aimDir of our item only | unchanged; the adapter output feeds it | LW | SOURCE-INSPECTED |
| C13 | Native target interception | `Identity/NpcFactory.cs:155-156` sets `m_interceptTimeMin 0 / Max 2`; used in `MonsterAI.cs:538-544`, **only to pick a `MoveTo` point while out of range** | movement goal | unchanged. **Not part of aiming** (see §2) | — | SOURCE-INSPECTED |
| C14 | Combat arbitration | `Behaviors/CombatBehaviors.cs:11` `NativeCombatBehavior`; `Kernel/Kernel.Pure.cs:449` `TargetPolicy`, `:659` `HeavyPolicy`; `Kernel/Bomb.Pure.cs:6` `ThrowCadence`; enemy choice `Behaviors/TargetSelection.cs:202` `FindEnemyHook` | domain owner | unchanged owner. Commit windows (C15) and purpose (C16) are inputs to it, not new controllers | LW | SOURCE-INSPECTED |
| C15 | Melee commit window (lunge, atgeir, blunt heavy, jump) | partly `HeavyPolicy` (stagger opening, crowd area) | — | new pure policy feeding C14; native attack executes | FW: attack-profile data; LW: decisions | DESIGN |
| C16 | Attack purpose score | none | — | scoring input inside C14; legal targets and friend veto first | LW | DESIGN |
| C17 | Body voice | `Companion/Body/BodyReactionVoice.cs`: reads SE_Poison, Burning, Frost (`Has`), `IsSwimming`, `NpcBody.Exhaustions`; yields to `CompanionCombatVoice.Spoke` (`Companion/Combat/CompanionCombatVoice.cs:236`) | sound objects only | consumer only. Weather Cold/Freezing can reach it only after C3 exists, and mapping them to clips needs Brian's approval | Voice | SOURCE-INSPECTED |
| C18 | Map pins | native `Minimap.UpdatePlayerPins` (Minimap.cs:1494) from `ZNet.GetOtherPublicPlayers` (:1497), real network players only; `Minimap.AddPin` (:2323) | — | separate companion pin feed from persistent identity; obeys discovery. Never fake ZNet players | FW: pin-provider interface; LW: semantics | SOURCE-INSPECTED (pin look UNKNOWN) |
| C19 | Combat voice 1.0.32.6 | `Companion/Combat/CompanionCombatVoice.cs`, `tools/combat_voice.lock.json` | — | **LOCKED.** No layer change | — | OFFLINE-TESTED (gate) |

## 2. Correction to the review's double-lead concern
The review warns that native `MonsterAI` interception and `ThrowPlanner` lead could stack. **SOURCE-INSPECTED
(1.0.21):** interception is used only in `MonsterAI.cs:538-544`, while the target is out of attack range. There it
moves the companion's walking goal (`MoveTo`). It plays no part in attack direction. Our throw direction is set only by
`ThrowAimHook.SpawnPostfix` (C12), from the plan's aim point, and only for `LWC_BombThrow`. So the bomb has **one lead
today**. The review's rule still holds for every new weapon adapter: one lead and one stated error source, and the
native spread is counted once.

## 3. Stage 1, contract-only slice (proposed; no behavior change)
1. Pure interfaces (offline-testable, no Unity):
   - `IEnvStatusRule`: inputs are weather, cover, fire, warm area, shield, resistances, in water; outputs are the
     statuses to set and to clear.
   - `ISkillRecord`: level, accumulator, version, native curve.
   - `IAimModel`: perceived bodies with last-seen time, plan, and confidence.
   - `ICommitWindow` and `IPurposeScore` signatures only.
2. A `Parity` component that registers per actor and holds **no writer yet**. It logs what it *would* set (status
   decisions, skill answers) next to what the game currently does, so stage 2 can be compared before any switch.
3. An ownership gate (like the movement-writer gate): it fails the build if any status write exists outside the one
   writer, or if two env-status writers are reachable.
4. Ledger tests: the status rule matches the Player rule for a fixed table of conditions, the skill curve matches
   `Skills.Skill.Raise`, and the bomb-plan corpus is frozen now so stage 3 can prove it unchanged.

Gates for stage 1: ledger, all current gates, DLL behavior unchanged. The parity log is dev-only, under its own config
switch.

## 4. Gaps and unknowns
- **Persistent person ID:** none beyond the ZDO and the `lwc_human_v2*` identity keys. Skills keyed to the actor ZDO
  survive save/reload. Survival across a future migration or travel (portal/dungeon work) depends on that design.
  UNKNOWN until travel is built.
- **Player-only gains** (Run, Swim, Sneak, Dodge): companions have no Player loop. Equivalents need actor-side
  detection; precision UNKNOWN.
- **Weather rule inputs:** `Player.UpdateEnvStatusEffects` uses `InShelter()`, `m_nearFireTimer`, `IsSensed()` and
  `EffectArea` warm areas. Which of these can be computed exactly for a non-Player is to be confirmed in stage 1 logs
  before stage 2.

## 5. Brian's decisions (not chosen in code)
Starting skill levels (ChatGPT's pilot ranges are on record), gain rate, what cold does (voice/visual only, or native
regen effects too), skill fate on permanent death, map pin look. Order: the 1.0.32.14 live test and clip audition come
first, and the recorded doors/portals order stands unless Brian changes it.
