using System;

namespace LivingWorld.HumanNpcV2
{
    internal sealed class HumanIdentityRecord
    {
        internal string Name;
        internal bool Female;
        internal string Hair;
        internal string Beard;

        static readonly string[] MaleNames = {
            "Alf", "Amund", "Anund", "Arne", "Arnfinn", "Arnketil", "Asbjorn", "Aslak", "Audun",
            "Bjorn", "Brand", "Einar", "Eirik", "Finn", "Frode", "Geir", "Gisle", "Gorm", "Grim",
            "Gunnar", "Guthorm", "Halfdan", "Hallvard", "Harald", "Hastein", "Helge", "Hrolf",
            "Ingvar", "Ivar", "Ketil", "Knut", "Leif", "Odd", "Olaf", "Orm", "Ragnar", "Ragnvald",
            "Sigfast", "Sigurd", "Skarde", "Sten", "Stig", "Sven", "Thorgeir", "Thorsten", "Toke",
            "Trygve", "Ulf", "Vestein"
        };

        static readonly string[] FemaleNames = {
            "Alfhild", "Arnfrid", "Asa", "Astrid", "Aud", "Borghild", "Bothild", "Freydis", "Geirlaug",
            "Gerlaug", "Gerd", "Gudfrid", "Gudlaug", "Gudrid", "Gunborg", "Gunhild", "Gyrid",
            "Hallfrid", "Helga", "Hild", "Holmfrid", "Inga", "Ingeborg", "Ingegerd", "Ingrid", "Jorunn",
            "Ragnfrid", "Ragnhild", "Runa", "Sigrid", "Signy", "Svanhild", "Thora", "Thorfrid",
            "Thorgunn", "Tove", "Turid", "Ulfhild", "Unn", "Vigdis", "Yrsa"
        };

        static readonly string[] Fathers = {
            "Alf", "Amund", "Anund", "Arne", "Asbjorn", "Asgot", "Aslak", "Audun", "Bjorn", "Brand",
            "Einar", "Eirik", "Finn", "Geir", "Gorm", "Grim", "Gunnar", "Guthorm", "Halfdan", "Harald",
            "Hastein", "Helge", "Ingvar", "Ivar", "Ketil", "Knut", "Leif", "Olaf", "Orm", "Ragnar",
            "Ragnvald", "Sigurd", "Sven", "Thorgeir", "Thorsten", "Ulf"
        };

        internal static HumanIdentityRecord Create(Random random, int hairCount, int beardCount)
        {
            if (random == null) throw new ArgumentNullException("random");
            if (hairCount < 1) throw new InvalidOperationException("Valheim exposed no native Hair1..N styles.");
            if (beardCount < 1) throw new InvalidOperationException("Valheim exposed no native Beard1..N styles.");
            bool female = random.Next(0, 2) == 1;
            string given = Pick(random, female ? FemaleNames : MaleNames);
            string father = Pick(random, Fathers);
            return new HumanIdentityRecord {
                Name = given + " " + father + (female ? "sdottir" : "sson"),
                Female = female,
                Hair = "Hair" + random.Next(1, hairCount + 1),
                Beard = female ? "" : "Beard" + random.Next(1, beardCount + 1)
            };
        }

        static string Pick(Random random, string[] values)
        {
            return values[random.Next(0, values.Length)];
        }
    }
}
