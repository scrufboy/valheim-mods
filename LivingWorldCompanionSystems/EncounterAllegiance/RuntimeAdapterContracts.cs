namespace LivingWorld.CompanionSystems.EncounterAllegiance
{
    public interface IEncounterAllegianceStore
    {
        PersonContactRecord Load(string personId);
        bool TrySave(long expectedRevision, PersonContactRecord next);
    }

    public interface IWitnessEvidenceAdapter
    {
        // Facts must come from server line-of-sight, damage, ownership, boundary,
        // aid, or authored grievance evidence. Client claims are not facts.
        ContactFact ReadVerifiedFact(string factId, string observerPersonId);
    }

    public interface IAllegianceIdentityAdapter
    {
        // Resolve authored Living World allegiance or an explicit other-mod ID
        // plus its authored disposition toward this observer.
        // Character.Faction may be evidence but must never be the sole answer.
        FactionIdentity Resolve(object nativeCharacter);
    }

    public interface IAuthorableFactionAdapter
    {
        // Prefer the persistent authored profile ID, then an exact registered
        // prefab binding. Never promote native Character.Faction into identity.
        AuthoredFactionIdentity ResolveAuthoredIdentity(object nativeCharacter);
        // False means no authored override: preserve vanilla tame, player,
        // group, and native faction behavior unchanged.
        bool TryResolveRelation(object observer, object target, out Disposition disposition);
    }

    public interface INativeCombatHandoff
    {
        void CommitTarget(string personId, object nativeTarget);
        void YieldPeacefulControl(string personId);
    }

    // One encounter authority owns this record. Shaman, Behemoth, minion and
    // presentation owners exchange verified receipts; none owns a second copy.
    public interface ICaptiveBehemothAllegianceStore
    {
        CaptiveBehemothRecord Load(string encounterId, string behemothId);
        bool TrySave(long expectedRevision, CaptiveBehemothRecord next);
    }

    public interface ICaptiveBehemothReceiptAdapter
    {
        // Only an accepted native bind completion may set VerifiedBindingReceipt.
        CaptiveBehemothEvent ReadBindingReceipt(string encounterId, string behemothId,
            string controllerId, long bindingRevision);
        // Physical chain deployment, generic damage and cosmetic effects are not
        // spell breaks. The spell owner supplies the verified break receipt.
        CaptiveBehemothEvent ReadSpellBreakReceipt(string encounterId, string behemothId);
    }

    public interface IInstanceRelationAdapter
    {
        // False means preserve the native/global relation. True is scoped only
        // to the exact persisted encounter and Behemoth identities.
        bool TryResolveCaptiveBehemoth(string encounterId, string firstActorId,
            EncounterActorRole firstRole, string secondActorId, EncounterActorRole secondRole,
            out InstanceDisposition disposition);
    }
}
