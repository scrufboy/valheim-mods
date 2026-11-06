using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.EncounterLoadouts
{
    public static class SplitAshProfiles
    {
        public static IList<EncounterProfile> Create()
        {
            return new[] { Runa(), Edda() };
        }

        static EncounterProfile Runa()
        {
            var p = Base("person.splitash.runa", "Runa", "close_first_scout",
                "Runa chose to keep Edda moving after their Second Breath and now watches the split-ash road.");
            p.Knowledge.Add(Knowledge("knowledge.splitash.blackforest_edge", "site.splitash.forest-edge", "personally traveled and hunted this road"));
            p.Variants.Add(Variant("runa.meadows.scout", BiomeTier.Meadows, "", "", "",
                "layered-hide-and-cloth", "round-shield-sword-and-stowed-bow",
                Gear("gear.runa.hide-layers", GearKind.Armor, BiomeTier.Meadows, 1, true),
                Gear("gear.runa.road-sword", GearKind.Weapon, BiomeTier.Meadows, 1, true),
                Gear("gear.runa.round-shield", GearKind.Shield, BiomeTier.Meadows, 1, true),
                Gear("gear.runa.hunting-bow", GearKind.Weapon, BiomeTier.Meadows, 1, false),
                Gear("gear.runa.arrow-reserve", GearKind.Ammunition, BiomeTier.Meadows, 20, false),
                Gear("supply.runa.travel-rations", GearKind.Supply, BiomeTier.Meadows, 4, false)));
            p.Variants.Add(Variant("runa.blackforest.scout", BiomeTier.BlackForest, "defeated_elder", "station.forge.available", "materials.blackforest.scout-kit",
                "reinforced-hide-and-mail", "round-shield-sword-and-stowed-bow",
                Gear("gear.runa.reinforced-layers", GearKind.Armor, BiomeTier.BlackForest, 1, true),
                Gear("gear.runa.forged-sword", GearKind.Weapon, BiomeTier.BlackForest, 1, true),
                Gear("gear.runa.reinforced-shield", GearKind.Shield, BiomeTier.BlackForest, 1, true),
                Gear("gear.runa.hunting-bow", GearKind.Weapon, BiomeTier.Meadows, 1, false),
                Gear("gear.runa.arrow-reserve", GearKind.Ammunition, BiomeTier.BlackForest, 24, false),
                Gear("supply.runa.travel-rations", GearKind.Supply, BiomeTier.Meadows, 4, false)));
            return p;
        }

        static EncounterProfile Edda()
        {
            var p = Base("person.splitash.edda", "Edda", "woodsman_camp_cook",
                "Edda keeps the cook fire, repairs the camp, and travels on a strained ankle with Runa.");
            p.Knowledge.Add(Knowledge("knowledge.splitash.boar-ground", "site.splitash.boar-ground", "personally foraged and set signs near this ground"));
            p.Variants.Add(Variant("edda.meadows.woodsman", BiomeTier.Meadows, "", "", "",
                "layered-work-clothes-and-hide", "wood-axe-and-round-shield",
                Gear("gear.edda.work-layers", GearKind.Armor, BiomeTier.Meadows, 1, true),
                Gear("gear.edda.woods-axe", GearKind.Weapon, BiomeTier.Meadows, 1, true),
                Gear("gear.edda.round-shield", GearKind.Shield, BiomeTier.Meadows, 1, true),
                Gear("gear.edda.camp-knife", GearKind.Tool, BiomeTier.Meadows, 1, false),
                Gear("supply.edda.shared-meals", GearKind.Supply, BiomeTier.Meadows, 6, false)));
            return p;
        }

        static EncounterProfile Base(string id, string name, string role, string history)
        {
            return new EncounterProfile { PersonId = id, DisplayName = name, OriginEncounterId = "encounter.splitash.01",
                OriginCampId = "camp.splitash.01", FactionId = "faction.hearthless.wary", RoleId = role,
                OriginBiome = BiomeTier.Meadows, DiscoveryWorldKey = "", RecruitmentWorldKey = "defeated_eikthyr",
                DistantSilhouette = "armed-road-survivor-ambiguous-with-pillager-at-distance", LocalHistory = history };
        }
        static KnowledgeRecord Knowledge(string id, string site, string source)
        { return new KnowledgeRecord { FactId = id, SiteId = site, Source = source, Verified = true }; }
        static GearRecord Gear(string id, GearKind kind, BiomeTier tier, int count, bool equipped)
        { return new GearRecord { ItemId = id, Kind = kind, Tier = tier, Quantity = count, Equipped = equipped, PersonalBound = true }; }
        static LoadoutVariant Variant(string id, BiomeTier tier, string key, string station, string materials, string armor, string weapon, params GearRecord[] gear)
        {
            var value = new LoadoutVariant { VariantId = id, Tier = tier, RequiredWorldKey = key,
                RequiredStationFact = station, RequiredMaterialFact = materials, ArmorSilhouette = armor, WeaponSilhouette = weapon };
            value.Gear.AddRange(gear); return value;
        }
    }
}
