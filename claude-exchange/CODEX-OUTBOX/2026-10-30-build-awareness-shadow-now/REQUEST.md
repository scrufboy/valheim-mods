# Build and present A1 shared awareness now

Brian has approved the corrected tactical direction. Stop returning the settled doctrine as preference questions.

Build **A1 shadow awareness** from the corrected design at `8e89866` now. This first slice is read-only and must
not change targeting, phases, movement, hands, look, combat, bombs, bows or voice.

## Required implementation

1. One bounded `PartyAwareness` service per actual companion party/leader.
2. Companion-owned perception only. Brian's camera, facing and line of sight are not inputs and consume no slot.
3. Fixed-capacity reusable contact and group storage; no growing histories and no per-tick collections.
4. Fair sight, hearing and contact sources; explicit occlusion and field-of-regard rules.
5. Contact states and evidence: Noticed, Alerted, Committed, Lost; last seen, visible motion, closing evidence,
   confidence/staleness and source.
6. Bounded party scan/LOS budget with fair observer scheduling and measured cost.
7. Grouping, group motion and time-to-contact estimates.
8. Learned susceptibility only: do not expose native poison modifiers as tactical knowledge unless the party's
   training/lore/experience knowledge permits it. `Unknown` must remain representable.
9. Telemetry for transitions, groups, observer/source, environment, ray budget, scan cost and dropped contacts.
10. Pure tests for transitions, commitment, decommitment, occlusion/staleness, grouping, capacity/eviction,
    fair scheduling, sneaking doctrine and unknown resistance.

## Existing behavior must remain unchanged

- Bomb behavior stays at the corrected intermediate `17edec6` behavior: no Calm/Readiness acquisition.
- No contact record may drive the arbiter yet.
- No new native writer and no domain ownership.
- Combat-voice lock and movement-writer lock remain unchanged.

## Required presentation back to Brian and ChatGPT

Do not merely say "built." Present:

- branch and exact commit;
- concise diagram of sense -> contact -> interpretation -> group -> telemetry;
- exact implemented ranges, field-of-regard, scan cadence, ray budget, capacities and staleness values;
- the source of every fact and which facts remain unknown;
- test/gate results and DLL hashes;
- measured allocation/CPU/ray cost or, if live measurement is impossible offline, exactly what telemetry will
  measure in the first session;
- sample telemetry showing one contact progressing through the states;
- an explicit statement that gameplay is unchanged;
- the exact video test Brian should run and what evidence will accept or reject A1.

Push the implementation and the presentation to GitHub, then notify the exchange. Do not install it and do not
advance to A2 without review. If a real implementation blocker exists, report the source evidence and the
smallest required decision; do not replace implementation work with broad design questions.
