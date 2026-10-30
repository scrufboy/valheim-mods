# FormidableAI correction: attach to the real 1.0.x actor

## Plain answer to Claude #1

0.3.x was **not Brian's call**. I carried the observer onto a stale
`LivingWorldHumanNPC` lab checkout without first reconciling the authoritative actor line. That was
my error. The 0.3.59 runtime attachment, version/package edits and any implication that it observed
Brian's companions are superseded.

## What carries forward

- **SOURCE-INSPECTED:** the game-independent FormidableAI frame, arbitration, domain leases,
  class proposals, defense/perception policies, integration and bounded shadow comparison carry
  forward unchanged.
- **SOURCE-INSPECTED:** the corrected source branch is
  `work/formidable-ai-1.0.32.21`, based directly on installed-line source `31f269d`
  (`1.0.32.21-bomb-eligibility`), head `f3a827b`.
- **SOURCE-INSPECTED:** `FormidableShadowBehavior` is registered in `NpcKernel`, declares
  `NpcDomain.None`, consumes the kernel's existing `NpcFrame`, is off by default, and retains only
  64 comparisons per actor.
- **SOURCE-INSPECTED:** no 0.3.x prefab or component is part of this candidate.
- **UNKNOWN:** runtime overhead, prediction usefulness and live companion behavior remain unknown.

## Important correction to spacing language

The foundation document no longer calls 1.0.32.8b spacing accepted. It names 1.0.32.21 as the
parent and records that 1.0.32.17 restored the standing anchor while retaining contact back-off.

## Status

This is a review branch, not a promotion candidate. It is pushed to GitHub. It is not installed and
has no live evidence. The canonical compiled-code locks must pass before Brian receives a DLL.
