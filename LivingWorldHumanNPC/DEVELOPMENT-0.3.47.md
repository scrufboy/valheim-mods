# Human NPC 0.3.47 torch ownership and companion voice correction

Observed in the 2026-10-18 18:23 runtime session:

- calm nighttime companions repeatedly alternated between a visible torch and the stowed sword/shield pair;
- telemetry recorded the torch disappearing and returning roughly every half second while combat authorization was false;
- the native Fuling alert source retained its stock 1.10-1.20 pitch and sounded too small for the human companions.

Corrections:

- `HumanEquipmentReadiness` now yields completely while `HumanUtilityTorch` owns peaceful hands. It no longer interprets the torch as a missing combat loadout and replaces it during the calm state;
- the utility claim uses the same party, combat-clear, five-second release, stealth, swimming, attack, bow-draw, cleric-cast and ambient-hand gates as the torch transaction, while reusing the bounded built-light scan;
- the alert source is cloned privately and retuned to the exact RRR human-NPC/Krauguk range of 0.80-0.90. The shared vanilla Fuling prefab remains untouched, and there is no Dverger or RRR runtime dependency;
- combat selection, attack behavior, formation, wetness, door behavior and weapon-back presentation are unchanged.

Runtime acceptance still required:

1. At night and away from built light, a visible torch remains continuously equipped while calm.
2. Curiosity/awareness does not remove the torch.
3. Combat commitment removes it once, draws combat equipment, and does not flicker.
4. Verified combat clearance returns the torch once.
5. The combat alert voice sounds in the lower adult-human range used by the Souls of the Land Krauguk Legions/pillagers.
