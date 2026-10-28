# Living World Companion Systems

Companion activities, classes, population, party continuity, tactics, and related system modules.

Current design authorities:

- [World companion discovery and biome-role architecture](../LivingWorldCORE/WORLD-COMPANION-DISCOVERY-AND-BIOME-ROLES.md)
- [Companion encounter and resolution arc](../LivingWorldCORE/COMPANION-ENCOUNTER-RESOLUTION-ARC.md)
- [The Second Breath lore bible](../LivingWorldCORE/LORE-BIBLE-THE-SECOND-BREATH.md)
- [Faction and peoples bible](../LivingWorldCORE/FACTION-AND-PEOPLES-BIBLE.md)
- [Encounter personality and dialogue grammar](./ENCOUNTER-PERSONALITY-AND-DIALOGUE.md)
- [Structured encounter dialogue repository](./data/encounter-dialogue.v1.json)
- [Specialist combat capability plan](../LivingWorldHumanNPC/SPECIALIST-COMBAT-CAPABILITY-PLAN.md)
- [External capability research](../research/EXTERNAL-COMPANION-CAPABILITIES.md)
- [Human companion behavior-state contract](../LivingWorldHumanNPC/BEHAVIOR-STATE-CONTRACT.md)

Companion encounter implementation (pure policies and authored content; the
maintained 1.0.x world adapter is still in progress):

- [Discovery and camp placement](./Discovery/ADAPTER-HANDOFF-1.0.x.md)
- [Uncertain first contact and authored allegiance](./EncounterAllegiance/README.md)
- [Camp routines and selected server-resident areas](./CampLifecycle/ADAPTER-HANDOFF.md)
- [Friend in Need shock alert](./FriendInNeed/ADAPTER-HANDOFF-1.0.x.md)
- [Rescue, recruitment, and safe release](./RescueRelease/README.md)
- [Concrete needs and remembered affinity](./NeedsAndAffinity/README.md)
- [Map-informed request broker](./QuestBroker/README.md)
- [Accepted-aid site activation](./EncounterActivation/ADAPTER-HANDOFF-1.0.x.md)
- [Same-person equipped variants](./EncounterLoadouts/README.md)
- [Server-selected biome population and client view contract](./EncounterPopulation/README.md)
- [Quest and relationship flags through zone changes](./EncounterContinuity/README.md)
- [Finite ward, heal, and missile resource policy](./CombatSupport/README.md)
- [Character blueprint and paired-combat telemetry](./CHARACTER-BLUEPRINT-AND-COMBAT-TELEMETRY.md)
- [Spear and bomb prediction](./SpecialistCombat/PredictionAndThrows/README.md)
- [Knife lunge and atgeir roles](./SpecialistCombat/MeleeRoles/ADAPTER-HANDOFF-1.0.x.md)
- [1.0.x integration contract and prototype](./RuntimeIntegration/INTEGRATION-CONTRACT-1.0.x.md)
- [Maintained 1.0.x adapter slice](./LiveCoreAdapter/README.md)
- [Split-Ash Camp and Plains encounter writing](./EncounterLore/WILDERNESS-COMPANION-ENCOUNTER-ARC.md)
- [Encounter voice candidate review](./EncounterVoice/REVIEW-NOTE.md)
- [Research on small respect requests and Valheim mod precedents](../research/CompanionRespectQuests/COMPANION-RESPECT-QUESTS-RESEARCH.md)

The baseline Scout owns bow-first and close-first preferences within one adaptable
class. Biome specialists are distinct only when they introduce a different combat
responsibility or capability. The next combat package joins spear use, a safe
fallback weapon, owned spear recovery, and finite bombs; knife and atgeir moves
follow their separately verified native attack paths. Traversal safety remains a
gate for every combat role.
