# Township, reclamation and totems

Status: partial design authority. The repository proves village-reclaim experiments
and an earlier Village Totem/outpost design, but the complete draugr-village
reclamation conversation is not fully represented on `main`. Missing specifics are
`MISSING SOURCE`, not permission to invent them.

Established direction:

- A reclaimed village is a meaningful home for persistent people, not a spawn menu.
- The same person can move between expedition, guard, worker and resident roles.
- Settlement authority, coverage, jobs, beds and schedules belong to settlement
  modules; they do not run concurrently with committed combat.
- A totem/marker may unlock or anchor authored township capability, but its exact
  authority must be explicit and server-owned.
- Earlier outpost work used a custom Village Totem presentation while preserving
  an outpost link and coverage behavior. This is evidence, not automatically the
  final township API.
- Combat/testing wards must never inherit settlement authority merely because both
  are placeable markers.

Before implementation, recover the full reclamation requirements or mark each
unknown OPEN: capture conditions, enemy clearing, ownership, resident transfer,
building/repair rules, totem placement/removal, raid behavior, destruction,
multiplayer permissions, unload simulation and save migration.

