# Bombs, clouds and status exposure — research and development notes

Status: **R&D, not built.** Started 2026-10-29 at Brian's request ("put that into research and development in the Git
yourself for the future of character options later"). This is a reference for future character options. It is not a
build order.

Evidence labels: SOURCE-INSPECTED (Valheim 1.0.21 decompile, ILSpy 8.2), LIVE-PROVEN (lab session 2026-10-28),
RESEARCH (public wiki, not game files), PROPOSAL (not approved).

## 1. What the game does today

### Cloud hit rules (`Aoe.ShouldHit`, SOURCE-INSPECTED)
- `m_hitOwner` defaults to false: a cloud never hits its own thrower.
- `m_hitSame` defaults to false: a cloud skips every character whose internal name equals the thrower's. All Living
  World companions share the internal name "Living World Companion", so they never hit each other. **This is a side
  effect of the shared name, not a faction rule.** If companions ever get distinct internal names, the protection
  falls to `m_hitFriendly`.
- `m_hitFriendly` defaults to true in code. Whether each bomb prefab turns it off is prefab data, UNKNOWN from code.
- The wiki states, for the Ooze and Bile bombs, "the thrower is excluded" (RESEARCH). A player cannot poison himself
  with his own Ooze or Bile bomb.

### Live evidence (LIVE-PROVEN, 2026-10-28 overload test)
- 149 native `Poison damage:` applications. The Ooze bomb does 40 poison; Greydwarfs and Greylings resist it and
  took 20, while Fulings and Fuling Berserkers took the full 40.
- Bomb hits on a Skeleton (t630) and a Draugr (t710) produced no poison: undead are immune (RESEARCH confirms).
- Companions never coughed from their own clouds, which is consistent with the owner and same-name rules above.
- Telemetry gap: poison ticks carry no attacker, so the hit tracker missed the cloud damage. Brian saw the damage
  numbers on screen and caught it.

### Smoke Bomb (RESEARCH + SOURCE-INSPECTED)
- **Purpose:** Ashlands item that "extinguish[es] fire in a 3-meter radius around the impact" (wiki).
- **Crafting:** Workbench level 1, Smoke Puff (found in the Ashlands) + Asksvin Bladder (dropped by Asksvin) makes 10.
- **Damage:** impact 5 blunt. The cloud applies **Smoked**, 2 *pure* damage per second (pure damage ignores armor).
  Cloud radius and duration are not stated on the wiki page; earlier research said 3 m for about 10 s (unconfirmed).
- **Who:** the wiki says it affects any creature without immunity, **including the thrower**. It is the one bomb that
  can hurt its thrower.
- **Smoke immunity is a flag:** `Character.m_tolerateSmoke` defaults to true, so most creatures ignore smoke. The
  Player prefab has it off. Companions copy the Player's serialized fields, so they are likely affected (not
  live-checked).
- **No cover:** AI sight (`BaseAI` view-block mask: Default, static_solid, Default_small, piece, terrain, viewblock,
  vehicle) does **not** include the smoke layer. Fire smoke does not hide anyone. Whether the bomb's own cloud object
  sits on a view-blocking layer is prefab data, UNKNOWN; the wiki claims no concealment.
- Fire smoke and the bomb apply the same status (`Character.UpdateSmoke` checks the "smoke" layer every 2 s).

### Damage-type table
`claude-exchange/CLAUDE-OUTBOX/2026-10-29-creature-damage-research/HANDOFF.md` has the per-creature modifiers: poison
immunity of undead and slimes, Greydwarf poison resistance, fire weakness, Wet ×1.5 frost and lightning.

## 2. Brian's direction (2026-10-29, his words condensed)
- He likes bombs and how they work now; it is fine for now.
- "We might have to implement a reduced effect", for both the player class and the non-player character class. It
  helps realism and survivability: "It won't hurt us. It maybe won't kill us. Maybe it will if we're too stupid to keep
  running into it."
- Pair it with "a heightened awareness of the cloud, maybe the cloud's area, maybe some avoidance."
- Hostile NPCs must be affected by poison, just as we are affected when they throw it. Running from the cloud should
  be a tactical decision.
- A thrower knows where his own bomb landed. Combat can still push someone into the cloud, and someone could be
  unaware, but that is unlikely. "It wouldn't make sense that people in a party would not know where the poison is."
- "Something has to be done though in this arena to test it."

## 3. Design options (PROPOSAL; Brian decides values and scope)

### A. Reduced self and party exposure
- **What:** a cloud can affect its thrower and the thrower's allies at a reduced fraction of its status damage, for
  example 25–50%. Enemies keep the full effect.
- **Who it covers:** companions certainly. The player's own bombs only if Brian wants the vanilla player rule changed
  too: that is a global gameplay change and must be a separate, switchable option.
- **How:** one owner-side rule in the cloud hit decision, for clouds owned by Living World companions (or by the player
  when the option is on). Native `SE_Poison` and `SE_Burning` keep doing the damage. No second damage writer.
- **Consequence:** body voice then gets real self-exposure coughs through the existing poison status. No new audio
  path.
- **Open:** fraction, whether Bile bomb fire is reduced the same way, and whether the Smoke Bomb (already self-hitting)
  is left native.

### B. Party hazard awareness (player-parity: a player sees the cloud)
- **Hazard memory:** every visible active cloud (own, ally or enemy) is recorded with centre, radius, expiry and
  damage type. A companion perceives only clouds it could see (line of sight, range). A thrower always knows its own
  cloud.
- **Avoidance:**
  - Movement and spacing treat an active cloud as a no-go area unless already inside.
  - If inside, choose the shortest exit away from enemies.
  - Bomb planning keeps the landing away from the party's paths: the current friend veto covers positions now and at
    landing; add the predicted paths of friends.
- **Combat pushback** can still put someone in a cloud. Reduced exposure (A) keeps that survivable, and the exit rule
  gets them out.
- **Enemy clouds:** the same awareness applies, so companions step out of shaman poison or Bonemass gas like a player
  would.

### C. Bomb choice by target family
Use the damage table to judge whether the *equipped* bomb is worth throwing. Never throw poison at poison-immune
undead or slimes. ChatGPT's review (`CODEX-OUTBOX/2026-10-29-biome-locked-bomb-response`) adds that biome class
defines the arsenal: no switching bomb types that the class does not carry.

### D. Hostile NPC poison
- **Who:** hostile humanoids (Living World Enemy Families) are enemies of the cloud owner, so native cloud rules
  already hit them, unless a family is authored poison-immune.
- **Their bombs:** hostile throwers' clouds hit companions and the player natively, at full effect (enemies of the
  thrower).

## 4. Test arena (PROPOSAL)
- **Own cloud:** thrower walks into its own cloud. Expect no effect today; under option A, reduced poison, a cough,
  and the exit rule taking it out.
- **Ally pushed in:** a companion knocked into a partner's cloud. Measure exposure time and damage.
- **Enemy cloud:** Greydwarf shaman poison and Bonemass gas. Expect the full effect, coughs and avoidance.
- **Hostile humanoids:** Fulings and future Enemy Families, bombed. Expect 40 per dose.
- **Undead:** bombed. Expect no poison, and under C, no throw.
- **Smoke:** Smoke Bomb and a smoky hut. Expect companions get Smoked if their tolerate flag copied the Player's, and
  creatures keep seeing them.
- **Telemetry needed first:** record status damage ticks with the cloud's owner (remember the owner per target when
  the cloud applies the status); record hazard memory entries and avoid/exit decisions.

## Sources
- Valheim 1.0.21 decompile: `Aoe.ShouldHit`, `Character.UpdateSmoke`, `BaseAI` view-block mask, `SE_Poison`.
- https://valheim.weirdgloop.org/w/Smoke_Bomb, https://valheim.weirdgloop.org/w/Smoked,
  https://valheim.weirdgloop.org/w/Bombs, https://valheim.weirdgloop.org/w/Poison, https://valheim.weirdgloop.org/w/Damage
