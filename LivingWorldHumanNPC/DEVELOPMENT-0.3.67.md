# Human NPC 0.3.67 — native facial-hair attachment completion

## Live failure accepted

The 0.3.66 run on 2026-10-21 loaded the intended DLL but all three profiled
humans remained bald. Their owner ZDO selection transaction completed, while
`m_currentHairItemHash` remained zero until the ten-second readiness deadline.
`lwparty` correctly refused those incomplete actors, leaving them inert.

## Narrow correction

Fresh authoritative appearance initialization still uses Valheim's normal
model, color, hair and beard setters and persists the native ZDO hashes. After
the ordinary `UpdateVisuals` pass, it now completes presentation through
Valheim 1.0.20's own `SetHairEquipped` and `SetBeardEquipped` methods, followed
by the native LOD and color updates. No replacement mesh, prefab, name, combat,
follow, formation, torch, stow, wetness, door, ambient, workstation, cleric,
raid or pathfinding logic changed.

The visual revision remains uncommitted until the selected native hashes and
rendered attachments are both verified. Therefore the two saved 0.3.66 actors
remain eligible for this same repair on the next load.

## Evidence boundary

The managed harness now reproduces the observed condition in which an ordinary
visual refresh reads the selections but does not create either attachment. It
passes only when both native equip calls complete. Static checks also verify
that Valheim 1.0.20's two equip methods terminate in its own `AttachItem` path.
The build remains a lab candidate until a live restart proves visible hair,
beard rules, two-member party recovery and follow behavior.
