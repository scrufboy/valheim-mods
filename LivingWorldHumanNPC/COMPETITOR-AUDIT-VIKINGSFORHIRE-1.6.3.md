# VikingsForHire 1.6.3 capability and architecture audit

Date: 2026-10-19

## Reference and boundary

The reviewed user-supplied package is
`Worobeyko-VikingsForHire-1.6.3.zip`, SHA-256
`9E400356509001D32641190D778A74A387CC15C6BCE696DAC506D0789DF60DA7`.
It contains the compiled `ValheimNpc.dll`, five small feature-gate DLLs,
documentation and package art. It does not contain source code or a license.

The public Nexus permissions identify Worobeyko as the author and require
permission to modify or reuse files/assets. No public source repository was
found. Living World may study runtime structure and interoperable behavior, but
must not ship decompiled source, copied assets, names, text or private data.
Every implementation below is an independently written Living World module over
Valheim/Jotunn interfaces and our own state contract.

## What the package demonstrates

VikingsForHire demonstrates that one Valheim human-shaped `Humanoid` plus
`MonsterAI` can support hiring, orders, combat roles, inventory, work, sleep,
doors, settlements, travel and recovery. Its strongest combat rule is also the
smallest: peaceful companion movement immediately yields when native
`MonsterAI` has a creature target. Native AI then owns pursuit, facing and
attacks. This is the boundary restored in Living World 0.3.56.

The five module DLLs are feature gates; the implementation is concentrated in
the core assembly. The principal NPC component is very large (over 1,400
methods and 1,000 fields). That breadth is useful as a capability roadmap, but
not as an architecture to reproduce. Living World will keep each optional
capability behind a narrow state/module contract.

## Mechanisms worth independently reproducing

- owner-authoritative ZDO state with remote clients limited to replication;
- a coarse decision clock, with visual/animation refresh separated from AI work;
- an explicit priority chain for downed, hostile, hired, working and wandering
  states;
- native combat ownership whenever a valid target exists;
- bounded additive defense reactions, healer support and role equipment;
- follow catch-up, portal, dungeon, ship and mounted travel adapters;
- door route memory, opening on approach and closing after passage;
- settlement route memory that invalidates when doors or structures change;
- real inventory, equipment comparison, finite ammunition and loot containers;
- beds, seats, workstation jobs and real tool use;
- downed/revive/grave recovery;
- radial controller orders with contextual commands;
- performance tiers and fault containment scoped to an individual NPC.

## Mechanisms not to reproduce as-is

- one monolithic actor class containing every behavior;
- fixed formation indexing as the long-term squad solution;
- global exception swallowing around all game AI;
- broad patches that change unrelated vanilla or third-party actors;
- any compiled asset, string, icon, source expression or package identity from
  VikingsForHire.

## Capability matrix and module destination

| Capability | VikingsForHire 1.6.3 | Living World direction |
| --- | --- | --- |
| Native target/pursuit/attack loop | Implemented | Core boundary corrected in 0.3.56 |
| Melee/ranged/tank/healer roles | Implemented | `CombatRoles`; preserve native selection, add only role constraints/support |
| Block/dodge response | Implemented | Existing `HumanCombatDefense`; tune from telemetry without targeting/movement ownership |
| Controller order wheel | Implemented | `CompanionOrdersUI`; fully gamepad-first, writes one command into state core |
| Follow/catch-up | Implemented | Existing formation leases/catch-up; expand travel recovery without teleport-by-default |
| Formation | Column/wedge/ring, indexed | Dynamic leases and sector spacing; companions may take any valid open position |
| Guard sectors | Basic guard modes | Existing short-lived opposing-sector leases are a Living World differentiator |
| Awareness presentation | Combat status/order UI | Existing curiosity/alarm markers and sector-facing behavior; retain outside combat |
| Torch behavior | Implemented | Existing darkness/light/clear-delay state; prohibit only after full combat commitment |
| Wet/status embodiment | Not the central advertised model | Existing replicated wet/drip/resource embodiment remains a differentiator |
| Doors | Open/close and route memory | Existing door state machine; add route memory and blocked/dead-end learning module |
| Portals/dungeons | Implemented | `CompanionTravel`; explicit ownership handoff and regroup recovery |
| Ships/horses | Implemented | Later `CompanionTravel` adapters, isolated from land follow/combat |
| Inventory/equipment | Inspect/equip/upgrade | `CompanionInventory`; real container UI, compare armor/weapons, drop replaced gear |
| Loot/chop/break/hunt | Implemented | `CompanionTasks`; explicit directive leases, never ordinary Guard acquisition |
| Workstations/jobs | Implemented | `ResidentWork`; real tool/effect where safe, animation-only where game API has no transaction |
| Beds/sleep/furniture | Implemented | `ResidentComfort`; owned dwelling/bed, seat, rest and wake schedules |
| Settlement navigation | Internal nav map/trails | `SettlementNavigation`; structure/door graph plus native pathfinding executor |
| Traders/relations/factions | Implemented | `ResidentSocial` and `Faction`; story-owned reputation and lawful transaction adapters |
| Downed/revive/graves | Implemented | `CompanionRecovery`; healable/savable friends before permanent death policy |
| Wandering recruits/names | Implemented | `PopulationIdentity`; seeded robust names, homes, roles and persistence |
| Hostile human variants | Supported by role/faction system | `LivingWorldEnemies`; same actor core, enemy faction/state modules and authorized armor packs |
| Performance controls | Several modes | Central tick scheduler, distance tiers and per-module budgets |
| Failure isolation | Broad update finalizers | Per-NPC/per-module circuit breakers; never swallow unrelated global AI faults |

## Required state/module architecture

One actor owns identity, networking, body, inventory and embodiment. Exactly one
top-level state owns decisions at a time:

1. incapacitated/downed;
2. combat;
3. combat clear and regroup;
4. ordered traversal (follow/hold/patrol/hunt/task);
5. town resident;
6. peaceful ambient/comfort.

Combat is never a collection of votes. When a valid native target exists,
combat disables traversal formation, leader gaze, ambient work, torch, peaceful
door-closing and posture mimicry. Role modules may offer bounded combat support
but may not replace native target, movement, facing or attack cadence.

Town proximity and maypole/town bounds select the resident module only when the
actor is not downed, in combat or under an explicit traversal order. Leaving
town or receiving Follow removes resident work leases and activates the
companion traversal module. Both modes share identity, equipment, resources and
status effects, not concurrent behavior loops.

## Delivery sequence

1. Accept 0.3.56 native combat ownership against a natural greydwarf raid and
   the bounded Pillager/Krauguk events.
2. Add combat-role profiles without adding a second combat controller.
3. Add controller order wheel over the existing state contract.
4. Add real companion inventory/equipment and finite supplies.
5. Add travel adapters and persistent door/route memory.
6. Add resident comfort: seats, beds, rest, wake and owned dwelling.
7. Add resident work and settlement navigation.
8. Add downed/revive/grave recovery.
9. Add story faction/reputation/trader modules.
10. Instantiate friendly and hostile Living World populations from the same
    actor core, including climate-specific small forest/northern/desert peoples
    using only Valheim or explicitly authorized asset packs.

Each step requires offline contracts, a live telemetry acceptance scenario and
a regression check for every already accepted state before the next module is
enabled by default.
