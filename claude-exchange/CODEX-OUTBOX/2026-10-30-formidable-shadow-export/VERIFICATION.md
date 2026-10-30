# Verification

- **OFFLINE-TESTED:** FormidableAI suites pass 47/47 after the export change.
- **OFFLINE-TESTED:** the 1.0.x ledger and native gate pass before the known local Windows IL-lock
  mismatch point.
- **OFFLINE-TESTED:** updated core compiles with the local Windows C# 5 compiler.
- **OFFLINE-TESTED:** updated telemetry DLL compiles against that core.
- **OFFLINE-TESTED:** analyzer self-test passes: clean fixture passes and regressed fixture reports
  five expected failures.
- **UNKNOWN:** canonical Mono compiled-code gates; Claude must run them because the Windows compiler
  produces different IL hashes for the locked methods.
- **INSTALLED:** no.
- **LIVE-PROVEN:** no.
