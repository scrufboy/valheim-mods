# Encounter allegiance adapter handoff for maintained 1.0.x

This directory is pure policy for the maintained 1.0.x source line. It makes no
claim that the current lab build already has campaign first contact.

The server adapter must:

1. Create a `PersonContactRecord` when a stable person and camp identity are
   committed. Store it under `lwc_encounter_allegiance_v1_<personId>` in a
   server-owned campaign store. Load before evaluation and save by expected
   revision; broadcast only after the authoritative save succeeds.
2. Resolve `FactionIdentity` from authored Living World profile data. For another
   mod, register an explicit adapter ID and relationship mapping. A native
   `Character.Faction` value may assist target filtering but is never sufficient
   allegiance evidence. In particular, Eitrherjar and Fulings remain hostile
   despite the installed actor's shared transitional combat faction.
3. Produce `ContactFact` only from server evidence: line of sight or perception,
   damage callbacks, weapon state, an authored camp boundary, item ownership,
   verified aid, or a persisted authored grievance. Reject client assertions and
   do not synthesize witnessed facts for unloaded or occluded actors.
4. Keep visual identity concealed until the runtime has presented behavior,
   silhouette, and gear and both notice flags are true. Call `TryReveal` once with
   the authoritative display name, persist it, and never reset it on unload,
   death/reload, or camp reconstruction.
5. On `YieldToNativeCombat`, release every peaceful action-owner lease before
   committing the target through native 1.0.x combat. Policy must not steer,
   face, animate, or attack after that handoff. On
   `YieldToPeacefulPresentation`, let the presentation layer play an approved
   warning/stow/signal without manufacturing locomotion or relationship facts.
6. Treat `CanRecruit` only as an allegiance precondition. Pillagers and
   Eitrherjar are not made recruitable by this module. Defectors require a
   separately authored persistent variant and encounter resolution; do not flip
   allegiance in combat.
7. Do not convert accepted aid into a bond. Temporary travel to a verified site
   references the same `PersonId` and equipment record, then normally returns the
   person home. A later party invitation is a separate persisted decision with
   home, progression, and roster gates.
8. Keep bounded hostile-event claiming outside this contact record. The request
   adapter must verify the site, progression, acceptance, loaded participants,
   and persist one server claim before spawning. Reload must not duplicate it and
   offscreen death must not complete or reward it.
9. Apply earned gear and specialist capabilities to the same persistent person;
   never replace them with a stronger actor. Spear, bomb, knife, atgeir, and heavy
   pressure requests enter only through their proven capability adapters and
   execute under native combat ownership.

Integration still required: campaign-store serialization, CAS retry/coalescing,
server witness extraction, profile-to-allegiance registration, other-mod adapter
registration, hidden/revealed UI, and native combat/action-owner handoff. The
pure tests cover authority, bilateral notice, reveal permanence, witnessed
escalation/de-escalation, matrix distinctions, idempotence, recruitment limits,
and reload persistence; they do not claim live networking or AI integration.
