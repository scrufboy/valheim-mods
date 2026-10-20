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

        // Darkness makes a torch eligible; it does not grant ownership of the
        // hands. Utility ownership begins only after the torch is genuinely
        // visible, every combat item is absent from the hands, and the combat
        // pair is still represented in the native hidden/back slots.
        public static bool UtilityOwnsHands(bool torchVisible, bool visibleCombat, bool hiddenCombat)
        { return torchVisible && !visibleCombat && hiddenCombat; }

        public static bool PairShield(bool authorizedCombat, bool selectedOneHandedWeapon,
            bool shieldAvailable, bool shieldEquipped)
        {
            return authorizedCombat && selectedOneHandedWeapon && shieldAvailable && !shieldEquipped;
        }
    }
}
