# Living World faction relations runtime candidate

Date: 2026-11-06

Execution authority relayed by the parent Workshop chat: “Please finish the
runtime half now, not only the pure policy/handoff.” Scope was explicitly limited
to a new isolated compatibility plug-in, offline build, and candidate export.

## Result

- SOURCE-INSPECTED: Real Valheim `BaseAI.IsEnemy(Character, Character)` checks
  same non-empty group before its native same-faction return. The candidate uses
  a Harmony `Priority.First` prefix on that exact static overload.
- OFFLINE-TESTED: Self, player, tamed, and same explicit group pairs always run
  vanilla. Unknown pairs also run vanilla. Only a validated Friendly/Hostile rule
  sets `__result` and skips the original method.
- OFFLINE-TESTED: Jotunn `Config.BindConfig(..., synchronized: true)` owns the
  three server-synchronized settings. Invalid updates are rejected and retain the
  last valid immutable registry.
- SOURCE-INSPECTED: On authoritative owned `Character.Start`, exact managed
  identity is persisted in ZDO key `lwc_managed_faction_v1`. Resolution prefers a
  recognized persistent ID, then stable `ZDO.GetPrefab()` lookup, then an object
  name fallback. Native faction is only an explicit relation selector.
- OFFLINE-TESTED: `RRRN_Pillager` maps to `LWC.Pillagers` despite stale
  `Undead`; `RRRN_OrcBrute`/`Krauguk` map to `LWC.Eitrherjar` despite stale
  `PlainsMonsters`. Pillager↔Undead, Pillager↔Eitrherjar,
  Eitrherjar↔Undead, and Eitrherjar↔PlainsMonsters are symmetric hostile rules.

## Candidate

`C:\Users\scruf\Documents\LWC Model Skin Studio\Exports\LWC-Faction-Relations-0.1.0-Candidate04`

DLL SHA-256:
`b251e2d5a561aa48f7127e7b9e0468896bfc9ac8efe6324f8d2138c5e238b45c`

The package contains the DLL, readable source, registry candidate, test source
and result, reference/file hash manifest, and the reviewed narrow Spawn That /
Custom Raids compatibility-config candidate.

## Boundary

- INSTALLED: no.
- LIVE-PROVEN: no.
- Game launched: no.
- UNKNOWN: actual Harmony interoperability with every installed hostility patch,
  multiplayer config propagation, ZDO identity persistence across save/reload,
  and observed Pillager/Draugr/Eitrherjar/Fuling targeting.
- Candidate01 through Candidate03 were intermediate local exports. Candidate04 is the
  only complete reviewed package.

Candidate04 corrects peer authority: the registry remains authored and
synchronized by the server, but deterministic `IsEnemy` resolution executes on
every peer because a nonserver client may own MonsterAI. Managed ZDO identity
writes remain restricted to the current valid ZNetView owner.

## Next actual gameplay check

After a separate explicit install authorization, use a disposable server-owned
world/profile and prove Pillager↔Draugr hostility in both acquisition orders;
Eitrherjar↔Fuling hostility despite `PlainsMonsters`; self/tamed/player/same-group
non-hostility; unknown-pair vanilla parity; two-client synchronized registry;
and the same managed ZDO identities after save, unload, restart, and reload.
