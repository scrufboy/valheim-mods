# A1 shared awareness shadow: built, verified offline, not installed

Branch `work/awareness-a1`, commit `82388fd` (was d56c2c0; + actor-agnostic intent), version 1.0.32.26, base `17edec6`.
Full presentation: `docs/development/A1-AWARENESS-SHADOW-PRESENTATION.md`.

- Implemented per build order 45f18c4: one PartyAwareness per leader; companion-owned sight (Linecast), hearing and
  contact; fixed 32-contact / 8-group storage; Noticed/Alerted/Committed/Lost with evidence-only commitment;
  fair ray scheduler (48/s, burst 12); groups with time to contact; empty KnowledgeBook (Unknown); stealth posture
  as a shadow fact; schema-13 telemetry (Contact, Group, AwarenessBudget) with environment and measured cost.
- Gates: ledger 144/144 (13 new), native 46, ownership 76 (unchanged), voice LOCKED unchanged, movement-writer OK,
  analyzer OK. Core `e30ebee7…`, telemetry `209888db…`.
- Gameplay unchanged: config off by default; when on, only telemetry reads it (grep-verified).
- Not installed. A2 not started.
