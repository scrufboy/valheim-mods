# Changed files: encounter/combat wave 2

Each path below is part of this submission. Build outputs, historical source copies, caches, and the active game profile are excluded.

## LivingWorldCORE

- `LivingWorldCORE/COMPANION-ENCOUNTER-RESOLUTION-ARC.md` — Defines the owner-approved encounter arc and invariants.

## CampLifecycle

- `LivingWorldCompanionSystems/CampLifecycle/ADAPTER-HANDOFF.md` — Adds cautious camp, hunt, and patrol observations while preserving lifecycle rules.
- `LivingWorldCompanionSystems/CampLifecycle/CampLifecyclePolicy.cs` — Adds cautious camp, hunt, and patrol observations while preserving lifecycle rules.
- `LivingWorldCompanionSystems/CampLifecycle/CampLifecyclePolicyTests.cs` — Adds cautious camp, hunt, and patrol observations while preserving lifecycle rules.

## Discovery

- `LivingWorldCompanionSystems/Discovery/ADAPTER-HANDOFF-1.0.x.md` — Adds encounter scene selection and stable local-site knowledge.
- `LivingWorldCompanionSystems/Discovery/EncounterScenePolicy.cs` — Adds encounter scene selection and stable local-site knowledge.
- `LivingWorldCompanionSystems/Discovery/EncounterScenePolicyTests.cs` — Adds encounter scene selection and stable local-site knowledge.
- `LivingWorldCompanionSystems/Discovery/SiteKnowledgePolicy.cs` — Adds encounter scene selection and stable local-site knowledge.
- `LivingWorldCompanionSystems/Discovery/Test-Discovery.ps1` — Adds encounter scene selection and stable local-site knowledge.

## EncounterActivation

- `LivingWorldCompanionSystems/EncounterActivation/ADAPTER-HANDOFF-1.0.x.md` — Claims one site event or spawn after accepted aid and verifies its result.
- `LivingWorldCompanionSystems/EncounterActivation/EncounterActivationDomain.cs` — Claims one site event or spawn after accepted aid and verifies its result.
- `LivingWorldCompanionSystems/EncounterActivation/EncounterActivationPolicy.cs` — Claims one site event or spawn after accepted aid and verifies its result.
- `LivingWorldCompanionSystems/EncounterActivation/EncounterActivationPolicyTests.cs` — Claims one site event or spawn after accepted aid and verifies its result.
- `LivingWorldCompanionSystems/EncounterActivation/RuntimeAdapterContracts.cs` — Claims one site event or spawn after accepted aid and verifies its result.
- `LivingWorldCompanionSystems/EncounterActivation/Test-EncounterActivation.ps1` — Claims one site event or spawn after accepted aid and verifies its result.

## EncounterAllegiance

- `LivingWorldCompanionSystems/EncounterAllegiance/ADAPTER-HANDOFF-1.0.x.md` — Models uncertain first contact, faction relationships, warnings, and hostility.
- `LivingWorldCompanionSystems/EncounterAllegiance/AllegianceMatrix.cs` — Models uncertain first contact, faction relationships, warnings, and hostility.
- `LivingWorldCompanionSystems/EncounterAllegiance/EncounterAllegianceDomain.cs` — Models uncertain first contact, faction relationships, warnings, and hostility.
- `LivingWorldCompanionSystems/EncounterAllegiance/EncounterAllegiancePersistence.cs` — Models uncertain first contact, faction relationships, warnings, and hostility.
- `LivingWorldCompanionSystems/EncounterAllegiance/EncounterAllegiancePolicy.cs` — Models uncertain first contact, faction relationships, warnings, and hostility.
- `LivingWorldCompanionSystems/EncounterAllegiance/README.md` — Models uncertain first contact, faction relationships, warnings, and hostility.
- `LivingWorldCompanionSystems/EncounterAllegiance/RuntimeAdapterContracts.cs` — Models uncertain first contact, faction relationships, warnings, and hostility.
- `LivingWorldCompanionSystems/EncounterAllegiance/tests/EncounterAllegiancePolicyTests.cs` — Models uncertain first contact, faction relationships, warnings, and hostility.
- `LivingWorldCompanionSystems/EncounterAllegiance/tests/Run-Tests.ps1` — Models uncertain first contact, faction relationships, warnings, and hostility.

## EncounterLoadouts

- `LivingWorldCompanionSystems/EncounterLoadouts/ADAPTER-HANDOFF-1.0.x.md` — Defines same-person equipped variants and legal gear progression.
- `LivingWorldCompanionSystems/EncounterLoadouts/EncounterLoadoutDomain.cs` — Defines same-person equipped variants and legal gear progression.
- `LivingWorldCompanionSystems/EncounterLoadouts/EncounterLoadoutPersistence.cs` — Defines same-person equipped variants and legal gear progression.
- `LivingWorldCompanionSystems/EncounterLoadouts/EncounterLoadoutPolicy.cs` — Defines same-person equipped variants and legal gear progression.
- `LivingWorldCompanionSystems/EncounterLoadouts/EncounterLoadoutPolicyTests.cs` — Defines same-person equipped variants and legal gear progression.
- `LivingWorldCompanionSystems/EncounterLoadouts/README.md` — Defines same-person equipped variants and legal gear progression.
- `LivingWorldCompanionSystems/EncounterLoadouts/Run-Tests.ps1` — Defines same-person equipped variants and legal gear progression.
- `LivingWorldCompanionSystems/EncounterLoadouts/SplitAshProfiles.cs` — Defines same-person equipped variants and legal gear progression.

## EncounterLore

- `LivingWorldCompanionSystems/EncounterLore/WILDERNESS-COMPANION-ENCOUNTER-ARC.md` — Adds varied encounter dialogue and a structured lore contract.
- `LivingWorldCompanionSystems/EncounterLore/encounter-arc-contract.v1.json` — Adds varied encounter dialogue and a structured lore contract.

## EncounterVoice

- `LivingWorldCompanionSystems/EncounterVoice/ADAPTER-HANDOFF-1.0.x.md` — Binds dialogue to candidate voice cues and tests safe presentation.
- `LivingWorldCompanionSystems/EncounterVoice/EncounterCuePresentationPolicyTests.cs` — Binds dialogue to candidate voice cues and tests safe presentation.
- `LivingWorldCompanionSystems/EncounterVoice/REVIEW-NOTE.md` — Binds dialogue to candidate voice cues and tests safe presentation.
- `LivingWorldCompanionSystems/EncounterVoice/encounter-voice-candidates.v1.json` — Binds dialogue to candidate voice cues and tests safe presentation.

## FriendInNeed

- `LivingWorldCompanionSystems/FriendInNeed/ADAPTER-HANDOFF-1.0.x.md` — Defines accepted-aid alert and native-combat handoff conditions.
- `LivingWorldCompanionSystems/FriendInNeed/FriendInNeedContract.cs` — Defines accepted-aid alert and native-combat handoff conditions.
- `LivingWorldCompanionSystems/FriendInNeed/FriendInNeedContractTests.cs` — Defines accepted-aid alert and native-combat handoff conditions.
- `LivingWorldCompanionSystems/FriendInNeed/README.md` — Defines accepted-aid alert and native-combat handoff conditions.

## LiveCoreAdapter

- `LivingWorldCompanionSystems/LiveCoreAdapter/.gitignore` — Adds a 1.0.x compile-in encounter seam, source checks, capability matrix, and build-gate proof.
- `LivingWorldCompanionSystems/LiveCoreAdapter/CAPABILITY-MATRIX-1.0.32.7.md` — Adds a 1.0.x compile-in encounter seam, source checks, capability matrix, and build-gate proof.
- `LivingWorldCompanionSystems/LiveCoreAdapter/Compile-Against-Core-1.0.32.8.ps1` — Adds a 1.0.x compile-in encounter seam, source checks, capability matrix, and build-gate proof.
- `LivingWorldCompanionSystems/LiveCoreAdapter/CoreEncounterAdapter.cs` — Adds a 1.0.x compile-in encounter seam, source checks, capability matrix, and build-gate proof.
- `LivingWorldCompanionSystems/LiveCoreAdapter/EncounterAdapterPolicy.cs` — Adds a 1.0.x compile-in encounter seam, source checks, capability matrix, and build-gate proof.
- `LivingWorldCompanionSystems/LiveCoreAdapter/EncounterAdapterPolicyTests.cs` — Adds a 1.0.x compile-in encounter seam, source checks, capability matrix, and build-gate proof.
- `LivingWorldCompanionSystems/LiveCoreAdapter/GateCompatibility/.gitignore` — Proposes token-independent gate comparison with positive and deliberate-mutation tests.
- `LivingWorldCompanionSystems/LiveCoreAdapter/GateCompatibility/README.md` — Proposes token-independent gate comparison with positive and deliberate-mutation tests.
- `LivingWorldCompanionSystems/LiveCoreAdapter/GateCompatibility/semantic_gate_compare.py` — Proposes token-independent gate comparison with positive and deliberate-mutation tests.
- `LivingWorldCompanionSystems/LiveCoreAdapter/GateCompatibility/semantic_il.py` — Proposes token-independent gate comparison with positive and deliberate-mutation tests.
- `LivingWorldCompanionSystems/LiveCoreAdapter/GateCompatibility/test_semantic_gate.py` — Proposes token-independent gate comparison with positive and deliberate-mutation tests.
- `LivingWorldCompanionSystems/LiveCoreAdapter/README.md` — Adds a 1.0.x compile-in encounter seam, source checks, capability matrix, and build-gate proof.
- `LivingWorldCompanionSystems/LiveCoreAdapter/Run-Tests.ps1` — Adds a 1.0.x compile-in encounter seam, source checks, capability matrix, and build-gate proof.
- `LivingWorldCompanionSystems/LiveCoreAdapter/Verify-Compiled-Gates-1.0.32.8.ps1` — Adds a 1.0.x compile-in encounter seam, source checks, capability matrix, and build-gate proof.
- `LivingWorldCompanionSystems/LiveCoreAdapter/Verify-Core-Signatures.ps1` — Adds a 1.0.x compile-in encounter seam, source checks, capability matrix, and build-gate proof.

## NeedsAndAffinity

- `LivingWorldCompanionSystems/NeedsAndAffinity/ADAPTER-HANDOFF-1.0.x.md` — Records aid agreement, witnessed contribution, trust, and invitation eligibility.
- `LivingWorldCompanionSystems/NeedsAndAffinity/NeedsAndAffinityDomain.cs` — Records aid agreement, witnessed contribution, trust, and invitation eligibility.
- `LivingWorldCompanionSystems/NeedsAndAffinity/NeedsAndAffinityPolicy.cs` — Records aid agreement, witnessed contribution, trust, and invitation eligibility.
- `LivingWorldCompanionSystems/NeedsAndAffinity/NeedsPersistence.cs` — Records aid agreement, witnessed contribution, trust, and invitation eligibility.
- `LivingWorldCompanionSystems/NeedsAndAffinity/README.md` — Records aid agreement, witnessed contribution, trust, and invitation eligibility.
- `LivingWorldCompanionSystems/NeedsAndAffinity/tests/NeedsAndAffinityPolicyTests.cs` — Records aid agreement, witnessed contribution, trust, and invitation eligibility.

## QuestBroker

- `LivingWorldCompanionSystems/QuestBroker/ADAPTER-HANDOFF-1.0.x.md` — Keeps accepted request, site arrival, completion, and reward claims durable.
- `LivingWorldCompanionSystems/QuestBroker/QuestBrokerDomain.cs` — Keeps accepted request, site arrival, completion, and reward claims durable.
- `LivingWorldCompanionSystems/QuestBroker/QuestBrokerPersistence.cs` — Keeps accepted request, site arrival, completion, and reward claims durable.
- `LivingWorldCompanionSystems/QuestBroker/QuestBrokerPolicy.cs` — Keeps accepted request, site arrival, completion, and reward claims durable.
- `LivingWorldCompanionSystems/QuestBroker/README.md` — Keeps accepted request, site arrival, completion, and reward claims durable.
- `LivingWorldCompanionSystems/QuestBroker/RuntimeAdapterContracts.cs` — Keeps accepted request, site arrival, completion, and reward claims durable.
- `LivingWorldCompanionSystems/QuestBroker/tests/QuestBrokerPolicyTests.cs` — Keeps accepted request, site arrival, completion, and reward claims durable.

## README.md

- `LivingWorldCompanionSystems/README.md` — Indexes the new encounter modules and their ownership.

## RescueRelease

- `LivingWorldCompanionSystems/RescueRelease/README.md` — Tracks temporary travel, safe return, and a separate final party offer.
- `LivingWorldCompanionSystems/RescueRelease/RescueReleaseDomain.cs` — Tracks temporary travel, safe return, and a separate final party offer.
- `LivingWorldCompanionSystems/RescueRelease/RescueReleasePolicy.cs` — Tracks temporary travel, safe return, and a separate final party offer.
- `LivingWorldCompanionSystems/RescueRelease/RuntimeAdapterContracts.cs` — Tracks temporary travel, safe return, and a separate final party offer.
- `LivingWorldCompanionSystems/RescueRelease/tests/RescueReleasePolicyTests.cs` — Tracks temporary travel, safe return, and a separate final party offer.

## RuntimeIntegration

- `LivingWorldCompanionSystems/RuntimeIntegration/EncounterArcRuntime.cs` — Composes module ledgers into one offline encounter journey and records port seams.
- `LivingWorldCompanionSystems/RuntimeIntegration/INTEGRATION-CONTRACT-1.0.x.md` — Composes module ledgers into one offline encounter journey and records port seams.
- `LivingWorldCompanionSystems/RuntimeIntegration/README.md` — Composes module ledgers into one offline encounter journey and records port seams.
- `LivingWorldCompanionSystems/RuntimeIntegration/Run-Tests.ps1` — Composes module ledgers into one offline encounter journey and records port seams.
- `LivingWorldCompanionSystems/RuntimeIntegration/RuntimeIntegrationTests.cs` — Composes module ledgers into one offline encounter journey and records port seams.
- `LivingWorldCompanionSystems/RuntimeIntegration/SOURCE-AND-PORT-READINESS-2026-10-28.md` — Composes module ledgers into one offline encounter journey and records port seams.

## SpecialistCombat

- `LivingWorldCompanionSystems/SpecialistCombat/MeleeRoles/ADAPTER-HANDOFF-1.0.x.md` — Bounds knife lunge and atgeir role requests before native attack commit.
- `LivingWorldCompanionSystems/SpecialistCombat/MeleeRoles/MeleeRolePolicy.cs` — Bounds knife lunge and atgeir role requests before native attack commit.
- `LivingWorldCompanionSystems/SpecialistCombat/MeleeRoles/MeleeRolePolicyTests.cs` — Bounds knife lunge and atgeir role requests before native attack commit.
- `LivingWorldCompanionSystems/SpecialistCombat/MeleeRoles/MeleeRoleProfiles.cs` — Bounds knife lunge and atgeir role requests before native attack commit.
- `LivingWorldCompanionSystems/SpecialistCombat/MeleeRoles/Test-MeleeRoles.ps1` — Bounds knife lunge and atgeir role requests before native attack commit.
- `LivingWorldCompanionSystems/SpecialistCombat/PredictionAndThrows/PredictionAndThrowsPolicy.cs` — Bounds spear and bomb attack requests with trajectory and ally-safety checks.
- `LivingWorldCompanionSystems/SpecialistCombat/PredictionAndThrows/PredictionAndThrowsTests.cs` — Bounds spear and bomb attack requests with trajectory and ally-safety checks.
- `LivingWorldCompanionSystems/SpecialistCombat/PredictionAndThrows/README.md` — Bounds spear and bomb attack requests with trajectory and ally-safety checks.
- `LivingWorldCompanionSystems/SpecialistCombat/PredictionAndThrows/Run-Tests.ps1` — Bounds spear and bomb attack requests with trajectory and ally-safety checks.

## claude-exchange

- `claude-exchange/CODEX-OUTBOX/2026-10-28-encounter-combat-wave2/HANDOFF.md` — Transfers exact request, implementation scope, verification, and remaining work to Claude.
- `claude-exchange/CODEX-OUTBOX/2026-10-28-encounter-combat-wave2/REQUEST.md` — Transfers exact request, implementation scope, verification, and remaining work to Claude.
- `claude-exchange/CODEX-OUTBOX/2026-10-28-encounter-combat-wave2/VERIFICATION.md` — Transfers exact request, implementation scope, verification, and remaining work to Claude.

## research

- `research/CompanionRespectQuests/COMPANION-RESPECT-QUESTS-RESEARCH.md` — Adds source-linked quest and compatibility research.

## This manifest

- `claude-exchange/CODEX-OUTBOX/2026-10-28-encounter-combat-wave2/CHANGED-FILES.md` — Enumerates every changed path and its purpose.
