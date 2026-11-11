# FORMIDABLE — Odin's Friends & Foes
### The Hearths of the Forgotten · Original Narrative & Event Bible
**Lore baseline v0.2 | 17 October 2026 | Single-player-first | Draft locked for research, not publication**

> In the tenth world, the Forsaken are not the only ones who endure. Beyond the abandoned halls and haunted forests are warriors still seeking glory, wanderers seeking shelter, outlaws turned against their own, and ancient beings consumed by powers they once sought to master. Some will stand against you. Others will stand beside you. And where the dead are driven from their stolen halls, the hearths of the forgotten may burn again.

## I. Why this world has people
Odin sent the player into Valheim to challenge the Forsaken. FORMIDABLE does not displace that mission. It asks what became of the halls, homesteads, fences, fields, longhouses and roads that already exist in this strange, severed realm. The player is not the only wanderer who ever arrived here. Others fought, sheltered, built, failed, fled, or chose to remain. A hero may seek glory; a farmer seeks tomorrow's bread. Both belong in the tenth world.

The Common Folk are survivors and settlers in a supernatural world. Some may once have been warriors; their individual origins are deliberately varied and not asserted as a universal official Valheim fact. They form settlements and exchange hospitality with the player, not rival political kingdoms. A companion may be an independent warrior who joins the player, lives at a settlement for a time, then joins again. They are people in the world, not merely replaceable equipment slots.

FORMIDABLE contains friendly and hostile populations in one NPC content ecosystem; the independent Living World C.O.R.E. holds the authoritative technical systems. The reclamation/village package works with those NPC contracts, and no second competing identity, faction, or AI implementation is authorized by the lore design.

## II. Four ages — proposed FORMIDABLE story
### The Age of Exile
When the tenth world became Odin's prison for the mighty and accursed, it did not remain empty. Its wild places attracted combatants, wanderers and stranded people. Some pursued the Forsaken. Others, tired of endless danger, built halls and planted fields under Yggdrasil's shadow.

### The Age of Ruin
As enemies and old powers spread, many halls fell. Draugr occupied settlements; pillagers preyed on travelers; forests concealed spirits and corrupted guardians. An otherworldly martial host, ruined by the pursuit of forbidden sorcery, kept its formation, weapons and terrible purpose. Villagers left possessions, farms and hearths behind and disappeared into the woods.

### The Age of Returning
When a warrior clears the dead from a village, witnesses may bring the news through the trees. A mark of reclaiming is raised. Small groups of refugees arrive with axes, sacks, tools, children of memory, injuries, doubts and hope. A repaired village becomes a home rather than simply a cleared encounter site. The hearth is relit; the people choose leadership and begin again.

### The Age of the Living Hearth
The settlement develops its own rhythm. Woodcutters bring timber. Hunters bring meat. Cooks tend fires. Guards patrol fences. Ordinary townsfolk can use cheap visible work loops while the actual economy is tracked authoritatively. The great hall remains the communal center. Raids by pillagers threaten hard-won progress; mysterious foes roam beyond the firelight. On rare occasions Odin's champions may be witnessed, but the village must usually protect itself.

## III. The people of FORMIDABLE
**Common Folk.** The population foundation: wanderers, refugees, citizens, farmers, cooks, workers, guards, smiths and possible companions. Those are roles, occupations or attachments; a person keeps their identity across permitted changes. Named persistent NPCs include the Jarl, steward, matron, notable guards and companions. Noncritical ambient performers can be lightweight and theatrical.

**Common Folk Pillagers.** Robbers, drifters and brigands; recognizably of the same humanity as villagers. They loiter around sparse, modest forest camps, with visible campfire smoke and silhouettes at a distance. Some sit until alarmed, others stand watch, and a spotted player may be chased. They threaten roads, food and developing settlements, but their numbers are not a constant swarm. Their hostile status and existing combat profile are retained until lab-approved changes. Defection and redemption are story possibilities, not mandatory unapproved dice-roll AI.

**The corrupted legions.** Keep these dangerous armored knights in the forest, and preserve the original intended biome coverage, encounter character, equipment and stat baselines. The old pack names them Krauguk Legionnaires / Krauguk Legions (including OrcBrute configuration). Our separate, original lore makes them an organized otherworldly host warped by dangerous magic—not ordinary knights merely 'going crazy' because they studied a spell. They retain tactics and military ranks. Working display name is **UNDECIDED**. An earlier E-initial proposal must be recovered from the other conversation; 'Myrkherjar' was an incorrect substitution and is not approved. Do not mistake **Einherjar** (Odin's honored warriors) for the hostile legion without deliberate creative approval.

**Forest Children.** Unsettling and sorrowful presences in groves and night places. Our tale treats them as souls bound to the landscape, not merely colorful monsters. Some may have a hidden noncombat interaction one day, subject to rights and design approval.

**Neckmen.** Distorted marsh-dwellers, their forms changed by long exile and corrupted waters; dangerous near swamps and drowned paths. We may call them Marshbound as a lore epithet, but do not change internal creature identifiers without approved migration.

**Dark Servants.** Guardians of ancient trees and forbidden places. Their obedience has outlasted their wisdom: they may attack a traveler unable to distinguish violation from passage. Preserve their threatening forest presence.

**Independent companions and exceptional heroes.** Other questing or stranded warriors, sometimes encountered on roads or living in villages. They are never automatic followers; joining is a player-controlled relationship supported by the core. The rare Einherji intervention is an extraordinary story event, not the basic settlement defense plan.

## IV. Reclamation Day — signature event v0.2 (LOCKED DIRECTION)
### Condition and invitation
The player discovers a Draugr-held village, including an old great hall or a suitable future great-hall site. When the actual dangerous occupants and their active spawn sources are cleared, the site becomes *eligible*, not immediately transformed. Hugin arrives at a safe, visible location and acknowledges the deed, then Munin may speak of who lived there and the meaning of remembering. The ravens are complementary narrators. The Norse meanings commonly rendered **thought** (Huginn/Hugin) and **memory** (Muninn/Munin) support this usage; do not present 'Hugin predicts future, Munin is past' as established game mechanics.

**Hugin (original proposed dialogue, not vanilla):**
> 'Kraa! The dead have kept this place long enough. You have made room for the living. But an empty hall cannot call its own people home.'

**Munin (original proposed dialogue):**
> 'I remember the warmth that once rose from this hearth, and the voices that passed through its doors. They are not all gone. Some have waited beyond the trees for a sign.'

The player is offered a meaningful **Reclaim this village? — Yes / Not yet** decision. This can be conveyed through a raven interaction or a distinct existing interaction pattern as engineering permits. No forced conversion occurs upon clearing alone.

### The NO branch: a deferred return
If the player declines, an **activation stone / reclaim marker** remains (or is activated in-place). It is discoverable and can recall the raven discussion later. Nothing is teleported in; no automatic maypole, population, resource production or structural reset. Re-approaching is idempotent: there is only one claim opportunity and no marker multiplication. A short response can say, 'The hearth may wait.' The exact marker prefab and restoration method require codebase verification.

### The YES branch: lightning chooses the center
The player does **not** build or manually place a maypole. The village system identifies a safe central communal location based on its actual generated footprint, great hall, accessibility and collisions—not an arbitrary world-map coordinate. If the geometric center is obstructed, the system chooses the nearest safe central ceremonial anchor or defers with clear feedback; it must not destroy the hall, buildings or player-made structures.

The screen darkens subtly, thunder sounds, and a **safe effect inspired by the vanilla Obliterator's Thor-lightning strike** hits the central reclaim point. Sparks and smoke clear. A somewhat larger maypole stands at the chosen center, its aged wood subtly darkened and greenery more vibrant. Optional flower or vine revisions should remain restrained and recognizably vanilla. This **reuses/extracts visual and audio cues only after prefab/VFX research; never execute the Obliterator's item-destruction or area damage logic**, and never scorch villagers, loot, pets or the player. The strike is singular, memorable, and replay-safe after reload.

### The return of the folk
A far-off horn is heard. Small groups of survivors appear from suitable off-screen woodland approaches, not in the middle of the square or as a boar-like crowd around a stone. They make different progress: some run, some walk, some are weary, some limp, some wave. One might call 'The dead are gone!' and another recognize the old hall. Light ambient animation sells the return; the authoritative population roster must not be multiplied by presentation effects.

**A survivor:** 'I remember this place.'
**Another:** 'We can mend this.'

Workers begin cleaning, carrying supplies and repairing existing pieces. The Great Hall hearth is restored or established as the first communal milestone. A temporary organizer becomes a steward; when shelter and population support it, the villagers select a Jarl, who formally appoints the steward. A matron represents food, comfort and the happiness of the community. The player can explore or help while all this unfolds—no long forced cinematic.

### Hugin / Munin after the event
Hugin can offer concise instructions for the next village milestone. Munin can return later for reflective testimony following the first repaired hearth, completed Great Hall, or a narrowly survived raid. New dialogue must be written in original language rather than copying official lines.

## V. Settlement pattern: rebuild first, expand second
Each functioning community should progress toward a Great Hall; homes; cooking/smokehouse; brewing/mead space; fields; animal pens; timber yard; storehouse; guard posts and palisades; and eventually a smithy. These are **functions/milestones**, not a decree to instantly spawn an entire new town. Use existing damaged buildings and vanilla structural assets first; blueprints/patches only for missing or suitable expansions. Position against actual terrain, obstruction, ownership, and the existing settlement footprint. Never delete or regenerate the existing village because a maypole event began.

The **Jarl** responds to threats and community welfare; **steward** watches supplies and assigns work; **matron** tracks food, warmth, shelter and confidence. Their numbers are a village-level model. Low-cost ambient laborers can carry wood, chop, cook, sweep or mend for show. Numbers/resource production remain authoritative: don't count a fake cooking animation as a second real food item. Villages can process bounded economic steps during player absence without needing each individual to pathfind continually.

## VI. Camps, inscriptions, raids and encounters
**Pillager camps** are sparse handcrafted micro-locations within large candidate biomes/regions, not a crowded spawn-bubble. The player should be able to see a campfire glow or smoke at range and choose whether to sneak, approach or shoot a distant sentry. A couple of authentic encounter sites in a wide forest can be more compelling than constant filler. Camp inventory/objects and nearby survivors should match the group's scale; positions should be reproducible by seed and remain stable once explored.

**Runestones** may preserve stories of exile, missing villagers, the dark host and the first hearth relit. Reuse vanilla visual style and its runic-text reading presentation where possible, but **research the actual runestone mesh/texture, `RuneStone` text properties and localization behavior** before claiming it can vary carved geometry per stone. Static model inscriptions can coexist with individually authored interaction text; do not invent per-stone rune meshes without proof. Story stones should be rare and linked to suitable encounter geography. Preserve original vanilla stones.

**Raids**: The desired reclamation-threat palette is modest pillager bands first, with the corrupted legion as an escalated and sparingly used threat if progression supports it. **No troll raid and no overwhelming waves as the intended village threat.** The attackers dislike flourishing settlements, but no automatic political faction war is being proposed. Raids should have bounded attackers, cool-downs, fair warning, accessibility, and real termination. Companions and guards defend villages, but Odin's champions are extraordinary exceptions.

## VII. Game-feel constraints
- Single-player intimacy over multiplayer faction spectacle; another person exists in the world alongside the player.
- Believable work and social animation over unnecessary per-NPC heavy AI; actual records/economy never contradict visual loops.
- Maintain forest knights, original hostile relationships, combat tuning, and content pack configuration identifiers unless evidence-backed technical migration is explicitly approved.
- Use vanilla or minimally reskinned building, totem, runestone, fire, portal and lightning assets whenever technically and legally appropriate.
- A totem may already represent the guard post; any kitchen/farm/post variants are visual research and later presentation work, not immediate core rewrites.
- The maypole reclaim sequence is world-changing, unique, opt-in, reversible only through intentional future rules, and never dependent on the player's manual centering skill.
- Narrative events are short, contextual and discoverable. Hugin guides; Munin remembers.
- No new endpoints, third-party asset redistribution, competing systems or unapproved scope added in the name of narrative depth.

## VIII. Status register: settled / provisional / open
**Locked narrative decisions (October 17):** FORMIDABLE — Odin's Friends & Foes; the player is one among other surviving people; villages return after clearing and opt-in; raven conversations; automatic safe central maypole and staged lightning; deferred activation stone when refused; visible refugee arrivals; Jarl/steward/matron division; camps with fires and ambush behavior; bounded pillager-led raids; forest knights retained; community simulation may use fake ambient work; original internal factions and aggression data preserved as technical baseline.

**Working lore (not yet confirmed canon):** The Hearths of the Forgotten as story arc, Ages of Exile/Ruin/Returning/Living Hearth, how the cursed legion lost its mind, other origins of Common Folk, individual dialogue, Munin appearance sequence, exact maypole reskin, speculative supernatural champions.

**Explicitly unresolved:** E-initial proper name of the corrupted legion; exact native runestone mesh/text behavior; precise village 'center' selection and network persistence; approved photo-mode tools; which of the proposed features engineering already has; rights to specific third-party assets/lore; detailed raid balance numbers and location counts; provenance of any screenshots.

## IX. Original short pitch
> A hearth can be taken by the dead, but it can be claimed again by the living. In Odin's tenth world, heroes and outlaws walk the same roads, survivors wait behind the treeline, and lost powers haunt the old forests. Drive the dead from their stolen halls. Hear the ravens. Watch the lightning strike, the maypole rise, and the people come home. Some will stand beside you. Others will come for all that you have rebuilt.

**Editorial caution:** Valheim lore and Salpati's documented creature backstories are reference material. This packet writes an original setting layer; it does not imply official Iron Gate endorsement or permission to redistribute third-party files. All quoted raven lines above are new draft text. Preserve authorship and rights.