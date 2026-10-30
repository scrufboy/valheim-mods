# Tactical answer to the four awareness questions

The design is directionally correct, but the four questions are framed as global switches where FormidableAI
must derive a response from facts. Apply these answers and revise the design before implementation.

## 1. Leader line of sight

**Yes, as a party report—not as borrowed vision.** An unobstructed, facing-aware observation from Brian may
create/update a shared contact and last-known position. It does not prove that a companion has a clear shot.
The companion performing an attack must validate its own current line, arc and friendly clearance. If no callout
mechanic is expressed yet, tag the source as party-shared and make that transfer visible later through look/voice.

## 2. 60 m day / 35 m night

Use **60 m as the first clear-day maximum for the shadow test**. Do not encode 35 m as the complete darkness
model. Visibility must accept an environment multiplier/fact for darkness, fog, Mistlands obscurity, weather,
local light, terrain and target salience. Start with 35 m as a temporary ordinary-dark test value only, record the
environment in telemetry, and tune from video. Noticing range is not attack range.

## 3. Committed contacts beyond 30 m

**Capability-specific, not a general engagement permission.** A trained bow user may engage within the bow's
effective range and a valid lane. Melee may not chase beyond its 30 m leash. A bomb with a 9–20 m envelope
cannot engage a target beyond 30 m; awareness can instead prepare, form, warn, reposition, take cover or wait
until a valid intercept window. Correct the design question that groups bows and bombs together here.

## 4. Sneaking

**Derive it from whether stealth remains tactically viable.** Merely noticed/alerted/suspicious contacts do not
justify breaking stealth. If the contact targets or hurts the party, or sustained evidence proves it is committed
and interception is unavoidable, companions may defend and the arbiter may choose a bomb when it is the best
survival action. Sneaking changes the utility/cost of actions; it is not an unconditional veto after commitment.

## Required design corrections found during review

1. `Poisonable` cannot come directly from native modifiers as tactical omniscience. Supply known/unknown and
   confidence from training, biome doctrine, compendium/lore or observed results. Unknown resistance must be a
   real uncertainty in arbitration.
2. Do not send every `Noticed` contact to Readiness. Looking/investigating is distinct from weapon readiness;
   wildlife and harmless/unconfirmed contacts must not ready the party. Alert/credible-threat evidence does.
3. Clarify who spends the party ray budget and guarantee fair scheduling across observers and urgent contacts.
4. Replace the statement that target ownership is inherently visible. Native target state may corroborate
   commitment only while the actor's facing/charge/attack behavior is perceptible or after a contact event.
5. Keep persistent learned knowledge separate from transient contact memory, but allow awareness interpretation
   to query that knowledge by enemy family.

This is the thinking layer's responsibility. Brian supplies the desired character and tactical doctrine; the
implementation should derive obvious consequences and bring him genuine balance/character choices, not ask him
to serve as the runtime arbiter.
