using System;

namespace LivingWorld.HumanNpc
{
    public enum PartyWeapon { SwordShield, Bow }
    // Decisions consume observations only. Native execution owns costs and hits.
    public static class PartyCombatPolicy
    {
        // Humanoid's player-facing bow draw mode must be suspended only for the
        // synchronous native AI release call, after every safety gate passed.
        public static bool BowReleaseBridge(bool ready, bool authorized, bool currentBow,
            bool nativeAttackRequest, bool laneClear, bool hasAmmo)
        { return ready && authorized && currentBow && nativeAttackRequest && laneClear && hasAmmo; }
        public static PartyWeapon Weapon(bool bowEquipped, bool ammo, bool laneClear,
            bool immediateThreat, float distance, float closingSpeed, float shotSeconds)
        {
            if (!ammo || !laneClear || immediateThreat || !Finite(distance) || distance < 0 ||
                !Finite(closingSpeed) || closingSpeed < 0 || !Finite(shotSeconds) || shotSeconds <= 0)
                return PartyWeapon.SwordShield;
            // Archer is a role, not an occasional inventory option. Once there
            // is room and a clear lane, prefer the bow. The retained lower
            // boundary prevents weapon chatter; an immediate close threat still
            // overrides the role and forces sword/shield.
            float boundary = bowEquipped ? 6.5f : 8f;
            return distance >= boundary ? PartyWeapon.Bow : PartyWeapon.SwordShield;
        }

        public static float PerceivedLight(float red, float green, float blue, float intensity)
        {
            if (!Finite(red) || !Finite(green) || !Finite(blue) || !Finite(intensity) ||
                red < 0 || green < 0 || blue < 0 || intensity < 0) return Single.PositiveInfinity;
            // Rec. 709 luminance reflects perceived brightness. Using the
            // brightest RGB channel made Valheim's blue night light read as
            // daylight even when the scene was visibly dark.
            return (red * .2126f + green * .7152f + blue * .0722f) * intensity;
        }

        public static bool Torch(bool dark, bool nearbyLight, bool stealth, bool combat, bool handsBusy)
        {
            // Weather is not a reason to discard useful light. Combat, stealth,
            // occupied hands, sufficient ambient light and a nearby built light
            // are the complete utility-light arbitration order.
            return dark && !nearbyLight && !stealth && !combat && !handsBusy;
        }

        public static bool HealEligible(bool ally, bool alive, bool owned, bool visible,
            float distance, float health, float maximum)
        {
            return ally && alive && owned && visible && Finite(distance) && distance >= 0 && distance <= 10f &&
                Finite(health) && Finite(maximum) && maximum > 0 && health > 0 && health < maximum * 0.85f;
        }

        public static bool ReactToAttack(bool perceived, bool facing, bool shield, bool stamina,
            bool incapacitated, float distance, float cueAge, float reactionDelay)
        {
            return perceived && facing && shield && stamina && !incapacitated &&
                Finite(distance) && distance >= 0 && distance <= 5f &&
                Finite(cueAge) && Finite(reactionDelay) && reactionDelay >= 0f &&
                cueAge >= reactionDelay && cueAge <= .8f;
        }
        static bool Finite(float value) { return !Single.IsNaN(value) && !Single.IsInfinity(value); }
    }
}

