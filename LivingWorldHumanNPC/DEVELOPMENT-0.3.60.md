# Human NPC 0.3.60 — encounter and presentation contracts

## Live failure

Fresh 0.3.59 companions entered calm follow with `ShieldWood` and
`SwordBronze` visible while no encounter was authorized. When nighttime torch
logic ran, native torch equip replaced only the sword, leaving
`ShieldWood + Torch` visible and clearing both back slots. Telemetry proved the
sequence for both party members.

The same fresh companions had non-empty generated appearance records but were
visibly bald and beardless. The reported catalog contained 87 "hair" entries,
which proved the generator was searching every loaded mod customization item
rather than using Valheim's actual Player facial-hair catalog.

During the first-raid test, target handoff repeatedly released individual
companions to social/peaceful behavior while the encounter was still active.
The archer and cleric changed target authorization 24 and 39 times, separated
by up to 26.1 metres, and the archer died during cleanup twelve seconds after
the raid banner ended.

## Cause

`ClaimsPeacefulHands` treated torch *eligibility* as completed ownership. That
made readiness skip its peaceful stow before the torch existed. Native
`EquipItem(torch)` then inherited the visible shield and cleared the hidden
combat slots.

Combat release accumulated its clear scans during the same short retention
timer used for target handoff. When that old timer expired, a target gap could
immediately release presentation and follow systems. Encounter ownership was
also actor-local, so one engaged companion could not keep the other in the
same fire-team state.

## Correction

- A darkness request no longer owns the hands.
- Readiness must first complete and verify the native sword/shield back stow.
- The torch may claim peaceful hands only while it is genuinely visible, no
  combat item is visible, and the combat pair remains in the hidden/back slots.
- Because native torch equip clears those hidden references, the adapter
  restores the same actor-owned sword/shield references directly to the back
  presentation without equipping either weapon.
- Combat or leader readiness still removes the torch before showing weapons.
- Ambient hand use still suspends torch ownership through the existing posture
  boundary.
- Fresh identity now reads the native Player hair/beard prefix and count, then
  validates every exact `prefix + index` prefab and its visual attachment.
  MadHatter, Runic and other customization items can no longer enter the native
  facial-hair lottery merely because a broad search matched their names.
- Appearance initialization does not commit or enable the NPC until the chosen
  hair hash has instantiated a real visual object; a selected beard must do the
  same. The exact chosen asset names are reported when initialization succeeds.
- A live target, damage, or an alerted sensed hostile now shares encounter
  evidence across companions bound to the same owner.
- Losing or retiring one target retains the party encounter; it cannot enter
  social, guard, torch, workstation or peaceful-stow behavior.
- Four consecutive party-clear scans establish only an all-clear candidate.
  Five additional uninterrupted clear seconds are required before the party
  returns to peaceful behavior.
- A targetless companion inside an owned encounter follows back toward the
  leader while remaining armed. It does not run ambient formation logic and
  does not start a second distant hunt.
- Recovery-defense completion retires the local target without ending the
  party encounter.

No name generation, native weapon selection, attack cadence, doors, wetness,
resources, town/workstation stow path, casual-guard behavior or ambient
scheduling was changed. Combat still delegates targeting, facing, weapon
choice, attacks and blocks to native MonsterAI/Humanoid after authorization.

## Live acceptance

In calm nighttime follow, telemetry must show `right=Torch`, no visible combat
item, `stowedLeft=ShieldWood`, and `stowedRight=SwordBronze`. Daylight or a lit
shelter must leave both hands empty with the same verified back presentation.
Combat must remove the torch and show the authorized combat loadout once.

Fresh replacements must report the bounded native Player catalog and a final
initialization line containing concrete `hair=` and `beard=` values. Both
selected visuals must be visibly attached; an attachment failure blocks that
actor instead of silently accepting a bald companion.

During a full first raid, neither companion may report social/peaceful or show
a torch while any party member has a target or an alerted sensed hostile
remains. A targetless separated member must move back toward the leader armed.
After the final hostile is gone, peaceful stow may occur only after four clear
scans and five further uninterrupted seconds.

## Installed candidate

- Installed in the `LWC Human Lab v1` profile after confirming Valheim was not
  running.
- Installed DLL SHA-256:
  `B7414AD9BD7B1BC4DF7E0A2AE6EC3B1A223373489954961455E6141AEB3AA652`.
- The active 0.3.59 DLL and its start document were preserved under
  `BepInEx\LivingWorldBackups\HumanNpc\20261020-pre-0.3.60`.
- Offline verification passed; Unity combat, appearance and save/reload remain
  pending live acceptance.
