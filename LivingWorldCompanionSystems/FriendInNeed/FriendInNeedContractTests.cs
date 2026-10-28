using System;
using LivingWorld.CompanionSystems.FriendInNeed;

class FriendInNeedContractTests
{
    static int checks;
    static void Check(bool condition, string label) { checks++; if (!condition) throw new Exception("FAIL " + label); }
    static bool Has(FriendInNeedResult result, FriendInNeedCommandKind kind)
    { foreach (FriendInNeedCommand command in result.Commands) if (command.Kind == kind) return true; return false; }
    static FriendInNeedResult Start(bool combat = false)
    { return FriendInNeedContract.Witness(null, "camp-7:attack-2", DangerSubject.Camp, "camp-7", ThreatEvidence.HostileTargetingProtectedActor, 100, combat); }

    static void Main()
    {
        FriendInNeedResult first = Start();
        FriendInNeedResult merelyVisible = FriendInNeedContract.Witness(null, "camp-8:seen-1", DangerSubject.Camp,
            "camp-8", ThreatEvidence.VisibleHostile, 99, false);
        Check(merelyVisible.Commands.Length == 0 && merelyVisible.State == null, "visible proximity remains awareness only");
        Check(first.State.Phase == FriendInNeedPhase.Alerting, "witnessed danger enters readiness alert");
        Check(Has(first, FriendInNeedCommandKind.PlayExistingSurprisedVoice), "initial shock requests voice");
        Check(first.Commands[0].VoicePool == FriendInNeedContract.ExistingSurprisedVoicePool, "only existing surprised pool is named");
        Check(Has(first, FriendInNeedCommandKind.ShowDangerSignal), "initial alert is player-facing");
        Check(!Has(first, FriendInNeedCommandKind.YieldToNativeCombat), "call for help is not combat commitment");

        FriendInNeedResult duplicate = FriendInNeedContract.Witness(first.State, first.State.EventId,
            DangerSubject.Camp, first.State.SubjectId, ThreatEvidence.CampDamage, 101, false);
        Check(!Has(duplicate, FriendInNeedCommandKind.PlayExistingSurprisedVoice), "same incident never replays shock");
        Check((duplicate.State.Evidence & ThreatEvidence.CampDamage) != 0, "evidence accumulates");

        FriendInNeedResult noNewFact = FriendInNeedContract.Tick(duplicate.State, 108, true, ThreatEvidence.None, false);
        Check(noNewFact.Commands.Length == 0, "elapsed cooldown alone never creates a follow-up");
        FriendInNeedResult nonMaterial = FriendInNeedContract.Tick(duplicate.State, 108, true, ThreatEvidence.VisibleHostile, false);
        Check(nonMaterial.Commands.Length == 0, "continued visibility is not a material follow-up");
        FriendInNeedResult early = FriendInNeedContract.Tick(duplicate.State, 107.9, true, ThreatEvidence.CampDamage, false);
        Check(early.Commands.Length == 0, "follow-up interval is bounded");
        FriendInNeedResult follow1 = FriendInNeedContract.Tick(duplicate.State, 108, true, ThreatEvidence.CampDamage, false);
        FriendInNeedResult follow2 = FriendInNeedContract.Tick(follow1.State, 116, true, ThreatEvidence.ProtectedActorIncapacitated, false);
        Check(Has(follow1, FriendInNeedCommandKind.ShowFollowUpSignal), "one material-fact follow-up emitted");
        Check(follow2.Commands.Length == 0 && follow1.State.FollowUpsEmitted == FriendInNeedContract.MaxFollowUps, "follow-ups stop at one-call cap");

        FriendInNeedResult combat = FriendInNeedContract.Tick(follow1.State, 117, true, ThreatEvidence.None, true);
        Check(combat.State.Phase == FriendInNeedPhase.YieldedToCombat && Has(combat, FriendInNeedCommandKind.YieldToNativeCombat), "native combat preempts readiness");
        Check(!Has(combat, FriendInNeedCommandKind.PlayExistingSurprisedVoice), "combat transition does not repurpose voice trigger");

        FriendInNeedResult immediateCombat = Start(true);
        Check(immediateCombat.State.Phase == FriendInNeedPhase.YieldedToCombat, "already committed threat starts yielded");
        Check(!Has(immediateCombat, FriendInNeedCommandKind.PlayExistingSurprisedVoice), "existing combat trigger remains authoritative");

        FriendInNeedResult resolved = FriendInNeedContract.Resolve(combat.State, ResolutionKind.Safe, 130);
        string save = FriendInNeedContract.Write(resolved.State);
        FriendInNeedState loaded;
        Check(FriendInNeedContract.TryRead(save, out loaded), "resolved record roundtrips");
        Check(loaded.Phase == FriendInNeedPhase.Resolved && loaded.Resolution == ResolutionKind.Safe, "resolution persists");
        FriendInNeedResult reloadWitness = FriendInNeedContract.Witness(loaded, loaded.EventId, loaded.Subject,
            loaded.SubjectId, ThreatEvidence.HostileTargetingProtectedActor, 131, false);
        Check(reloadWitness.Commands.Length == 0, "resolved incident does not replay after reload");

        Check(!FriendInNeedContract.TryRead(save + "|extra", out loaded), "extra fields rejected");
        Check(!FriendInNeedContract.TryRead(save.Replace("friend-in-need-v1", "friend-in-need-v2"), out loaded), "future schema fails closed");
        string[] invalidPhase = save.Split('|');
        invalidPhase[4] = "99";
        Check(!FriendInNeedContract.TryRead(String.Join("|", invalidPhase), out loaded), "unknown enum rejected");
        Check(Start().State.EventId == first.State.EventId, "event identity is adapter supplied and deterministic");

        FriendInNeedResult stale = FriendInNeedContract.Tick(first.State, 146, false, ThreatEvidence.None, false);
        Check(stale.State.Phase == FriendInNeedPhase.Resolved && stale.State.Resolution == ResolutionKind.Expired, "stale evidence resolves persistently");
        FriendInNeedResult competing = FriendInNeedContract.Witness(first.State, "friend-9:attack-1", DangerSubject.KnownFriend,
            "friend-9", ThreatEvidence.AttributedDamage, 102, false);
        Check(competing.Commands.Length == 0 && competing.State.EventId == first.State.EventId, "unresolved incident is not silently replaced");

        Console.WriteLine("PASS " + checks + " Friend in Need contract checks (pure offline; Unity execution NOT RUN)");
    }
}
