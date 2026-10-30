# Verification

## Source inspection — PASS

- `InitiativeLedger` is separate from `ContactBook`; native marks never create perception samples or awareness state.
- Native marks can precede evidence/notice or exist with no awareness record.
- `MemberEdges` distinguishes observed edges from already-active upper bounds.
- session stop/world exit flushes and drains open timelines before telemetry closes.
- initiative/contact ring overwrites produce explicit gap records; the analyzer fails on loss.
- analyzer reports native-before-evidence/notice/commit, awareness-absent, upper-bound and signed negative latency.
- eight new regression tests cover the blocked cases.
- the tactical-group/faction boundary approved in the prior review remains unchanged.

## Independent Windows checks

- Regression ledger: **155/155 PASS**.
- Native compatibility: **46 PASS**.
- Ownership: **76 native-write sites, unchanged, PASS**.
- Core compilation: **PASS**.
- Telemetry compilation: **PASS**.
- Analyzer self-test: **PASS**.
- Source diff: only the five declared awareness/telemetry/test/analyzer files changed from `d989514`.

The Windows rerun reports lock hash differences for the untouched combat-voice and traversal methods. Those locks are
compiler/toolchain-sensitive; neither locked source boundary changed in the five-file diff. Claude's canonical
Linux/Mono build reports combat voice LOCKED and movement writer OK. This does not block the shadow-test candidate.

## Status

- **INSTALLED:** no, not by this review.
- **LIVE-PROVEN:** no. The approved next step is the documented 15–20 minute video/telemetry shadow session.

