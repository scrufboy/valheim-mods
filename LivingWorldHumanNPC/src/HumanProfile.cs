using System;
using System.Globalization;

namespace LivingWorld.HumanNpc
{
    public enum HumanSex { Male = 0, Female = 1 }

    // Immutable, class/settlement-independent identity. Temperament is descriptive
    // initial data (0..100), not a damage, movement, bravery-AI or stamina modifier.
    public sealed class HumanProfile
    {
        public const string RecordKey = "livingworld_human_identity";
        public const string RequiredKey = "livingworld_human_identity_required";
        public const int MaximumRecordLength = 256;
        public string Seed { get; private set; }
        public string Id { get { return "human-" + Seed; } }
        public string Name { get; private set; }
        public HumanSex Sex { get; private set; }
        public string Hair { get; private set; }
        public string Beard { get; private set; }
        public byte SkinR { get; private set; }
        public byte SkinG { get; private set; }
        public byte SkinB { get; private set; }
        public byte HairR { get; private set; }
        public byte HairG { get; private set; }
        public byte HairB { get; private set; }
        public byte InitialCourage { get; private set; }
        public byte InitialPatience { get; private set; }
        public byte InitialSociability { get; private set; }
        public string InitialRole { get; private set; }

        // Normalized ASCII forms of Viking-age and Old Norse names. Runtime
        // identity strings stay compatible with Valheim's existing text path,
        // while avoiding the prior modern-fantasy given names and decorative
        // nature surnames. Patronymics provide believable variety without
        // inventing hereditary fantasy houses.
        static readonly string[] MaleNames = {
            "Alf", "Amund", "Anund", "Arne", "Arnfinn", "Arnketil", "Asbjorn", "Asgot", "Aslak", "Audun",
            "Bjorn", "Bove", "Brand", "Einar", "Eirik", "Finn", "Frode", "Geir", "Gisle", "Gorm", "Grim",
            "Gunnar", "Gunne", "Guthorm", "Halfdan", "Hallvard", "Harald", "Hastein", "Helge", "Holmger",
            "Hrolf", "Ingvar", "Ivar", "Ketil", "Knut", "Leif", "Odd", "Olaf", "Orm", "Ragnar", "Ragnvald",
            "Sigfast", "Sigurd", "Skarde", "Sten", "Stig", "Sven", "Thorgeir", "Thorgny", "Thorger",
            "Thorsten", "Toke", "Trygve", "Ulf", "Vestein"
        };
        static readonly string[] FemaleNames = {
            "Alfhild", "Arnfrid", "Asa", "Astrid", "Aud", "Borghild", "Bothild", "Freydis", "Geirlaug",
            "Gerlaug", "Gerd", "Gudfrid", "Gudlaug", "Gudrid", "Gunborg", "Gunhild", "Gulla", "Gyrid",
            "Hallfrid", "Helga", "Hild", "Holmfrid", "Holmvi", "Inga", "Ingeborg", "Ingegerd", "Ingethora",
            "Ingrid", "Jorunn", "Ketilvi", "Ragnfrid", "Ragnhild", "Runa", "Sigrid", "Signy", "Svanhild",
            "Thora", "Thorfrid", "Thorgunn", "Tove", "Turid", "Ulfhild", "Unn", "Vigdis", "Yrsa"
        };
        static readonly string[] MalePatronymics = {
            "Alfsson", "Amundsson", "Anundsson", "Arnason", "Asbjornsson", "Asgotsson", "Aslaksson", "Audunsson",
            "Bjornsson", "Brandsson", "Einarsson", "Eriksson", "Finnsson", "Geirsson", "Gormsson", "Grimsson",
            "Gunnarsson", "Guthormsson", "Halfdansson", "Haraldsson", "Hasteinsson", "Helgason", "Ingvarsson",
            "Ivarsson", "Ketilsson", "Knutsson", "Leifsson", "Olafsson", "Ormsson", "Ragnarsson",
            "Ragnvaldsson", "Sigurdsson", "Svensson", "Thorgeirsson", "Thorstensson", "Ulfsson"
        };
        static readonly string[] FemalePatronymics = {
            "Alfsdottir", "Amundsdottir", "Anundsdottir", "Arnadottir", "Asbjornsdottir", "Asgotsdottir",
            "Aslaksdottir", "Audunsdottir", "Bjornsdottir", "Brandsdottir", "Einarsdottir", "Eriksdottir",
            "Finnsdottir", "Geirsdottir", "Gormsdottir", "Grimsdottir", "Gunnarsdottir", "Guthormsdottir",
            "Halfdansdottir", "Haraldsdottir", "Hasteinsdottir", "Helgadottir", "Ingvarsdottir", "Ivarsdottir",
            "Ketilsdottir", "Knutsdottir", "Leifsdottir", "Olafsdottir", "Ormsdottir", "Ragnarsdottir",
            "Ragnvaldsdottir", "Sigurdsdottir", "Svensdottir", "Thorgeirsdottir", "Thorstensdottir", "Ulfsdottir"
        };
        static readonly byte[][] SkinColors = {
            new byte[] { 235, 205, 181 }, new byte[] { 211, 174, 139 }, new byte[] { 177, 133, 96 },
            new byte[] { 143, 99, 65 }, new byte[] { 104, 72, 50 }, new byte[] { 78, 52, 37 }
        };
        static readonly byte[][] HairColors = {
            new byte[] { 29, 22, 18 }, new byte[] { 64, 40, 24 }, new byte[] { 111, 71, 35 },
            new byte[] { 177, 138, 66 }, new byte[] { 119, 48, 26 }, new byte[] { 155, 151, 141 }
        };

        HumanProfile() { }

        // Only the explicit fresh-spawn adapter calls generation. The seed is 128
        // bits of caller-provided identity entropy, and is stored verbatim. Neither
        // System.Random nor the Unity global random state participates.
        public static HumanProfile Generate(string seed, string[] availableHair, string[] availableBeards)
        {
            if (!ValidSeed(seed)) throw new ArgumentException("A lowercase 32-digit hexadecimal identity seed is required.", "seed");
            if (availableHair == null || availableHair.Length == 0) throw new ArgumentException("At least one runtime-verified native hair choice is required.", "availableHair");
            if (availableBeards == null || availableBeards.Length == 0) throw new ArgumentException("At least one runtime-verified native beard choice is required.", "availableBeards");
            // A blank beard is a valid clean-shaven presentation. A blank hair
            // item is not part of the companion appearance contract: it made
            // freshly generated people appear bald even though the live game
            // exposed a complete native hair catalog.
            foreach (string value in availableHair) if (String.IsNullOrEmpty(value) || !ValidAppearanceName(value)) throw new ArgumentException("Fresh humans require a concrete runtime hair choice.", "availableHair");
            foreach (string value in availableBeards) if (!ValidAppearanceName(value)) throw new ArgumentException("Invalid runtime beard choice.", "availableBeards");
            var random = new Sequence(seed);
            var sex = (HumanSex)random.Take(2);
            var names = sex == HumanSex.Male ? MaleNames : FemaleNames;
            var patronymics = sex == HumanSex.Male ? MalePatronymics : FemalePatronymics;
            string name = names[random.Take(names.Length)] + " " + patronymics[random.Take(patronymics.Length)];
            string hair = availableHair[random.Take(availableHair.Length)];
            string beard = sex == HumanSex.Male ? availableBeards[random.Take(availableBeards.Length)] : "";
            var skin = SkinColors[random.Take(SkinColors.Length)];
            var tint = HairColors[random.Take(HairColors.Length)];
            return new HumanProfile {
                Seed = seed, Name = name, Sex = sex, Hair = hair, Beard = beard,
                SkinR = skin[0], SkinG = skin[1], SkinB = skin[2], HairR = tint[0], HairG = tint[1], HairB = tint[2],
                InitialCourage = (byte)random.Take(101), InitialPatience = (byte)random.Take(101), InitialSociability = (byte)random.Take(101),
                InitialRole = "unassigned"
            };
        }

        public string Save()
        {
            // Store the results, not just a seed: loading never depends on a later
            // name list, palette, PRNG implementation or generation revision.
            return String.Join("|", new[] { "1", Seed, Name, ((int)Sex).ToString(CultureInfo.InvariantCulture), Hair, Beard,
                Number(SkinR), Number(SkinG), Number(SkinB), Number(HairR), Number(HairG), Number(HairB),
                Number(InitialCourage), Number(InitialPatience), Number(InitialSociability), InitialRole });
        }

        public static bool TryRead(string record, out HumanProfile value)
        {
            value = null;
            if (String.IsNullOrEmpty(record) || record.Length > MaximumRecordLength) return false;
            var fields = record.Split('|');
            if (fields.Length != 16 || fields[0] != "1" || fields[15] != "unassigned" || !ValidSeed(fields[1]) || !ValidName(fields[2]) ||
                (fields[3] != "0" && fields[3] != "1") || !ValidAppearanceName(fields[4]) || !ValidAppearanceName(fields[5]) ||
                (fields[3] == "1" && fields[5] != "")) return false;
            var numbers = new byte[9];
            for (int i = 0; i < numbers.Length; ++i)
                if (!Byte.TryParse(fields[6 + i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]) ||
                    Number(numbers[i]) != fields[6 + i] || (i >= 6 && numbers[i] > 100)) return false;
            value = new HumanProfile {
                Seed = fields[1], Name = fields[2], Sex = fields[3] == "0" ? HumanSex.Male : HumanSex.Female,
                Hair = fields[4], Beard = fields[5], SkinR = numbers[0], SkinG = numbers[1], SkinB = numbers[2],
                HairR = numbers[3], HairG = numbers[4], HairB = numbers[5],
                InitialCourage = numbers[6], InitialPatience = numbers[7], InitialSociability = numbers[8], InitialRole = fields[15]
            };
            return true;
        }

        static bool ValidSeed(string seed)
        {
            if (seed == null || seed.Length != 32) return false;
            foreach (char c in seed) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) return false;
            return true;
        }

        static bool ValidName(string name)
        {
            if (String.IsNullOrEmpty(name) || name.Length > 40 || name[0] == ' ' || name[name.Length - 1] == ' ') return false;
            foreach (char c in name) if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && c != ' ' && c != '-' && c != '\'') return false;
            return true;
        }

        static bool ValidAppearanceName(string value)
        {
            if (value == null || value.Length > 64) return false;
            foreach (char c in value)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '_' && c != '-') return false;
            return true;
        }
        static string Number(byte value) { return value.ToString(CultureInfo.InvariantCulture); }

        sealed class Sequence
        {
            uint state;
            internal Sequence(string seed)
            {
                state = 2166136261;
                foreach (char c in seed) state = unchecked((state ^ c) * 16777619);
                if (state == 0) state = 0x9e3779b9;
            }
            internal int Take(int count)
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                return (int)(state % (uint)count);
            }
        }
    }
}
