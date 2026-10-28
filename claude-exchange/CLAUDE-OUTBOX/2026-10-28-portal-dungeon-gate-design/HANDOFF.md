# Portal and dungeon traversal gate: native facts and narrow design

Date: 2026-10-28. Answers work-order step 5 and gate
`design/lwc-design-archive:docs/design-archive/03-TRAVERSAL-TRANSITION-GATE.md`.
Owner trigger: "I walked into a troll cave and my guys aren't with me."

Game build inspected: Valheim **1.0.20** (`Version.CurrentVersion`), from Brian's
`assembly_valheim.dll`, decompiled read-only. No game, profile or DLL was changed.

## 1. Native facts (all SOURCE-INSPECTED, Valheim 1.0.20)

1. **Only the local player ever uses a door or portal.** Both triggers
   (`Teleport.OnTriggerEnter` for cave/dungeon doors, `TeleportWorldTrigger.OnTriggerEnter`
   for portals) act only when the collider is `Player.m_localPlayer`. No NPC,
   tame or companion is ever carried by the game.
2. **NPCs cannot teleport natively.** `Character.TeleportTo` returns `false`;
   only `Player` overrides it. A companion must be moved by our own code.
3. **One choke point covers both cases.** Doors call
   `character.TeleportTo(target, rot, distantTeleport: false)`; portals call
   `player.TeleportTo(pos, rot, distantTeleport: true)`. A postfix on
   `Player.TeleportTo` with `__result == true` for the local player sees every
   legal transition, after the game's own rules have already approved it
   (NoPortals, NoBossPortals, boss-dungeon lock, item check, 2 s cooldown).
4. **Player timeline.** At `TeleportTo` the player only records the target. At
   2 s, `UpdateTeleport` moves the player. Teleporting ends when the target area is
   ready and `ZoneSystem.FindFloor` succeeds. A door with no floor rolls the
   player back with `$msg_portal_blocked`; a portal waits up to 15 s, then drops
   the player to solid height.
5. **Dungeon interiors sit in the same zone, 5,000 m up.** `Location` builds the
   interior at the entrance's x/z with y + 5000. The active area is computed from
   x/z zones, so companions **stay loaded** when the player enters a troll cave.
   They are simply left 5 km below with no path, which is the bug Brian saw.
6. **Portals change the active area.** After the player moves, the source zone
   falls outside the active area: `ZNetScene.RemoveObjects` destroys the companion
   instance (its persistent ZDO survives), and `ZDOMan.ReleaseNearbyZDOS` drops
   ownership of persistent ZDOs left behind. Without help, companions are frozen
   at the source portal until someone returns.
7. **A ZDO can be moved safely by its owner.** `ZDO.SetPosition` updates the
   sector and data revision, so it replicates. `ZSyncTransform.SyncNow` pushes a
   moved transform into the ZDO immediately.
8. **Portal item rule.** `TeleportWorld.Teleport` refuses a player whose inventory
   fails `Humanoid.IsTeleportable(m_allowAllItems)`. The same method exists on
   any `Humanoid`, so it can be applied to each companion's own inventory.

## 2. Design

One module, `CompanionTransit`, owning one transition at a time. It adds no
movement controller: it only places actors, then hands them to the existing
regroup/formation path and native combat.

### Eligibility (checked at the `TeleportTo` postfix)

- Active expedition companion bound to this leader, order Follow, alive, owned
  by this peer, within 20 m (PROPOSED) of the player, and not in committed combat
  with a target within 8 m (PROPOSED).
- Portals only: the companion's own inventory passes `IsTeleportable(portal.m_allowAllItems)`.
- Residents, Hold/guard actors, camp NPCs and Tooler fixtures never transfer.
- An ineligible companion stays where it is and says why once (truthful refusal).

### Door into or out of a cave or dungeon (`distantTeleport == false`)

1. Record eligible companion ZDOIDs and the target pose.
2. Wait until the player's teleport completes (`IsTeleporting()` false). If the
   player was rolled back (position back near the source), cancel: nobody moves.
3. Place each companion at a staggered point behind the target: 1.5 m and 3 m
   back from the target pose, offset left/right, each verified with
   `ZoneSystem.FindFloor` and a short capsule overlap check (no walls, no player).
4. Set transform and body velocity to zero, `ZSyncTransform.SyncNow()`, clear the
   pathing target, then release to regroup. Same instance, same identity: there
   is no despawn or respawn.

### Portal (`distantTeleport == true`)

1. Record eligible companions. Immediately set each one's transform and ZDO
   position to a staggered point near the destination exit, then `SyncNow()`.
   The instance is then outside the active area, so `ZNetScene` destroys it while
   the persistent ZDO keeps its new position and stays owned by this peer
   (ownership release only scans ZDOs near the player).
2. When the player arrives, the destination area loads and `ZNetScene`
   recreates the companion from the same ZDO: same ZDOID, same saved identity.
3. After the player's teleport completes, verify each recreated companion's
   floor. If one has no floor, move it to the player's solid height plus 0.5 m at
   its staggered offset. Then release to regroup.
4. Duplicate guard: a companion is "in transit" (ZDO flag with a transit
   sequence number) from step 1 until it is seen alive at the destination. Any
   second request for the same ZDOID with a stale sequence is ignored.

### Edge cases (the gate's required list)

| Case | Rule |
| --- | --- |
| Companion in committed combat | Stays and fights; regroups through the door/portal is not attempted; player gets one line ("Ulf is still fighting") |
| Dead or incapacitated | Never transferred; mortality stays off by design |
| Too far | Not transferred; may follow manually later; no teleport-by-default |
| Player rolled back by `$msg_portal_blocked` | Cancel; nobody moved |
| Rapid re-entry | One transition at a time; a new request waits for the previous one to finish |
| Disconnect mid-transit | Companion ZDO already sits at the destination; it loads there next time the area is active |
| Save/restart | Same as disconnect; transit flag cleared when seen alive |
| Remote client leader | Only the peer that owns the companion moves it; if the leader does not own it, it sends one RPC to the owner with the target pose |
| Dedicated server | Same ownership rule; no server-only code |
| Boss dungeon locked | The game refuses the player first; our postfix never fires |
| Full inventory / restricted items | Portal refusal per companion, as above |

### Prerequisites and limits

- **Persistence.** 1.0.x saves identity, leader binding and kit tier only.
  Inventory beyond the kit is not saved, so a portal (which recreates the
  instance) would lose loaned items. Door transitions keep the same instance and
  lose nothing. Portal transfer should ship with, or after, inventory persistence;
  until then it can be enabled for kit-only companions.
- **Kernel re-entry.** A recreated companion must rebind to its leader from the
  saved `lwc_human_v2_leader` key before its first tick. Verify this path exists
  in 1.0.x.

## 3. Proposed acceptance matrix

Solo host first, then remote client and dedicated server:
- troll cave in and out; burial chamber; sunken crypt; frost cave; infested mine;
- portal round trip; portal with ore (refusal); portal with one companion far;
- rapid in/out x5; save and quit inside a cave; reload;
- telemetry per transition: request, eligible ids, refusal reasons, placement
  points, floor check, recreated ZDOID equals original, regroup release.

## 4. Unknowns

- UNKNOWN: whether 1.0.x companions keep `ZSyncTransform` and a persistent
  `ZNetView` as the Player-clone prefab suggests.
- UNKNOWN: whether instance recreation at the destination re-runs the 1.0.x
  factory's fresh-spawn path (must not reroll identity).
- UNKNOWN: interior floor readiness timing in each dungeon type; live telemetry decides.
