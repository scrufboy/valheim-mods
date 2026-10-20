using System;

namespace LivingWorld.HumanNpc
{
    // Pure UI formatting. Canonical identity stays plain and contains no tags.
    // This class does not write Character.m_name or persist role assignments.
    public static class HumanNamePresentation
    {
        public static string Format(HumanProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            // InitialRole currently admits only unassigned. Do not invent a
            // class/title from loadout, appearance or a presentation heuristic.
            return Format(profile.Name, "");
        }

        // Optional title must come from an authoritative role/assignment owner,
        // never an inferred class or arbitrary generated rich-text fragment.
        public static string Format(string name, string authoritativeTitle)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.", "name");
            string result = "<b>" + Escape(name) + "</b>";
            if (!String.IsNullOrWhiteSpace(authoritativeTitle))
                result += "\n<size=80%><color=#B8B3A8>" + Escape(authoritativeTitle) + "</color></size>";
            return result;
        }

        static string Escape(string value)
        {
            // Escape all tag delimiters even when an upstream validator normally
            // excludes them. Provider data cannot become executable markup.
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
