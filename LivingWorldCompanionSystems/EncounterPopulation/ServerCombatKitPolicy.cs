namespace LivingWorld.CompanionSystems.EncounterPopulation
{
    // Server/actor-owner request routing only. Native AI executes any approved attack.
    public static class ServerCombatKitPolicy
    {
        public static CombatChoice Choose(Archetype kit, CombatFacts f)
        {
            if(EncounterPopulationPolicy.Validate(kit)!=null || f==null || !f.ServerAuthority || !f.ActorOwner ||
               !f.CombatOwnsAction || !f.TargetAuthorized) return CombatChoice.None;
            if((kit.Specials&SpecialMove.BombThrow)!=0 && f.BombLiveVerified &&
               f.BombsAvailable>0 && f.BombRequestAuthorized) return CombatChoice.RequestBombThrow;
            if((kit.Specials&SpecialMove.SpearThrow)!=0 && f.SpearThrowLiveVerified &&
               f.Spear==SpearStatus.Held && f.PrimaryEquipped && f.SpearRequestAuthorized)
                return CombatChoice.RequestSpearThrow;
            if(kit.Primary==WeaponFamily.Spear && f.SpearRecoveryLiveVerified &&
               f.Spear==SpearStatus.Airborne && f.SpearRecoverySafe &&
               f.SpearProjectileOwned && !string.IsNullOrWhiteSpace(f.SpearProjectileId))
                return CombatChoice.RequestSpearRecovery;
            if((kit.Specials&SpecialMove.KnifeLunge)!=0 && f.KnifeLungeLiveVerified &&
               f.PrimaryEquipped && f.KnifeRequestAuthorized) return CombatChoice.RequestKnifeLunge;
            if((kit.Specials&SpecialMove.AtgeirSweep)!=0 && f.AtgeirSweepLiveVerified &&
               f.PrimaryEquipped && f.AtgeirRequestAuthorized) return CombatChoice.RequestAtgeirSweep;
            if(kit.Primary==WeaponFamily.Spear && f.Spear!=SpearStatus.Held)
                return f.FallbackEquipped?CombatChoice.UseFallback:CombatChoice.None;
            if(f.PrimaryEquipped) return CombatChoice.UsePrimary;
            return f.FallbackEquipped?CombatChoice.UseFallback:CombatChoice.None;
        }
    }
}
