namespace LivingWorld.CompanionSystems.EncounterPopulation
{
    // The client receives this server-produced display contract, never SpawnMember or CombatFacts.
    public sealed class ClientMemberView
    {
        public string PersonId, Silhouette, ArmorLook, VisibleWeapon, RevealedFaction;
    }
}
