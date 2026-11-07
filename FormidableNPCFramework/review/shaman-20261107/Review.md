# Independent shaman source review — 2026-11-07

Reviewed `src/ShamanControlPolicy.cs` and `tests/ShamanControlTests.cs` without editing them. `ReviewReproductions.cs` compiles against the same three kernel files and demonstrates the following behavior. These checks establish source-policy behavior only. They do not establish native actors, animation, visual effects, faction changes, spawning, or multiplayer gameplay integration.

## Findings

1. **Stale beast identity can block recovery permanently.** Bind beast A, then supply an owner snapshot where the removed beast has `BeastIdentity=null` and `BeastAlive=false`. The policy releases its link but retains stored identity A because removal cleanup requires the incoming identity to equal A. A subsequent valid live beast B cannot bind because the policy requires stored identity to be empty or equal B. A production adapter could avoid this by retaining A's identity in an explicit death/removal observation, but that is an undocumented input requirement. Prefer an explicit observation of the recorded beast's status; do not clear a genuinely living, temporarily unobserved A merely because a candidate changes.

2. **One beast summon attempt is not represented in state.** After one beast is summoned, bound, and dies, the same shaman can produce another `SummonBeast` when `BeastSummonReady=true`. `BeastSummonReserved` only prevents concurrent pending requests; it does not record a completed attempt. If one attempt per encounter is the doctrine, persist an encounter-scoped attempt receipt/latch. A ready/cooldown flag alone does not express this guarantee. Attempt recording belongs at authoritative reservation/action acceptance, rather than consuming an attempt just because a proposal was evaluated.

3. **Proposal boundary permits ranged Attack for dead/uncommitted combat frames.** A matching sequence/actor decision with `Mode=Support` and `Action=RangedBlast` acquires Attack even if `frame.HealthFraction=0` and `frame.CombatCommitted=false`. The neighboring dart module rejects both cases. Add a finite/positive health guard for all shaman proposals and a combat-commitment guard for offensive ranged blasts. Support actions outside committed combat may remain intentional.

## Required integration contracts

- Retreat leases Movement only. Attack falls back to NativeMonsterAI; the policy cannot guarantee no melee by itself. A ranged-only loadout or explicit native executor restriction must enforce that requirement.
- Two owners/shamans can both observe available minion capacity or an unreserved beast and both propose a spawn. The existing reservation fields help only when native action acceptance atomically reserves capacity/claim before instantiation, counts pending reservations, and makes retries idempotent. The policy does not implement such transactions.
- `BindingRevision` rejects receipts from a canceled prior bind attempt in the existing damage/recovery tests. Receipt provenance must additionally be tied by the adapter to actor, beast, encounter, and authority epoch; the fact type only carries a revision. Do not treat policy tests as evidence that network handoff or delayed native callbacks are safe.
- `ReleaseBeast` has no explicit beast identity in the decision. A native adapter must use the recorded beast identity and release only this controller's lease; it must not release whichever candidate happens to appear in current facts.
- Orange eyes/tether and berserk flags are intents. They require replicated native state, claim ownership, and target-selection handling. Healing is absent, consistent with the requested doctrine.

## Verification

Independent reproductions compiled with .NET Framework C# 5 (`/warnaserror+`) against the current source and all reproduced the reported behavior. The program intentionally asserts existing defects/contract gaps; it is a review harness, not a passing correctness suite. Rerunning it after fixes should fail the relevant reproduction assertion until converted into regression expectations.
