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
            const string legacyGolden = "1|0123456789abcdef0123456789abcdef|Egan Brook|0|Hair2|Beard1|211|174|139|119|48|26|20|41|50|unassigned";
            Check(original.Id == "human-" + Seed, "stable individual ID");
            Check(original.InitialRole == "unassigned", "no class or settlement assignment");
            Check(saved.Length <= HumanProfile.MaximumRecordLength, "bounded generated record");
            Check(HumanProfile.Generate(Seed, HairChoices, BeardChoices).Save() == saved, "seeded generation is deterministic");
            HumanProfile restored;
            Check(HumanProfile.TryRead(legacyGolden, out restored) && restored.Save() == legacyGolden && restored.Name == "Egan Brook",
                "legacy golden identity spelling and seed preserved after name pool expansion");
            Check(HumanProfile.TryRead(saved, out restored) && restored.Save() == saved, "all generated values survive exact roundtrip");
            foreach (string unicodeName in new[] { "Björn Vale", "Søren Reed", "Håkon Moss", "Ásgeir Brook", "Eiríkr Flint", "Þóra Hill", "Guðrún Grove", "Björn Q\u0301" })
            {
                string unicodeRecord = Field(saved, 2, unicodeName);
                Check(HumanProfile.TryRead(unicodeRecord, out restored) && restored.Name == unicodeName && restored.Save() == unicodeRecord,
                    "canonical Unicode identity roundtrip: " + unicodeName);
            }
            Reject(Field(saved, 2, "Bjo\u0308rn Vale"), "non-NFC spelling refused without rewriting stored bytes");
            Reject(Field(saved, 2, "\u0301Björn"), "leading unattached combining mark refused");
            Reject(Field(saved, 2, "Björn\u202eVale"), "directional format injection refused");
            var appearanceEdit = original.WithAppearance("Hair4", "Beard3");
            string expectedAppearance = Field(Field(saved, 4, "Hair4"), 5, "Beard3");
            Check(appearanceEdit.Save() == expectedAppearance && original.Save() == saved && appearanceEdit.Id == original.Id,
                "appearance edit clones only customization, preserves exact name/id/traits and leaves original immutable");
            string unicodeMale = Field(Field(saved, 2, "Ásgeir Björn"), 3, "0");
            Check(HumanProfile.TryRead(unicodeMale, out restored) &&
                restored.WithAppearance("Hair2", "Beard2").Save() == Field(Field(unicodeMale, 4, "Hair2"), 5, "Beard2"),
                "appearance edit preserves exact accented canonical identity and every unrelated field");
            Check(HumanNamePresentation.Format(original) == "<b>" + original.Name + "</b>",
                "unassigned profile gets bold name without invented role");
            Check(HumanNamePresentation.Format("Björn <tag>", "Companion & Guard") ==
                "<b>Björn &lt;tag&gt;</b>\n<size=80%><color=#B8B3A8>Companion &amp; Guard</color></size>",
                "nameplate escapes name and authoritative role fragments");
            string edited = Field(saved, 2, "Mira Reed");
            Check(HumanProfile.TryRead(edited, out restored) && restored.Name == "Mira Reed" && restored.Save() == edited,
                "load uses stored fields rather than regenerating from seed");
            string savedBaldMale = Field(Field(Field(saved, 3, "0"), 4, ""), 5, "");
            Check(HumanProfile.TryRead(savedBaldMale, out restored) && restored.Save() == savedBaldMale &&
                restored.Name == original.Name && restored.Hair == "" && restored.Beard == "",
                "saved bald male identity and customization preserved without reroll");
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
            Reject(Field(saved, 2, "Björn|Extra"), "name record delimiter injection");
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
            try { original.WithAppearance("Other/Hair", "Beard1"); } catch (ArgumentException) { threw = true; }
            Check(threw, "appearance edit refuses unsafe asset path");
            string femaleRecord = Field(Field(saved, 3, "1"), 5, "");
            Check(HumanProfile.TryRead(femaleRecord, out restored), "female profile fixture remains valid");
            threw = false;
            try { restored.WithAppearance("Hair1", "Beard1"); } catch (ArgumentException) { threw = true; }
            Check(threw, "appearance edit preserves female beard compatibility");
            var sexes = new HashSet<HumanSex>(); var names = new HashSet<string>(); var hair = new HashSet<string>(); var ids = new HashSet<string>();
            for (int i = 0; i < 512; i++)
            {
                var profile = HumanProfile.Generate(i.ToString("x32", CultureInfo.InvariantCulture), HairChoices, BeardChoices);
                Check(HumanProfile.TryRead(profile.Save(), out restored) && restored.Save() == profile.Save(), "sample roundtrip " + i);
                Check(ids.Add(profile.Id), "distinct full-seed IDs " + i);
                Check(profile.Sex != HumanSex.Female || profile.Beard == "", "female model beard compatibility " + i);
                var visible = HumanProfile.Generate(i.ToString("x32", CultureInfo.InvariantCulture),
                    new[] { "", "HairNone", "Hair1", "Hair2" }, new[] { "", "BeardNone", "Beard1", "Beard2" });
                Check(visible.Sex != HumanSex.Male ||
                    (visible.Hair != "" && visible.Hair != "HairNone" && visible.Beard != "" && visible.Beard != "BeardNone"),
                    "fresh male skips empty/None hair and beard when eligible assets exist " + i);
                Check(HumanProfile.TryRead(visible.Save(), out restored) && restored.Save() == visible.Save(),
                    "visible fresh profile persists exact selected values " + i);
                sexes.Add(profile.Sex); names.Add(profile.Name); hair.Add(profile.Hair);
            }
            Check(sexes.Count == 2 && names.Count > 100 && hair.Count == 4, "small generator yields human variety");
            var emptyOnly = HumanProfile.Generate(Seed, new[] { "" }, new[] { "" });
            Check(emptyOnly.Hair == "" && emptyOnly.Beard == "", "empty-only catalogs remain safe and bounded");
            Console.WriteLine("HumanProfile PASS: " + checks + " assertions; pure deterministic generation/serialization only.");
            Console.WriteLine("Stable sample: " + saved);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
