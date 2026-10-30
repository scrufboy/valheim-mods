# FormidableAI character profile build

Implementation has resumed on branch `work/formidable-ai-1.0.32.21`, head `91b361f`, candidate
version 1.0.32.23.

The observer now constructs an inspectable character-combat profile from the persisted role and
actual inventory. Guard, archer and scout map explicitly to Warden, Huntress and Pathfinder. Weapon
and survival doctrines are authorized by present equipment rather than prefab-name assumptions.

No final combat numbers were chosen. Origin, culture, food/stats and progression remain explicit
unknowns for existing companions rather than fabricated values. The observer remains read-only,
off by default and `NpcDomain.None`.
