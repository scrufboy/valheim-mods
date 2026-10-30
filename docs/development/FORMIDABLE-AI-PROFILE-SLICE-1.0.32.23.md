# FormidableAI character combat profile slice

Status: offline candidate on the 1.0.x actor line. Not installed or live-proven.

## What changed

The shadow observer no longer maps every non-archer to Warden. It constructs an inspectable combat
profile from the companion's persisted role and actual inventory, with explicit mappings:

- guard to Warden;
- archer to Huntress;
- scout to Pathfinder.

Weapon doctrine is authorized only by present equipment. Bow inventory enables firing-lane and kite
doctrine. An eligible one-handed weapon plus shield enables shield/block/protect doctrine. Spears
enable spear and throwing doctrine. Heavy and magic equipment are recorded without inventing live
behavior that is not yet implemented.

All profiles share survival doctrine for dodge, cohesion, disengage/rally and biome-hazard awareness.
The profile declares endless utility torches as a quality-of-life invariant.

## Evidence boundary

This slice does not choose numeric skills, ranges or reaction timing. Those remain stress-test tuning.
It does not yet persist origin biome, culture, starting food/stats or individual skill progression;
those fields remain explicit and unknown instead of being fabricated for existing companions.

The telemetry now records profile job, Formidable class, weapon doctrine and survival doctrine beside
each shadow comparison. The observer remains off by default, read-only and owns no behavior domain.

## Verification

- 55 FormidableAI offline tests pass, including eight profile/loadout tests.
- The 1.0.x core compiles and reaches the approved compiled-code locks under the local Windows build.
- Canonical Mono build and unchanged voice/movement locks remain required before installation.
