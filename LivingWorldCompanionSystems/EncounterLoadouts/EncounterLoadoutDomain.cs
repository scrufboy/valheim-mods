using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.EncounterLoadouts
{
    public enum BiomeTier { Meadows = 0, BlackForest = 1, Swamp = 2, Mountain = 3, Plains = 4, Mistlands = 5, Ashlands = 6, DeepNorth = 7 }
    public enum GearKind { Armor, Weapon, Shield, Ammunition, Tool, Supply }
    public enum LifeState { Living, Dead }

    public sealed class GearRecord
    {
        public string ItemId;
        public GearKind Kind;
        public BiomeTier Tier;
        public int Quantity;
        public bool Equipped;
        public bool PersonalBound;

        public GearRecord Copy() { return (GearRecord)MemberwiseClone(); }
    }

    public sealed class KnowledgeRecord
    {
        public string FactId;
        public string SiteId;
        public string Source;
        public bool Verified;

        public KnowledgeRecord Copy() { return (KnowledgeRecord)MemberwiseClone(); }
    }

    public sealed class LoadoutVariant
    {
        public string VariantId;
        public BiomeTier Tier;
        public string RequiredWorldKey;
        public string RequiredStationFact;
        public string RequiredMaterialFact;
        public string ArmorSilhouette;
        public string WeaponSilhouette;
        public readonly List<GearRecord> Gear = new List<GearRecord>();

        public LoadoutVariant Copy()
        {
            var value = new LoadoutVariant { VariantId = VariantId, Tier = Tier, RequiredWorldKey = RequiredWorldKey,
                RequiredStationFact = RequiredStationFact, RequiredMaterialFact = RequiredMaterialFact,
                ArmorSilhouette = ArmorSilhouette, WeaponSilhouette = WeaponSilhouette };
            foreach (GearRecord item in Gear) value.Gear.Add(item.Copy());
            return value;
        }
    }

    public sealed class EncounterProfile
    {
        public string PersonId;
        public string DisplayName;
        public string OriginEncounterId;
        public string OriginCampId;
        public string FactionId;
        public string RoleId;
        public BiomeTier OriginBiome;
        public string DiscoveryWorldKey;
        public string RecruitmentWorldKey;
        public string DistantSilhouette;
        public string LocalHistory;
        public readonly List<KnowledgeRecord> Knowledge = new List<KnowledgeRecord>();
        public readonly List<LoadoutVariant> Variants = new List<LoadoutVariant>();
    }

    public sealed class PersonLoadoutState
    {
        public int SchemaVersion = 1;
        public string PersonId;
        public string OriginEncounterId;
        public string OriginCampId;
        public string FactionId;
        public string RoleId;
        public BiomeTier OriginBiome;
        public string ActiveVariantId;
        public int Revision;
        public LifeState Life;
        public string Relationship;
        public string Wounds;
        public string DeathRecord;
        public readonly List<KnowledgeRecord> Knowledge = new List<KnowledgeRecord>();
        public readonly List<GearRecord> PersonalGear = new List<GearRecord>();

        public PersonLoadoutState Copy()
        {
            var value = new PersonLoadoutState {
                SchemaVersion = SchemaVersion, PersonId = PersonId, OriginEncounterId = OriginEncounterId,
                OriginCampId = OriginCampId, FactionId = FactionId, RoleId = RoleId,
                OriginBiome = OriginBiome, ActiveVariantId = ActiveVariantId, Revision = Revision,
                Life = Life, Relationship = Relationship, Wounds = Wounds, DeathRecord = DeathRecord
            };
            foreach (KnowledgeRecord fact in Knowledge) value.Knowledge.Add(fact.Copy());
            foreach (GearRecord item in PersonalGear) value.PersonalGear.Add(item.Copy());
            return value;
        }
    }

    public sealed class UpgradeRequest
    {
        public string PersonId;
        public string TargetVariantId;
        public ISet<string> WorldKeys;
        public ISet<string> VerifiedFacts;
    }

    public sealed class PolicyResult
    {
        public bool Accepted;
        public string Reason;
        public PersonLoadoutState State;
    }
}
