namespace LivingWorld.CompanionSystems.EncounterPopulation
{
    public static class ServerPresentationProjection
    {
        // Only the server may decide when a hidden faction becomes public.
        public static ClientMemberView Build(SpawnMember member, bool factionRevealed)
        {
            if(member==null || member.Kit==null) return null;
            return new ClientMemberView { PersonId=member.PersonId,Silhouette=member.Kit.Silhouette,
                ArmorLook=member.Kit.ArmorLook,VisibleWeapon=member.Kit.Primary.ToString(),
                RevealedFaction=factionRevealed?member.Kit.Faction.ToString():null };
        }
    }
}
