using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.NeedsAndAffinity
{
    // Implement in the maintained 1.0.x server runtime. Missing observations fail closed.
    public interface INeedsWorldFacts
    {
        bool IsServerAuthority { get; }
        bool IsPersonLoadedAndNearby(string personId);
        bool IsTargetLoadedAndNearby(string personId, string targetId);
        bool IsAuthoredNeedCurrentlyTrue(string personId, string authoredNeedId, string targetId);
        bool IsPlayerOwnedTarget(string targetId);
        bool IsMaterialProgressionLegal(string materialId);
        int ReadDeliveredAmount(string personId, string targetId, string materialId);
        IList<NeedCandidate> ReadAuthoredCandidates(string encounterId, string personId);
    }

    public interface INeedsStateStore
    {
        NeedsState Load();
        bool TrySave(long expectedRevision, NeedsState next);
    }

    public interface INeedsPresentation
    {
        // Presentation may show the concrete request and qualitative acknowledgement only.
        // It must never expose a score, grind bar, recruitment promise, or automatic reward.
        void ShowRequest(string personId, string requestId, string description);
        void ShowAcknowledgement(string personId, string requestId, RememberedChoice choice);
    }
}
