using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LivingWorld.CompanionSystems.Discovery
{
    public sealed class NearbyLocationEvidence
    {
        public string LocationId;
        public string LocationKind;
        public bool RuntimeValidated;
        public bool WithinAuthoredKnowledgeRadius;
    }

    public sealed class SiteKnowledgeRecord
    {
        public string KnowledgeId;
        public string EncounterId;
        public string SiteId;
        public string LocationId;
        public string LocationKind;
        public CampActivity ObservedActivity;
        public int LearnedDay;
    }

    public static class SiteKnowledgePolicy
    {
        public static bool TryCreate(bool isServer, long worldSeed, int worldDay, EncounterScene scene,
            IList<NearbyLocationEvidence> evidence, out SiteKnowledgeRecord knowledge)
        {
            knowledge = null;
            if (!isServer || worldDay < 0 || scene == null || String.IsNullOrEmpty(scene.EncounterId) ||
                String.IsNullOrEmpty(scene.SiteId) || evidence == null) return false;
            NearbyLocationEvidence selected = null;
            ulong selectedScore = UInt64.MaxValue;
            string selectedIdentity = null;
            foreach (NearbyLocationEvidence item in evidence)
            {
                if (item == null || !item.RuntimeValidated || !item.WithinAuthoredKnowledgeRadius ||
                    String.IsNullOrEmpty(item.LocationId) || String.IsNullOrEmpty(item.LocationKind)) continue;
                string identity = item.LocationId + "|" + item.LocationKind;
                ulong score = Score(worldSeed, scene.EncounterId + "|" + identity);
                if (score < selectedScore || (score == selectedScore &&
                    (selectedIdentity == null || StringComparer.Ordinal.Compare(identity, selectedIdentity) < 0)))
                { selected = item; selectedScore = score; selectedIdentity = identity; }
            }
            if (selected == null) return false;
            knowledge = new SiteKnowledgeRecord {
                KnowledgeId = "lwc-knowledge-" + Score(worldSeed, scene.EncounterId + "|" + selected.LocationId).ToString("x16", CultureInfo.InvariantCulture),
                EncounterId = scene.EncounterId, SiteId = scene.SiteId, LocationId = selected.LocationId,
                LocationKind = selected.LocationKind, ObservedActivity = scene.Activity, LearnedDay = worldDay
            };
            return true;
        }

        public static string Describe(SiteKnowledgeRecord record)
        {
            if (record == null) return String.Empty;
            string source = record.ObservedActivity == CampActivity.Hunting ? "The hunters know" :
                record.ObservedActivity == CampActivity.Patrol ? "The watch route passes" : "The camp keeps word of";
            // The adapter may resolve LocationId for a map marker or dialogue.
            // This policy never fabricates a coordinate or an allegiance.
            return source + " a nearby " + record.LocationKind + " [" + record.LocationId + "].";
        }

        static ulong Score(long seed, string value)
        {
            ulong hash = 14695981039346656037UL;
            byte[] bytes = Encoding.UTF8.GetBytes(seed.ToString(CultureInfo.InvariantCulture) + "|" + value);
            for (int i = 0; i < bytes.Length; i++) { hash ^= bytes[i]; hash *= 1099511628211UL; }
            return hash;
        }
    }
}
