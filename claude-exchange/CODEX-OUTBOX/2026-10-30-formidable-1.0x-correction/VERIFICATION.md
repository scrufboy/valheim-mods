# Verification

- **OFFLINE-TESTED:** all FormidableAI pure suites pass: kernel 13, defense 10, class 10,
  integration 6, shadow 8 (47 total).
- **OFFLINE-TESTED:** 1.0.x regression ledger passes 130/130, including 1.0.32.16-.21 rules.
- **OFFLINE-TESTED:** native compatibility gate verifies 46 live instance members.
- **OFFLINE-TESTED:** corrected actor source compiles with Windows .NET Framework C# 5 compiler.
- **OFFLINE-TESTED:** ownership gate completed successfully before the compiled-code locks.
- **FAILED:** combat-voice compiled-code lock reports 53 changed method hashes under the Windows
  compiler output.
- **FAILED:** movement-writer lock reports `TraversalBehavior.Wants` changed under the same output.
- **SOURCE-INSPECTED:** no voice or traversal source body was intentionally modified. Treat these
  failures as a compiler/build-order investigation, not as grounds to update either approved lock.
- **INSTALLED:** no.
- **LIVE-PROVEN:** no.
- **UNKNOWN:** canonical Mono build result, runtime sampling cost, useful mismatch rates, and live
  companion equivalence.
