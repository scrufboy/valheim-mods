# Encounter loadout profiles

Status: engine-free policy. Nothing here registers a prefab, installs a profile,
or proves an item mesh, animation, attachment, or live inventory transaction.

This module gives the Split-Ash camp's Runa and Edda stable, explicit person
profiles. Each profile binds origin encounter/camp, faction, role, biome,
discovery and recruitment gates, local history, verified site-backed knowledge,
personal equipment, finite reserve, and bounded starting supplies.

The profile is carried unchanged through cautious first contact and any accepted
temporary aid journey to a verified site. This module does not authorize contact,
combat, a hostile event spawn, recruitment, or an expedition slot. Those remain
separate server-owned encounter/request decisions. If another module lawfully
claims one bounded hostile event, the same person's finite kit is used under
native combat ownership; completing that task still does not recruit them.

The distant silhouette is intentionally readable only as an armed road survivor:
at range the same broad shield/weapon/layered-clothing shapes could belong to a
friendly traveler or a Pillager. That is presentation uncertainty, not hidden
mechanics. `FactionId` remains present and authoritative; body, gear, biome, and
prefab never infer allegiance. The abstract gear IDs describe policy slots and do
not claim corresponding runtime assets exist.

## Progression boundary

- Meadows gear is allowed before a boss key, while permanent recruitment remains
  gated by Eikthyr for this encounter.
- Higher variants require the canonical companion-role gate plus verified station
  and material facts. A profile cannot equip gear above its own biome tier.
- Every item is personal-bound. Supply and ammunition caps prevent an encounter
  from becoming a free loot chest.
- Advancement copies the persisted record and changes only the active variant and
  lawful personal gear. `PersonId`, origin, faction, role, knowledge, wounds,
  relationship, and death record remain the same. A dead identity cannot advance.
- A roster refuses two records with the same `PersonId`; unloading and reloading
  never create a replacement person.

The policy uses abstract tier keys matching the canonical companion sequence:
the Elder opens Black Forest roles, Bonemass opens Swamp, Moder opens Mountain,
Yagluth opens Plains, and the Queen opens Mistlands. Ashlands and Deep North use
explicit veteran-approval gates because canon does not yet authorize a precise
boss-key shortcut. A live adapter must map and verify the maintained runtime's
exact world-key spelling before using any of them.

Run `Run-Tests.ps1` for the engine-free acceptance suite.
