using System;
using LivingWorld.CompanionSystems.EncounterActivation;

class EncounterActivationPolicyTests
{
    static int checks; static void Check(bool x,string n){checks++;if(!x)throw new Exception("FAIL "+n);}
    static ActivationPlan Plan(TargetMode mode){return new ActivationPlan { RequestId="lwc-request-1",PartyId="party-a",IssuerPersonId="runa",
        WorldId="world-9",SiteId="site-ash-2",SiteIdentity="site-ash-2:v4",Mode=mode,AdapterId="safe-adapter-v1",AuthoredEncounterId="ash-brute-small"};}
    static ArmingProof Arm(){return new ArmingProof { RequestId="lwc-request-1",WorldId="world-9",SiteId="site-ash-2",SiteIdentity="site-ash-2:v4",
        ObservingPlayerId="player-a",ServerAuthority=true,BrokerRequestAccepted=true,ProgressionLegal=true,SiteStillSafe=true,SiteLoaded=true,
        IssuerGroupLoadedAtTarget=true,PlayerLoadedAtTarget=true,PlayerDeliberatelyApproached=true,
        IssuerPersonIdentityPreserved=true,IssuerPersonalEquipmentPreserved=true,WorldTick=1200};}
    static ActivationResult Accepted(TargetMode mode){var a=EncounterActivationPolicy.RegisterOffer(new ActivationLedger(),"offer",Plan(mode));
        return EncounterActivationPolicy.Accept(a.State,"accept",a.Entry.Plan.RequestId,true,1100);}
    static SpawnConfirmation Confirmation(ActivationEntry e){return new SpawnConfirmation {RequestId=e.Plan.RequestId,SpawnClaimId=e.SpawnClaimId,
        SpawnRequestId=e.SpawnRequestId,SpawnReceiptId="receipt-1",WorldId=e.Plan.WorldId,SiteId=e.Plan.SiteId,SiteIdentity=e.Plan.SiteIdentity,
        ServerAuthority=true,AdapterAccepted=true,ActorProvenanceBound=true,WorldTick=1210};}
    static OutcomeProof Clear(ActivationEntry e){return new OutcomeProof {RequestId=e.Plan.RequestId,WorldId=e.Plan.WorldId,SiteId=e.Plan.SiteId,
        SiteIdentity=e.Plan.SiteIdentity,EvidenceId="death-zdo-44",SpawnClaimId=e.SpawnClaimId,ServerAuthority=true,TargetLoaded=true,
        PlayerLoadedAtTarget=true,IssuerWitnessOrAlertPreserved=true,ObjectiveObservedComplete=true,PlayerOrPartyContributed=true,
        AllClaimedActorsDead=true,CommittedBossDeath=true,WorldTick=1400};}
    static void Main(){
        var accepted=Accepted(TargetMode.AuthoredMiniboss); var wrong=Arm(); wrong.SiteIdentity="lookalike:v1";
        Check(EncounterActivationPolicy.Arm(accepted.State,"wrong-site",wrong).Reason=="arming_site_identity_mismatch","site mismatch blocks arming");
        var dual=Arm(); dual.ObservingPlayerId="player-b"; dual.PlayerLoadedAtTarget=false;
        Check(EncounterActivationPolicy.Arm(accepted.State,"dual-player",dual).Reason=="loaded_group_and_player_at_target_required","remote second player cannot activate");
        var armed=EncounterActivationPolicy.Arm(accepted.State,"arm",Arm()); Check(armed.Directive==ActivationDirective.RequestSpawn,"accepted deliberate approach requests spawn");
        var reloaded=armed.State.Copy(); var replay=EncounterActivationPolicy.Arm(reloaded,"arm",Arm());
        Check(replay.Replayed && reloaded.Entries[Plan(TargetMode.AuthoredMiniboss).RequestId].SpawnRequestId==armed.Entry.SpawnRequestId,"unload reload preserves one spawn request");
        Check(EncounterActivationPolicy.Accept(accepted.State,"accept-again",accepted.Entry.Plan.RequestId,true,1150).Reason=="execution_not_offered","duplicate acceptance refused");
        var active=EncounterActivationPolicy.ConfirmSpawn(reloaded,"spawn-confirm",Confirmation(armed.Entry));
        var bad=Clear(active.Entry); bad.CommittedBossDeath=false;
        Check(EncounterActivationPolicy.ObserveClear(active.State,"false-clear",bad).Reason=="committed_boss_death_required","miniboss requires committed death");
        var clear=EncounterActivationPolicy.ObserveClear(active.State,"clear",Clear(active.Entry));
        Check(clear.Directive==ActivationDirective.NotifyBrokerClear && clear.Entry.Status==ActivationStatus.Clear,"committed boss death yields broker evidence");
        Check(EncounterActivationPolicy.ObserveClear(clear.State,"clear-twice",Clear(clear.Entry)).Reason=="execution_outcome_already_committed","no duplicate completion");
        var unloaded=Accepted(TargetMode.NaturalWorld); var naturalArm=Arm(); naturalArm.SiteLoaded=false;
        Check(EncounterActivationPolicy.Arm(unloaded.State,"natural-unloaded",naturalArm).Reason=="loaded_group_and_player_at_target_required","unloaded natural target cannot falsely activate");
        var natural=EncounterActivationPolicy.Arm(unloaded.State,"natural-arm",Arm()); Check(natural.Directive==ActivationDirective.ObserveNaturalTarget && natural.Entry.SpawnClaimId==null,"natural target uses no spawner");
        var falseClear=Clear(natural.Entry); falseClear.TargetLoaded=false;
        Check(EncounterActivationPolicy.ObserveClear(natural.State,"natural-false",falseClear).Reason=="loaded_witnessed_completion_required","no offscreen false completion");
        var transfer=active.State.Copy(); Check(EncounterActivationPolicy.ConfirmSpawn(transfer,"owner-transfer-confirm",Confirmation(active.Entry)).Reason=="spawn_not_armed","owner transfer cannot spawn twice");
        var abandoned=EncounterActivationPolicy.Close(active.State,"abandon",active.Entry.Plan.RequestId,true,true,1500);
        Check(abandoned.Entry.Status==ActivationStatus.Abandoned && abandoned.Directive==ActivationDirective.None,"abandon does not delete living hostiles");
        Console.WriteLine("EncounterActivationPolicyTests PASS: "+checks+" checks");
    }
}
