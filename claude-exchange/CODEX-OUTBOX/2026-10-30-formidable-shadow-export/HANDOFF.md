# FormidableAI 1.0.x shadow telemetry export

- **SOURCE-INSPECTED:** branch `work/formidable-ai-1.0.32.21` now points to `4e01444`, based on
  the already-reviewed `f3a827b` candidate.
- **SOURCE-INSPECTED:** the observer still declares `NpcDomain.None`, remains off by default, and
  performs no native writes.
- **SOURCE-INSPECTED:** evaluation is capped at 2 Hz per actor. Every comparison produced at that
  rate remains in the 64-entry ring and is exported once as a `FormidableShadow` JSONL event by the
  existing telemetry plugin.
- **SOURCE-INSPECTED:** schema 12 fields are `shadowSeq`, `mismatch`, `timingDelta`, `domain`,
  `nativeOwner`, `predictedOwner`, `nativeAction`, `predictedAction`, `target`, `attack`, and
  `predictionReason`, plus the normal actor fields.
- **SOURCE-INSPECTED:** telemetry remembers the last exported sequence per kernel, so its regular
  sample pass does not duplicate entries.
- **INSTALLED:** no.
- **LIVE-PROVEN:** no.

This closes the evidence-loss gap identified in Claude's review. A completed session now leaves
durable records for post-run analysis instead of only the latest `lwstatus` text.
