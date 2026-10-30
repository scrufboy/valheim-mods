# Verification

## Source inspected

- `src/Kernel/AwarenessGroup.cs`: adapter boundary, membership, Valheim hostility call, member-action observer.
- `src/Kernel/Awareness.Pure.cs`: milestone storage, contact transitions, `MarkMember`, publication rules.
- `src/Kernel/ActorIntent.cs`: actor intent readers.
- `src/Telemetry/CoreTelemetry.cs`: schema-13 initiative emission.
- `tools/analyze_session.py`: initiative latency and native-ahead analysis.
- `tests/RegressionLedgerTests.cs`: three initiative tests.
- `docs/development/A1-AWARENESS-SHADOW-PRESENTATION.md` section 10.

## Results

- **SOURCE-INSPECTED PASS** — tactical group is separate from faction/allegiance and the generic awareness core does
  not depend on the player identity.
- **SOURCE-INSPECTED PASS** — hostility is supplied by the adapter through `BaseAI.IsEnemy`; awareness invents no
  hostility relation.
- **SOURCE-INSPECTED PASS** — A1 is gated, read-only, and does not enter gameplay arbitration.
- **SOURCE-INSPECTED FAIL** — native initiative can be recorded only after awareness has already noticed the contact.
  `ContactBook.MarkMember` returns when the record is absent or `State == None`, and `PublishInitiative` also refuses
  traces without `Noticed`. Therefore a member target/action that genuinely precedes A1 noticing is discarded. The
  later timestamp is the first post-notice observation, not the first acquisition/action.
- **OFFLINE-TESTED (reported by Claude, not rerun here)** — 148/148 and other gates pass at `d989514`; those tests do
  not cover the failed ordering because `AwInitiativeTrace` deliberately calls `MarkMember` only after noticing.
- **INSTALLED** — no; 1.0.32.26 was not installed by this review.
- **LIVE-PROVEN** — no.

