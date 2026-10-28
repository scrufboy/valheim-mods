# Persistence contract

Each legitimate person has a stable identity record containing at minimum:

- identity/version and debug-versus-campaign provenance;
- name, appearance, sex/body recipe, origin and faction;
- relationship, promises, history and memorial fields;
- current home, last safe position and availability;
- bonded-roster and active-expedition assignment;
- persistent order, directive, role/class and progression;
- health/resources, injuries/recovery and environment state where appropriate;
- personal versus loaned inventory/equipment, durability, ammo and stow state.

Changing Follow/Hold/Resident or class never rerolls identity. Network authority
must suppress duplicate materialization and make transitions idempotent.

Default design limits: two active expedition companions, six living bonded
companions total. Debug-spawned actors can never become campaign people.

Actual committed death is terminal. It closes active/bonded assignment, permits
one lawful corpse/tombstone equipment-recovery transaction, and preserves an
immutable memorial. Reload, portal, recall, bed, cleric, console or migration may
not recreate the person alive. Mortality stays disabled until traversal, hazard
safety, network ownership and death attribution are live-proven.

