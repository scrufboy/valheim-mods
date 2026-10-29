using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LivingWorld.CompanionSystems.Discovery
{
    public enum CampActivity { Hunting, Camping, Patrol }

    public sealed class EncounterSceneDefinition
    {
        public string TemplateId;
        public int MinimumPeople = 1;
        public int MaximumPeople = 2;
        public IList<CampActivity> Activities;
    }

    public sealed class EncounterScene
    {
        public string EncounterId;
        public string TemplateId;
        public string SiteId;
        public int People;
        public CampActivity Activity;
        public string DistantRead;
    }

    public static class EncounterScenePolicy
    {
        public static bool TryCompose(long worldSeed, LeadRecord lead, EncounterSceneDefinition definition,
            out EncounterScene scene)
        {
            scene = null;
            if (lead == null || definition == null || lead.State != LeadState.Active) return false;
            if (String.IsNullOrEmpty(lead.EncounterId) || String.IsNullOrEmpty(lead.SiteId) ||
                definition.TemplateId != lead.TemplateId) return false;
            if (definition.MinimumPeople < 1 || definition.MaximumPeople > 2 ||
                definition.MinimumPeople > definition.MaximumPeople || definition.Activities == null ||
                definition.Activities.Count == 0) return false;
            for (int i = 0; i < definition.Activities.Count; i++)
                if (!DefinedActivity(definition.Activities[i])) return false;

            ulong peopleScore = Score(worldSeed, lead.EncounterId + "|people");
            int people = definition.MinimumPeople + (int)(peopleScore %
                (ulong)(definition.MaximumPeople - definition.MinimumPeople + 1));
            ulong activityScore = Score(worldSeed, lead.EncounterId + "|activity");
            CampActivity activity = definition.Activities[(int)(activityScore % (ulong)definition.Activities.Count)];
            scene = new EncounterScene {
                EncounterId = lead.EncounterId, TemplateId = lead.TemplateId, SiteId = lead.SiteId,
                People = people, Activity = activity, DistantRead = DistantRead(people, activity)
            };
            return true;
        }

        static string DistantRead(int people, CampActivity activity)
        {
            string number = people == 1 ? "A distant human silhouette" : "Two distant human silhouettes";
            string motion = activity == CampActivity.Hunting ? "moves through the hunting ground" :
                activity == CampActivity.Patrol ? "paces a watch route" : "works around a small camp";
            // Allegiance is deliberately absent. EncounterAllegiance owns reveal
            // after the runtime has enough evidence to distinguish friend from foe.
            return number + " " + motion + "; allegiance is not yet clear.";
        }

        static bool DefinedActivity(CampActivity value)
        { return value == CampActivity.Hunting || value == CampActivity.Camping || value == CampActivity.Patrol; }

        static ulong Score(long seed, string value)
        {
            ulong hash = 14695981039346656037UL;
            byte[] bytes = Encoding.UTF8.GetBytes(seed.ToString(CultureInfo.InvariantCulture) + "|" + value);
            for (int i = 0; i < bytes.Length; i++) { hash ^= bytes[i]; hash *= 1099511628211UL; }
            return hash;
        }
    }
}
