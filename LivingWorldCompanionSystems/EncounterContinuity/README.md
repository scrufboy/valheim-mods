# Encounter flags across zone load and unload

This module projects flags from the existing server-owned ledgers. It does not
create another quest, relationship, person, or party database. Discovery owns
site/person knowledge; EncounterAllegiance owns contact and relationship;
NeedsAndAffinity owns need/aid facts; QuestBroker owns requests;
EncounterActivation owns the one site event; RescueRelease owns temporary and
permanent assignments. The adapter must read those ledgers in one consistent
world snapshot before calling `EncounterFlagProjection.Project`.

| Moment | Durable facts | Scene availability |
| --- | --- | --- |
| Claimed camp, first sight | site and person IDs | actor only in a loaded, owned zone |
| Mutual notice | contact fact | request may be offered; no party membership |
| Aid accepted and person agrees | request and agreement IDs | temporary travel may start if a field slot is open |
| Claimed target reached | one event claim | target zone and arriving player permit activation, even if issuer camp unloads |
| Either zone unloads | all durable facts remain | only unavailable scene actions pause |
| Target returns | same claim and people | event resumes without a second roll or reward |
| Aid verified and temporary travel ends | witnessed aid and return facts | later invitation may be offered |
| Invitation accepted | bond fact | active field position only if below the two-person limit |

Server-resident camps can keep an actor available without a nearby player only
when the adapter proves retained-zone safety. The target site is independent of
the issuer camp. A loaded target with player arrival may activate the claimed
event while its issuer is elsewhere.

The view flags are recomputed on every load. They are never themselves a reason
to spawn a replacement person, reroll gear, force a follow command, grant trust,
or pay out a quest. A mismatch among source ledgers fails closed and requires
diagnostic repair from the owning ledger. `Run-Tests.ps1` checks these sequences.
