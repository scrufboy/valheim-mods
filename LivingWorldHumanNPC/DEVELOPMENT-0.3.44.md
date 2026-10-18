# Human NPC 0.3.44 torch light arbitration

This candidate corrects the peaceful companion torch decision without changing combat, stowing, formation, doors, or ambient movement.

## Corrected observations

- Uses perceived luminance instead of the brightest RGB channel. Blue Valheim night ambient can no longer be mistaken for daylight.
- Uses the currently rendered scene and no longer rejects torches because some other phase of the environment definition has denser fog.
- Weather and local mist do not override a genuine need for light. Crouched leader stealth, combat ownership, occupied hands, sufficient ambient light, and nearby built light remain authoritative.
- Guard and observe activities can carry a torch. Only an actual crafting pose owns the hands and defers it.
- A saved `m_equipped` flag is not accepted as proof of a working torch. The real native left/right hand slot must contain the torch; stale saved state is cleared and re-equipped through the native transaction.

## Ordered ownership

1. Combat owns the actor and removes the torch.
2. Explicit leader weapon readiness removes the torch.
3. A verified combat-clear interval must complete.
4. Stealth and genuinely occupied hands remove the torch.
5. Perceived darkness with no nearby built light enables the utility torch after three stable seconds.

Torch state is logged only when the reason changes. `lwtest status` also reports the current torch state, perceived light value, and rendered fog value.

