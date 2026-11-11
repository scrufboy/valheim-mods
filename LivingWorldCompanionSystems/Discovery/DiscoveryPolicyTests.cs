using System;
using System.Collections.Generic;
using LivingWorld.CompanionSystems.Discovery;

class DiscoveryPolicyTests
{
    static int checks;
    static void Check(bool value, string label) { checks++; if (!value) throw new Exception("FAIL " + label); }
    static SiteCandidate Site(string id, float x, DiscoveryBiome biome = DiscoveryBiome.Meadows)
    { return new SiteCandidate { Id = id, Biome = biome, X = x, Z = 100, Reachable = true, TerrainLoaded = true,
        WaterDepth = 0, SlopeDegrees = 5, PlayerStructureDistance = 200, BossAreaDistance = 400, Landmark = "a split oak" }; }
    static DiscoveryRequest Request()
    {
        return new DiscoveryRequest { IsServer = true, WorldSeed = 99, PartyId = 7, WorldDay = 12,
            PlayerX = 0, PlayerZ = 0, WorldKeys = new HashSet<string> { "defeated_eikthyr" },
            Encounters = new List<EncounterDefinition> { new EncounterDefinition { Id = "meadows-scout", Biome = DiscoveryBiome.Meadows,
                DiscoveryWorldKey = "", RecruitmentWorldKey = "defeated_eikthyr", SiteKind = "road watch" } },
            Candidates = new List<SiteCandidate> { Site("west", -600), Site("east", 800) },
            Camps = new List<CampRecord>(), Leads = new List<LeadRecord>() };
    }
    static void Main()
    {
        DiscoveryConfig config = new DiscoveryConfig();
        DiscoveryRequest a = Request(); DiscoveryResult first = DiscoveryPolicy.Select(a, config);
        Check(first.Accepted, "eligible request accepted"); Check(first.RecruitmentUnlocked, "recruitment key reported");
        DiscoveryResult again = DiscoveryPolicy.Select(Request(), config);
        Check(again.Lead.SiteId == first.Lead.SiteId, "selection deterministic");
        DiscoveryRequest reordered = Request(); ((List<SiteCandidate>)reordered.Candidates).Reverse();
        Check(DiscoveryPolicy.Select(reordered, config).Lead.SiteId == first.Lead.SiteId, "input order irrelevant");
        DiscoveryRequest client = Request(); client.IsServer = false;
        Check(DiscoveryPolicy.Select(client, config).Reason == "server-authority-required", "client cannot select");
        DiscoveryRequest active = Request(); active.Leads.Add(first.Lead);
        Check(DiscoveryPolicy.Select(active, config).Reason == "active-lead-exists", "one active lead");
        Check(DiscoveryPolicy.TryClose(first.Lead, LeadState.Resolved, 12), "lead closes");
        DiscoveryRequest cooldown = Request(); cooldown.Leads.Add(first.Lead); cooldown.WorldDay = 16;
        Check(DiscoveryPolicy.Select(cooldown, config).Reason == "cooldown-active", "five day cooldown blocks day four");
        cooldown.WorldDay = 17; Check(DiscoveryPolicy.Select(cooldown, config).Accepted, "five day cooldown opens on boundary");

        string saved = DiscoveryPersistence.Write(first.Lead); LeadRecord restored;
        Check(DiscoveryPersistence.TryRead(saved, out restored), "persistent record roundtrip");
        Check(restored.TemplateId == "meadows-scout" && restored.EncounterId == first.Lead.EncounterId &&
            restored.SiteId == first.Lead.SiteId && restored.State == LeadState.Resolved, "template, instance, site and state retained");
        Check(!DiscoveryPersistence.TryRead(saved.Replace("lwc-discovery-v1", "future"), out restored), "future schema fails closed");
        Check(!DiscoveryPersistence.TryRead(saved.Replace("Resolved", "Active"), out restored), "invalid active close day rejected");

        DiscoveryRequest unsafeRequest = Request();
        foreach (SiteCandidate s in unsafeRequest.Candidates) s.HasLava = true;
        Check(DiscoveryPolicy.Select(unsafeRequest, config).Reason == "no-eligible-safe-site", "lava rejected");
        unsafeRequest = Request(); foreach (SiteCandidate s in unsafeRequest.Candidates) s.Reachable = false;
        Check(!DiscoveryPolicy.Select(unsafeRequest, config).Accepted, "unreachable rejected");
        unsafeRequest = Request(); foreach (SiteCandidate s in unsafeRequest.Candidates) s.PlayerStructureDistance = 79;
        Check(!DiscoveryPolicy.Select(unsafeRequest, config).Accepted, "player structures protected");
        unsafeRequest = Request(); foreach (SiteCandidate s in unsafeRequest.Candidates) s.BossAreaDistance = 199;
        Check(!DiscoveryPolicy.Select(unsafeRequest, config).Accepted, "boss areas protected");
        unsafeRequest = Request(); unsafeRequest.Camps.Add(new CampRecord { SiteId = "old", X = -600, Z = 100 });
        unsafeRequest.Camps.Add(new CampRecord { SiteId = "east", X = 800, Z = 100 });
        Check(!DiscoveryPolicy.Select(unsafeRequest, config).Accepted, "duplicate and overlapping camps rejected");
        DiscoveryRequest sharedTemplate = Request();
        sharedTemplate.Camps.Add(new CampRecord { TemplateId = "meadows-scout", EncounterId = "existing-instance",
            SiteId = "elsewhere", X = 5000, Z = 5000 });
        DiscoveryResult secondInstance = DiscoveryPolicy.Select(sharedTemplate, config);
        Check(secondInstance.Accepted && secondInstance.Lead.TemplateId == "meadows-scout" &&
            secondInstance.Lead.EncounterId != "existing-instance", "authored template supports independent world instances");
        DiscoveryRequest occupied = Request();
        occupied.Camps.Add(new CampRecord { TemplateId = "meadows-scout", EncounterId = first.Lead.EncounterId,
            SiteId = first.Lead.SiteId, X = first.Lead.X, Z = first.Lead.Z });
        occupied.Candidates = new List<SiteCandidate> { Site(first.Lead.SiteId, first.Lead.X) };
        Check(!DiscoveryPolicy.Select(occupied, config).Accepted, "active persistent instance and site cannot duplicate");
        ((CampRecord)occupied.Camps[0]).Retired = true;
        Check(DiscoveryPolicy.Select(occupied, config).Accepted, "retired camp releases its site for authored reuse");

        DiscoveryRequest gated = Request(); gated.Encounters = new List<EncounterDefinition> { new EncounterDefinition { Id = "plains-pair",
            Biome = DiscoveryBiome.Plains, DiscoveryWorldKey = "defeated_moder", RecruitmentWorldKey = "defeated_yagluth", SiteKind = "siege remnant" } };
        gated.Candidates = new List<SiteCandidate> { Site("plains", 600, DiscoveryBiome.Plains) };
        Check(!DiscoveryPolicy.Select(gated, config).Accepted, "discovery world key gates biome");
        gated.WorldKeys.Add("defeated_moder"); DiscoveryResult evidence = DiscoveryPolicy.Select(gated, config);
        Check(evidence.Accepted && !evidence.RecruitmentUnlocked, "evidence may precede recruitment key");
        gated.WorldKeys.Add("defeated_yagluth"); Check(DiscoveryPolicy.Select(gated, config).RecruitmentUnlocked, "recruitment key unlocks without changing site policy");
        Check(first.Clue.Contains("Tracks point") && first.Clue.Contains("split oak"), "clue gives direction and landmark");
        Console.WriteLine("DiscoveryPolicyTests PASS: " + checks + " checks");
    }
}
