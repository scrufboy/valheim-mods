using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.SpecialistCombat.MeleeRoles
{
    public sealed class MeleeRoleProfile
    {
        public string RoleId, Biome, CanonicalLineage, BattlefieldResponsibility;
        public string[] AllowedWeaponFamilies, FallbackWeaponFamilies;
        public bool UsesCommittedLunge, UsesHeavyPressure;
        public string PresentationProofState;
    }

    public static class MeleeRoleProfiles
    {
        public static readonly MeleeRoleProfile BlackForestKnifeSkirmisher = new MeleeRoleProfile {
            RoleId="black_forest_knife_skirmisher", Biome="Black Forest", CanonicalLineage="Darkwood Stalker",
            BattlefieldResponsibility="Mobile forest scout; knife pressure only through a safe committed native lunge.",
            AllowedWeaponFamilies=new[]{"knife"}, FallbackWeaponFamilies=new[]{"bow","atgeir"},
            UsesCommittedLunge=true, UsesHeavyPressure=false,
            PresentationProofState="Role data only. Exact knife item, grip, animation and contact event require current 1.0.x live proof."
        };

        public static readonly MeleeRoleProfile PlainsHeavyReaver = new MeleeRoleProfile {
            RoleId="plains_heavy_reaver", Biome="Plains", CanonicalLineage="Goblin Slayer",
            BattlefieldResponsibility="Hold close space, exploit stagger, and protect the mobile hunter's release window.",
            AllowedWeaponFamilies=new[]{"atgeir","two-handed axe","heavy club"}, FallbackWeaponFamilies=new string[0],
            UsesCommittedLunge=false, UsesHeavyPressure=true,
            PresentationProofState="Candidate families only. No thrust, sweep, spin or equipment presentation is enabled until each attack item passes current 1.0.x live proof."
        };

        public static IEnumerable<MeleeRoleProfile> All { get { yield return BlackForestKnifeSkirmisher; yield return PlainsHeavyReaver; } }
    }
}
