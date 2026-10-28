using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LivingWorld.CompanionSystems.Discovery
{
    public enum DiscoveryBiome { Meadows, BlackForest, Swamp, Mountain, Plains, Mistlands, Ashlands, DeepNorth }
    public enum LeadState { None, Active, Resolved, Refused, Abandoned, Death }

    public sealed class DiscoveryConfig
    {
        public int CooldownDays = 5;
        public float MinPlayerStructureDistance = 80f;
        public float MinBossAreaDistance = 200f;
        public float MinCampSeparation = 300f;
        public float MaxSlopeDegrees = 22f;
        public float MaxWaterDepth = .25f;
    }

    public sealed class EncounterDefinition
    {
        public string Id;
        public DiscoveryBiome Biome;
        public string DiscoveryWorldKey;
        public string RecruitmentWorldKey;
        public string SiteKind;
    }

    public sealed class SiteCandidate
    {
        public string Id;
        public DiscoveryBiome Biome;
        public float X, Y, Z;
        public bool Reachable;
        public bool TerrainLoaded;
        public bool HasLava;
        public float WaterDepth;
        public float SlopeDegrees;
        public float PlayerStructureDistance;
        public float BossAreaDistance;
        public string Landmark;
    }

    public sealed class CampRecord
    {
        public string TemplateId;
        public string EncounterId;
        public string SiteId;
        public long PartyId;
        public float X, Y, Z;
        public bool Retired;
    }

    public sealed class LeadRecord
    {
        public long PartyId;
        public string TemplateId;
        public string EncounterId;
        public string SiteId;
        public LeadState State;
        public int CreatedDay;
        public int ClosedDay;
        public float X, Y, Z;
    }

    public sealed class DiscoveryRequest
    {
        public bool IsServer;
        public long WorldSeed;
        public long PartyId;
        public int WorldDay;
        public float PlayerX, PlayerZ;
        public ISet<string> WorldKeys;
        public IList<EncounterDefinition> Encounters;
        public IList<SiteCandidate> Candidates;
        public IList<CampRecord> Camps;
        public IList<LeadRecord> Leads;
    }

    public sealed class DiscoveryResult
    {
        public bool Accepted;
        public string Reason;
        public LeadRecord Lead;
        public string Clue;
        public bool RecruitmentUnlocked;
    }

    public static class DiscoveryPolicy
    {
        public const string Schema = "lwc-discovery-v1";

        public static DiscoveryResult Select(DiscoveryRequest request, DiscoveryConfig config)
        {
            if (request == null || config == null) return Denied("invalid-request");
            if (!request.IsServer) return Denied("server-authority-required");
            if (request.PartyId == 0 || request.WorldDay < 0) return Denied("invalid-party-or-day");
            if (config.CooldownDays < 1) return Denied("invalid-cooldown");
            if (HasActiveLead(request.Leads, request.PartyId)) return Denied("active-lead-exists");
            int lastClosed = LastClosedDay(request.Leads, request.PartyId);
            if (lastClosed >= 0 && request.WorldDay < lastClosed + config.CooldownDays)
                return Denied("cooldown-active");

            EncounterDefinition bestEncounter = null;
            SiteCandidate bestSite = null;
            ulong bestScore = UInt64.MaxValue;
            string bestIdentity = null;
            if (request.Encounters == null || request.Candidates == null) return Denied("no-candidates");
            foreach (EncounterDefinition encounter in request.Encounters)
            {
                if (!ValidEncounter(encounter) || !HasKey(request.WorldKeys, encounter.DiscoveryWorldKey)) continue;
                foreach (SiteCandidate site in request.Candidates)
                {
                    if (!Safe(site, encounter.Biome, request.Camps, config, encounter.Id)) continue;
                    string identity = encounter.Id + "|" + site.Id;
                    ulong score = StableScore(request.WorldSeed, request.PartyId, request.WorldDay, identity);
                    if (score < bestScore || (score == bestScore &&
                        (bestIdentity == null || StringComparer.Ordinal.Compare(identity, bestIdentity) < 0)))
                    {
                        bestScore = score;
                        bestIdentity = identity;
                        bestEncounter = encounter;
                        bestSite = site;
                    }
                }
            }
            if (bestEncounter == null) return Denied("no-eligible-safe-site");

            LeadRecord lead = new LeadRecord {
                PartyId = request.PartyId, TemplateId = bestEncounter.Id,
                EncounterId = InstanceId(request.WorldSeed, bestEncounter.Id, bestSite.Id), SiteId = bestSite.Id,
                State = LeadState.Active, CreatedDay = request.WorldDay, ClosedDay = -1,
                X = bestSite.X, Y = bestSite.Y, Z = bestSite.Z
            };
            return new DiscoveryResult {
                Accepted = true, Reason = "accepted", Lead = lead,
                RecruitmentUnlocked = HasKey(request.WorldKeys, bestEncounter.RecruitmentWorldKey),
                Clue = BuildClue(request.PlayerX, request.PlayerZ, bestSite, bestEncounter.SiteKind)
            };
        }

        public static bool Safe(SiteCandidate site, DiscoveryBiome biome, IList<CampRecord> camps,
            DiscoveryConfig config, string templateId)
        {
            if (site == null || config == null || String.IsNullOrEmpty(site.Id) || site.Biome != biome) return false;
            if (!Finite(site.X) || !Finite(site.Y) || !Finite(site.Z) || !site.Reachable || !site.TerrainLoaded) return false;
            if (site.HasLava || !Finite(site.WaterDepth) || site.WaterDepth > config.MaxWaterDepth || site.WaterDepth < 0f) return false;
            if (!Finite(site.SlopeDegrees) || site.SlopeDegrees < 0f || site.SlopeDegrees > config.MaxSlopeDegrees) return false;
            if (!Finite(site.PlayerStructureDistance) || site.PlayerStructureDistance < config.MinPlayerStructureDistance) return false;
            if (!Finite(site.BossAreaDistance) || site.BossAreaDistance < config.MinBossAreaDistance) return false;
            if (camps == null) return true;
            foreach (CampRecord camp in camps)
            {
                if (camp == null || camp.Retired) continue;
                if (camp.SiteId == site.Id) return false;
                float dx = camp.X - site.X, dz = camp.Z - site.Z;
                if (dx * dx + dz * dz < config.MinCampSeparation * config.MinCampSeparation) return false;
            }
            return true;
        }

        public static string BuildClue(float playerX, float playerZ, SiteCandidate site, string siteKind)
        {
            float dx = site.X - playerX, dz = site.Z - playerZ;
            double distance = Math.Sqrt(dx * dx + dz * dz);
            double angle = Math.Atan2(dx, dz) * 180.0 / Math.PI;
            if (angle < 0) angle += 360.0;
            string[] directions = { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };
            string direction = directions[((int)Math.Floor((angle + 22.5) / 45.0)) % 8];
            string band = distance < 500 ? "nearby" : distance < 1500 ? "a fair walk" : "far away";
            string landmark = String.IsNullOrEmpty(site.Landmark) ? "distinctive ground" : site.Landmark;
            string kind = String.IsNullOrEmpty(siteKind) ? "camp" : siteKind;
            return String.Format(CultureInfo.InvariantCulture,
                "Tracks point {0}; the {1} is {2}, close to {3}.", direction, kind, band, landmark);
        }

        public static bool TryClose(LeadRecord lead, LeadState outcome, int worldDay)
        {
            if (lead == null || lead.State != LeadState.Active || worldDay < lead.CreatedDay) return false;
            if (outcome != LeadState.Resolved && outcome != LeadState.Refused &&
                outcome != LeadState.Abandoned && outcome != LeadState.Death) return false;
            lead.State = outcome; lead.ClosedDay = worldDay; return true;
        }

        static bool ValidEncounter(EncounterDefinition value)
        { return value != null && !String.IsNullOrEmpty(value.Id) && !String.IsNullOrEmpty(value.SiteKind); }
        static bool HasKey(ISet<string> keys, string key)
        { return String.IsNullOrEmpty(key) || (keys != null && keys.Contains(key)); }
        static bool HasActiveLead(IList<LeadRecord> leads, long party)
        {
            if (leads == null) return false;
            foreach (LeadRecord lead in leads) if (lead != null && lead.PartyId == party && lead.State == LeadState.Active) return true;
            return false;
        }
        static int LastClosedDay(IList<LeadRecord> leads, long party)
        {
            int result = -1;
            if (leads != null) foreach (LeadRecord lead in leads)
                if (lead != null && lead.PartyId == party && lead.State != LeadState.Active && lead.ClosedDay > result) result = lead.ClosedDay;
            return result;
        }
        static DiscoveryResult Denied(string reason) { return new DiscoveryResult { Accepted = false, Reason = reason }; }
        static bool Finite(float value) { return !Single.IsNaN(value) && !Single.IsInfinity(value); }

        static ulong StableScore(long worldSeed, long partyId, int day, string text)
        {
            ulong hash = 14695981039346656037UL;
            string input = worldSeed.ToString(CultureInfo.InvariantCulture) + "|" +
                partyId.ToString(CultureInfo.InvariantCulture) + "|" + day.ToString(CultureInfo.InvariantCulture) + "|" + text;
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++) { hash ^= bytes[i]; hash *= 1099511628211UL; }
            return hash;
        }

        static string InstanceId(long worldSeed, string templateId, string siteId)
        {
            ulong value = StableScore(worldSeed, 0L, 0, templateId + "|" + siteId);
            return "lwc-enc-" + value.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
