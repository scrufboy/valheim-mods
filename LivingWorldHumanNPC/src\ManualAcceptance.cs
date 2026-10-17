using System;

namespace LivingWorld.ManualTest
{
    internal static class ManualAcceptance
    {
        internal const string WorldName = "LWC_HumanLab_v1";
        internal const string ProfileName = "LWC_Tester";
        internal static bool BlocksLabFootprint(bool hasCharacter, bool hasPiece)
        {
            // The disposable test's prepared site is explicitly chosen by the user.
            // Preserve occupants and buildings; generic scenery is not a veto.
            return hasCharacter || hasPiece;
        }
        internal static bool WorldAllowed(string world, string profile, bool host, bool openServer, int peers)
        {
            // Valheim lowercases character filenames when creating a named profile.
            return world == WorldName && String.Equals(profile, ProfileName, StringComparison.OrdinalIgnoreCase) && host && !openServer && peers == 0;
        }
        internal static bool MovementPassed(bool nativePath, bool grounded, float verticalSpeed,
            float initialDistance, float remainingDistance, float playerDrift)
        {
            return nativePath && grounded && Math.Abs(verticalSpeed) <= 1f && playerDrift <= 1.5f &&
                initialDistance >= 6f && initialDistance - remainingDistance >= 2.5f && remainingDistance <= 3.75f;
        }
        internal static bool CombatPassed(bool targetSeen, float attributedDamage, bool weaponEquipped)
        {
            return targetSeen && attributedDamage > 0f && weaponEquipped;
        }
        internal static bool FullCombatPassed(bool targetSeen, float attributedDamage, bool weaponEquipped, bool deathObserved, bool otherDamage)
        {
            return CombatPassed(targetSeen, attributedDamage, weaponEquipped) && deathObserved && !otherDamage;
        }
    }
}
