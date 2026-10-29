namespace LivingWorld.Companions.RescueRelease
{
    // Implemented by the maintained 1.0.x runtime. Policy code never calls Unity.
    public interface IRescueReleaseStore
    {
        WorldState Load();
        bool TrySave(long expectedRevision, WorldState next);
    }

    public interface IWorldHandoffAdapter
    {
        // Must return true only after the destination zone is loaded and the same
        // persistent actor is physically present at its safe anchor.
        bool TryPlaceAtLoadedDestination(string personId, string destinationId);
        bool IsUnsafeTransition(string personId);
    }

    public interface IEquipmentExchangeAdapter
    {
        // Move player-owned loans through a visible inventory transaction.
        // Personal gear remains on the companion/corpse and is never cloned.
        bool TryResolvePlayerLoans(string personId);
        bool TryRecoverCorpseItem(string personId, string itemId);
    }

    public interface IDeathEvidenceAdapter
    {
        // The runtime must coalesce duplicate callbacks before policy commit.
        // A committed record must suppress spawn/recall/revive for this person id.
        DeathRecord ReadConfirmedDeath(string personId);
    }

    public interface ITemporaryAidWorldAdapter
    {
        // Zone unload releases only the process actor claim. Persisted assignment,
        // PersonId, request ID, and gear survive. Reload restores the same person.
        bool TryRestoreSamePerson(string personId, string requestId);

        // The adapter must check TemporaryPortalTravelAllowed. Portal transit may
        // neither complete return nor delete/recreate the person as someone new.
        bool TryTravelPortalAsSamePerson(string personId, bool termsAllowPortal);
    }
}
