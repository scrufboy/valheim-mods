# Human NPC 0.3.50 follow ownership and recovery

## Runtime failures established by 0.3.49 telemetry

- Both companions could stop more than 20 metres behind the leader while their reported pace remained `Run`.
- A peaceful leader-readiness weapon selection could keep native `MonsterAI.UpdateAI` from reaching its own `Follow` branch.
- Both actors stopped at shoreline elevations while the configured humanoid AI explicitly rejected water paths.
- A utility torch briefly appeared as native combat selection.
- A shield alone was accepted as combat-ready, leaving no offensive weapon in hand.

## Corrections

- Native attack selection returns no attack item outside authorized combat. Leader readiness remains visible presentation but cannot take movement ownership away from peaceful follow.
- Authorized combat still uses native `EquipBestWeapon`; the adapter does not choose a weapon or run a parallel combat controller.
- Utility torches are rejected from combat selection and their actor-local shared data is made AI-ineligible.
- Readiness now requires an offensive weapon. Shield-only state restores the persistent sword/shield pair.
- Humanoid companions can use native swimming routes instead of rejecting water.
- At twelve metres, a dedicated leader catch-up state releases optional ambience and formation leases, runs toward the leader through native `MoveTo`, and invalidates a failed cached path at a bounded interval. It never teleports or targets the player as an enemy.

## Preserved behavior

Combat authorization, Souls of the Land-style native combat, wetness, doors, stowing, leader crouch, torch darkness rules, party creation, and ambient/formation behavior inside the catch-up release distance remain intact.

## Required runtime evidence

1. `lwparty` creates the complete party and status reports 0.3.50.
2. Companions keep following while the leader's weapons are visibly drawn but no hostile is authorized.
3. Separating past twelve metres produces catch-up rather than a stationary `Run` state.
4. Companions enter water, swim, exit, and re-equip appropriately.
5. Torches never become a selected combat weapon; committed combat restores an offensive weapon.
