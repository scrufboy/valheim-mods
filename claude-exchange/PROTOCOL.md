# Claude/Codex exchange protocol

## Submission directory

Create one dated directory beneath your outbox:

`claude-exchange/CODEX-OUTBOX/YYYY-MM-DD-short-topic/`

or

`claude-exchange/CLAUDE-OUTBOX/YYYY-MM-DD-short-topic/`

Never replace or delete a prior submission.

## Required files

Each submission must include:

1. `HANDOFF.md` — intent, evidence, exact scope, and remaining unknowns.
2. `CHANGED-FILES.md` — every changed file and why.
3. `VERIFICATION.md` — offline tests, installation status, live tests, hashes, and failures.
4. `REQUEST.md` or `RESPONSE.md` — the exact question or return message for the other agent.

Source changes should be delivered on a branch or pull request. Do not use the exchange folder as an unreviewed dumping ground for replacement binaries.

## Evidence labels

Every claim must be labeled as one of:

- SOURCE-INSPECTED
- OFFLINE-TESTED
- INSTALLED
- LIVE-PROVEN
- FAILED
- UNKNOWN

Offline success must never be written as live success.

## Promotion

A candidate may replace the current baseline only when:

- its parent/baseline is named;
- working behavior is protected by explicit checks;
- changes are scoped to the evidenced failing owner;
- complete affected regression gates pass;
- package and installed hashes match;
- runtime version text and package documentation agree;
- live evidence exists for the repaired behavior;
- remaining unknowns are explicit.

## Conflict handling

When Claude and Codex propose different changes, preserve both submissions. Compare evidence and changed boundaries before choosing. Do not merge two speculative fixes merely because both compile.

## Character-semantics gate

Character identity, roster membership, class/role meaning, biome tier, default pair and
starting package are owner-controlled semantics. A source author may not infer a change
from a new feature list or from mechanical similarity. Specifically:

- an omitted existing character/type remains present;
- a similar higher-biome character is additive, not an upgrade that replaces the
  earlier-biome character;
- shared weapon doctrine or equipment does not imply shared identity;
- a candidate with an unresolved semantic decision remains uninstalled and off.

Every submission touching these fields must include an explicit before/after roster
matrix. Any row that disappears, changes identity or gains an alias requires a quoted
Brian decision authorizing that exact change.

## Discussion is not authorization

The exchange may record ideas from any chat, but it may promote work only from an
explicit execution instruction. Questions, hypotheticals and design comparisons remain
non-mutating evidence of what Brian is considering.

Every implementation or installation submission must quote the exact owner-authored
action sentence that authorized it. A quotation that contains only a question,
brainstorm, preference or approval of a concept is insufficient.

Separate chats may be intentionally scoped for discussion, design writing, diagnosis,
implementation or testing. An agent must not merge their authority merely because they
belong to the same project. Cross-chat material supplies context, not permission.
