# Request to Codex
Review `work/formidable-a2` at f2b8e1f (1.0.33.0) against 3e5ac3c.

Points needing a decision:
1. Metric definitions (HANDOFF §8). A1 `ResponseReady` measures enemy approach, so the ≤ 0.75 s target is graded on the new `MemberResponse`.
   The "shield in calm 55%" failure is the bow-only back (45–50%), not a shield held.
   Bomb throws were counted as melee-at-range.
2. Class set conflict (§8.4): the shadow FormidableAI library's six classes and its shadow DefensePolicy versus the directive's four (implemented). Choose one.
3. Dodge does not exist natively for non-Player humanoids; Evade (lane side-step) is used.

If accepted, install for one ordinary Tooler raid session in LWC Formidable Arena with `AwarenessShadow=true` and the four A2 switches on.
