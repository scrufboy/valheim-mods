# Character blueprint and paired-combat evidence contract

This is the authoring surface for a future request such as “build this
character.” A blueprint creates a candidate person and encounter, then the
server's ordinary world-selection, identity, progression, and consent rules
decide when that person may appear. Developer authoring is distinct from a
campaign control that prints a companion beside the player.

## Blueprint fields

| Field | Required content | Owning module |
| --- | --- | --- |
| Person and lineage | stable person key; validated Norse first name and parent genitive, or deterministic catalog selection | naming/identity |
| Place | home biome, verified site, camp/route role, local knowledge source | Discovery and CampLifecycle |
| Allegiance | Hearthless, Pillager, or Eitrherjar; initial relationship and reveal terms | EncounterAllegiance |
| Appearance | presentation, hair/face, armor layers, visible silhouette; no faction inference from looks | EncounterLoadouts and client view |
| Equipment | primary, different melee fallback, finite throwables, owned projectile recovery, upgrade gates | EncounterLoadouts and EncounterPopulation |
| Combat role | range, lane, target preference, lunge/heavy/atgeir conditions, support priority | specialist policies and native combat owner |
| Needs and invitation | one authored need, exact site fact, accepted-aid terms, trust evidence, later distinct yes/no offer | NeedsAndAffinity, QuestBroker, RescueRelease |
| Voice | candidate cue and text line, with approval status | EncounterVoice and lore |
| Return | safe home/camp, release route, same-person reappearance | RescueRelease and CampLifecycle |

Example candidate: a long red-haired Plains spear warden with weathered regional
armor, one personal spear, an axe fallback, and a verified route she knows. She
may retrieve only her own thrown spear when the native projectile and safe path
are proven. Her name and parent lineage come from the Norse catalog or an
explicit validated author choice. Her appearance makes her memorable; it does
not make her allied. A witnessed, accepted request may lead to temporary travel.
Only a later separate offer can make her an active companion.

## Combat timing and balance

- The active field limit stays two, including a temporary helper. One useful
  companion is the early-game norm; a third fighter is not silently granted.
- Knife lunge and heavy attacks share a commitment lesson: begin only when the
  target, lane, timing, and recovery window are favorable. A knife lunge seeks a
  reachable close endpoint and may miss a dodging target. It never teleports or
  retargets after commitment.
- Atgeir thrust, sweep, and spin are separate native attack approvals. A
  pushback request needs several real threats in its arc, a clear ally lane,
  bounded space, and a useful recovery position. It is not a constant crowd
  control aura.
- Spear throw, recovery, fallback draw, and finite bomb use form one equipment
  lifecycle. A spent or lost item changes the next legal choice; no invisible
  replacement appears.
- Ward, heal, and missile are separate candidate magic paths. The Fuling shaman
  is a reference to inspect for support timing and presentation, not proof that
  its prefab supplies a reusable heal. Verify each exact native animation,
  status, projectile, owner, and target rule before enabling it.
- Magic has finite reserve, cost per committed cast, cooldown, interruption,
  and explicit safe-rest or verified-supply restoration. A ward does not stack
  freely; a heal cannot revive death or waste reserve on a healthy target;
  missiles respect ally safety. `CombatSupport` implements the request/resource
  policy, while native game code must apply effects.

## Development telemetry per class pair

Record one event stream keyed by build hash, encounter/site, stable person IDs,
biome, class pair, target ID, server owner, and world tick. Each decision event
records sensed threats; role and intended move; target position and velocity;
safe lane and endpoint facts; ally positions; item, spear, bomb, and magic
resources before/after; native attack start, event, hit/miss/interruption,
recovery, and weapon switch. Support events record ward target/duration/absorb,
heal eligibility/amount, missile impact, and restoration source. Raid events
record player damage and support contribution, companion contribution, injuries,
deaths, regroup, and whether the player remained passive.

Compare one companion, two complementary companions, and a passive-player trial
against same-tier raids and bosses. Tune timing, distance, resource economy,
and role pairing to keep the player necessary. A person dying to a superior
creature is a combat outcome. A code failure requires a violated contract such
as unauthorized attack, duplicate projectile, missing debit, wrong owner,
teleporting lunge, unsafe ally hit, lost fallback, stuck animation, reset quest,
or an illegal third active fighter.

Telemetry is development evidence only. Removing the telemetry plugin must not
change NPC decisions or combat behavior.
