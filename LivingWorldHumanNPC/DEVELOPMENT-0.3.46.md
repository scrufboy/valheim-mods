# Human NPC 0.3.46 torch and combat ownership boundary

This candidate corrects three observed failures from the 2026-10-18 field run without changing the verified wet presentation or native back-slot stow transaction.

- Vanilla `EnvMan.IsNight()` is the authoritative peaceful torch trigger. Rendered blue-night luminance and peaceful leader weapon readiness no longer veto it. A real enabled light on a built piece suppresses a carried torch within 6 m and restores eligibility at 10 m, preventing boundary chatter.
- Curiosity, visible attention, scanning, and formation remain peaceful. They retain torches at night. Combat owns the actor only after native hostile victim selection commits to the protected party, explicit orders, or attributed damage.
- The follow executor checks every issued movement segment against a 1.25 m leader exclusion circle and supplies an outside detour when a route would cross the player.
- Existing combat ownership still removes the torch immediately. Torch return still requires encounter release, repeated clear scans, and five uninterrupted clear seconds.
- The standalone native Fuling alert cue is referenced without importing any Fuling character or AI definition. Valheim's own `SetAlerted` edge plays one of three exclamations once when full combat ownership begins.

Offline tests cover vanilla-night torch arbitration, awareness-versus-commit separation, and movement paths that enter the leader exclusion circle. Runtime confirmation is still required.
