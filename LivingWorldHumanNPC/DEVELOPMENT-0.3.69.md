# Human NPC 0.3.69 — native human startup order

## Live failure accepted

The 0.3.68 live run created bald, unusable actors. One actor falsely crossed
the appearance boundary, then failed behavior installation because the stamina
configuration existed only as nonserialized private prefab state. The other
actor was rejected when a customization attachment introduced an additional
Animator beneath the visual hierarchy.

## Root correction

The fresh-spawn transaction now completes in this order:

1. allocate the native Player-clone human graph;
2. persist one immutable random human profile and name;
3. stage the public native model, skin, hair color, hair, and beard selections
   synchronously before `VisEquipment.Start`;
4. let Valheim's own visual startup create and register the attachments;
5. verify the selected native attachment objects and renderers;
6. install the unchanged companion modules and enable native AI last.

The late private repair path has been removed. Living World no longer invokes
`SetHairEquipped`, `SetBeardEquipped`, `UpdateVisuals`, `UpdateLodgroup`, or
`UpdateColors` through reflection. This matches the successful construction
sequence independently confirmed in Vikings for Hire: public appearance
selection occurs during the actor's startup window and native Valheim owns the
rendering pass.

The behavior installer's prefab-configured stamina values now carry explicit
`SerializeField` metadata so spawned and reloaded actors retain them. Runtime
validation still requires one native body Animator and CharacterAnimEvent
receiver, but no longer misclassifies an Animator owned by a valid attached
customization object as another body.

One `lwparty` command still allocates both missing people in one pass and issues
no Follow order until both independently cross the native-human and behavior
boundaries. Combat, follow, formation, torch, stow, wetness, doors, ambient,
workstation, cleric, raid, and pathfinding implementations are unchanged.

## Evidence boundary

Offline tests verify the transaction order, public-only appearance selection,
serialized installer state, two-person party boundary, and preservation of all
existing behavior modules. Visible hair/beard and live companion behavior still
require the next Unity playtest.
