# Human NPC 0.3.58 — moving-anchor defense

## Correction

Valheim's `MonsterAI.m_maxChaseDistance` is not a tactical radius around the
leader or the current target. `MonsterAI.UpdateTarget` compares it with distance
from `BaseAI.m_spawnPoint`. A positive 30-metre value would therefore make a
travelling companion eligible to drop alert merely because the party had moved
away from the point where that actor originally spawned.

The native spawn-point limit is now zero, which disables that obsolete check.
The active companion policy supplies the meaningful boundaries instead:

- 30 metres: awareness and ranged observation, not travel authority;
- 18 metres: maximum committed distance from the moving leader anchor;
- 18 metres: maximum distance from the nearest living party companion;
- 8 metres: bounded local self-defense after a cohesion boundary is crossed.

## Defensive movement

The companions do not need to pursue a raid actor across the terrain: committed
raid enemies are already moving toward the player or party. Native MonsterAI
continues to own movement and attacks inside the defended space. Its proven Souls
of the Land values remain intact: circulate while charging, an eight-metre target
circle, a six-second circling window and an eight-second circling interval. That
supports short lateral movement, kiting and repositioning without adding a second
combat controller.

The fire-team cohesion adapter remains the outer boundary. Once an actor reaches
that boundary it may finish only immediate local defense, then must regroup rather
than selecting another target farther away.
