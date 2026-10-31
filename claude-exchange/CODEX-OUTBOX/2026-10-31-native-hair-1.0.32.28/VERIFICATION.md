# Verification

## Passed locally

- **OFFLINE-TESTED:** C# 5 core compile succeeds against the installed Valheim 1.0.21 assemblies.
- **OFFLINE-TESTED:** regression ledger 157/157.
- **OFFLINE-TESTED:** native compatibility gate verifies 56 live private instance members (46 prior plus the 10 restored appearance members).
- **OFFLINE-TESTED:** new appearance transaction gate passes.
- **OFFLINE-TESTED:** ownership gate passes with 82 authorized native write sites.
- **OFFLINE-TESTED:** `git diff --check` passes.
- **SOURCE-INSPECTED:** protected combat/voice source files are unchanged.

## Canonical build still required

- **FAILED locally / compiler mismatch, not promoted:** the Windows .NET Framework compiler emits different short/long branch forms than the Mono compiler used to create the approved combat-voice IL lock. The strict voice gate therefore rejects the Windows rebuild even though the protected source is unchanged. The installed/canonical Mono 1.0.32.27 DLL still passes that lock.
- **UNKNOWN:** canonical Mono candidate hashes and complete gate result. Claude must build commit `af31a58` with `build.sh`; do not re-lock the voice module.
- **NOT INSTALLED:** no DLL from this branch has replaced 1.0.32.27.
- **NOT LIVE-PROVEN:** the hair repair needs the bounded `lwparty` live gate after canonical build and install.

The Windows output must not be installed or described as the candidate.

