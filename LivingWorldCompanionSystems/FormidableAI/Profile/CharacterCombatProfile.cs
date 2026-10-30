using System;
using LivingWorld.Companions.FormidableAI.Classes;

namespace LivingWorld.FormidableAI.Profile
{
    public enum CombatJob { Guard, Archer, Scout }
    public enum OriginBiome { Unknown, Meadows, BlackForest, Swamp, Mountains, Plains, Mistlands, Ashlands, DeepNorth }
    public enum TrainingBand { Untrained, Familiar, Trained, Veteran, Master }

    [Flags]
    public enum WeaponDoctrine { None=0, OneHanded=1, Shield=2, Bow=4, Spear=8, Throwing=16, Heavy=32, Magic=64 }

    [Flags]
    public enum SurvivalDoctrine
    {
        None=0, BlockObservedStrike=1, DodgeUnblockableStrike=2, ProtectPartner=4,
        PreserveCohesion=8, MaintainFiringLane=16, KitePressure=32,
        InvestigateEvidence=64, CommunicateThreat=128, DisengageAndRally=256,
        ReadBiomeHazards=512
    }

    public sealed class CharacterCombatProfile
    {
        public string PersonIdentity { get; private set; }
        public CombatJob Job { get; private set; }
        public FormidableClass FormidableClass { get; private set; }
        public OriginBiome Origin { get; private set; }
        public string Culture { get; private set; }
        public string GenderExpression { get; private set; }
        public TrainingBand Training { get; private set; }
        public WeaponDoctrine Weapons { get; private set; }
        public SurvivalDoctrine Survival { get; private set; }
        public bool EndlessUtilityTorch { get; private set; }

        public CharacterCombatProfile(string personIdentity, CombatJob job, FormidableClass formidableClass,
            OriginBiome origin, string culture, string genderExpression, TrainingBand training,
            WeaponDoctrine weapons, SurvivalDoctrine survival, bool endlessUtilityTorch)
        {
            if(string.IsNullOrEmpty(personIdentity)) throw new ArgumentException("person identity is required","personIdentity");
            PersonIdentity=personIdentity; Job=job; FormidableClass=formidableClass; Origin=origin;
            Culture=culture??string.Empty; GenderExpression=genderExpression??string.Empty;
            Training=training; Weapons=weapons; Survival=survival; EndlessUtilityTorch=endlessUtilityTorch;
        }
        public bool Has(WeaponDoctrine value){return (Weapons&value)==value;}
        public bool Has(SurvivalDoctrine value){return (Survival&value)==value;}
        public string Describe(){return Job+"/"+FormidableClass+"; origin="+Origin+"; training="+Training+
            "; weapons="+Weapons+"; survival="+Survival+"; utilityTorch="+(EndlessUtilityTorch?"endless":"limited");}
    }

    public sealed class CharacterRollupFacts
    {
        public string PersonIdentity, SavedRole, Culture, GenderExpression;
        public OriginBiome Origin;
        public TrainingBand Training;
        public bool HasOneHanded, HasShield, HasBow, HasSpear, HasHeavy, HasMagic;
    }

    public static class CharacterCombatProfileFactory
    {
        public static CharacterCombatProfile Create(CharacterRollupFacts facts)
        {
            if(facts==null) throw new ArgumentNullException("facts");
            CombatJob job=Job(facts.SavedRole);
            FormidableClass role=job==CombatJob.Archer?FormidableClass.Huntress:
                job==CombatJob.Scout?FormidableClass.Pathfinder:FormidableClass.Warden;
            WeaponDoctrine weapons=WeaponDoctrine.None;
            if(facts.HasOneHanded) weapons|=WeaponDoctrine.OneHanded;
            if(facts.HasShield&&facts.HasOneHanded) weapons|=WeaponDoctrine.Shield;
            if(facts.HasBow) weapons|=WeaponDoctrine.Bow;
            if(facts.HasSpear) weapons|=WeaponDoctrine.Spear|WeaponDoctrine.Throwing;
            if(facts.HasHeavy) weapons|=WeaponDoctrine.Heavy;
            if(facts.HasMagic) weapons|=WeaponDoctrine.Magic;
            SurvivalDoctrine survival=SurvivalDoctrine.DodgeUnblockableStrike|
                SurvivalDoctrine.PreserveCohesion|SurvivalDoctrine.DisengageAndRally|SurvivalDoctrine.ReadBiomeHazards;
            if((weapons&WeaponDoctrine.Shield)!=0) survival|=SurvivalDoctrine.BlockObservedStrike|SurvivalDoctrine.ProtectPartner;
            if((weapons&WeaponDoctrine.Bow)!=0) survival|=SurvivalDoctrine.MaintainFiringLane|SurvivalDoctrine.KitePressure;
            if(job==CombatJob.Scout) survival|=SurvivalDoctrine.InvestigateEvidence|SurvivalDoctrine.CommunicateThreat;
            TrainingBand training=facts.Training==TrainingBand.Untrained?TrainingBand.Trained:facts.Training;
            return new CharacterCombatProfile(facts.PersonIdentity,job,role,facts.Origin,facts.Culture,
                facts.GenderExpression,training,weapons,survival,true);
        }
        public static CombatJob Job(string savedRole)
        {
            if(string.Equals(savedRole,"archer",StringComparison.OrdinalIgnoreCase)) return CombatJob.Archer;
            if(string.Equals(savedRole,"scout",StringComparison.OrdinalIgnoreCase)) return CombatJob.Scout;
            return CombatJob.Guard;
        }
    }
}
