using System;
using System.Collections.Generic;
using LivingWorld.CompanionSystems.EncounterLoadouts;

class EncounterLoadoutPolicyTests
{
    static int checks;
    static void Check(bool value, string label) { checks++; if (!value) throw new Exception("FAIL " + label); }
    static ISet<string> Set(params string[] values) { return new HashSet<string>(values, StringComparer.Ordinal); }

    static void Main()
    {
        IList<EncounterProfile> profiles = SplitAshProfiles.Create();
        EncounterProfile runa = profiles[0], edda = profiles[1];
        Check(EncounterLoadoutPolicy.Validate(runa) == null && EncounterLoadoutPolicy.Validate(edda) == null, "authored profiles valid");
        Check(runa.PersonId != edda.PersonId && runa.FactionId == edda.FactionId, "camp has two people and explicit faction");
        Check(runa.DistantSilhouette.Contains("pillager") && runa.FactionId.Contains("hearthless"), "visual ambiguity never hides mechanical faction");

        PolicyResult created = EncounterLoadoutPolicy.Create(runa, "runa.meadows.scout", Set());
        Check(created.Accepted && created.State.ActiveVariantId == "runa.meadows.scout", "Meadows tier needs no boss key");
        Check(!EncounterLoadoutPolicy.IsRecruitable(runa, Set()) && EncounterLoadoutPolicy.IsRecruitable(runa, Set("defeated_eikthyr")), "recruitment key separate from discovery");
        PolicyResult locked = EncounterLoadoutPolicy.Create(runa, "runa.blackforest.scout", Set());
        Check(!locked.Accepted && locked.Reason == "world_key_locked", "Black Forest tier requires Elder");

        EncounterProfile illegal = SplitAshProfiles.Create()[0];
        illegal.Variants[0].Gear.Add(new GearRecord { ItemId = "gear.illegal.endgame", Kind = GearKind.Weapon,
            Tier = BiomeTier.Mistlands, Quantity = 1, PersonalBound = true });
        Check(EncounterLoadoutPolicy.Validate(illegal) == "gear_exceeds_variant_tier", "illegal higher-tier gear rejected");
        EncounterProfile freeLoot = SplitAshProfiles.Create()[0];
        freeLoot.Variants[0].Gear.Add(new GearRecord { ItemId = "supply.illegal.hoard", Kind = GearKind.Supply,
            Tier = BiomeTier.Meadows, Quantity = 99, PersonalBound = true });
        Check(EncounterLoadoutPolicy.Validate(freeLoot) == "starting_supply_exceeds_cap", "no free endgame loot or supply hoard");
        EncounterProfile unbound = SplitAshProfiles.Create()[0]; unbound.Variants[0].Gear[0].PersonalBound = false;
        Check(EncounterLoadoutPolicy.Validate(unbound) == "personal_gear_must_be_bound", "starting gear cannot be strip-loot");

        PersonLoadoutState state = created.State;
        state.Relationship = "allied"; state.Wounds = "healing-left-shoulder";
        string beforePerson = state.PersonId, beforeOrigin = state.OriginEncounterId;
        PolicyResult noStation = EncounterLoadoutPolicy.Upgrade(runa, state, new UpgradeRequest { PersonId = state.PersonId,
            TargetVariantId = "runa.blackforest.scout", WorldKeys = Set("defeated_elder"), VerifiedFacts = Set("materials.blackforest.scout-kit") });
        Check(!noStation.Accepted && noStation.Reason == "legal_upgrade_evidence_required", "upgrade needs station and materials");
        PolicyResult upgraded = EncounterLoadoutPolicy.Upgrade(runa, state, new UpgradeRequest { PersonId = state.PersonId,
            TargetVariantId = "runa.blackforest.scout", WorldKeys = Set("defeated_elder"),
            VerifiedFacts = Set("station.forge.available", "materials.blackforest.scout-kit") });
        Check(upgraded.Accepted && upgraded.State.PersonId == beforePerson && upgraded.State.OriginEncounterId == beforeOrigin, "upgrade preserves person and origin");
        Check(upgraded.State.Relationship == "allied" && upgraded.State.Wounds == "healing-left-shoulder", "upgrade preserves relationship and wounds");
        Check(state.ActiveVariantId == "runa.meadows.scout", "upgrade is immutable transaction");

        string saved = EncounterLoadoutPersistence.Write(upgraded.State); PersonLoadoutState restored;
        Check(EncounterLoadoutPersistence.TryRead(saved, out restored), "upgraded state reloads");
        Check(restored.PersonId == beforePerson && restored.ActiveVariantId == "runa.blackforest.scout" &&
            restored.Relationship == "allied" && restored.Wounds == "healing-left-shoulder", "variant continuity survives reload");
        Check(restored.Knowledge.Count == 1 && restored.Knowledge[0].SiteId == "site.splitash.forest-edge", "verified local knowledge survives reload");

        var roster = new List<PersonLoadoutState> { restored, restored.Copy() };
        Check(EncounterLoadoutPolicy.ValidateRoster(roster) == "duplicate_or_invalid_person", "duplicate person refused");
        PersonLoadoutState dead = restored.Copy(); dead.Life = LifeState.Dead; dead.DeathRecord = "fell-at-site.splitash.forest-edge";
        PolicyResult reviveByUpgrade = EncounterLoadoutPolicy.Upgrade(runa, dead, new UpgradeRequest { PersonId = dead.PersonId,
            TargetVariantId = "runa.blackforest.scout", WorldKeys = Set("defeated_elder"),
            VerifiedFacts = Set("station.forge.available", "materials.blackforest.scout-kit") });
        Check(!reviveByUpgrade.Accepted && reviveByUpgrade.Reason == "dead_person_cannot_advance", "upgrade cannot replace or revive dead person");
        Check(!EncounterLoadoutPersistence.TryRead(saved.Replace("lwc-encounter-loadout-v1", "future"), out restored), "unknown schema fails closed");
        Console.WriteLine("EncounterLoadoutPolicyTests PASS: " + checks + " checks");
    }
}
