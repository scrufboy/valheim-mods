# Human NPC 0.3.51 follow and self-defense recovery

## Runtime evidence

The 2026-10-18 21:18:29 telemetry run disproved the provisional live-log diagnosis for the first disappearance:

- Neither companion was dead, despawned, or missing its persisted Follow order.
- Both remained peaceful with no authorized target while separation grew to roughly 46-47 metres.
- Catch-up was active, but ordinary run speed was the same as player run speed, so sustained player travel could grow or preserve the gap.
- During a later encounter Ivar Willow took repeated attributed hits while separated and produced no attack attempts.
- The leader-relative pursuit leash rejected local self-defense after separation.
- Threat scanning could still claim encounter ownership before authorization succeeded, clearing Follow while leaving the companion with no legal combat target.

## Correction

- Catch-up retains the existing 12 metre enter / 7 metre release hysteresis and gains a temporary, bounded 20 percent run-speed advantage (at least +1 m/s).
- The ordinary configured run speed is restored before combat, after catch-up, on fault, disable, and destruction.
- A separated companion may defend against an immediate attacker within eight metres.
- Recovery defense is bounded to the attack site, actor, target, sensing interval, and eight-second refreshed engagement window; it cannot become a distant chase.
- Threat scans cannot claim combat ownership until a target has actually been authorized.
- Existing stowing, torch, wetness, formation, awareness, door, cleric, and native combat paths remain intact.

## Required live test

1. Confirm `lwtest status` reports 0.3.51 and both party roles loaded.
2. Run continuously until both companions cross 12 metres behind; they must close through seven metres without a new party command.
3. Let a nearby hostile strike a separated companion. The struck companion must draw and defend locally instead of remaining targetless.
4. Move away during the fight. Once the immediate attacker is dead or outside the recovery-defense boundary, the companion must return to catch-up.
5. Confirm peaceful back stow and night torch behavior resume only after the normal verified-clear gate.

