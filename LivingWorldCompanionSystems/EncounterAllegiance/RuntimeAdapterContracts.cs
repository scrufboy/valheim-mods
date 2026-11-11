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
}
