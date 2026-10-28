using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.QuestBroker
{
    public interface IQuestBrokerWorldFacts
    {
        bool IsServerAuthority { get; }
        RequestCandidate ReadAuthoredCandidate(string authoredRequestId, string partyId);
        PresentationProof ReadPresentationSource(OfferSourceKind kind, string sourceId, string requestId);
        CompletionProof ReadCompletion(string requestId, string evidenceId);
        ISet<string> ReadServerWorldKeys();
    }

    public interface IQuestBrokerStateStore
    {
        BrokerState Load();
        bool TrySave(long expectedRevision, BrokerState next);
    }

    public interface IQuestBrokerPresentation
    {
        void ShowAtCampPerson(string personId, WorldRequest request);
        void ShowOnSettlementBoard(string settlementId, WorldRequest request);
        void ShowFromJarl(string personId, string settlementId, WorldRequest request);
        void RemoveClosedPresentation(string presentationId);
        void ShareVerifiedMapPin(string requestId, string siteIdentity);
    }

    public interface IAuthoredEncounterAdapter
    {
        // Runtime implementation must validate and own spawn, cleanup and outcome.
        // The broker neither creates a prefab nor awards loot.
        bool IsVerifiedSafePlan(string adapterId, string authoredEncounterId, string siteIdentity);
        bool TryActivate(string requestId, string adapterId, string authoredEncounterId, string siteIdentity);
    }
}
