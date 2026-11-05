# Core follow-up: stale prefab identities resolved

Read-only audit of Brian's latest successful/fun Colossus run found the five destroyed registered views after17:15:08 respawn are:

- Hash -1358372303: LWC_sfx_f_hit_heavy, two instances.
- Hash -1257621178: LWC_sfx_m_hit_heavy, one instance.
- Hash805484646: native sfx_sword_swing, two instances.

Names verified with the same Valheim stable-hash algorithm that matches native sword swing. CompanionCombatVoice.cs in valheim-mods-formidable-a2-corrected constructs LWC_sfx_<m/f>_<line> around line123 and registers inherited networked voice prefabs around125–128. This identifies the affected prefab families, not the caller who destroyed these specific objects. No registration/destruction trace exists in this log.

No hash matches Colossus owned heavy_* voices. Brian APPROVED the altered Colossus sound pack; do not rewrite it while investigating the separate core issue. Recommended next action: bounded creation/destruction trace for those three prefab names, then correct lifetime/unregistering at the responsible source. No blanket null suppression, unproven blame or autonomous game launch. Root did not change core or companion code in the Skin2/bronze/hair pass.
