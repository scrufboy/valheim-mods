# Encounter allegiance and first contact

This engine-free module decides how an unknown distant camp reads another actor
from server-verified facts. It does not move actors, choose attacks, reveal names
from proximity alone, or infer Living World allegiance from Valheim's native
`Character.Faction` enum.

## Contact contract

- The first presentation is silhouette, equipment, activity, and behavior.
- Notice is bilateral evidence: the record separately remembers whether the
  person noticed the player and the player noticed the person.
- A verified identity reveal requires both notice facts and is permanent. It
  cannot be replayed with a different name after a reload.
- Hearthless have a friendly authored disposition, unaffiliated people are
  neutral, and Pillager and Eitrherjar Legionary variants are hostile to the
  player. First sight uses those facts only to select posture; it leaves the
  witnessed relationship unknown and does not prove friendship or authorize an
  attack. A hostile lineage is not
  thereby recruitable; only a friendly Hearthless record passes this module's
  narrow allegiance gate. Full recruitment still belongs to encounter policy.
- Raised weapons and boundary violations change readiness. Witnessed aid, theft,
  authored faction grievance, and committed attacks change relationship. An
  unwitnessed act cannot create omniscient hostility.
- Once an attack commits, `YieldToNativeCombat` tells the runtime to surrender
  movement, facing, targeting, and attack execution to native combat. Peaceful
  signals similarly yield presentation control and never pull a committed actor
  out of combat by themselves.

`AllegianceMatrix` distinguishes player, Hearthbound, Hearthless/unaffiliated
humans, Pillagers, Eitrherjar, Fulings, draugr, wildlife, and other-mod actors.
Other mods require an explicit adapter identity; an absent adapter ID fails to a
neutral disposition, not a guessed native-faction alliance.

Accepted-request travel remains a temporary aid agreement owned by request and
party policy. It must retain this `PersonId`, relationship, wounds, and personal
equipment; it is not recruitment. A verified destination and any bounded hostile
event use separate server records, with the event claim persisted before a
single spawn. Later recruitment is a distinct invitation. Gear progression and
specialist capabilities update the same person, while specialist movement and
attacks remain under native combat ownership.

Run `tests/Run-Tests.ps1` for the pure C# 5 policy suite.
