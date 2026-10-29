using System;
using System.Collections.Generic;
using LivingWorld.CompanionSystems.Discovery;

class EncounterScenePolicyTests
{
    static int checks;
    static void Check(bool value, string label) { checks++; if (!value) throw new Exception("FAIL " + label); }
    static LeadRecord Lead()
    { return new LeadRecord { PartyId = 5, TemplateId = "meadows-watch", EncounterId = "lwc-enc-123",
        SiteId = "site-real-7", State = LeadState.Active, CreatedDay = 4, ClosedDay = -1 }; }
    static EncounterSceneDefinition Definition()
    { return new EncounterSceneDefinition { TemplateId = "meadows-watch", MinimumPeople = 1, MaximumPeople = 2,
        Activities = new List<CampActivity> { CampActivity.Hunting, CampActivity.Camping, CampActivity.Patrol } }; }
    static void Main()
    {
        EncounterScene scene, repeat;
        Check(EncounterScenePolicy.TryCompose(77, Lead(), Definition(), out scene), "valid scene composed");
        Check(scene.People >= 1 && scene.People <= 2, "scene limited to one or two people");
        Check(EncounterScenePolicy.TryCompose(77, Lead(), Definition(), out repeat) &&
            repeat.People == scene.People && repeat.Activity == scene.Activity, "scene composition deterministic");
        Check(scene.DistantRead.Contains("human silhouette") && scene.DistantRead.Contains("allegiance is not yet clear"),
            "distant read stays human and allegiance-neutral");
        Check(!scene.DistantRead.Contains("friendly") && !scene.DistantRead.Contains("Pillager"), "distance does not reveal faction");
        EncounterSceneDefinition invalid = Definition(); invalid.MaximumPeople = 3;
        Check(!EncounterScenePolicy.TryCompose(77, Lead(), invalid, out repeat), "larger camp rejected");
        invalid = Definition(); invalid.TemplateId = "other";
        Check(!EncounterScenePolicy.TryCompose(77, Lead(), invalid, out repeat), "template mismatch rejected");
        invalid = Definition(); invalid.Activities.Add((CampActivity)99);
        Check(!EncounterScenePolicy.TryCompose(77, Lead(), invalid, out repeat), "unknown authored activity rejected");

        var evidence = new List<NearbyLocationEvidence> {
            new NearbyLocationEvidence { LocationId = "runtime-location-a", LocationKind = "burial site", RuntimeValidated = true, WithinAuthoredKnowledgeRadius = true },
            new NearbyLocationEvidence { LocationId = "unverified", LocationKind = "tower", RuntimeValidated = false, WithinAuthoredKnowledgeRadius = true },
            new NearbyLocationEvidence { LocationId = "too-far", LocationKind = "cave", RuntimeValidated = true, WithinAuthoredKnowledgeRadius = false }
        };
        SiteKnowledgeRecord knowledge, knowledgeAgain;
        Check(SiteKnowledgePolicy.TryCreate(true, 77, 4, scene, evidence, out knowledge), "validated nearby knowledge created");
        Check(knowledge.LocationId == "runtime-location-a" && knowledge.SiteId == scene.SiteId &&
            knowledge.EncounterId == scene.EncounterId, "knowledge preserves real location and site provenance");
        Check(SiteKnowledgePolicy.TryCreate(true, 77, 4, scene, evidence, out knowledgeAgain) &&
            knowledgeAgain.KnowledgeId == knowledge.KnowledgeId, "knowledge identity deterministic");
        string description = SiteKnowledgePolicy.Describe(knowledge);
        Check(description.Contains("runtime-location-a") && !description.Contains("friendly") && !description.Contains("Pillager"),
            "knowledge describes validated ID without faction invention");
        Check(!SiteKnowledgePolicy.TryCreate(false, 77, 4, scene, evidence, out knowledgeAgain), "client cannot author knowledge");
        Check(!SiteKnowledgePolicy.TryCreate(true, 77, 4, scene, new List<NearbyLocationEvidence> {
            new NearbyLocationEvidence { LocationId = "invented", LocationKind = "tower", RuntimeValidated = false, WithinAuthoredKnowledgeRadius = true }
        }, out knowledgeAgain), "unvalidated location cannot become knowledge");
        Console.WriteLine("EncounterScenePolicyTests PASS: " + checks + " checks");
    }
}
