# Needs and affinity adapter handoff — maintained 1.0.x

This directory owns pure policy only. Do not copy Unity objects, scene instances,
or mutable inventory references into its state.

1. On the authoritative server, read the person's stable campaign identity and
   encounter-authored need definitions. Query only the loaded local scene around
   that person. Populate every `NeedCandidate` fact; a missing query is false.
2. `TargetId` must identify the exact authored danger, dungeon/material source,
   food shortage, or settlement obligation. Reject player structures/containers
   and arbitrary nearest-object scans. Dungeon material legality comes from
   current server world keys and the encounter's approved material allow-list.
3. Call `SelectRequest`, compare-and-swap the complete state, then present the
   request. Never present before persistence succeeds. No request is selected or
   completed while the person/target is unloaded or remote.
4. For completion, construct fresh `CompletionEvidence` from an authoritative
   visible transaction: actual item delivery, verified authored danger clear, or
   settlement objective commit. Match person, target, kind, amount, and stable
   evidence ID. Do not accept client claims or infer success from absence.
5. Save `NeedsPersistence.Write` in a server-owned campaign record. Preserve
   applied command IDs, request outcomes, and choice memories across zone unload,
   owner transfer, and restart. Retry conflicts from a fresh read.
6. Refusal closes the request without hostility. Griefing requires witnessed
   authoritative evidence and produces an `Unsafe` reading; ordinary refusal is
   never griefing. Presentation uses words and authored dialogue, not numbers.
7. Pass `InvitationEvidenceHandoff` into the integration layer only as supporting
   facts. `RescueRelease.Invite` remains the sole owner of final deterministic
   yes/no, home/world gates, bonded/expedition limits, and assignment. Never call
   it automatically after aid; the player must reach the authored invitation.
8. QuestBroker may read `QuestRequestFact` after the needs state is committed.
   It owns seed/map queries, target-location disclosure, and whether the request
   appears through a camp, settlement board, or jarl. Do not feed coordinates or
   unverified inferred targets back into affinity state. A small-boss request is
   eligible only when its owning encounter supplies progression legality and a
   verified safe authored site; this module neither searches for nor places it.
9. QuestBroker remains authoritative for operational request status. When the
   player agrees and reaches the verified site, EncounterActivation alone spawns
   or activates the event and returns its stable activation ID. Record that fact
   with `RecordEncounterActivation`; it proves only the claimed event.
   Record contribution only from an authoritative local witness tied to the same
   activation. `BuildTemporaryTravelHandoff` may send person agreement evidence
   to RescueRelease, which alone decides `TemporaryAlly`, return state, and active
   slot accounting. After the concrete need is completed, use
   `BuildPersonalInvitationHandoff` to permit later dialogue—not recruitment.

Required live proof: dedicated-server authority, two-player duplicate delivery,
save/restart, target destroyed before completion, zone unload during an accepted
request, legal and illegal dungeon materials, settlement ownership boundaries,
and full-roster handoff with no actor deletion or automatic following.
