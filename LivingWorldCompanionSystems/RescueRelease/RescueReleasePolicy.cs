using System;
using System.Collections.Generic;

namespace LivingWorld.Companions.RescueRelease
{
    public sealed class RescueReleasePolicy
    {
        public const int ExpeditionLimit = 2;
        public const int BondedLimit = 6;

        public PolicyResult ResolveRescue(WorldState source, string commandId, string personId, RescueOutcome outcome)
        {
            var replay = Replay(source, commandId);
            if (replay != null) return replay;
            CompanionRecord person;
            if (!source.People.TryGetValue(personId, out person)) return Reject(source, "unknown_person");
            if (person.Life == LifeState.Dead) return Reject(source, "committed_death_is_terminal");
            if (person.Rescue != RescueOutcome.Unresolved)
            {
                if (person.Rescue == outcome) return CommitNoChange(source, commandId, "rescue_already_resolved");
                return Reject(source, "rescue_outcome_already_committed");
            }
            if (outcome == RescueOutcome.Unresolved) return Reject(source, "outcome_must_be_final");
            if (outcome == RescueOutcome.Died)
                return Reject(source, "use_commit_death_for_a_factual_death_record");

            var next = source.Copy();
            next.People[personId].Rescue = outcome;
            Commit(next, commandId);
            return Accept(next, "rescue_outcome_committed");
        }

        public InvitationResult Invite(WorldState source, string commandId, string personId, InvitationFacts facts, int unlockedExpeditionSlots)
        {
            if (source.AppliedCommands.Contains(commandId))
                return new InvitationResult { Decision = InvitationDecision.Yes, Reasons = new[] { "command_already_applied" }, State = source, Replayed = true };

            CompanionRecord person;
            var reasons = new List<string>();
            SafeDestination safeDestination;
            var hasSafeDestination = !String.IsNullOrEmpty(facts.SafeDestinationId) && source.Destinations.TryGetValue(facts.SafeDestinationId, out safeDestination) && safeDestination.Established;
            if (!source.People.TryGetValue(personId, out person)) reasons.Add("unknown_person");
            else
            {
                if (person.DebugIdentity) reasons.Add("debug_identity_cannot_be_recruited");
                if (person.Life == LifeState.Dead) reasons.Add("committed_death_is_terminal");
                if (person.Assignment != Assignment.Known && person.Assignment != Assignment.ReleasedKnown) reasons.Add("person_is_already_bonded_or_assigned");
                if (String.IsNullOrEmpty(person.OriginEncounterId)) reasons.Add("no_persistent_world_encounter");
                if (person.Rescue != RescueOutcome.Rescued || !facts.RescueCompleted) reasons.Add("rescue_not_factually_complete");
                if (facts.Hostile) reasons.Add("hostile");
                if (facts.Betrayed) reasons.Add("betrayal");
                if (facts.DependentCannotTravel) reasons.Add("dependent_cannot_travel");
                if (facts.ImmediateDutyUnfinished) reasons.Add("immediate_duty_unfinished");
                if (facts.IncompatibleAllegiance) reasons.Add("incompatible_allegiance");
                if (facts.EssentialSafeDestinationMissing || !hasSafeDestination) reasons.Add("essential_safe_destination_missing");
                if (facts.RoleVowBlocksTravel) reasons.Add("role_vow_blocks_travel");
                if (!HasPositiveFact(facts)) reasons.Add("no_supporting_world_fact");
                if (CountBonded(source) >= BondedLimit) reasons.Add("bonded_roster_full");
            }

            if (reasons.Count != 0)
                return new InvitationResult { Decision = InvitationDecision.No, Reasons = reasons.ToArray(), State = source };

            var next = source.Copy();
            var target = next.People[personId];
            var slots = Math.Max(0, Math.Min(ExpeditionLimit, unlockedExpeditionSlots));
            target.Assignment = CountFieldActive(source) < slots ? Assignment.Expedition : Assignment.BondedResident;
            target.HomeId = facts.SafeDestinationId;
            target.LastSafePositionId = facts.SafeDestinationId;
            Commit(next, commandId);
            return new InvitationResult { Decision = InvitationDecision.Yes, Reasons = new[] { target.Assignment == Assignment.Expedition ? "facts_support_invitation_and_expedition_slot_open" : "facts_support_invitation_as_resident" }, State = next };
        }

        public ReleaseResult RequestRelease(WorldState source, string commandId, string personId, string destinationId, ReleaseSafety safety)
        {
            if (source.AppliedCommands.Contains(commandId)) return ReplayedRelease(source);
            CompanionRecord person;
            if (!source.People.TryGetValue(personId, out person)) return RejectedRelease(source, "unknown_person");
            if (person.Life == LifeState.Dead) return RejectedRelease(source, "committed_death_is_terminal");
            if (person.Assignment != Assignment.Expedition && person.Assignment != Assignment.AwaitingSafeHandoff)
                return RejectedRelease(source, "person_is_not_in_the_expedition");

            SafeDestination destination;
            var destinationValid = !String.IsNullOrEmpty(destinationId) && source.Destinations.TryGetValue(destinationId, out destination) && destination.Established;
            if (!destinationValid || Unsafe(safety))
            {
                var waiting = source.Copy();
                var target = waiting.People[personId];
                target.Assignment = Assignment.AwaitingSafeHandoff;
                target.PendingDestinationId = destinationValid ? destinationId : null;
                Commit(waiting, commandId);
                return new ReleaseResult { Decision = ReleaseDecision.WaitingForSafety, Reason = destinationValid ? "unsafe_transition" : "no_valid_safe_destination", State = waiting };
            }
            return CommitRelease(source, commandId, personId, destinationId, safety);
        }

        public ReleaseResult CommitRelease(WorldState source, string commandId, string personId, string destinationId, ReleaseSafety proof)
        {
            if (source.AppliedCommands.Contains(commandId)) return ReplayedRelease(source);
            CompanionRecord person;
            SafeDestination destination;
            if (!source.People.TryGetValue(personId, out person)) return RejectedRelease(source, "unknown_person");
            if (person.Life == LifeState.Dead) return RejectedRelease(source, "committed_death_is_terminal");
            if (person.Assignment != Assignment.Expedition && person.Assignment != Assignment.AwaitingSafeHandoff)
                return RejectedRelease(source, "person_is_not_in_the_expedition");
            if (!source.Destinations.TryGetValue(destinationId, out destination) || !destination.Established)
                return RejectedRelease(source, "invalid_safe_destination");
            if (Unsafe(proof) || !proof.DestinationZoneLoaded || !proof.CompanionPresentAtDestination)
                return RejectedRelease(source, "world_handoff_not_proven");
            if (!proof.LoanExchangeComplete || HasPlayerLoans(person))
                return RejectedRelease(source, "player_loan_exchange_required");

            var next = source.Copy();
            var target = next.People[personId];
            target.Assignment = Assignment.BondedResident;
            target.HomeId = destinationId;
            target.PendingDestinationId = null;
            target.LastSafePositionId = destinationId;
            Commit(next, commandId);
            return new ReleaseResult { Decision = ReleaseDecision.Committed, Reason = "safe_handoff_committed", State = next };
        }

        public TemporaryAidResult StartTemporaryAid(WorldState source, string commandId, TemporaryAidFacts facts)
        {
            if (source.AppliedCommands.Contains(commandId)) return ReplayedTemporary(source);
            if (facts == null || String.IsNullOrEmpty(facts.PersonId) || String.IsNullOrEmpty(facts.RequestId)) return RejectedTemporary(source, "invalid_temporary_aid_facts");
            CompanionRecord person;
            SafeDestination destination;
            if (!source.People.TryGetValue(facts.PersonId, out person)) return RejectedTemporary(source, "unknown_person");
            if (person.Life == LifeState.Dead) return RejectedTemporary(source, "committed_death_is_terminal");
            if (person.DebugIdentity) return RejectedTemporary(source, "debug_identity_cannot_join_campaign_aid");
            if (person.Assignment != Assignment.Known && person.Assignment != Assignment.ReleasedKnown) return RejectedTemporary(source, "person_already_assigned");
            if (!facts.AuthoredRequest || !facts.RequestAccepted || !facts.PersonExplicitlyAgreed) return RejectedTemporary(source, "temporary_aid_not_factually_agreed");
            if (facts.PersonId != person.PersonId || facts.EncounterId != person.OriginEncounterId) return RejectedTemporary(source, "temporary_aid_identity_mismatch");
            if (String.IsNullOrEmpty(facts.ReturnDestinationId) || !source.Destinations.TryGetValue(facts.ReturnDestinationId, out destination) || !destination.Established)
                return RejectedTemporary(source, "safe_return_destination_required");
            if (CountFieldActive(source) >= ExpeditionLimit) return RejectedTemporary(source, "active_field_slots_full");

            var next = source.Copy();
            var target = next.People[facts.PersonId];
            target.Assignment = Assignment.TemporaryAlly;
            target.TemporaryRequestId = facts.RequestId;
            target.TemporaryReturnDestinationId = facts.ReturnDestinationId;
            target.TemporaryPortalTravelAllowed = facts.AllowPortalTravel;
            Commit(next, commandId);
            return new TemporaryAidResult { Decision = TemporaryAidDecision.Started, Reason = "temporary_aid_started_without_bond", State = next };
        }

        public TemporaryAidResult RequestTemporaryReturn(WorldState source, string commandId, string personId, TemporaryAidEndReason reason, ReleaseSafety safety)
        {
            if (source.AppliedCommands.Contains(commandId)) return ReplayedTemporary(source);
            CompanionRecord person;
            if (!source.People.TryGetValue(personId, out person)) return RejectedTemporary(source, "unknown_person");
            if (person.Life == LifeState.Dead) return RejectedTemporary(source, "committed_death_is_terminal");
            if (person.Assignment != Assignment.TemporaryAlly && person.Assignment != Assignment.AwaitingTemporaryReturn)
                return RejectedTemporary(source, "person_is_not_temporary_ally");

            var next = source.Copy();
            next.People[personId].Assignment = Assignment.AwaitingTemporaryReturn;
            next.People[personId].PendingDestinationId = person.TemporaryReturnDestinationId;
            Commit(next, commandId);
            if (Unsafe(safety) || !safety.DestinationZoneLoaded || !safety.CompanionPresentAtDestination)
                return new TemporaryAidResult { Decision = TemporaryAidDecision.WaitingForReturn, Reason = "temporary_return_waits_for_safe_world_handoff", State = next };
            return CommitTemporaryReturn(next, commandId + ":commit", personId, safety);
        }

        public TemporaryAidResult CommitTemporaryReturn(WorldState source, string commandId, string personId, ReleaseSafety proof)
        {
            if (source.AppliedCommands.Contains(commandId)) return ReplayedTemporary(source);
            CompanionRecord person;
            SafeDestination destination;
            if (!source.People.TryGetValue(personId, out person)) return RejectedTemporary(source, "unknown_person");
            if (person.Life == LifeState.Dead) return RejectedTemporary(source, "committed_death_is_terminal");
            if (person.Assignment != Assignment.TemporaryAlly && person.Assignment != Assignment.AwaitingTemporaryReturn)
                return RejectedTemporary(source, "person_is_not_temporary_ally");
            if (String.IsNullOrEmpty(person.TemporaryReturnDestinationId) || !source.Destinations.TryGetValue(person.TemporaryReturnDestinationId, out destination) || !destination.Established)
                return RejectedTemporary(source, "safe_return_destination_missing");
            if (Unsafe(proof) || !proof.DestinationZoneLoaded || !proof.CompanionPresentAtDestination)
                return RejectedTemporary(source, "temporary_return_world_handoff_not_proven");
            if (HasPlayerLoans(person)) return RejectedTemporary(source, "player_loan_exchange_required");

            var next = source.Copy();
            var target = next.People[personId];
            target.Assignment = Assignment.ReleasedKnown;
            target.HomeId = person.TemporaryReturnDestinationId;
            target.LastSafePositionId = person.TemporaryReturnDestinationId;
            target.PendingDestinationId = null;
            target.TemporaryRequestId = null;
            target.TemporaryReturnDestinationId = null;
            target.TemporaryPortalTravelAllowed = false;
            Commit(next, commandId);
            return new TemporaryAidResult { Decision = TemporaryAidDecision.Returned, Reason = "temporary_ally_returned_without_promotion", State = next };
        }

        public bool CanTemporaryAllyUsePortal(WorldState source, string personId)
        {
            CompanionRecord person;
            return source != null && source.People.TryGetValue(personId, out person) && person.Life == LifeState.Alive &&
                person.Assignment == Assignment.TemporaryAlly && person.TemporaryPortalTravelAllowed;
        }

        public PolicyResult ReturnPlayerLoan(WorldState source, string commandId, string personId, string itemId)
        {
            var replay = Replay(source, commandId);
            if (replay != null) return replay;
            CompanionRecord person;
            if (!source.People.TryGetValue(personId, out person)) return Reject(source, "unknown_person");
            var next = source.Copy();
            var gear = next.People[personId].Gear;
            for (var i = 0; i < gear.Count; i++)
            {
                if (gear[i].ItemId == itemId && gear[i].Owner == GearOwner.PlayerLoan)
                {
                    gear.RemoveAt(i);
                    Commit(next, commandId);
                    return Accept(next, "player_loan_returned");
                }
            }
            return Reject(source, "player_loan_not_found");
        }

        public PolicyResult CommitDeath(WorldState source, string commandId, string personId, DeathRecord death)
        {
            var replay = Replay(source, commandId);
            if (replay != null) return replay;
            CompanionRecord person;
            if (!source.People.TryGetValue(personId, out person)) return Reject(source, "unknown_person");
            if (person.Life == LifeState.Dead) return Reject(source, "committed_death_is_terminal");
            if (death == null || String.IsNullOrEmpty(death.Place) || String.IsNullOrEmpty(death.Cause))
                return Reject(source, "complete_death_evidence_required");

            var next = source.Copy();
            var target = next.People[personId];
            target.Life = LifeState.Dead;
            target.Rescue = RescueOutcome.Died;
            target.Assignment = Assignment.Dead;
            target.PendingDestinationId = null;
            target.Death = death.Copy();
            target.Death.Name = target.Name;
            target.Death.Role = target.Role;
            target.Death.AppearanceId = target.AppearanceId;
            Commit(next, commandId);
            return Accept(next, "death_permanently_committed");
        }

        public PolicyResult RecoverDeadGear(WorldState source, string commandId, string personId, string itemId)
        {
            var replay = Replay(source, commandId);
            if (replay != null) return replay;
            CompanionRecord person;
            if (!source.People.TryGetValue(personId, out person)) return Reject(source, "unknown_person");
            if (person.Life != LifeState.Dead) return Reject(source, "person_is_not_dead");
            var next = source.Copy();
            var gear = next.People[personId].Gear;
            for (var i = 0; i < gear.Count; i++)
            {
                if (gear[i].ItemId != itemId) continue;
                if (gear[i].Recovered) return Reject(source, "gear_already_recovered");
                gear[i].Recovered = true;
                Commit(next, commandId);
                return Accept(next, "gear_recovery_committed");
            }
            return Reject(source, "gear_not_found");
        }

        public string[] ValidateLoadedState(WorldState state)
        {
            var errors = new List<string>();
            if (state == null) return new[] { "state_is_null" };
            if (state.SchemaVersion != WorldState.CurrentSchemaVersion) errors.Add("unsupported_schema_version");
            if (CountFieldActive(state) > ExpeditionLimit) errors.Add("active_field_limit_exceeded");
            if (CountBonded(state) > BondedLimit) errors.Add("bonded_limit_exceeded");
            foreach (var pair in state.People)
            {
                var p = pair.Value;
                if (p == null || p.PersonId != pair.Key) { errors.Add("person_key_mismatch:" + pair.Key); continue; }
                if (p.Life == LifeState.Dead && (p.Death == null || p.Assignment != Assignment.Dead)) errors.Add("dead_identity_not_closed:" + pair.Key);
                if (p.Life == LifeState.Alive && p.Death != null) errors.Add("living_identity_has_death_record:" + pair.Key);
                if (p.DebugIdentity && (p.Assignment == Assignment.Expedition || p.Assignment == Assignment.BondedResident || p.Assignment == Assignment.AwaitingSafeHandoff ||
                    p.Assignment == Assignment.TemporaryAlly || p.Assignment == Assignment.AwaitingTemporaryReturn)) errors.Add("debug_identity_promoted:" + pair.Key);
                if ((p.Assignment == Assignment.TemporaryAlly || p.Assignment == Assignment.AwaitingTemporaryReturn) &&
                    (String.IsNullOrEmpty(p.TemporaryRequestId) || String.IsNullOrEmpty(p.TemporaryReturnDestinationId))) errors.Add("temporary_aid_contract_incomplete:" + pair.Key);
            }
            return errors.ToArray();
        }

        private static bool HasPositiveFact(InvitationFacts f)
        {
            return f.SharedDefense || f.RescueCompleted || f.HonoredWarning || f.ReturnedProperty || f.SanctuaryOffered || f.CompatiblePurpose || f.PriorStanding || f.DefendedHearth;
        }

        private static int CountFieldActive(WorldState state)
        {
            var count = 0;
            foreach (var p in state.People.Values)
                if (p.Life == LifeState.Alive && (p.Assignment == Assignment.Expedition || p.Assignment == Assignment.AwaitingSafeHandoff ||
                    p.Assignment == Assignment.TemporaryAlly || p.Assignment == Assignment.AwaitingTemporaryReturn)) count++;
            return count;
        }

        private static int CountBonded(WorldState state)
        {
            var count = 0;
            foreach (var p in state.People.Values)
                if (p.Life == LifeState.Alive && (p.Assignment == Assignment.Expedition || p.Assignment == Assignment.AwaitingSafeHandoff || p.Assignment == Assignment.BondedResident)) count++;
            return count;
        }

        private static bool HasPlayerLoans(CompanionRecord person)
        {
            for (var i = 0; i < person.Gear.Count; i++) if (person.Gear[i].Owner == GearOwner.PlayerLoan && !person.Gear[i].Recovered) return true;
            return false;
        }

        private static bool Unsafe(ReleaseSafety s)
        {
            return s == null || s.InCombat || s.Incapacitated || s.Sailing || s.PortalTransit || s.UnsafeTerrain;
        }

        private static void Commit(WorldState state, string commandId)
        {
            if (String.IsNullOrEmpty(commandId)) throw new ArgumentException("A stable command id is required.", "commandId");
            state.AppliedCommands.Add(commandId);
            state.Revision++;
        }

        private static PolicyResult Replay(WorldState state, string id)
        {
            return state.AppliedCommands.Contains(id) ? new PolicyResult { Accepted = true, Reason = "command_already_applied", State = state, Replayed = true } : null;
        }

        private static PolicyResult CommitNoChange(WorldState state, string id, string reason)
        {
            var next = state.Copy(); Commit(next, id); return Accept(next, reason);
        }

        private static PolicyResult Accept(WorldState state, string reason) { return new PolicyResult { Accepted = true, Reason = reason, State = state }; }
        private static PolicyResult Reject(WorldState state, string reason) { return new PolicyResult { Accepted = false, Reason = reason, State = state }; }
        private static ReleaseResult ReplayedRelease(WorldState state) { return new ReleaseResult { Decision = ReleaseDecision.Committed, Reason = "command_already_applied", State = state, Replayed = true }; }
        private static ReleaseResult RejectedRelease(WorldState state, string reason) { return new ReleaseResult { Decision = ReleaseDecision.Rejected, Reason = reason, State = state }; }
        private static TemporaryAidResult ReplayedTemporary(WorldState state) { return new TemporaryAidResult { Decision = TemporaryAidDecision.Started, Reason = "command_already_applied", State = state, Replayed = true }; }
        private static TemporaryAidResult RejectedTemporary(WorldState state, string reason) { return new TemporaryAidResult { Decision = TemporaryAidDecision.Rejected, Reason = reason, State = state }; }
    }
}
