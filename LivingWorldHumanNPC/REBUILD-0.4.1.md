# Living World Human NPC 0.4.1 acceptance correction

The first 0.4.0 live run proved that identity generation and the failure
transaction were working: four actors received different native hair and beard
records, none passed visible-appearance verification, and neither attempted
party was retained.

The cause was the foundation loadout. Both definitions included `HelmetIron`.
Valheim's native `VisEquipment` processing applies a helmet's hair and beard
visibility rules after reading the saved facial-hair hashes, and the iron
helmet suppresses the selected hair. The fully armored actors therefore looked
bald even though their saved identities were correct.

0.4.1 removes the hair-hiding helmet from the two acceptance loadouts. It does
not change creation order, identity selection, combat ownership, following,
formation, or the durable combat-state latch. Appearance failures now report
the selected hash, rendered hash, and attachment-object state.
