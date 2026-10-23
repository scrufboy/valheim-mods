# Living World C.O.R.E. NPC 1.0 solidification handoff

Date: 2026-10-23  
Recipient: Claude  
Parent candidate: `claude/lwcore-npc-1.0.0`, based on `handoff-rimebound-emberworn@4c09a79`

## Intent

**SOURCE-INSPECTED:** Brian has made the architectural decision that maypole
settlement recognition, NPC-usable workstation/social totems, Hold, town context,
release, home citizenship, Go Home, and migration belong in Living World C.O.R.E.
They cannot exist only inside Village Reclamation because the same person must
recognize and behave appropriately in any compatible town.

**SOURCE-INSPECTED:** Reclamation remains responsible for structures, restoration,
governance, jobs, and real economic transactions. It must supply settlement and
job facts to core interfaces without replacing the NPC controller.

**UNKNOWN:** The complete proposed model and delivery sequence are specified in
`SPECIFICATION.md`. They require Claude's review before implementation.

## Current evidence

**INSTALLED:** The active Human NPC DLL is Claude's 1.0.0 candidate, SHA-256
`71D0F72DB02E5614C9BEC3BC899D49A6DB045DDA308755B8FCB44B4AB8BAB37D`.
The active telemetry DLL is SHA-256
`68A9A5E202DBF4B1CDC87004AA1241881B051EAF6E506599D9FE6E018520E90F`.

**OFFLINE-TESTED:** Claude reports 23 regression-ledger passes, 16 native-member
checks, 42 ownership-gate sites, and an analyzer self-test. The source and package
were inspected locally; no additional build or test was run for this handoff.

**UNKNOWN:** 1.0.0 has not yet produced a live Unity run. The user is testing that
installed candidate directly. Do not alter the installed DLL during this baseline.

**SOURCE-INSPECTED:** The current source has one context value,
`NpcContext.FieldCompanion`. Hold, town/resident life, workstation use, beds,
doors, citizenship, Go Home, Release Free, and migration are not implemented.

**SOURCE-INSPECTED:** Current `TorchPolicy` turns the torch off in `Readiness`.
Brian's rule is that a valid night torch remains through curiosity/attention and
is doused on committed combat, stealth, swimming, or stable illumination.

## Scope

**SOURCE-INSPECTED:** The requested core model separates durable identity,
citizenship/home, engagement order, spatial context, duty, and temporary action.
It includes a maypole-backed settlement registry, a shared bounded affordance
registry, reservations, safe workstation/seat/bed/door adapters, and explicit
Hold, Go Home, Release Free, Settle Here, and loaded migration transactions.

**SOURCE-INSPECTED:** Workstation animation and real work are separate. Ambient
activity may animate but cannot create or consume resources. Reclamation or
another provider authorizes real idempotent jobs.

**SOURCE-INSPECTED:** Native combat remains the highest-priority all-domain owner.
No settlement, activity, torch, door, bed, hold, or migration writer may operate
against it.

**UNKNOWN:** Off-screen travel may remain gated if it cannot be made identity-safe
in the consolidated release. Loaded movement, durable journey identity, and a
single authoritative representation are required foundations.

## Required next response

Return the seven items in `REQUEST.md` before changing the installed profile.
Use `SPECIFICATION.md` as the complete requested contract and source-access map.
