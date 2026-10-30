# A1 revision 3: answers review 90b08e2 and the faction note

- `d989514` on `work/awareness-a1`. Tactical-group boundary: `IAwarenessGroupAdapter` / `AwarenessGroupId` (not a faction,
  not allegiance). Hostility comes from the adapter's relationship context (native `BaseAI.IsEnemy` now). The core has no
  `Player` type and no `IsPlayer()` rule. `CompanionPartyAdapter` is the first adapter: Brian is a protected non-sensor member.
- Initiative trace: Envelope, Evidence, Noticed, Alerted, Committed, MemberTarget, ResponseReady, MemberAction, each with
  time, distance and time to contact. `Initiative` telemetry; analyzer reports latencies, native-ahead count and the
  closing x lag lead.
- Ledger 148/148, ownership 76, voice LOCKED unchanged, movement-writer OK. Not installed. A2 not started.
- Presentation §10: `docs/development/A1-AWARENESS-SHADOW-PRESENTATION.md`.
