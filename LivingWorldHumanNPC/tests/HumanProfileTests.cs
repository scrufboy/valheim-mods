using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using LivingWorld.HumanNpc;

static class HumanProfileTests
{
    static int checks;
    const string Seed = "0123456789abcdef0123456789abcdef";
    static readonly string[] HairChoices = { "Hair1", "Hair2", "Hair3", "Hair4" };
    static readonly string[] BeardChoices = { "", "Beard1", "Beard2", "Beard3" };
    static void Check(bool condition, string label) { ++checks; if (!condition) throw new Exception(label); }
    static string Field(string record, int index, string value) { var parts = record.Split('|'); parts[index] = value; return String.Join("|", parts); }
    static void Reject(string record, string label) { HumanProfile result; Check(!HumanProfile.TryRead(record, out result) && result == null, label); }
    public static int Main()
    {
        try
        {
            var original = HumanProfile.Generate(Seed, HairChoices, BeardChoices);
            string saved = original.Save();
            Check(saved == "1|0123456789abcdef0123456789abcdef|Gunnar Knutsson|0|Hair2|Beard1|211|174|139|119|48|26|20|41|50|unassigned", "version-one golden generation record");
            Check(original.Id == "human-" + Seed, "stable individual ID");
            Check(original.InitialRole == "unassigned", "no class or settlement assignment");
            Check(saved.Length <= HumanProfile.MaximumRecordLength, "bounded generated record");
            Check(HumanProfile.Generate(Seed, HairChoices, BeardChoices).Save() == saved, "seeded generation is deterministic");
            HumanProfile restored;
            Check(HumanProfile.TryRead(saved, out restored) && restored.Save() == saved, "all generated values survive exact roundtrip");
            string edited = Field(saved, 2, "Mira Reed");
            Check(HumanProfile.TryRead(edited, out restored) && restored.Name == "Mira Reed" && restored.Save() == edited,
                "load uses stored fields rather than regenerating from seed");
            var prior = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                Check(HumanProfile.Generate(Seed, HairChoices, BeardChoices).Save() == saved && HumanProfile.TryRead(saved, out restored) && restored.Save() == saved,
                    "culture-independent generation and persistence");
            }
            finally { Thread.CurrentThread.CurrentCulture = prior; }
            Reject(null, "null"); Reject("", "empty"); Reject(new String('x', 257), "oversized");
            Reject(saved + "|extra", "trailing field"); Reject(saved.Substring(0, saved.LastIndexOf('|')), "truncated");
            Reject(Field(saved, 0, "2"), "future version"); Reject(Field(saved, 0, "01"), "ambiguous version");
            Reject(Field(saved, 1, Seed.ToUpperInvariant()), "noncanonical seed"); Reject(Field(saved, 1, "0000"), "short seed");
            Reject(Field(saved, 2, "<color=red>X</color>"), "name markup"); Reject(Field(saved, 2, "Line\nBreak"), "name control character");
            Reject(Field(saved, 2, new String('A', 41)), "name length"); Reject(Field(saved, 2, " Leading"), "name whitespace");
            Reject(Field(saved, 3, "2"), "unknown sex/model"); Reject(Field(saved, 4, "Other/Hair"), "unsafe hair asset name");
            Reject(Field(saved, 5, "Other/Beard"), "unsafe beard asset name");
            Reject(Field(Field(saved, 3, "1"), 5, "Beard1"), "incompatible beard/model");
            foreach (string value in new[] { "-1", "256", "NaN", "Infinity", "0.5", "+1", " 1", "01", "" })
                Reject(Field(saved, 6, value), "invalid color: " + value);
            foreach (int index in new[] { 12, 13, 14 }) Reject(Field(saved, index, "101"), "temperament bound " + index);
            Reject(Field(saved, 15, "warrior"), "unknown initial role");
            bool threw = false;
            try { HumanProfile.Generate("bad", HairChoices, BeardChoices); } catch (ArgumentException) { threw = true; }
            Check(threw, "invalid fresh seed refused");
            threw = false;
            try { HumanProfile.Generate(Seed, new[] { "", "Hair1" }, BeardChoices); } catch (ArgumentException) { threw = true; }
            Check(threw, "blank fresh hair choice refused");
            threw = false;
            try { HumanProfile.Generate(Seed, new[] { "Hair1" }, new[] { "", "Beard1" }); } catch (ArgumentException) { threw = true; }
            Check(!threw, "blank beard remains a valid clean-shaven choice");
            var sexes = new HashSet<HumanSex>(); var names = new HashSet<string>(); var hair = new HashSet<string>(); var ids = new HashSet<string>();
            for (int i = 0; i < 512; i++)
            {
                var profile = HumanProfile.Generate(i.ToString("x32", CultureInfo.InvariantCulture), HairChoices, BeardChoices);
                Check(HumanProfile.TryRead(profile.Save(), out restored) && restored.Save() == profile.Save(), "sample roundtrip " + i);
                Check(ids.Add(profile.Id), "distinct full-seed IDs " + i);
                Check(profile.Sex != HumanSex.Female || profile.Beard == "", "female model beard compatibility " + i);
                sexes.Add(profile.Sex); names.Add(profile.Name); hair.Add(profile.Hair);
            }
            Check(sexes.Count == 2 && names.Count > 350 && hair.Count == 4, "expanded generator yields broad human variety");
            Check(!saved.Contains("Ashford") && !saved.Contains("Brook") && !saved.Contains("Hearth") && !saved.Contains("Vale"),
                "fresh identity excludes retired fantasy family names");
            foreach (string identity in names)
                Check(identity.EndsWith("son", StringComparison.Ordinal) || identity.EndsWith("dottir", StringComparison.Ordinal),
                    "fresh identity uses a normalized patronymic: " + identity);
            Console.WriteLine("HumanProfile PASS: " + checks + " assertions; pure deterministic generation/serialization only.");
            Console.WriteLine("Stable sample: " + saved);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
