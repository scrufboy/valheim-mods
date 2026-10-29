using System;
using System.Collections.Generic;
using LivingWorld.CompanionSystems.EncounterLoadouts;
using LivingWorld.CompanionSystems.EncounterPopulation;

static class EncounterPopulationTests
{
    static int checks;
    static void Check(bool value,string label) { if(!value) throw new Exception("FAIL "+label); checks++; }
    static PopulationRequest Request(BiomeTier biome,PeopleFaction faction,int seed)
    { return new PopulationRequest { WorldId="world.test",SiteId="site.forest.17",EncounterId="encounter.17",
        WorldSeed=seed,Count=2,Biome=biome,Faction=faction,ServerAuthority=true,SiteClaimPersisted=true }; }
    static Archetype Role(BiomeTier biome,PeopleFaction faction,string suffix)
    { foreach(var a in EncounterPopulationPolicy.Available(biome,faction)) if(a.Id.EndsWith(suffix)) return a;
      throw new Exception("Missing role "+suffix); }
    static CombatFacts Combat()
    { return new CombatFacts { ServerAuthority=true,ActorOwner=true,CombatOwnsAction=true,TargetAuthorized=true,
        PrimaryEquipped=true,FallbackEquipped=true,BombLiveVerified=true,SpearThrowLiveVerified=true,
        SpearRecoveryLiveVerified=true,KnifeLungeLiveVerified=true,AtgeirSweepLiveVerified=true,
        Spear=SpearStatus.Held }; }

    static void Main()
    {
        int catalog=0;
        for(int tier=0;tier<=(int)BiomeTier.Ashlands;tier++)
            foreach(PeopleFaction faction in Enum.GetValues(typeof(PeopleFaction))) {
                IList<Archetype> available=EncounterPopulationPolicy.Available((BiomeTier)tier,faction);
                Check(available.Count>=4,"biome and faction have varied roles: "+tier+"/"+faction);
                foreach(var a in available) {
                    Check(EncounterPopulationPolicy.Validate(a)==null && a.Biome==(BiomeTier)tier && a.Faction==faction,
                        "authored role is biome and faction bound");
                    Check(a.Fallback!=WeaponFamily.Bow && a.Primary!=a.Fallback && a.ThreatScore>=2+tier*2,
                        "every role has a distinct melee fallback and local threat tier");
                    catalog++;
                }
            }
        Check(catalog>=90,"whole package has a broad biome role catalog");
        Check(EncounterPopulationPolicy.Available(BiomeTier.DeepNorth,PeopleFaction.Hearthless).Count==0,
            "unapproved future biome has no invented kit");
        var illegalBomb=Role(BiomeTier.Meadows,PeopleFaction.Hearthless,"spear-warden");
        illegalBomb.Specials|=SpecialMove.BombThrow;illegalBomb.BombReserve=2;
        Check(EncounterPopulationPolicy.Validate(illegalBomb)=="invalid_weapon_package",
            "early biome cannot silently gain a bomb kit");
        var illegalThreat=Role(BiomeTier.Meadows,PeopleFaction.Hearthless,"knife-hunter");
        illegalThreat.ThreatScore=99;
        Check(EncounterPopulationPolicy.Validate(illegalThreat)=="invalid_archetype",
            "low biome cannot import an endgame threat score");

        var request=Request(BiomeTier.Swamp,PeopleFaction.Pillager,2718);
        PopulationPlan plan=EncounterPopulationPolicy.Select(request,null);
        Check(plan!=null && plan.Members.Count==2 && plan.Members[0].ArchetypeId!=plan.Members[1].ArchetypeId,
            "one site gets a varied two-person set");
        Check(plan.Members[0].PersonId!=plan.Members[1].PersonId && plan.Members[0].Kit.Faction==PeopleFaction.Pillager,
            "members keep distinct stable IDs and mechanical faction");
        PopulationPlan repeated=EncounterPopulationPolicy.Select(request,null);
        Check(repeated.Members[0].PersonId==plan.Members[0].PersonId &&
            repeated.Members[0].ArchetypeId==plan.Members[0].ArchetypeId,"seed selection repeats exactly");
        PopulationPlan replay=EncounterPopulationPolicy.Select(request,plan);
        Check(replay!=plan && replay.Members[0].ArchetypeId==plan.Members[0].ArchetypeId,
            "committed plan is replayed without rerolling or sharing mutable state");
        replay.Members[0].Kit.Weight=999;
        Check(plan.Members[0].Kit.Weight!=999,"replay cannot mutate the committed kit");
        var corrupted=plan.Copy();corrupted.Members[0].Kit.Faction=PeopleFaction.Hearthless;
        Check(EncounterPopulationPolicy.Select(request,corrupted)==null,
            "saved plan cannot smuggle a different allegiance into the same person");
        corrupted=plan.Copy();corrupted.Members[0].PersonId="person.replacement";
        Check(EncounterPopulationPolicy.Select(request,corrupted)==null,
            "saved plan cannot substitute a replacement identity");
        request.SiteId="invalid|site";
        Check(EncounterPopulationPolicy.Select(request,null)==null,"site identity rejects ambiguous separators");
        request.SiteId="site.forest.17";
        var client=ServerPresentationProjection.Build(plan.Members[0],false);
        Check(client.RevealedFaction==null && client.VisibleWeapon.Length>0 && client.Silhouette.Length>0,
            "unrevealed client sees equipment but not hidden allegiance");
        Check(ServerPresentationProjection.Build(plan.Members[0],true).RevealedFaction=="Pillager",
            "server reveal may add faction to presentation");
        request.ServerAuthority=false;
        Check(EncounterPopulationPolicy.Select(request,null)==null,"client cannot select or spawn a set");
        request.ServerAuthority=true;request.SiteClaimPersisted=false;
        Check(EncounterPopulationPolicy.Select(request,null)==null,"unclaimed site cannot select people");
        request.SiteClaimPersisted=true;request.Faction=PeopleFaction.Hearthless;
        Check(EncounterPopulationPolicy.Select(request,plan)==null,"committed hostile camp cannot silently switch faction");

        var spear=Role(BiomeTier.Meadows,PeopleFaction.Hearthless,"spear-warden");
        var f=Combat();f.SpearRequestAuthorized=true;
        Check(ServerCombatKitPolicy.Choose(spear,f)==CombatChoice.RequestSpearThrow,
            "spear throw enters native specialist path only after authorization");
        f.Spear=SpearStatus.Airborne;f.PrimaryEquipped=false;f.SpearRequestAuthorized=false;
        Check(ServerCombatKitPolicy.Choose(spear,f)==CombatChoice.UseFallback,
            "airborne spear switches to a real fallback weapon");
        f.SpearRecoverySafe=true;
        Check(ServerCombatKitPolicy.Choose(spear,f)==CombatChoice.UseFallback,
            "safe location alone cannot reclaim an unowned projectile");
        f.SpearProjectileOwned=true;f.SpearProjectileId="projectile.spear.17";
        Check(ServerCombatKitPolicy.Choose(spear,f)==CombatChoice.RequestSpearRecovery,
            "owned spear may be reclaimed only after a safe recovery fact");
        f.SpearRecoveryLiveVerified=false;
        Check(ServerCombatKitPolicy.Choose(spear,f)==CombatChoice.UseFallback,
            "unverified spear recovery cannot block fallback fighting");
        f.TargetAuthorized=false;
        Check(ServerCombatKitPolicy.Choose(spear,f)==CombatChoice.None,
            "unknown or unauthorized relationship never attacks");

        var bombardier=Role(BiomeTier.Swamp,PeopleFaction.Pillager,"spear-bombardier");
        f=Combat();f.BombsAvailable=2;f.BombRequestAuthorized=true;f.SpearRequestAuthorized=true;
        Check(ServerCombatKitPolicy.Choose(bombardier,f)==CombatChoice.RequestBombThrow,
            "bomb and spear capabilities coexist on one person and kit");
        f.BombsAvailable=0;
        f.SpearThrowLiveVerified=false;
        Check(ServerCombatKitPolicy.Choose(bombardier,f)==CombatChoice.UsePrimary,
            "proving bomb behavior does not silently approve spear throw");
        f.SpearThrowLiveVerified=true;
        Check(ServerCombatKitPolicy.Choose(bombardier,f)==CombatChoice.RequestSpearThrow,
            "spent bombs leave spear attack available");
        f.Spear=SpearStatus.Lost;f.PrimaryEquipped=false;
        Check(ServerCombatKitPolicy.Choose(bombardier,f)==CombatChoice.UseFallback,
            "lost spear and exhausted bombs still leave fallback weapon");
        f.ActorOwner=false;
        Check(ServerCombatKitPolicy.Choose(bombardier,f)==CombatChoice.None,
            "remote client never commits combat choice");

        var knife=Role(BiomeTier.BlackForest,PeopleFaction.Eitrherjar,"knife-hunter");
        f=Combat();f.KnifeRequestAuthorized=true;
        Check(ServerCombatKitPolicy.Choose(knife,f)==CombatChoice.RequestKnifeLunge,
            "knife role uses the bounded lunge request");
        f.KnifeLungeLiveVerified=false;
        Check(ServerCombatKitPolicy.Choose(knife,f)==CombatChoice.UsePrimary,
            "unproved lunge falls back to ordinary native knife use");
        var atgeir=Role(BiomeTier.Plains,PeopleFaction.Hearthless,"atgeir-guard");
        f=Combat();f.AtgeirRequestAuthorized=true;
        Check(ServerCombatKitPolicy.Choose(atgeir,f)==CombatChoice.RequestAtgeirSweep,
            "atgeir role uses separately authorized native move");
        Console.WriteLine("EncounterPopulationTests PASS: "+checks+" checks");
    }
}
