using System;

namespace LivingWorld.HumanNpc
{
    internal sealed class NorsePersonalName
    {
        public readonly string Given;
        // Explicit possessive form. Never guess declension by removing the last letter.
        // Empty for feminine entries: automatic matronymics are not enabled in this revision.
        public readonly string Genitive;
        public NorsePersonalName(string given, string genitive) { Given = given; Genitive = genitive; }
    }

    // Living World content, not a framework-wide naming policy. Normalized West
    // Norse spelling (j convention); not a facsimile of manuscript orthography.
    // Sources, scope, meanings and kinship contract: ../../LivingWorldCORE/NAMING-CANON.md.
    internal static class NorseNameCatalog
    {
        public static readonly NorsePersonalName[] Masculine = Parse(
            "Þorsteinn:Þorsteins;Þórðr:Þórðar;Þorkell:Þorkels;Þorbjǫrn:Þorbjarnar;Þórir:Þóris;Þorgeirr:Þorgeirs;Helgi:Helga;Þórarinn:Þórarins;Ketill:Ketils;Bjǫrn:Bjarnar;" +
            "Þorgrímr:Þorgríms;Grímr:Gríms;Þórólfr:Þórólfs;Þorgils:Þorgils;Einarr:Einars;Eyvindr:Eyvindar;Þorvaldr:Þorvalds;Ormr:Orms;Þormóðr:Þormóðs;Oddr:Odds;" +
            "Þorleifr:Þorleifs;Úlfr:Úlfs;Brandr:Brands;Hrólfr:Hrólfs;Óláfr:Óláfs;Ásgeirr:Ásgeirs;Bárðr:Bárðar;Eyjólfr:Eyjólfs;Halldórr:Halldórs;Hallr:Halls;" +
            "Hrafn:Hrafns;Sigurðr:Sigurðar;Atli:Atla;Ásbjǫrn:Ásbjarnar;Ingjaldr:Ingjalds;Eysteinn:Eysteins;Skeggi:Skeggja;Snorri:Snorra;Arnórr:Arnórs;Hróaldr:Hróalds;" +
            "Steinn:Steins;Steinólfr:Steinólfs;Ǫnundr:Ǫnundar;Ásmundr:Ásmundar;Bjarni:Bjarna;Bǫðvarr:Bǫðvars;Illugi:Illuga;Jǫrundr:Jǫrundar;Már:Más;Þóroddr:Þórodds;" +
            "Eiríkr:Eiríks;Hildir:Hildis;Þorfinnr:Þorfinns;Þrándr:Þrándar;Ǫrn:Arnar;Auðunn:Auðunar;Egill:Egils;Guðmundr:Guðmundar;Gunnarr:Gunnars;Herjólfr:Herjólfs;" +
            "Sigmundr:Sigmundar;Þorlákr:Þorláks;Héðinn:Héðins;Ófeigr:Ófeigs;Vémundr:Vémundar;Þorbrandr:Þorbrands;Ǫzurr:Ǫzurar;Án:Áns;Kolbeinn:Kolbeins;Kollr:Kolls;" +
            "Sǫlvi:Sǫlva;Þorleikr:Þorleiks;Þorvarðr:Þorvarðar;Þróndr:Þróndar;Ǫlvir:Ǫlvis;Ǫrnólfr:Ǫrnólfs;Ari:Ara;Eilífr:Eilífs;Finnr:Finns;Geirmundr:Geirmundar;" +
            "Gizurr:Gizurar;Hallsteinn:Hallsteins;Haraldr:Haralds;Kolli:Kolla;Loftr:Lopts;Sturla:Sturlu;Þórhallr:Þórhalls;Flosi:Flosa;Gísli:Gísla;Guðlaugr:Guðlaugs;" +
            "Gunnsteinn:Gunnsteins;Hallvarðr:Hallvarðar;Hólmsteinn:Hólmsteins;Hrappr:Hrapps;Hóskuldr:Hóskulds;Óleifr:Óleifs;Refr:Refs;Auðr:Auðar;Sveinbjǫrn:Sveinbjarnar;Torfi:Torfa;" +
            "Þorbergr:Þorbergs;Arngrímr:Arngríms;Álfr:Álfs;Bergþórr:Bergþórs;Bersi:Bersa;Glúmr:Glúms;Hallbjǫrn:Hallbjarnar;Hámundr:Hámundar;Hǫgni:Hǫgna;Kári:Kára;" +
            "Óttarr:Óttars;Sighvatr:Sighvats;Steingrímr:Steingríms;Geirr:Geirs;Hákon:Hákonar;Ívarr:Ívars;Ingólfr:Ingólfs;Ragnarr:Ragnars;Sveinn:Sveins;Tryggvi:Tryggva;" +
            "Bragi:Braga;Flóki:Flóka;Haukr:Hauks;Hálfdan:Hálfdanar;Máni:Mána;Dagr:Dags;Fróði:Fróða;Grettir:Grettis");

        public static readonly NorsePersonalName[] Feminine = Parse(
            "Þuríðr;Þorgerðr;Þórdís;Helga;Þórunn;Guðrún;Þóra;Valgerðr;Yngvildr;Vigdís;" +
            "Þorbjǫrg;Jórunn;Steinunn;Þorkatla;Halldóra;Gróa;Halla;Álǫf;Ástríðr;Hallbera;" +
            "Þorlaug;Hallveig;Herdís;Rannveig;Æsa;Ingibjǫrg;Sigríðr;Þórhildr;Arnóra;Gunnhildr;" +
            "Þórarna;Ásdís;Guðríðr;Hallgerðr;Hlíf;Kolfinna;Salgerðr;Þórný;Arnbjǫrg;Arndís;" +
            "Arngerðr;Arnkatla;Arnleif;Auðr;Álfgerðr;Ásgerðr;Ásný;Ásvǫr;Bergþóra;Bjǫrg;" +
            "Dalla;Finna;Friðgerðr;Geirhildr;Geirlaug;Geirríðr;Gerðr;Guðlaug;Guðný;Hallfríðr;" +
            "Hallkatla;Hróðný;Ingunn;Ísgerðr;Járngerðr;Jófríðr;Jóra;Jóreiðr;Oddný;Otkatla;" +
            "Ragnheiðr;Ragnhildr;Rjúpa;Signý;Valdís;Vilborg;Þjóðhildr;Þórey;Þorlót;Aldís;" +
            "Arneiðr;Arnfríðr;Arngunnr;Arnþrúðr;Álfdís;Álfeiðr;Ása;Ásbjǫrg;Áshildr;Ásleif;" +
            "Ásta;Bera;Bergdís;Bergljót;Birna;Bjargey;Bót;Bótey;Bryngerðr;Dagrún;" +
            "Dís;Dýrfinna;Eirný;Eydís;Eyja;Fastný;Fjǫrleif;Geirbjǫrg;Geirný;Gríma;" +
            "Guðbjǫrg;Guðleif;Gunnvǫr;Hafþóra;Halldís;Hallvǫr;Heiðr;Heimlaug;Hervǫr;Hildigunnr;" +
            "Hrefna;Hrafnhildr;Iðunn;Ingigerðr;Ingríðr;Ingileif;Jódís;Katla;Kolgríma;Mjǫll;" +
            "Móeiðr;Mýrún;Oddbjǫrg;Oddfríðr;Ósk;Sólveig;Svana;Úlfhildr");

        static NorsePersonalName[] Parse(string rows)
        {
            string[] entries = rows.Split(';');
            var result = new NorsePersonalName[entries.Length];
            for (int i = 0; i < entries.Length; ++i) {
                string[] pair = entries[i].Split(':');
                result[i] = new NorsePersonalName(pair[0], pair.Length == 2 ? pair[1] : "");
            }
            return result;
        }
    }
}
