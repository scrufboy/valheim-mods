# Response

Portal and dungeon gate design is in HANDOFF.md. The cave bug's cause is
SOURCE-INSPECTED: interiors are 5,000 m above the entrance in the same zone, and
only the local player ever triggers a door, so companions stay loaded but
stranded. The fix hooks `Player.TeleportTo` and places companions after the
player lands. Doors can ship first (same instance, nothing lost); portals need
inventory persistence or a kit-only limit. Owner approval requested to implement
doors first on the 1.0.x line after the 8b spacing test.
