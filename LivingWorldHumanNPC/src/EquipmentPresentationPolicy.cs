namespace LivingWorld.HumanNpc
{
    public enum StowAction { Complete, HideVisible, RestoreThenHide }

    public static class EquipmentPresentationPolicy
    {
        public static StowAction Stow(bool visibleCombat, bool hiddenCombat)
        {
            if (!visibleCombat && hiddenCombat) return StowAction.Complete;
            if (visibleCombat) return StowAction.HideVisible;
            return StowAction.RestoreThenHide;
        }

        public static bool VerifiedStowed(bool visibleCombat, bool hiddenCombat)
        { return !visibleCombat && hiddenCombat; }

        public static bool PairShield(bool authorizedCombat, bool selectedOneHandedWeapon,
            bool shieldAvailable, bool shieldEquipped)
        {
            return authorizedCombat && selectedOneHandedWeapon && shieldAvailable && !shieldEquipped;
        }
    }
}
