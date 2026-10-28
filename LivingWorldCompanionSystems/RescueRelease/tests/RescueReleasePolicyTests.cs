using System;
using LivingWorld.Companions.RescueRelease;

internal static class RescueReleasePolicyTests
{
    private static int failures;
    private static readonly RescueReleasePolicy Policy = new RescueReleasePolicy();

    private static void Check(bool condition, string name)
    {
        if (!condition) { failures++; Console.Error.WriteLine("FAIL " + name); }
        else Console.WriteLine("PASS " + name);
    }

    private static CompanionRecord Person(string id, Assignment assignment)
    {
        return new CompanionRecord { PersonId = id, Name = id, Role = "Scout", AppearanceId = "look-" + id, OriginEncounterId = "enc-" + id, OriginCampId = "camp-" + id, Rescue = RescueOutcome.Rescued, Assignment = assignment };
    }

    private static InvitationFacts GoodFacts() { return new InvitationFacts { SafeDestinationId = "home", RescueCompleted = true, SanctuaryOffered = true, CompatiblePurpose = true }; }

    private static void AddHome(WorldState state)
    {
        state.Destinations.Add("home", new SafeDestination { DestinationId = "home", Kind = DestinationKind.Home, Established = true });
    }

    private static void InvitationIsFactualAndBounded()
    {
        var s = new WorldState();
        s.People.Add("a", Person("a", Assignment.Expedition));
        s.People.Add("b", Person("b", Assignment.Expedition));
        s.People.Add("c", Person("c", Assignment.Known));
        AddHome(s);
        var full = Policy.Invite(s, "invite-c", "c", GoodFacts(), 2);
        Check(full.Decision == InvitationDecision.Yes && full.State.People["c"].Assignment == Assignment.BondedResident, "full expedition accepts valid bond as resident");
        Check(full.State.People["c"].HomeId == "home", "resident invitation records safe home");
        Check(s.People["c"].Assignment == Assignment.Known, "invitation leaves prior snapshot unchanged");

        s.People["c"].DebugIdentity = true;
        var debug = Policy.Invite(s, "debug", "c", GoodFacts(), 2);
        Check(debug.Decision == InvitationDecision.No && Array.IndexOf(debug.Reasons, "debug_identity_cannot_be_recruited") >= 0, "debug actor cannot promote");

        var roster = new WorldState();
        for (var i = 0; i < 6; i++) roster.People.Add("p" + i, Person("p" + i, Assignment.BondedResident));
        roster.People.Add("seventh", Person("seventh", Assignment.Known));
        AddHome(roster);
        var seventh = Policy.Invite(roster, "seven", "seventh", GoodFacts(), 2);
        Check(Array.IndexOf(seventh.Reasons, "bonded_roster_full") >= 0, "seventh living bond rejected");

        var homeless = new WorldState();
        homeless.People.Add("h", Person("h", Assignment.Known));
        var noHome = Policy.Invite(homeless, "homeless", "h", GoodFacts(), 2);
        Check(noHome.Decision == InvitationDecision.No && Array.IndexOf(noHome.Reasons, "essential_safe_destination_missing") >= 0, "permanent yes requires established safe home");

        var beforeBoss = new WorldState();
        beforeBoss.People.Add("early", Person("early", Assignment.Known));
        AddHome(beforeBoss);
        var early = Policy.Invite(beforeBoss, "early-invite", "early", GoodFacts(), 0);
        Check(early.Decision == InvitationDecision.Yes && early.State.People["early"].Assignment == Assignment.BondedResident, "zero unlocked field slots still permits resident bond");
    }

    private static void ReleaseRequiresWorldProofAndPreservesIdentity()
    {
        var s = new WorldState();
        var p = Person("a", Assignment.Expedition);
        p.Relationship = "allied"; p.History = "held the ford";
        p.Gear.Add(new GearItem { ItemId = "personal-sword", Owner = GearOwner.Companion });
        s.People.Add("a", p);
        s.Destinations.Add("village", new SafeDestination { DestinationId = "village", Kind = DestinationKind.ReclaimedVillage, Established = true });

        var wait = Policy.RequestRelease(s, "release-request", "a", null, new ReleaseSafety());
        Check(wait.Decision == ReleaseDecision.WaitingForSafety && wait.State.People["a"].Assignment == Assignment.AwaitingSafeHandoff, "no home waits rather than deleting");

        var noLoad = Policy.CommitRelease(wait.State, "release-commit-1", "a", "village", new ReleaseSafety { LoanExchangeComplete = true });
        Check(noLoad.Decision == ReleaseDecision.Rejected, "unloaded destination cannot commit");

        var done = Policy.CommitRelease(wait.State, "release-commit-2", "a", "village", new ReleaseSafety { DestinationZoneLoaded = true, CompanionPresentAtDestination = true, LoanExchangeComplete = true });
        Check(done.Decision == ReleaseDecision.Committed && done.State.People["a"].Assignment == Assignment.BondedResident, "safe village handoff becomes resident");
        Check(done.State.People["a"].History == "held the ford" && done.State.People["a"].Gear.Count == 1, "release preserves identity and personal gear");
        var replay = Policy.CommitRelease(done.State, "release-commit-2", "a", "village", new ReleaseSafety());
        Check(replay.Replayed && replay.State.Revision == done.State.Revision, "release command is idempotent after reload");
        Check(s.People["a"].Assignment == Assignment.Expedition && s.People["a"].HomeId == null, "new snapshots never mutate prior persisted snapshot");

        var origin = done.State.Copy();
        origin.People["a"].Assignment = Assignment.Expedition;
        origin.Destinations.Add("origin", new SafeDestination { DestinationId = "origin", Kind = DestinationKind.OriginCamp, Established = true });
        var originRelease = Policy.CommitRelease(origin, "origin-release", "a", "origin", new ReleaseSafety { DestinationZoneLoaded = true, CompanionPresentAtDestination = true, LoanExchangeComplete = true });
        Check(originRelease.Decision == ReleaseDecision.Committed && originRelease.State.People["a"].Assignment == Assignment.BondedResident, "origin-camp party release retains living bond");
    }

    private static void LoansAndDeathAreTransactions()
    {
        var s = new WorldState();
        var p = Person("a", Assignment.Expedition);
        p.Gear.Add(new GearItem { ItemId = "own", Owner = GearOwner.Companion });
        p.Gear.Add(new GearItem { ItemId = "loan", Owner = GearOwner.PlayerLoan });
        s.People.Add("a", p);
        s.Destinations.Add("home", new SafeDestination { DestinationId = "home", Kind = DestinationKind.Home, Established = true });
        var proof = new ReleaseSafety { DestinationZoneLoaded = true, CompanionPresentAtDestination = true, LoanExchangeComplete = true };
        Check(Policy.CommitRelease(s, "r", "a", "home", proof).Decision == ReleaseDecision.Rejected, "loan must really leave record before release");
        var returned = Policy.ReturnPlayerLoan(s, "loan-return", "a", "loan");
        Check(returned.Accepted && returned.State.People["a"].Gear.Count == 1, "loan exchange removes only player loan");

        var death = Policy.CommitDeath(returned.State, "death", "a", new DeathRecord { Place = "ford", WorldTime = 42, Cause = "troll" });
        Check(death.Accepted && death.State.People["a"].Assignment == Assignment.Dead && death.State.People["a"].Death.Name == "a", "death closes identity with memorial facts");
        Check(!Policy.ResolveRescue(death.State, "revive", "a", RescueOutcome.Rescued).Accepted, "committed death cannot resurrect");
        var first = Policy.RecoverDeadGear(death.State, "recover-1", "a", "own");
        Check(first.Accepted, "corpse gear recovers once");
        Check(!Policy.RecoverDeadGear(first.State, "recover-2", "a", "own").Accepted, "corpse gear cannot duplicate");
        var replay = Policy.CommitDeath(death.State, "death", "a", new DeathRecord { Place = "elsewhere", Cause = "reload" });
        Check(replay.Replayed && replay.State.People["a"].Death.Place == "ford", "duplicate death callback cannot rewrite history");
        Check(Policy.ValidateLoadedState(replay.State).Length == 0, "committed snapshot passes reload validation");

        var corrupt = replay.State.Copy();
        corrupt.People["a"].Life = LifeState.Alive;
        Check(Policy.ValidateLoadedState(corrupt).Length != 0, "reload validator rejects resurrection-shaped state");
    }

    public static int Main()
    {
        InvitationIsFactualAndBounded();
        ReleaseRequiresWorldProofAndPreservesIdentity();
        LoansAndDeathAreTransactions();
        if (failures != 0) Console.Error.WriteLine(failures + " test(s) failed");
        return failures == 0 ? 0 : 1;
    }
}
