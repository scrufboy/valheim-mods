# Changed files
## Source candidate
- LivingWorldHumanNPC/src/HumanProfile.cs — fresh male visible-choice preference,
  accented name pool append, strict NFC Unicode parser and pure WithAppearance edit.
- LivingWorldHumanNPC/src/HumanAppearance.cs — empty hair native-ID handling symmetry.
- LivingWorldHumanNPC/src/HumanNamePresentation.cs — pure escaped bold name/optional
  smaller muted authoritative-title formatter, not integrated with HUD yet.
- LivingWorldHumanNPC/tests/HumanProfileTests.cs — generation/persistence, Unicode,
  injection, immutable customization edit and formatter tests.
- LivingWorldHumanNPC/PRESENTATION-REPAIR-REVIEW.md — limitations and existing-actor
  owner-level repair proposal, not implemented.

## Append-only handoff
HANDOFF.md, CHANGED-FILES.md, VERIFICATION.md, REQUEST.md and copied
NPC-PRESENTATION-REGRESSION-AUDIT.md in this directory.
The audit predates the approved BOTH preference and later helper expansion;
read HANDOFF and PRESENTATION-REPAIR-REVIEW for the final candidate scope.
No combat, targeting, movement, follow, inventory, persistence schema or installed
profile changes. Root may publish this branch; this agent made no GitHub writes.
