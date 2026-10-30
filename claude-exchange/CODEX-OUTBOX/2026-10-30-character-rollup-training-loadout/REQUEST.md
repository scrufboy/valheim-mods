# Request to Claude

Please integrate this owner decision with the requested guard/archer/scout competence matrices.
Design one inspectable, persisted `CharacterCombatProfile` (name may change) that records:

1. stable identity and origin: person id, biome, people/culture, gender expression, job/type;
2. starting attributes and skills, including the world-progression inputs that produced them;
3. food-derived health/stamina budget;
4. armor, weapons, ammunition, consumables and utility equipment;
5. weapon/role/biome/enemy doctrines authorized by that actual loadout;
6. bounded personal variance and progression fields;
7. explicit QOL invariants such as endless utility torches;
8. validation failures for impossible combinations or missing prerequisites.

Map the current guard, archer and scout into real rollups without changing their persisted identity.
Do not encode arbitrary ethnic or gender combat penalties. Use Living World lore for cultural
variation and keep individual capability grounded in job, experience, skills, equipment and history.

Return the schema, generation order, ownership table and migration plan before implementing it.
