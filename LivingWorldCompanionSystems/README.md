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
- [Offline Companions and Vikings for Hire audit](../research/COMPETITOR-AUDIT-OFFLINE-COMPANIONS.md)
- [Human companion behavior-state contract](../LivingWorldHumanNPC/BEHAVIOR-STATE-CONTRACT.md)

Companion encounter implementation (pure policies and authored content; the
maintained 1.0.x world adapter is still in progress):

- [Discovery and camp placement](./Discovery/ADAPTER-HANDOFF-1.0.x.md)
- [Loaded camp routines and away-state lifecycle](./CampLifecycle/ADAPTER-HANDOFF.md)
- [Friend in Need shock alert](./FriendInNeed/ADAPTER-HANDOFF-1.0.x.md)
- [Rescue, recruitment, and safe release](./RescueRelease/README.md)
- [Concrete needs and remembered affinity](./NeedsAndAffinity/README.md)
- [Map-informed request broker](./QuestBroker/README.md)
- [1.0.x integration contract and prototype](./RuntimeIntegration/INTEGRATION-CONTRACT-1.0.x.md)
- [Split-Ash Camp and Plains encounter writing](./EncounterLore/WILDERNESS-COMPANION-ENCOUNTER-ARC.md)
- [Encounter voice candidate review](./EncounterVoice/REVIEW-NOTE.md)
- [Research on small respect requests and Valheim mod precedents](../research/CompanionRespectQuests/COMPANION-RESPECT-QUESTS-RESEARCH.md)

The baseline Scout owns bow-first and close-first preferences within one adaptable
class. Biome specialists are distinct only when they introduce a different combat
responsibility or capability. Current delivery order is traversal safety, reusable
bomb prediction/execution, the Plains spear/heavy pair, knife lunge, then bounded
magic profiles.
