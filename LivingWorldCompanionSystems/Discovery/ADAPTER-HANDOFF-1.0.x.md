# Discovery adapter handoff for the maintained 1.0.x runtime

This folder is a pure, runtime-independent campaign policy. It does not depend on
or modify the 0.3.x lab and must be ported into the maintained 1.0.x source line
(`LivingWorldCORE/npc`), currently represented by the separately maintained
1.0.32.x history.

The 1.0.x server adapter should:

1. Run `DiscoveryPolicy.Select` only when `ZNet.instance.IsServer()` is true.
2. Build world-key evidence from the server's `ZoneSystem` global keys. Preserve
   distinct discovery and recruitment keys: later-biome clues may be visible
   before the recruit can join.
3. Sample candidates through 1.0.x world/zone services, then populate every
   `SiteCandidate` safety fact. A missing terrain, path, structure, boss-area,
   water, lava, or slope reading must fail closed; do not guess a safe value.
4. Persist the returned `LeadRecord` under
   `DiscoveryPersistence.LeadKey` (`lwc_discovery_lead_v1`) in a server-owned
   world campaign record. Save camp records independently so unloaded camps still
   prevent duplicate placement. Broadcast the accepted record and clue only after
   the authoritative write succeeds.
   `TemplateId` identifies reusable authored content; `EncounterId` identifies one
   deterministic world instance; `SiteId` identifies its placement. Never use the
   template ID as the instance ID. Active camps reserve their instance/site and
   separation radius. A deliberately retired camp releases its site for later
   authored reuse while its historical encounter-resolution record remains.
5. Instantiate an authored site and stable NPC identities through the 1.0.x
   factory/network registration path. Never call the debug `lwparty`/tool spawner,
   and never promote a tool-created actor into a campaign identity.
6. On recruitment, refusal, abandonment, or permanent death, call `TryClose` and
   persist the close day. The default five-day gate is server configuration with
   a production minimum of one day; the authored default remains five.

Integration still required: a server-owned world campaign store; authoritative
structure/boss/lava/path queries; authored camp prefab registration; stable person
creation linked to encounter/site IDs; clue delivery UI; and multiplayer party-ID
resolution. These pure tests do not claim live Valheim placement or networking.
