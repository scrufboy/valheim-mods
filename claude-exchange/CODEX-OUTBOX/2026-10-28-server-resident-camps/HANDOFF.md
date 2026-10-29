# Claude handoff: selected server-resident camps

Date: 2026-10-28. This is an additive correction to the two earlier companion-encounter submissions. It does not alter their historical evidence.

- SOURCE-INSPECTED: Jotunn documents 64 m zones and says persistent `ZNetView` objects are saved and instantiated when their zone loads. Persistence alone is not continuous actor AI: https://valheim-modding.github.io/Jotunn/tutorials/zones.html
- SOURCE-INSPECTED: The previous `CampLifecyclePolicy.Observe` always requested despawn when no player was near. The earlier arc therefore did not satisfy Brian's selected always-active dedicated-server areas.
- OFFLINE-TESTED: `CampState.ServerResident` now permits bounded camp routines without a nearby player only when the adapter supplies loaded zone, actual retained zone, existing actor, authoritative owner, and enforced away safety. It still refuses an unwitnessed death transaction.
- UNKNOWN: The maintained 1.0.x runtime has no wired zone-retention or away-safety adapter from this submission. Do not call the feature live.

## Implement next in the maintained core

1. Persist an authored resident-camp designation and finite server budget; retain only chosen camp zones while the dedicated server is running.
2. Prove `ServerZoneRetained` from the actual zone system and `AwaySafetyEnforced` from real prevention of unwitnessed native damage/death. A saved ZDO or config flag cannot supply either proof.
3. Keep one owned actor per stable `PersonId`; preserve identity, equipment, memory, camp assignment, and routine epoch across zone, server, and player changes.
4. Run the ordinary watch/cook/rest/repair/patrol schedule in retained areas. Combat may preempt only under the native owner contract. Decide and test the away-combat policy before allowing hostiles into unattended retained camps.
5. On server shutdown, failed retention, safety failure, or budget exhaustion, checkpoint and release the actor without recording death. On re-entry, reconstruct the same person.
6. Test with zero, one, and two players; server restart; zone transfer; native creature and third-party creature contact; duplicate actor prevention; and resource impact.

The design goal is a living resident camp while the dedicated server runs. This patch only adds the decision seam and written requirement.
