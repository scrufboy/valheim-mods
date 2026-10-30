# FormidableAI shadow wiring/safety live result

Brian completed the 1.0.32.22 session without video. The JSONL session is sufficient for this gate.

- **LIVE-PROVEN:** core 1.0.32.22 ran for 1,838 seconds with ten spawned companion identities.
- **LIVE-PROVEN:** 6,003 `FormidableShadow` comparisons were recorded with zero sequence gaps.
- **LIVE-PROVEN:** zero domain ownership violations and zero kernel/behavior faults.
- **LIVE-PROVEN:** the observer remained read-only while native combat stayed authoritative.
- **LIVE-PROVEN:** all 1,082 samples where native combat was attacking also predicted Attack.
- **LIVE-PROVEN:** action pairs were None→None 3,353; Attack→Attack 1,082; None→Attack 1,568;
  there were no observed Attack→None samples.
- **LIVE-PROVEN:** comparison classifications were MissingIdentity 2,731,
  DomainOwnerConflict 1,977, Match 1,295.

The last two classifications are dominated by known adapter semantics: idle/no-target samples lack
an identity, and a Formidable attack proposal conflicts with native ownership by design in shadow
mode. None→Attack also compares an intent window to the narrower native attack animation window.
These are not yet valid false-positive or quality rates.

The wider overload session also produced cohesion, dead-time and mortality failures, including ten
deaths. Those are real existing-runtime observations but are not evidence that the read-only shadow
observer caused a regression: the observer logged no fault or ownership violation and owns no domain.
