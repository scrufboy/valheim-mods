# Live core adapter — first 1.0.x vertical slice

Baseline: Living World C.O.R.E. `1.0.32.7`, commit `9933763978fd7c29bfa9909e7ea8b6c47f86e421`.
Nothing in this folder is installed or deployed.

## What is implemented

- strict stable `PersonId`, `EncounterId`, `SiteId`, `CampId`, and `IncidentId` validation;
- an engine-free process claim that refuses two loaded actors for one person;
- compile-in core source for server-only creation after enumerating all maintained companion ZDO records; creation rejects every prefab outside that exact scanned allow-list;
- all five IDs and a schema written to the owned ZDO immediately after native prefab validation;
- unload/removal releases only the in-memory claim, never durable identity;
- witnessed first-contact reduction requiring server, owner, loaded actor, local witness, and material danger;
- native combat handoff is observation-only from `NpcKernel.Frame.Phase`; the adapter never writes AI target/combat state.

`CoreEncounterAdapter.cs` and `EncounterAdapterPolicy.cs` are intended to compile into the core assembly. Add an owned `CoreEncounterAdapter` field to `CorePlugin`, construct it in `Awake`, dispose it in `OnDestroy`, and drive `CreateOnce` through `StartCoroutine`. The caller remains responsible for persisting the encounter aggregate before creation and for consuming the returned contact directive through Friend in Need. This slice deliberately does not invent that missing aggregate CAS store.

## Proof and limitations

Run `Run-Tests.ps1` for the engine-free policy/claim tests and `Verify-Core-Signatures.ps1` for exact source seams at the pinned commit. The latter reads Git objects only and creates no checkout.

`Compile-Against-Core-1.0.32.8.ps1` is a separate, stronger compatibility check. It compiles the read-only checkout at `33addad` (1.0.32.8) plus the adapter into this folder's `build-1.0.32.8` directory, using the same game/Jotunn reference set as the core build. Passing that check does not replace the pinned 1.0.32.7 signature gate and is not live-game proof.

The combined 1.0.32.8 compilation succeeds. Its SHA-256 after the allow-list and exactly-once callback fixes was `B353CBF5DF58F8874C77FEF762BC118638AE9B940FBB87EBDC99154A3BF77D84`. The compiler reports one pre-existing core warning in `NpcBody.cs` for obsolete `Rigidbody.velocity`; adapter code emits no warning. Engine-free regression coverage rejects an unscanned prefab and proves that a throwing completion consumer is invoked once, with its exception propagated rather than retried.

`Verify-Compiled-Gates-1.0.32.8.ps1` records the remaining integration blocker. The ownership gate passes (61 native write sites, all in the approved core owners). The combat-voice and movement gates fail because their locks hash compiled method bodies containing metadata tokens. A clean compilation of approved 1.0.32.7 source fails those same legacy locks, so the failure reflects compiler/metadata-layout dependence rather than a demonstrated adapter change. The observed failures cover all 53 voice-locked methods and `TraversalBehavior.Wants`, while the ownership scan finds no new native writer. `GateCompatibility/` supplies a semantic comparison proposal with positive and deliberate-mutation tests; the maintained gates and trusted lock still need a reviewed update before deployment.

The source file is a compile-ready handoff, but this workspace does not place it into the existing clone or active profile. Dedicated-server behavior, ZDO iteration under concurrent zone load, peer replication, reconnect behavior, and all game animations remain unproven. Creation is intentionally limited to the three maintained core prefab identities; adding another prefab requires a migration-safe enumeration rule.
