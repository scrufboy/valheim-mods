# Living World faction relations compatibility 0.1.0 candidate

Isolated BepInEx/Jotunn/Harmony runtime candidate. It does not modify Formidable,
RRR, Souls JSON, native faction fields, ZDO faction values, raids, or spawners.

The server-synchronized registry gives exact prefabs managed string identities.
A highest-priority prefix on the static `BaseAI.IsEnemy(Character, Character)` applies a configured Friendly
or Hostile result only when a rule matches. Unknown pairs execute the untouched
vanilla result. Self, player, tamed, and equal non-empty `Character.m_group`
pairs never receive an override. The prefix returns to the complete vanilla method
when no explicit rule matches; it skips vanilla only for a validated tri-state
override, before the native same-faction early return. Managed identity wins over a stale native
faction, so `RRRN_Pillager` does not match `native:Undead` as its own identity.
On authoritative `Character.Start`, an owned valid ZDO receives
`lwc_managed_faction_v1`; subsequent resolution prefers that validated persistent
identity, then the stable ZDO prefab hash/name, then the non-networked object-name
fallback. Existing recognized managed identity is never silently replaced.

Default candidate identities:

- `RRRN_Pillager` → `LWC.Pillagers`
- `RRRN_OrcBrute` and compatibility alias `Krauguk` → `LWC.Eitrherjar`

Default relations are symmetric hostility between Pillagers and native Undead,
Pillagers and Eitrherjar, Eitrherjar and native Undead, and Eitrherjar and native
PlainsMonsters (including Fulings). Symmetry is expanded
into two directed rules so both `BaseAI.IsEnemy` argument orders work.

Jotunn synchronizes `Enabled`, `PrefabAssignments`, and `Relations` from the
server. Deterministic relation resolution runs on every peer because a nonserver
client may own a MonsterAI ZNetView. Only the current valid ZNetView owner may
write the managed identity ZDO key. Invalid synchronized registry text is
rejected and the last valid registry remains active.

Build with `Build-Candidate.ps1`. It compiles against the real locally installed
Valheim 1.0.21, BepInEx, Harmony, and Jotunn assemblies and writes a fresh package
under `Documents/LWC Model Skin Studio/Exports`. It never installs or launches.

Live behavior, multiplayer synchronization, and interaction with every other
hostility patch remain unproven until a separately authorized disposable-profile
test. Harmony postfix priority/interoperability must be inspected before install
if another mod also short-circuits `BaseAI.IsEnemy` at equal/higher priority.
