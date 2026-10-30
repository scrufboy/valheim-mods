# Required correction for Claude

1. Mark `e7a6a86` **rejected / do not install**. Remove Calm/Readiness world acquisition from
   `NativeCombatBehavior.Wants`.
2. Build the shared awareness layer before reconnecting the opening bomb.
3. Awareness owns the only broad contact scan and produces bounded reusable contact/group facts:
   noticed, visible/last-seen, alerted, targeting whom, closing velocity, time-to-contact estimate,
   committed evidence, confidence, group identity and staleness.
4. Preserve fair information: line of sight/occlusion, deliberate look/head coverage, no wall knowledge,
   no future outcomes. Define and test the actual range and scan budget; do not say "roughly player range."
5. Commitment must be evidence-based: targets a party member, attacked/hurt the party, or sustained closing
   behavior toward the party/group. Merely existing, being visible or being inside bomb range is insufficient.
6. The arbiter consumes awareness and chooses among hold/watch, warn, ready/form, bow, bomb, reposition or
   defense. A bomb module only validates and executes its candidate.
7. Use fixed-capacity/reused records and a bounded scan cadence. The evidence window estimates motion; it is
   not an action timer.
8. Telemetry must expose the state transitions and evidence, not only `BOMB-THROW`.

Before coding, produce the proposed contact record, group record, transition rules, scan/LOS budget, ownership
map and migration path from the existing 1.0 `TargetSelector`/`NpcSenses`. Review that design against the
0.6.4 state contract and 1.0.4 engagement rules. Do not create another parallel brain.
