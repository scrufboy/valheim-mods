# Direct response to Claude — party rotation is a character-life-cycle gate
Brian is asking for **WHO / WHAT / WHEN / WHERE / WHY / HOW** of a Fallout-style companion return-home mechanic because distinct biome classes, earned skills and role-based expedition composition have no purpose if the player cannot rotate trusted people.

**Current authority already provides:** two unlocked expedition slots (first Eikthyr, second Elder), six bonded living roster ceiling, meet-many/travel-few population split, persistent origin and home, ordinary **Release from Party** preserving identity and relationship, later independent camp-of-one concept. See `LivingWorldCORE/COMPANION-PROGRESSION-ROSTER-AND-RELEASE.md` and `claude-exchange/DECISIONS.md`.

**NEW PROPOSED CLARITY:** Keep **Return Home / Release from Party** as a roster-preserving active-slot change, **Rejoin Expedition** for an already bonded available person, and **Part Ways** as separate explicit relationship termination that frees a bonded seat while the independent person persists. Never treat the old “Release from Party” label as automatic severance.

**Technical shape:** a single persistent, authority-bound `PersonId` + home/camp/charter anchor + relationship + roster seat + field assignment + class/skill/gear data; safe, atomic departure with idempotent return/transit and lawful rejoin. Every state keeps a home/rally/fallback. The currently active combat owner remains native `MonsterAI`, the future Formidable framework owns generic persistence/transfer, Living World owns player relations and class decisions.

**Critical progression:** each biome-/role-authored person can use ONLY real earned/equipped abilities; do not dynamically grant a counter-weapon or replace an NPC class when facing poison-immune undead. Party swap is the player-driven answer. Homecoming must retain training and equipment; no free seventh recruit, third field fighter, remote combat deployment, free XP, or dead NPC resurrection.

**DO NOT IMPLEMENT based on this outbox alone.** This is a design contract for approval and regression planning. Complete outstanding 1.0.32.14 acceptance and identity-preserving traversal prerequisites before changing the installed line. Full six-W specifications, transition contract, fallback and test matrix are in `HANDOFF.md`.
