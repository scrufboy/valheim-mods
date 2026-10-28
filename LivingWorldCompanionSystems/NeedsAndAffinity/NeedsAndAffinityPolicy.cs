using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LivingWorld.CompanionSystems.NeedsAndAffinity
{
    public static class NeedsAndAffinityPolicy
    {
        public static RequestSelection SelectRequest(NeedsState source, string commandId, long worldSeed,
            long worldTick, string personId, IList<NeedCandidate> candidates)
        {
            if (source == null || String.IsNullOrEmpty(commandId)) return Denied(source, "invalid_request");
            if (source.AppliedCommands.Contains(commandId))
                return new RequestSelection { Offered = false, Reason = "command_already_applied", State = source, Replayed = true };
            if (String.IsNullOrEmpty(personId) || worldTick < 0 || candidates == null)
                return Denied(source, "invalid_request");
            foreach (var existing in source.Requests.Values)
                if (existing.PersonId == personId && (existing.Status == RequestStatus.Offered || existing.Status == RequestStatus.Accepted))
                    return Denied(source, "person_has_active_request");

            NeedCandidate best = null;
            ulong bestScore = UInt64.MaxValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (!Eligible(candidate, personId, source)) continue;
                var score = StableScore(worldSeed, candidate.PersonId + "|" + candidate.RequestId);
                if (best == null || score < bestScore || (score == bestScore &&
                    StringComparer.Ordinal.Compare(candidate.RequestId, best.RequestId) < 0))
                { best = candidate; bestScore = score; }
            }
            if (best == null) return Denied(source, "no_verified_authored_need");

            var next = source.Copy();
            var record = new RequestRecord {
                RequestId = best.RequestId, PersonId = best.PersonId, EncounterId = best.EncounterId,
                AuthoredNeedId = best.AuthoredNeedId, TargetId = best.TargetId, Description = best.Description,
                Kind = best.Kind, Status = RequestStatus.Offered, RequiredAmount = best.RequiredAmount,
                OfferedWorldTick = worldTick, ResolvedWorldTick = -1
            };
            next.Requests.Add(record.RequestId, record);
            next.AidArcs.Add(record.RequestId, new AidArcRecord { RequestId = record.RequestId,
                PersonId = record.PersonId, Phase = AidArcPhase.NeedKnown });
            Commit(next, commandId);
            return new RequestSelection { Offered = true, Reason = "verified_authored_need_offered", Request = record.Copy(), State = next };
        }

        public static ChoiceResult Respond(NeedsState source, string commandId, string requestId, bool accept, long worldTick)
        {
            var replay = Replay(source, commandId); if (replay != null) return replay;
            RequestRecord request;
            if (source == null || !source.Requests.TryGetValue(requestId, out request)) return Rejected(source, "unknown_request");
            if (request.Status != RequestStatus.Offered) return Rejected(source, "request_not_awaiting_response");
            var next = source.Copy(); var target = next.Requests[requestId];
            target.Status = accept ? RequestStatus.Accepted : RequestStatus.Refused;
            if (!accept) target.ResolvedWorldTick = worldTick;
            AddMemory(next, "choice:" + requestId, request.PersonId, requestId, "response:" + requestId,
                accept ? RememberedChoice.HonoredBoundary : RememberedChoice.RefusedWithoutHarm, worldTick);
            AidArcRecord arc;
            if (next.AidArcs.TryGetValue(requestId, out arc)) {
                arc.PlayerAgreed = accept; arc.BoundaryHonored = true;
                arc.Phase = accept ? AidArcPhase.AidAgreed : AidArcPhase.Closed;
            }
            Commit(next, commandId);
            return Accepted(next, accept ? "request_accepted" : "refusal_remembered_without_hostility", false);
        }

        public static ChoiceResult Complete(NeedsState source, string commandId, CompletionEvidence evidence)
        {
            var replay = Replay(source, commandId); if (replay != null) return replay;
            if (source == null || evidence == null) return Rejected(source, "invalid_evidence");
            RequestRecord request;
            if (!source.Requests.TryGetValue(evidence.RequestId, out request)) return Rejected(source, "unknown_request");
            if (request.Status == RequestStatus.Completed) return Rejected(source, "aid_already_committed");
            if (request.Status != RequestStatus.Accepted) return Rejected(source, "request_not_accepted");
            if (!ValidCompletion(request, evidence)) return Rejected(source, "world_evidence_does_not_match_request");

            var next = source.Copy();
            next.Requests[request.RequestId].Status = RequestStatus.Completed;
            next.Requests[request.RequestId].ResolvedWorldTick = evidence.WorldTick;
            AddMemory(next, "help:" + request.RequestId, request.PersonId, request.RequestId,
                evidence.EvidenceId, RememberedChoice.Helped, evidence.WorldTick);
            AidArcRecord completedArc;
            if (next.AidArcs.TryGetValue(request.RequestId, out completedArc) && completedArc.ContributionWitnessed && completedArc.BoundaryHonored)
                completedArc.Phase = AidArcPhase.TrustEstablished;
            Commit(next, commandId);
            return Accepted(next, "concrete_aid_remembered", true);
        }

        public static AidArcResult RecordEncounterActivation(NeedsState source, string commandId,
            string requestId, string activationId, bool encounterActivationVerified)
        {
            var replay = ArcReplay(source, commandId, requestId); if (replay != null) return replay;
            AidArcRecord arc;
            if (source == null || !source.AidArcs.TryGetValue(requestId, out arc)) return ArcRejected(source, "unknown_aid_arc");
            if (!arc.PlayerAgreed || arc.Phase != AidArcPhase.AidAgreed) return ArcRejected(source, "aid_not_agreed");
            if (!encounterActivationVerified || String.IsNullOrEmpty(activationId)) return ArcRejected(source, "encounter_activation_not_verified");
            var next = source.Copy(); var target = next.AidArcs[requestId];
            target.EventActivationVerified = true; target.ActivationId = activationId; target.Phase = AidArcPhase.EventActivated;
            Commit(next, commandId); return ArcAccepted(next, target, "encounter_activation_fact_remembered");
        }

        public static AidArcResult RecordWitnessedContribution(NeedsState source, string commandId,
            string requestId, string activationId, string contributionEvidenceId, bool authoritativeWitness)
        {
            var replay = ArcReplay(source, commandId, requestId); if (replay != null) return replay;
            AidArcRecord arc;
            if (source == null || !source.AidArcs.TryGetValue(requestId, out arc)) return ArcRejected(source, "unknown_aid_arc");
            if (arc.Phase != AidArcPhase.EventActivated || arc.ActivationId != activationId)
                return ArcRejected(source, "encounter_activation_mismatch");
            if (!authoritativeWitness || String.IsNullOrEmpty(contributionEvidenceId)) return ArcRejected(source, "contribution_not_witnessed");
            var next = source.Copy(); var target = next.AidArcs[requestId];
            target.ContributionWitnessed = true; target.ContributionEvidenceId = contributionEvidenceId;
            target.Phase = AidArcPhase.ContributionWitnessed; Commit(next, commandId);
            return ArcAccepted(next, target, "contribution_fact_remembered");
        }

        public static PersonalInvitationHandoff BuildPersonalInvitationHandoff(NeedsState state, string requestId)
        {
            var reasons = new List<string>(); AidArcRecord arc; RequestRecord request;
            if (state == null || !state.AidArcs.TryGetValue(requestId, out arc) || !state.Requests.TryGetValue(requestId, out request))
                return new PersonalInvitationHandoff { RequestId = requestId, MayPresentPersonalOffer = false, Reasons = new[] { "unknown_aid_arc" } };
            if (request.Status != RequestStatus.Completed) reasons.Add("concrete_need_not_completed");
            if (!arc.ContributionWitnessed) reasons.Add("contribution_not_witnessed");
            if (!arc.BoundaryHonored) reasons.Add("boundary_not_honored");
            if (ReadRelationship(state, arc.PersonId) == RelationshipReading.Unsafe) reasons.Add("unresolved_griefing");
            return new PersonalInvitationHandoff { RequestId = requestId, PersonId = arc.PersonId,
                MayPresentPersonalOffer = reasons.Count == 0, Reasons = reasons.ToArray() };
        }

        public static TemporaryTravelEvidenceHandoff BuildTemporaryTravelHandoff(NeedsState state, string requestId)
        {
            AidArcRecord arc;
            if (state == null || !state.AidArcs.TryGetValue(requestId, out arc))
                return new TemporaryTravelEvidenceHandoff { RequestId = requestId, MayRequestTemporaryAssignment = false,
                    Reasons = new[] { "unknown_aid_arc" } };
            var reasons = new List<string>();
            if (!arc.PlayerAgreed || arc.Phase != AidArcPhase.AidAgreed) reasons.Add("aid_not_agreed");
            if (!arc.BoundaryHonored) reasons.Add("boundary_not_honored");
            if (ReadRelationship(state, arc.PersonId) == RelationshipReading.Unsafe) reasons.Add("unresolved_griefing");
            return new TemporaryTravelEvidenceHandoff { RequestId = requestId, PersonId = arc.PersonId,
                MayRequestTemporaryAssignment = reasons.Count == 0, Reasons = reasons.ToArray() };
        }

        public static AidArcResult RecordInvitationEligibility(NeedsState source, string commandId, string requestId)
        {
            var replay = ArcReplay(source, commandId, requestId); if (replay != null) return replay;
            var handoff = BuildPersonalInvitationHandoff(source, requestId);
            if (!handoff.MayPresentPersonalOffer) return ArcRejected(source,
                handoff.Reasons.Length == 0 ? "invitation_not_eligible" : handoff.Reasons[0]);
            var next = source.Copy(); var arc = next.AidArcs[requestId];
            arc.Phase = AidArcPhase.InvitationEligible; Commit(next, commandId);
            return ArcAccepted(next, arc, "personal_invitation_eligible");
        }

        public static ChoiceResult RecordGriefing(NeedsState source, string commandId, string personId,
            string evidenceId, long worldTick)
        {
            var replay = Replay(source, commandId); if (replay != null) return replay;
            if (source == null || String.IsNullOrEmpty(personId) || String.IsNullOrEmpty(evidenceId))
                return Rejected(source, "invalid_evidence");
            var next = source.Copy();
            AddMemory(next, "grief:" + personId + ":" + evidenceId, personId, "", evidenceId,
                RememberedChoice.Griefed, worldTick);
            Commit(next, commandId);
            return Accepted(next, "griefing_remembered", false);
        }

        public static RelationshipReading ReadRelationship(NeedsState state, string personId)
        {
            var helped = false; var boundary = false;
            foreach (var memory in state.Memories.Values) if (memory.PersonId == personId)
            {
                if (memory.Choice == RememberedChoice.Griefed) return RelationshipReading.Unsafe;
                if (memory.Choice == RememberedChoice.Helped) helped = true;
                if (memory.Choice == RememberedChoice.HonoredBoundary || memory.Choice == RememberedChoice.RefusedWithoutHarm) boundary = true;
            }
            if (helped && boundary) return RelationshipReading.Trusting;
            if (helped || boundary) return RelationshipReading.Known;
            return RelationshipReading.Wary;
        }

        public static InvitationEvidenceHandoff BuildInvitationHandoff(NeedsState state, string personId,
            bool bondedRosterSeatOpen)
        {
            var helped = false; var boundary = false; var grief = false;
            foreach (var memory in state.Memories.Values) if (memory.PersonId == personId)
            {
                helped |= memory.Choice == RememberedChoice.Helped;
                boundary |= memory.Choice == RememberedChoice.HonoredBoundary || memory.Choice == RememberedChoice.RefusedWithoutHarm;
                grief |= memory.Choice == RememberedChoice.Griefed;
            }
            var reasons = new List<string>();
            if (!helped) reasons.Add("no_completed_concrete_need");
            if (grief) reasons.Add("unresolved_griefing");
            if (!bondedRosterSeatOpen) reasons.Add("bonded_roster_full");
            return new InvitationEvidenceHandoff { PersonId = personId, ConcreteNeedCompleted = helped,
                HonoredBoundary = boundary, UnresolvedGriefing = grief,
                ReadyForRescueReleaseReview = helped && !grief && bondedRosterSeatOpen, Reasons = reasons.ToArray() };
        }

        public static bool TryBuildQuestRequestFact(NeedsState state, string requestId, out QuestRequestFact fact)
        {
            fact = null; RequestRecord request;
            if (state == null || String.IsNullOrEmpty(requestId) || !state.Requests.TryGetValue(requestId, out request)) return false;
            fact = new QuestRequestFact { RequestId = request.RequestId, PersonId = request.PersonId,
                EncounterId = request.EncounterId, AuthoredNeedId = request.AuthoredNeedId, TargetId = request.TargetId,
                Description = request.Description, Kind = request.Kind, Status = request.Status,
                RequiredAmount = request.RequiredAmount };
            return true;
        }

        static bool Eligible(NeedCandidate c, string personId, NeedsState state)
        {
            if (c == null || c.PersonId != personId || String.IsNullOrEmpty(c.RequestId) ||
                String.IsNullOrEmpty(c.EncounterId) || String.IsNullOrEmpty(c.AuthoredNeedId) ||
                String.IsNullOrEmpty(c.TargetId) || String.IsNullOrEmpty(c.Description)) return false;
            if (!c.PersonNearby || !c.TargetNearby || !c.TargetLoaded || !c.AuthoredForPerson || !c.FactCurrentlyTrue) return false;
            if (c.PlayerOwnsTarget || c.IsRepeatable || c.RequiredAmount < 1) return false;
            if (c.Kind == NeedKind.LegalDungeonMaterials && !c.ProgressionLegal) return false;
            return !state.Requests.ContainsKey(c.RequestId);
        }

        static bool ValidCompletion(RequestRecord request, CompletionEvidence e)
        {
            if (!e.Authoritative || !e.PersonNearby || !e.TargetNearby || !e.TargetLoaded || !e.RequirementSatisfied) return false;
            if (e.RequestId != request.RequestId || e.PersonId != request.PersonId || e.TargetId != request.TargetId || e.Kind != request.Kind) return false;
            if (String.IsNullOrEmpty(e.EvidenceId) || e.PlayerOwnsTarget || e.DeliveredAmount < request.RequiredAmount) return false;
            return request.Kind != NeedKind.LegalDungeonMaterials || e.ProgressionLegal;
        }

        static void AddMemory(NeedsState state, string id, string personId, string requestId,
            string evidenceId, RememberedChoice choice, long tick)
        {
            if (!state.Memories.ContainsKey(id)) state.Memories.Add(id, new ChoiceMemory {
                MemoryId = id, PersonId = personId, RequestId = requestId, EvidenceId = evidenceId,
                Choice = choice, WorldTick = tick });
        }
        static void Commit(NeedsState state, string commandId) { state.AppliedCommands.Add(commandId); state.Revision++; }
        static ChoiceResult Replay(NeedsState state, string id)
        { return state != null && state.AppliedCommands.Contains(id) ? new ChoiceResult { Accepted = true, Reason = "command_already_applied", State = state, Replayed = true, GrantsReward = false } : null; }
        static ChoiceResult Accepted(NeedsState state, string reason, bool reward)
        { return new ChoiceResult { Accepted = true, Reason = reason, State = state, GrantsReward = reward }; }
        static ChoiceResult Rejected(NeedsState state, string reason)
        { return new ChoiceResult { Accepted = false, Reason = reason, State = state, GrantsReward = false }; }
        static RequestSelection Denied(NeedsState state, string reason)
        { return new RequestSelection { Offered = false, Reason = reason, State = state }; }
        static AidArcResult ArcReplay(NeedsState state, string id, string requestId)
        { AidArcRecord arc; return state != null && state.AppliedCommands.Contains(id) ? new AidArcResult { Accepted = true,
            Replayed = true, Reason = "command_already_applied", State = state,
            Arc = state.AidArcs.TryGetValue(requestId, out arc) ? arc.Copy() : null } : null; }
        static AidArcResult ArcAccepted(NeedsState state, AidArcRecord arc, string reason)
        { return new AidArcResult { Accepted = true, Reason = reason, State = state, Arc = arc.Copy() }; }
        static AidArcResult ArcRejected(NeedsState state, string reason)
        { return new AidArcResult { Accepted = false, Reason = reason, State = state }; }
        static ulong StableScore(long seed, string text)
        {
            ulong hash = 14695981039346656037UL;
            var input = seed.ToString(CultureInfo.InvariantCulture) + "|" + text;
            var bytes = Encoding.UTF8.GetBytes(input);
            for (var i = 0; i < bytes.Length; i++) { hash ^= bytes[i]; hash *= 1099511628211UL; }
            return hash;
        }
    }
}
