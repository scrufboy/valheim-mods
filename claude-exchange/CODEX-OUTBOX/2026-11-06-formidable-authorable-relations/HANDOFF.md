# Formidable authorable faction/relation seam

Date: 2026-11-06. Scope: engine-free policy and adapter contract only. No game
launch, live profile installation, or current Formidable raid-config edit was
performed.

## Implemented

- `LivingWorldCompanionSystems/EncounterAllegiance/AuthorableRelations.cs`
  adds string faction IDs, exact prefab bindings, explicit native compatibility
  adapters, directed relationship rules, validation, and deterministic lookup.
- Managed identity resolution is persistent authored profile → exact prefab.
  This preserves `RRRN_Pillager` as `LWC.Pillagers`
  even though installed WorldSpawner evidence says `SetFaction=Undead`.
- Native buckets never become authored identity. The supplied compatibility
  blueprint adds one symmetric `LWC.Pillagers`↔native-`Undead` selector.
  Undead↔Undead and unknown pairs return no override and fall through to vanilla.
- `RRRN_OrcBrute` and `Krauguk` exact bindings preserve `LWC.Eitrherjar` before
  any `PlainsMonsters` compatibility filtering.
- Exact intended IDs are `RRRN_Pillager` → `LWC.Pillagers` and
  `RRRN_OrcBrute`/`Krauguk` → `LWC.Eitrherjar`.
- Candidate config contract (not installed and not yet runtime-consumed):
  `LivingWorldCompanionSystems/EncounterAllegiance/formidable-relations.lwc.candidate.json`.
  The compatibility-config agent can target that schema/path without editing the
  current Formidable supplemental raid configuration.
- The model is open to independent mod faction IDs and asymmetric/directed
  relations; it does not require extending the Living World `Allegiance` enum.

## Verification

`EncounterAllegiance/tests/Run-Tests.ps1` compiles the pure C# 5 policy with
warnings as errors. Tests cover exact managed identity despite stale native
faction, Pillager/Draugr hostility in both argument orders, unchanged Undead
self-relation, Eitrherjar identity, explicit vanilla fallback, and freely
authored directed relationships.

## Runtime work still required

Formidable must load and synchronize the validated registry on the server and
consult it for both argument orders before native same-faction rejection. It should
log the winning identity source and relation rule for diagnostics. No claim is
made that the current installed DLL already exposes that hook or that this pure
policy is playable. A false/no-override result must preserve native tame, player,
group, and faction rules unchanged.

Candidate runtime contract: resolve `LWC.*` managed identity from the persistent
profile even when a saved/native ZDO faction is stale; otherwise use the exact
prefab compatibility binding. Call `TryGetOverride(observer, target)` before the
native same-faction early return. If it returns true, use that disposition for
target eligibility. If false, execute the original vanilla path without mutation.

The exact installed config inventory remains owned by the parallel variation
audit. The Behemoth rage-roll work is unrelated and remains owned by root.

## Reviewed compatibility-config half

The variation audit completed the complementary, non-installed candidate at:

- `valheim-mods-formidable-current/compatibility/LivingWorldFactionCorrection/`
- staged output:
  `valheim-mods-formidable-current/build/faction-correction-20261106-final/`

REVIEWED/OFFLINE-TESTED on 2026-11-06:

- Natural `SetFaction` is removed only from `WorldSpawner.9004`
  (`RRRN_OrcBrute`) and `.9005` (`RRRN_Pillager`).
- Event-wide `Faction=PlainsMonsters` and the inherited target-slot overrides are
  removed from the two mixed Souls raids.
- The four unrelated mixed-raid slots explicitly retain their previous effective
  `PlainsMonsters` value.
- `LWC_UndeadColossus_Test` retains explicit `Faction=Undead`.
- No arbitrary `LWC.*` value is written through RRR's enum parser. Therefore this
  config candidate is intentionally inert/incomplete without the managed relation
  runtime described above.
- Staged SHA-256 values match `candidate-report.json`:
  - natural spawner candidate:
    `eaa7cc22412a37444f1e6dbd13e3b108cd661a1a38b4fc528e70286a9929366d`
  - mixed raid candidate:
    `56484b17c9add9b6822f1bbb27f9f4a114ff39e632c0c98b28c8643dc4c3c8f9`

Together, the complete candidate is: stage those narrowly corrected configs only
when a maintained LWC runtime implements managed identity and calls
`TryGetOverride` before native same-faction rejection in both argument orders.
Unknown pairs must fall through to vanilla, and tame/player/group protections must
remain ahead of or otherwise authoritative over hostile target selection.

No live profile installation, DLL mutation, or game launch was performed during
this review.

## Runtime half completed

OFFLINE-TESTED: the isolated BepInEx/Jotunn/Harmony runtime is now implemented in
`valheim-mods-formidable-current/compatibility/LivingWorldFactionRuntime/` and
compiled against the real installed Valheim/BepInEx/Harmony/Jotunn assemblies.
The complete non-installed package, including this reviewed config correction,
is:

`C:\Users\scruf\Documents\LWC Model Skin Studio\Exports\LWC-Faction-Relations-0.1.0-Candidate04`

DLL SHA-256:
`b251e2d5a561aa48f7127e7b9e0468896bfc9ac8efe6324f8d2138c5e238b45c`

The runtime uses a highest-priority tri-state prefix on the exact static
`BaseAI.IsEnemy(Character, Character)` overload. Explicit rules override before
native same-faction rejection in both argument orders. Self, player, tamed,
same-group, and unknown pairs remain vanilla. Managed identity is persisted by
the authoritative owner under ZDO key `lwc_managed_faction_v1` and wins over a
stale native faction. Eitrherjar also has an explicit symmetric relation to
native `PlainsMonsters`, preventing inherited Fuling non-hostility.

Candidate04 authority correction: server-synchronized registry values are
evaluated deterministically on every peer so client-owned MonsterAI receives the
same relations. Managed ZDO writes remain owner-only.

OFFLINE-TESTED: runtime registry tests pass 13/13; engine-free policy remains
55/55. INSTALLED: no. LIVE-PROVEN: no.

Next actual gameplay check after separately authorized installation: verify
Pillager↔Draugr and Eitrherjar↔Fuling acquisition in both argument orders,
protected self/tamed/player/same-group behavior, unknown-pair vanilla parity,
multiplayer synchronized configuration, and managed identity persistence across
save/restart/reload.
