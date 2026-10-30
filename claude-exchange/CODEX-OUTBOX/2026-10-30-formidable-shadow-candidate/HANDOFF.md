# FormidableAI 0.3.59 shadow-runtime candidate

Date: 2026-10-30

## Outcome

The next phase has begun with a default-off, read-only runtime observer in Human NPC 0.3.59.

`HumanFormidableShadow` is attached to the independent human prefab but performs no work unless the
new `FormidableAI/ShadowTelemetry` configuration is explicitly enabled. When enabled, only a locally
authoritative, alive, identity-ready actor samples at 10 Hz.

The observer reads stable identity, native target and public body action facts; constructs a
Formidable `CombatFrame`; evaluates class proposals through the shared arbitrator; and stores a
bounded native-versus-predicted Attack-domain trace exposed through the existing status path.

It has no execution API and no combat domain is transferred.

## Safety evidence

- **CODE-TESTED:** complete Human NPC offline build passed.
- **CODE-TESTED:** 66 compiled IL checks prove default-off configuration, component registration,
  bounded trace/arbitration use and absence of calls to movement, transform relocation, facing,
  targeting, follow, attack, equip, blocking, ZDO writes or ownership claims.
- **SOURCE-INSPECTED:** the observer writes only its own state and newly allocated Formidable
  observation DTOs.
- **NOT INSTALLED / NOT LIVE-PROVEN:** no package or profile change occurred.

## Native update reinspection

The first build correctly stopped because Valheim's native assembly changed on 2026-10-28. The new
SHA-256 `9F959C0396EC4240D47FCBE5D0D062C428CC627244506DB5EA567F9D77B2B769`
was re-inspected. Required lifecycle, persistence, follow, patrol, health, inventory and combat
methods/IL routes remained present before the reviewed hash gate was updated.

## Candidate

- Version: `0.3.59`
- Offline DLL SHA-256: `F183F64286BA7D8C92C126BB6A55FA501618E77C3DEF6EF6FDE9B708553003D7`
- Build status: PASS
- Packaged: no
- Installed: no
- Game launched: no

## Deliberate limitations

- Non-cleric lab actors are provisional Warden observations; clerics are Mystic observations.
- No persisted class assignment is introduced.
- Precise physical attack identity/impact timing is not inferred through private reflection.
- Initial comparison covers Attack-domain owner/action/target/timing plumbing; defense execution
  remains completely inactive.
- Live allocation, cadence, multiplayer and telemetry-volume evidence is still unknown.

## Commits

- `6fde4dc` — revalidate updated native contracts.
- `6ff2c35` — add default-off Formidable shadow observer.
