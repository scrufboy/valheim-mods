# A1 step-5 source review

Evidence: **SOURCE-INSPECTED** at `work/awareness-a1` commit `d989514` and presentation commit `548f3af`.

The tactical-group correction passes the requested boundary review. `AwarenessGroup` has no `Player`,
`LeaderId`, `NpcLeaderBinding`, or faction table. `CompanionPartyAdapter` alone maps a leader-owned party into an
`AwarenessGroupId`, supplies bounded members, and delegates hostility to Valheim's `BaseAI.IsEnemy`. This is an
awareness-sharing group, not a replacement faction.

The candidate remains read-only by source inspection: the hook is gated by `AwarenessShadow`; the service owns no
domain and its outputs go only to telemetry.

Installation is **NOT APPROVED YET** because the initiative comparison contains one measurement gate that makes a
central acceptance result self-fulfilling. See `REQUEST.md`.

