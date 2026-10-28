using System;
using System.Globalization;
using System.Text;

namespace LivingWorld.CompanionSystems.QuestBroker
{
    public static class QuestBrokerPolicy
    {
        public static BrokerResult Create(BrokerState state, BrokerConfig config, string commandId,
            long worldSeed, long worldTick, RequestCandidate c, PresentationProof source)
        {
            var replay = Replay(state, commandId); if (replay != null) return replay;
            if (state == null || config == null || String.IsNullOrEmpty(commandId) || c == null)
                return Reject(state, "invalid_request");
            string invalid = ValidateCandidate(state, config, worldTick, c);
            if (invalid != null) return Reject(state, invalid);
            foreach (var old in state.Requests.Values)
                if (old.AuthoredRequestId == c.AuthoredRequestId && old.PartyId == c.PartyId)
                    return Reject(state, "authored_request_already_exists");
            if (CooldownActive(state, c.PartyId, worldTick, config.ResolutionCooldownTicks))
                return Reject(state, "resolution_cooldown_active");
            if (config.MaximumActiveRequestsPerParty < 1 || ActiveCount(state, c.PartyId) >= config.MaximumActiveRequestsPerParty)
                return Reject(state, "active_request_limit_reached");

            string requestId = "lwc-request-" + StableHash(worldSeed, c.PartyId + "|" + c.AuthoredRequestId + "|" + c.SiteIdentity);
            var next = state.Copy();
            var request = new WorldRequest {
                RequestId = requestId, AuthoredRequestId = c.AuthoredRequestId, PartyId = c.PartyId,
                IssuerPersonId = c.IssuerPersonId, EncounterId = c.EncounterId, WorldId = c.WorldId,
                SiteId = c.SiteId, SiteIdentity = c.SiteIdentity, Biome = c.Biome, Description = c.Description,
                RequiredWorldKey = c.RequiredWorldKey, AuthoredEncounterId = c.AuthoredEncounterId,
                EncounterAdapterId = c.EncounterAdapterId, Kind = c.Kind, Visibility = c.Visibility, Status = RequestStatus.Offered,
                CreatedWorldTick = worldTick, ExpiresWorldTick = c.ExpiresWorldTick, ResolvedWorldTick = -1
            };
            next.Requests.Add(requestId, request);
            var presentationError = AddPresentationInternal(next, request, source);
            if (presentationError != null) return Reject(state, presentationError);
            Commit(next, commandId);
            var presentation = FindPresentation(next, requestId, source.SourceKind, source.SourceId);
            return Success(next, request, presentation, "persistent_world_request_created", true, false);
        }

        public static BrokerResult PresentExisting(BrokerState state, string commandId, string requestId, PresentationProof source)
        {
            var replay = Replay(state, commandId); if (replay != null) return replay;
            WorldRequest request;
            if (state == null || String.IsNullOrEmpty(commandId) || !state.Requests.TryGetValue(requestId, out request))
                return Reject(state, "unknown_request");
            if (request.Status != RequestStatus.Offered && request.Status != RequestStatus.Accepted)
                return Reject(state, "request_closed");
            var next = state.Copy();
            var error = AddPresentationInternal(next, next.Requests[requestId], source);
            if (error != null) return Reject(state, error);
            Commit(next, commandId);
            return Success(next, next.Requests[requestId], FindPresentation(next, requestId, source.SourceKind, source.SourceId),
                "existing_request_presented", true, false);
        }

        public static BrokerResult Respond(BrokerState state, string commandId, string requestId, bool accept, long worldTick)
        {
            var replay = Replay(state, commandId); if (replay != null) return replay;
            WorldRequest r;
            if (state == null || String.IsNullOrEmpty(commandId) || !state.Requests.TryGetValue(requestId, out r)) return Reject(state, "unknown_request");
            if (r.Status != RequestStatus.Offered) return Reject(state, "request_not_awaiting_response");
            if (worldTick < r.CreatedWorldTick || worldTick >= r.ExpiresWorldTick) return Reject(state, "request_expired");
            var next = state.Copy(); var n = next.Requests[requestId];
            n.Status = accept ? RequestStatus.Accepted : RequestStatus.Refused;
            if (!accept) n.ResolvedWorldTick = worldTick;
            Commit(next, commandId);
            return Success(next, n, null, accept ? "request_accepted" : "request_refused", false, false);
        }

        public static BrokerResult Complete(BrokerState state, string commandId, CompletionProof proof)
        {
            var replay = Replay(state, commandId); if (replay != null) return replay;
            WorldRequest r;
            if (state == null || String.IsNullOrEmpty(commandId) || proof == null || !state.Requests.TryGetValue(proof.RequestId, out r))
                return Reject(state, "unknown_request");
            if (r.Status == RequestStatus.Completed) return Reject(state, "outcome_already_claimed");
            if (r.Status != RequestStatus.Accepted) return Reject(state, "request_not_accepted");
            if (proof.WorldTick < r.CreatedWorldTick || proof.WorldTick >= r.ExpiresWorldTick) return Reject(state, "request_expired");
            if (!proof.ServerAuthority || !proof.TargetLoaded || !proof.PlayerAtTarget || !proof.ObjectiveObservedComplete)
                return Reject(state, "authoritative_loaded_completion_required");
            if (String.IsNullOrEmpty(proof.EvidenceId) || proof.WorldId != r.WorldId || proof.SiteId != r.SiteId || proof.SiteIdentity != r.SiteIdentity)
                return Reject(state, "completion_site_identity_mismatch");
            if (!proof.ProgressionLegal) return Reject(state, "progression_gate_failed");
            if (r.Kind == RequestKind.AuthoredMiniboss && !proof.EncounterAdapterOwnsOutcome)
                return Reject(state, "verified_encounter_adapter_outcome_required");
            var next = state.Copy(); var n = next.Requests[r.RequestId];
            n.Status = RequestStatus.Completed; n.ResolvedWorldTick = proof.WorldTick;
            n.CompletionEvidenceId = proof.EvidenceId; n.ClaimId = "claim:" + r.RequestId;
            Commit(next, commandId);
            var result = Success(next, n, null, "single_claim_outcome_committed", false, true);
            result.ClaimId = n.ClaimId; return result;
        }

        public static BrokerResult Expire(BrokerState state, string commandId, string requestId, bool serverAuthority, long worldTick)
        {
            var replay = Replay(state, commandId); if (replay != null) return replay;
            WorldRequest r;
            if (state == null || !serverAuthority || String.IsNullOrEmpty(commandId) || !state.Requests.TryGetValue(requestId, out r))
                return Reject(state, "unknown_or_unauthorized_request");
            if (r.Status != RequestStatus.Offered && r.Status != RequestStatus.Accepted) return Reject(state, "request_closed");
            if (worldTick < r.ExpiresWorldTick) return Reject(state, "expiry_not_reached");
            var next = state.Copy(); var n = next.Requests[requestId];
            n.Status = RequestStatus.Expired; n.ResolvedWorldTick = worldTick; Commit(next, commandId);
            return Success(next, n, null, "request_expired_without_simulation", false, false);
        }

        static string ValidateCandidate(BrokerState state, BrokerConfig cfg, long tick, RequestCandidate c)
        {
            if (!c.ServerAuthority) return "server_authority_required";
            if (String.IsNullOrEmpty(c.AuthoredRequestId) || String.IsNullOrEmpty(c.PartyId) ||
                String.IsNullOrEmpty(c.IssuerPersonId) || String.IsNullOrEmpty(c.WorldId) ||
                String.IsNullOrEmpty(c.SiteId) || String.IsNullOrEmpty(c.SiteIdentity) ||
                String.IsNullOrEmpty(c.Biome) || String.IsNullOrEmpty(c.Description)) return "missing_stable_identity";
            if (!c.SitePersisted || !c.SiteKnownToIssuer || !c.WorldFactCurrent) return "persisted_known_world_fact_required";
            if (!c.SafeGround || !c.Reachable || !c.BiomeMatches) return "unsafe_or_unreachable_target";
            if (!c.ProgressionLegal) return "progression_gate_failed";
            if (Double.IsNaN(c.DistanceFromIssuer) || Double.IsInfinity(c.DistanceFromIssuer) || c.DistanceFromIssuer < 0d || c.DistanceFromIssuer > cfg.MaximumTargetDistance)
                return "target_outside_bounded_distance";
            if (tick < 0 || cfg.MinimumLifetimeTicks < 1 || c.ExpiresWorldTick < tick + cfg.MinimumLifetimeTicks) return "invalid_expiry";
            if (c.Kind == RequestKind.CompanionNeed && String.IsNullOrEmpty(c.EncounterId)) return "companion_encounter_identity_required";
            if (c.Kind == RequestKind.AuthoredMiniboss && (!c.AuthoredEncounter || !c.SafeSpawnAdapterVerified ||
                String.IsNullOrEmpty(c.AuthoredEncounterId) || String.IsNullOrEmpty(c.EncounterAdapterId)))
                return "verified_authored_encounter_adapter_required";
            return null;
        }

        static string AddPresentationInternal(BrokerState state, WorldRequest r, PresentationProof p)
        {
            if (p == null || !p.ServerAuthority || !p.SourceExists || !p.SourceLoadedAndNearby || !p.SourceKnowsRequest || String.IsNullOrEmpty(p.SourceId))
                return "verified_offer_source_required";
            if (p.WorldTick < r.CreatedWorldTick || p.WorldTick >= r.ExpiresWorldTick) return "presentation_outside_request_lifetime";
            if (p.SourceKind == OfferSourceKind.CampPerson && (String.IsNullOrEmpty(p.SourcePersonId) || p.SourcePersonId != r.IssuerPersonId))
                return "issuer_identity_mismatch";
            if ((p.SourceKind == OfferSourceKind.SettlementBoard || p.SourceKind == OfferSourceKind.Jarl) &&
                r.Visibility != RequestVisibility.PublicSurfaces) return "private_request_cannot_use_public_surface";
            if (p.SourceKind == OfferSourceKind.Jarl && String.IsNullOrEmpty(p.SourcePersonId)) return "jarl_identity_required";
            if ((p.SourceKind == OfferSourceKind.SettlementBoard || p.SourceKind == OfferSourceKind.Jarl) &&
                (!p.BoardOrJarlAtKnownSettlement || String.IsNullOrEmpty(p.SettlementId))) return "known_settlement_source_required";
            string id = "presentation:" + r.RequestId + ":" + ((int)p.SourceKind).ToString(CultureInfo.InvariantCulture) + ":" + p.SourceId;
            if (state.Presentations.ContainsKey(id)) return "presentation_already_recorded";
            state.Presentations.Add(id, new OfferPresentation { PresentationId = id, RequestId = r.RequestId,
                SourceId = p.SourceId, SourcePersonId = p.SourcePersonId, SettlementId = p.SettlementId,
                SourceKind = p.SourceKind, PresentedWorldTick = p.WorldTick });
            return null;
        }

        static OfferPresentation FindPresentation(BrokerState s, string requestId, OfferSourceKind kind, string sourceId)
        {
            string id = "presentation:" + requestId + ":" + ((int)kind).ToString(CultureInfo.InvariantCulture) + ":" + sourceId;
            return s.Presentations[id].Copy();
        }
        static bool CooldownActive(BrokerState s, string party, long tick, long cooldown)
        {
            foreach (var r in s.Requests.Values) if (r.PartyId == party && r.ResolvedWorldTick >= 0 && tick < r.ResolvedWorldTick + cooldown) return true;
            return false;
        }
        static int ActiveCount(BrokerState s, string party)
        {
            int count = 0;
            foreach (var r in s.Requests.Values) if (r.PartyId == party && (r.Status == RequestStatus.Offered || r.Status == RequestStatus.Accepted)) count++;
            return count;
        }
        static void Commit(BrokerState s, string id) { s.AppliedCommands.Add(id); s.Revision++; }
        static BrokerResult Replay(BrokerState s, string id)
        { return s != null && !String.IsNullOrEmpty(id) && s.AppliedCommands.Contains(id) ? new BrokerResult { Applied = true, Replayed = true, Reason = "command_already_applied", State = s } : null; }
        static BrokerResult Reject(BrokerState s, string reason) { return new BrokerResult { Reason = reason, State = s }; }
        static BrokerResult Success(BrokerState s, WorldRequest r, OfferPresentation p, string reason, bool present, bool claim)
        { return new BrokerResult { Applied = true, Reason = reason, State = s, Request = r.Copy(), Presentation = p, MayPresent = present, SingleClaimCommitted = claim }; }
        static string StableHash(long seed, string value)
        {
            ulong h = 14695981039346656037UL; var bytes = Encoding.UTF8.GetBytes(seed.ToString(CultureInfo.InvariantCulture) + "|" + value);
            for (int i = 0; i < bytes.Length; i++) { h ^= bytes[i]; h *= 1099511628211UL; }
            return h.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
