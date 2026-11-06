namespace LivingWorld.CompanionSystems.EncounterActivation
{
    public interface IEncounterActivationFacts
    {
        ArmingProof ReadArmingProof(string requestId);
        OutcomeProof ReadOutcomeProof(string requestId, string evidenceId);
    }

    public interface IEncounterActivationStore
    {
        ActivationLedger Load();
        bool TrySave(long expectedRevision, ActivationLedger next);
    }

    public sealed class SpawnRequest
    {
        public string RequestId, SpawnRequestId, SpawnClaimId, AdapterId, AuthoredEncounterId;
        public string WorldId, SiteId, SiteIdentity;
    }

    public interface IEncounterSpawnAdapterV1
    {
        // Must be server-authoritative and idempotent by SpawnRequestId + SpawnClaimId.
        // It owns placement and provenance tags, but does not touch the vanilla raid clock.
        SpawnConfirmation RequestOnce(SpawnRequest request);
    }

    public interface IEncounterCombatObserver
    {
        // Observe native damage/death and FriendInNeed witness/alert facts. Never kill actors to resolve a request.
        OutcomeProof Observe(string requestId, string evidenceId);
    }
}
