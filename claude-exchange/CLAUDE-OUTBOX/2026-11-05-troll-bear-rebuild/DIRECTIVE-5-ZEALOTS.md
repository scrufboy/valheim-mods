# DIRECTIVE 5 — The Zealots: swamp faction design (Brian, 2026-11-05 12:09). Supersedes D4 §C where they differ.

Status: DESIGN. Build only after the current four creatures pass Brian's video test. Do not start without his go.

## Core idea (Brian)
"Think Zakarum Zealots from Diablo 2 — that is our whole secret." Crazy, fast, little jungle fighters who come at you in numbers. "We make swamp Fulings from Necks, Neckmen, Neck Shamans and Neck Marauders." They act like **Dvergr-Fulings**: not as beastly as Fulings, more intelligent — men in regression, struggling with the curse.

## Name
Faction = **the Zealots**. Necks stay Necks. Each class is named under the Zealots (class names below are Claude's proposals — Brian picks).

## Roster
| Class | Body | Role |
|---|---|---|
| **Neckman** (Zealot warrior) | Inca Warrior base (human enemy NPC), Neck head | Line fighter. Human NPC behavior, not Fuling. |
| **Zealot Priest / Neck Shaman** | Inca Warrior base, red robes | Cleric magic + ranged; leads; binds and frees the Colossus. |
| **Neck Marauder — 3 variants** | Frog Marauder, Fuling body actions/combat/weapon choices | Zakarum-style swarm: **5–10 at a time, bounded bursts of super speed, individually weak** so numbers stay fair. |
| — Dart Marauder (proposal) | | **Blowgun**: dart fired like an arrow from a tube held to the mouth. |
| — Blade Marauder (proposal) | | **Very fast knife** (weapon pack knives at real size — never shrunk). |
| — Raider Marauder (proposal) | | Spears, torches, clubs. |
| Sunken miners (existing) | Frog Marauder, picks | In crypts/temples: mining, working, praying until aggroed. |
| **Necks** | vanilla | Wildlife at temples; little Necks hunt Leeches. |

## Behavior
- Zealots **do not aggro on zombies**; zombies come against them.
- Living settlement life at our ruins/temple: **mining, working, praying** (Diablo 2 feel).
- **Undead Colossus: always hostile to everyone except undead/zombie kind.** In Zealot raids the priests bring one **bound in chains attached to its bronze nose ring**. During the fight they break the chain (or it breaks free): it **roars** and becomes a summoned beast that kills all the Zealots' enemies.

## World
- Our ruins (KumaBeer pack, owned) and the temple placed in swamps (Upgrade World for existing worlds).
- Pillars/totems/columns crowned with giant stone **Neck, Neckman and Frog Marauder heads** (from the trophy-statue work, head-swap on Structure_04_02..05).

## Blowgun animation — the honest part
Vanilla Valheim has no blowgun animation (Claude: not verified by dump; ask before assuming one exists). Options:
1. Author one short **humanoid clip** (raise tube to mouth, puff, lower) in Unity on the Frog Marauder's avatar; fire the dart at the puff frame (`OnAttackTrigger`). Projectile = arrow-style dart with poison.
2. Interim: reuse the closest Fuling ranged/cast action and swap only the held prop to a tube.
Read the Goblin/GoblinShaman controller action list once before choosing.

## Open for Brian
1. Class names (above are proposals).
2. Marauder variant split: is "Raider" right (spear/torch/club), or should torches be their own?
3. Dart effect: poison, slow, or plain damage?
4. Swarm numbers per biome tier / raid.
