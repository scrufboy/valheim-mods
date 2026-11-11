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
3. `VERIFICATION.md` — code checks, installation status, live tests, hashes, and failures.
4. `REQUEST.md` or `RESPONSE.md` — the exact question or return message for the other agent.

Source changes should be delivered on a branch or pull request. Do not use the exchange folder as an unreviewed dumping ground for replacement binaries.

## Evidence labels

Every claim must be labeled as one of:

- SOURCE-INSPECTED
- CODE-TESTED (checks run outside Valheim)
- INSTALLED
- LIVE-PROVEN
- FAILED
- UNKNOWN

Code-test success must never be written as live success. Older dated submissions
retain their original wording as historical evidence; new submissions use this
vocabulary.

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
