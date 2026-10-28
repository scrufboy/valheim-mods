# Shared-core friendly and hostile world variants

Date: 2026-10-27

## Owner direction

The practical goal is good people and bad people living throughout the world.
Souls NPCs supply proven loadouts, appearances, attacks, and role ideas, but their
old prefab faction and display name do not have to remain one indivisible actor.

## Architecture decision

Build Living World variants on the Formidable actor core. An approved archetype
may have hostile, neutral, or friendly content profiles, and may have living,
corrupted, bound, revenant, or freed presentations where lore supports them.

This does not mean recoloring one spawned person during combat or making every
enemy recruitable. Each variant owns a stable identity, origin, faction profile,
role, loadout, camp/encounter placement, and persistence record.

## War-lieutenant uncertainty

The old OrcBrute-derived lieutenant may be undead; the project has not decided.
Do not infer life-state from `PlainsMonsters`, appearance, or old naming. Preserve
both design options until the origin is approved. If appropriate, create a hostile
corrupted/revenant lieutenant and a distinct living or freed counterpart using
shared capabilities but different identity and allegiance.

## Scope

Documentation only. No runtime prefabs, faction values, overlay names, or spawns
change in this submission.

