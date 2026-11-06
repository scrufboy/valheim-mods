using System;

namespace LivingWorld.CompanionSystems.EncounterActivation
{
    public static class EncounterActivationPolicy
    {
        public static ActivationResult RegisterOffer(ActivationLedger state, string commandId, ActivationPlan plan)
        {
            var replay=Replay(state,commandId); if(replay!=null)return replay;
            if(state==null || String.IsNullOrEmpty(commandId) || !Valid(plan)) return Reject(state,"invalid_activation_plan");
            if(state.Entries.ContainsKey(plan.RequestId)) return Reject(state,"request_execution_already_exists");
            var next=state.Copy(); var e=new ActivationEntry { Plan=plan.Copy(), Status=ActivationStatus.Offered,
                Revision=1, AcceptedWorldTick=-1, ActivatedWorldTick=-1, ResolvedWorldTick=-1 };
            next.Entries.Add(plan.RequestId,e); return Commit(next,commandId,e,"offer_execution_registered",ActivationDirective.None);
        }

        // Call only after QuestBroker has durably accepted this same RequestId.
        public static ActivationResult Accept(ActivationLedger state, string commandId, string requestId, bool brokerAccepted, long tick)
        {
            var replay=Replay(state,commandId); if(replay!=null)return replay; ActivationEntry current;
            if(!Find(state,requestId,out current)) return Reject(state,"unknown_request_execution");
            if(current.Status!=ActivationStatus.Offered) return Reject(state,"execution_not_offered");
            if(!brokerAccepted) return Reject(state,"broker_acceptance_required");
            var next=state.Copy(); var e=next.Entries[requestId]; e.Status=ActivationStatus.Accepted; e.AcceptedWorldTick=tick; e.Revision++;
            return Commit(next,commandId,e,"accepted_request_ready_for_deliberate_approach",ActivationDirective.None);
        }

        public static ActivationResult Arm(ActivationLedger state, string commandId, ArmingProof p)
        {
            var replay=Replay(state,commandId); if(replay!=null)return replay; ActivationEntry current;
            if(p==null || !Find(state,p.RequestId,out current)) return Reject(state,"unknown_request_execution");
            if(current.Status!=ActivationStatus.Accepted) return Reject(state,"execution_not_accepted");
            if(!p.ServerAuthority || !p.BrokerRequestAccepted) return Reject(state,"authoritative_broker_acceptance_required");
            if(!Exact(current.Plan,p.WorldId,p.SiteId,p.SiteIdentity)) return Reject(state,"arming_site_identity_mismatch");
            if(!p.ProgressionLegal || !p.SiteStillSafe) return Reject(state,"progression_or_site_safety_failed");
            if(!p.SiteLoaded || !p.IssuerGroupLoadedAtTarget || !p.PlayerLoadedAtTarget || !p.PlayerDeliberatelyApproached || String.IsNullOrEmpty(p.ObservingPlayerId))
                return Reject(state,"loaded_group_and_player_at_target_required");
            if(!p.IssuerPersonIdentityPreserved || !p.IssuerPersonalEquipmentPreserved)
                return Reject(state,"same_person_and_equipment_required");
            var next=state.Copy(); var e=next.Entries[p.RequestId]; e.Revision++;
            if(e.Plan.Mode==TargetMode.NaturalWorld) { e.Status=ActivationStatus.Active; e.ActivatedWorldTick=p.WorldTick;
                return Commit(next,commandId,e,"natural_target_observation_active",ActivationDirective.ObserveNaturalTarget); }
            e.Status=ActivationStatus.Arming; e.SpawnClaimId="encounter-claim:"+e.Plan.RequestId;
            e.SpawnRequestId="encounter-spawn:"+e.Plan.RequestId; // stable logical request; adapter must deduplicate it
            return Commit(next,commandId,e,"spawn_claim_persisted_before_external_request",ActivationDirective.RequestSpawn);
        }

        public static ActivationResult ConfirmSpawn(ActivationLedger state, string commandId, SpawnConfirmation p)
        {
            var replay=Replay(state,commandId); if(replay!=null)return replay; ActivationEntry current;
            if(p==null || !Find(state,p.RequestId,out current)) return Reject(state,"unknown_request_execution");
            if(current.Status!=ActivationStatus.Arming) return Reject(state,"spawn_not_armed");
            if(!p.ServerAuthority || !p.AdapterAccepted || !p.ActorProvenanceBound || String.IsNullOrEmpty(p.SpawnReceiptId))
                return Reject(state,"verified_adapter_receipt_required");
            if(p.SpawnClaimId!=current.SpawnClaimId || p.SpawnRequestId!=current.SpawnRequestId || !Exact(current.Plan,p.WorldId,p.SiteId,p.SiteIdentity))
                return Reject(state,"spawn_confirmation_identity_mismatch");
            var next=state.Copy(); var e=next.Entries[p.RequestId]; e.Status=ActivationStatus.Active;
            e.SpawnReceiptId=p.SpawnReceiptId; e.ActivatedWorldTick=p.WorldTick; e.Revision++;
            return Commit(next,commandId,e,"authored_encounter_active",ActivationDirective.None);
        }

        public static ActivationResult ObserveClear(ActivationLedger state, string commandId, OutcomeProof p)
        {
            var replay=Replay(state,commandId); if(replay!=null)return replay; ActivationEntry current;
            if(p==null || !Find(state,p.RequestId,out current)) return Reject(state,"unknown_request_execution");
            if(current.Status==ActivationStatus.Clear) return Reject(state,"execution_outcome_already_committed");
            if(current.Status!=ActivationStatus.Active) return Reject(state,"execution_not_active");
            if(!p.ServerAuthority || !p.TargetLoaded || !p.PlayerLoadedAtTarget || !p.IssuerWitnessOrAlertPreserved ||
                !p.ObjectiveObservedComplete || !p.PlayerOrPartyContributed || String.IsNullOrEmpty(p.EvidenceId))
                return Reject(state,"loaded_witnessed_completion_required");
            if(!Exact(current.Plan,p.WorldId,p.SiteId,p.SiteIdentity)) return Reject(state,"completion_site_identity_mismatch");
            if(current.Plan.Mode!=TargetMode.NaturalWorld && (p.SpawnClaimId!=current.SpawnClaimId || !p.AllClaimedActorsDead))
                return Reject(state,"claimed_actor_outcome_required");
            if(current.Plan.Mode==TargetMode.AuthoredMiniboss && !p.CommittedBossDeath)
                return Reject(state,"committed_boss_death_required");
            var next=state.Copy(); var e=next.Entries[p.RequestId]; e.Status=ActivationStatus.Clear;
            e.OutcomeEvidenceId=p.EvidenceId; e.ResolvedWorldTick=p.WorldTick; e.Revision++;
            return Commit(next,commandId,e,"execution_clear_evidence_ready_for_broker",ActivationDirective.NotifyBrokerClear);
        }

        public static ActivationResult Close(ActivationLedger state,string commandId,string requestId,bool serverAuthority,bool abandon,long tick)
        {
            var replay=Replay(state,commandId); if(replay!=null)return replay; ActivationEntry current;
            if(!Find(state,requestId,out current))return Reject(state,"unknown_request_execution");
            if(!serverAuthority)return Reject(state,"server_authority_required");
            if(current.Status==ActivationStatus.Clear || current.Status==ActivationStatus.Failed || current.Status==ActivationStatus.Abandoned)
                return Reject(state,"execution_already_closed");
            var next=state.Copy(); var e=next.Entries[requestId]; e.Status=abandon?ActivationStatus.Abandoned:ActivationStatus.Failed;
            e.ResolvedWorldTick=tick; e.Revision++;
            // No destruction directive: living visible hostiles remain native world actors.
            return Commit(next,commandId,e,abandon?"execution_abandoned_without_forced_cleanup":"execution_failed_without_offscreen_resolution",ActivationDirective.None);
        }

        static bool Valid(ActivationPlan p) { return p!=null && !String.IsNullOrEmpty(p.RequestId) && !String.IsNullOrEmpty(p.PartyId) &&
            !String.IsNullOrEmpty(p.IssuerPersonId) && !String.IsNullOrEmpty(p.WorldId) && !String.IsNullOrEmpty(p.SiteId) &&
            !String.IsNullOrEmpty(p.SiteIdentity) && (p.Mode==TargetMode.NaturalWorld ||
            (!String.IsNullOrEmpty(p.AdapterId) && !String.IsNullOrEmpty(p.AuthoredEncounterId))); }
        static bool Exact(ActivationPlan x,string world,string site,string identity) { return x.WorldId==world && x.SiteId==site && x.SiteIdentity==identity; }
        static bool Find(ActivationLedger s,string id,out ActivationEntry e) { e=null; return s!=null && !String.IsNullOrEmpty(id) && s.Entries.TryGetValue(id,out e); }
        static ActivationResult Commit(ActivationLedger s,string command,ActivationEntry e,string reason,ActivationDirective d)
        { s.AppliedCommands.Add(command); s.Revision++; return new ActivationResult { Applied=true,Reason=reason,Directive=d,Entry=e.Copy(),State=s }; }
        static ActivationResult Reject(ActivationLedger s,string reason) { return new ActivationResult { Reason=reason,State=s }; }
        static ActivationResult Replay(ActivationLedger s,string command) { return s!=null && !String.IsNullOrEmpty(command) && s.AppliedCommands.Contains(command)
            ? new ActivationResult { Applied=true,Replayed=true,Reason="command_already_applied",State=s } : null; }
    }
}
