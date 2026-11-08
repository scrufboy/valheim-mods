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

## Authorable Formidable relation seam

`AuthorableRelations` is the engine-free shape for a framework-level public API:
mods register string faction IDs, exact prefab/profile bindings, directed
authored relations, and explicit authored-to-native compatibility selectors.
Identity resolution priority is persistent authored profile, then exact prefab.
Native buckets never become authored identity.

The Living World compatibility defaults bind `RRRN_Pillager` to
`LWC.Pillagers` despite its stale/forced native `Undead` value. A pairwise
Pillager↔native-Undead selector returns hostile in either argument order, but
unknown pairs return no override and therefore keep vanilla behavior.
Undead↔Undead is not rewritten. `RRRN_OrcBrute` and `Krauguk` similarly preserve Eitrherjar identity
instead of inheriting `PlainsMonsters` as story allegiance.

Accepted-request travel remains a temporary aid agreement owned by request and
party policy. It must retain this `PersonId`, relationship, wounds, and personal
equipment; it is not recruitment. A verified destination and any bounded hostile
event use separate server records, with the event claim persisted before a
single spawn. Later recruitment is a distinct invitation. Gear progression and
specialist capabilities update the same person, while specialist movement and
attacks remain under native combat ownership.

Run `tests/Run-Tests.ps1` for the pure C# 5 policy suite.

## Captive Behemoth instance authority

`CaptiveBehemothAllegiance` is the shared, engine-free relationship state for
the swamp binding ritual. It does not own the Behemoth prefab, shaman combat,
spell visuals, minion defense, movement, damage, attacks, drops, corpse, or
global faction matrix.

The exact encounter and Behemoth IDs persist one state machine:

- `DormantBound`: captors are safe; no control glow is implied by allegiance.
- `Controlled`: a verified completed shaman bind receipt establishes orange
  control and captor protection; physical deployment does not break the spell.
- `ReleasedFrenzy`: a verified spell-break receipt starts a caller-supplied,
  bounded red interval hostile to every other valid actor.
- `ReleasedUndead`: expiry restores normal green behavior, friendly to undead
  and hostile to the shaman/Necks/minions and player.
- `Dead`: terminal, clears ritual presentation and permits no rebound.

A successful newer binding receipt can restore control from red or green.
Binding revisions reject stale casts, event IDs make callbacks idempotent, and a
rebound state cannot be changed by the old frenzy timer. Relationship overrides
work in both argument orders and return false for actors outside the exact
instance so native/global behavior remains untouched.

Cross-owner contract: the shaman owner supplies only accepted bind receipts;
the spell owner supplies actual break receipts and the explicitly configured
frenzy duration; the Behemoth owner consumes presentation flags and retains
native combat; minion owners read the state while protecting the shaman; one
CAS persistence adapter commits transitions. No owner should mirror or infer
the allegiance state from eye color, chains, animation, damage alone, or native
`Character.Faction`.
