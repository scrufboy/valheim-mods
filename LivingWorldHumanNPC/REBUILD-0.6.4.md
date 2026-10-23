# Living World Human NPC 0.6.4

## Failed-run evidence

The 0.6.3 `army_bonemass` run lasted 155 seconds. Gunborg disappeared about
60 seconds after the event began and Aud about 66 seconds after it began.
Telemetry recorded zero dropped records and the log contained no Living World
exception, so this was a behavior failure rather than a loader failure.

Gunborg's first committed target was approximately 21.6 metres away while she
was only 4.2 metres from the leader and 4.0 metres from Aud. Nevertheless, she
entered with `SwordIron`, and the old target-to-leader boundary retired that
valid ranged encounter one second later. Her eight recorded native-combat
windows had a median duration of 1.27 seconds; seven lasted under three seconds.
Across both companions the controller executed nineteen boundary releases in
roughly the first 66 seconds of the raid.

The same run proved that 0.6.3's equipment presentation repair remained valid:
all observed logical and persistent hand/back slots matched.

## Correction

- Enemy distance no longer controls the party boundary. A ranged target may
  remain distant while its attacker stays with the party.
- A dedicated cohesion latch observes the actor's distance from the leader and
  nearest living party member. Only sustained separation enters one armed
  `CombatRegroup` return.
- The return uses distinct outer and inner boundaries. Native reacquisition is
  suppressed until the actor reaches the inner party area, preventing the old
  release/reacquire loop.
- Every newly committed target receives one immediate call to Valheim's own
  `Humanoid.EquipBestWeapon`. Living World does not choose a bow, sword, shield,
  attack, aim, or facing.
- Valheim's native target-search schedule is centrally calibrated from its
  two-second near and six-second far defaults to 0.5 and 2 seconds. The native
  `FindEnemy` and `UpdateTarget` logic remains unchanged; no parallel scan loop
  is added.
- `HumanEquipmentModule` has a native-owned posture for full combat,
  combat-clear, and combat-regroup. In those states it mirrors the exact native
  logical slots to presentation but never calls `ShowHandItems` or restores the
  previously stowed pair over MonsterAI's choice.
- Telemetry schema 9 records combat-regroup state, boundary incidents,
  suppressed reacquisitions, and read-only attack/bow-draw/block/stagger edges.

## Preserved milestones

The change does not modify the two-member `lwparty` transaction, identity,
random sex/hair/male-beard generation, inventory loadouts, health, native
Player-clone construction, formation leases, settled guard coverage, or calm
visible-back stowing.

## Verification and installation

- Foundation contract tests: PASS
- Human NPC SHA-256: `40A859D0FFBE9B96C84F6C7E5E7C98DED595AFF4ADB1FC5D245F2175F6A6BFEB`
- Telemetry SHA-256: `1FF47FD91DE9B74174E9ACCB4E13E8AE6FE847F12E0B406B75D8C30FF591EC63`
- Installed profile: `LWC Human Lab v1`
- Recoverable pre-install backup:
  `BepInEx/LivingWorldBackups/Rebuild/20261022-233330-pre-0.6.4`
- Live Unity raid acceptance: pending
