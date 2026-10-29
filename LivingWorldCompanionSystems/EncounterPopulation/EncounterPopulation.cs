using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using LivingWorld.CompanionSystems.EncounterLoadouts;

namespace LivingWorld.CompanionSystems.EncounterPopulation
{
    public enum PeopleFaction { Hearthless, Pillager, Eitrherjar }
    public enum WeaponFamily { Axe, Sword, Spear, Knife, Atgeir, Bow }
    [Flags] public enum SpecialMove { None=0, SpearThrow=1, BombThrow=2, KnifeLunge=4, AtgeirSweep=8 }
    public enum SpearStatus { Held, Airborne, Lost }
    public enum CombatChoice { None, UsePrimary, UseFallback, RequestBombThrow, RequestSpearThrow, RequestSpearRecovery, RequestKnifeLunge, RequestAtgeirSweep }

    public sealed class Archetype
    {
        public string Id, ArmorLook, Silhouette;
        public BiomeTier Biome;
        public PeopleFaction Faction;
        public WeaponFamily Primary, Fallback;
        public SpecialMove Specials;
        public int Weight, ThreatScore, BombReserve;
        public Archetype Copy() { return (Archetype)MemberwiseClone(); }
    }

    public sealed class PopulationRequest
    {
        public string WorldId, SiteId, EncounterId;
        public int WorldSeed, Count;
        public BiomeTier Biome;
        public PeopleFaction Faction;
        public bool ServerAuthority, SiteClaimPersisted;
    }

    public sealed class SpawnMember
    {
        public string PersonId, ArchetypeId;
        public Archetype Kit;
        public SpawnMember Copy() { return new SpawnMember { PersonId=PersonId,ArchetypeId=ArchetypeId,
            Kit=Kit==null?null:Kit.Copy() }; }
    }

    public sealed class PopulationPlan
    {
        public const int CurrentCatalogVersion = 1;
        public int CatalogVersion = CurrentCatalogVersion;
        public string WorldId, SiteId, EncounterId;
        public BiomeTier Biome;
        public PeopleFaction Faction;
        public readonly List<SpawnMember> Members = new List<SpawnMember>();
        public PopulationPlan Copy()
        { var p=new PopulationPlan { CatalogVersion=CatalogVersion,WorldId=WorldId,SiteId=SiteId,
            EncounterId=EncounterId,Biome=Biome,Faction=Faction };
          foreach(var m in Members) p.Members.Add(m.Copy()); return p; }
    }

    public sealed class CombatFacts
    {
        public bool ServerAuthority, ActorOwner, CombatOwnsAction, TargetAuthorized;
        public bool PrimaryEquipped, FallbackEquipped;
        // Proof is per native move; approving one never enables another.
        public bool BombLiveVerified, SpearThrowLiveVerified, SpearRecoveryLiveVerified;
        public bool KnifeLungeLiveVerified, AtgeirSweepLiveVerified;
        public bool BombRequestAuthorized, SpearRequestAuthorized, KnifeRequestAuthorized, AtgeirRequestAuthorized;
        public bool SpearRecoverySafe, SpearProjectileOwned;
        public string SpearProjectileId;
        public int BombsAvailable;
        public SpearStatus Spear;
    }

    public static class EncounterPopulationPolicy
    {
        static readonly IList<Archetype> Catalog = BuildCatalog();

        public static string Validate(Archetype a)
        {
            if(a==null || String.IsNullOrWhiteSpace(a.Id) || String.IsNullOrWhiteSpace(a.ArmorLook) ||
               String.IsNullOrWhiteSpace(a.Silhouette) || a.Weight<1 || a.ThreatScore<1 ||
               a.ThreatScore>4+(int)a.Biome*2 || a.BombReserve<0 || a.BombReserve>4 ||
               a.Biome<BiomeTier.Meadows || a.Biome>BiomeTier.Ashlands ||
               !Enum.IsDefined(typeof(PeopleFaction),a.Faction) || !Enum.IsDefined(typeof(WeaponFamily),a.Primary) ||
               !Enum.IsDefined(typeof(WeaponFamily),a.Fallback)) return "invalid_archetype";
            if(a.Primary==a.Fallback ||
               !(a.Fallback==WeaponFamily.Axe || a.Fallback==WeaponFamily.Sword || a.Fallback==WeaponFamily.Knife) ||
               ((int)a.Specials & ~15)!=0 ||
               (a.BombReserve>0 && a.Biome<BiomeTier.Swamp) ||
               (a.Specials&SpecialMove.SpearThrow)!=0 && a.Primary!=WeaponFamily.Spear ||
               (a.Specials&SpecialMove.KnifeLunge)!=0 && a.Primary!=WeaponFamily.Knife ||
               (a.Specials&SpecialMove.AtgeirSweep)!=0 && a.Primary!=WeaponFamily.Atgeir ||
               ((a.Specials&SpecialMove.BombThrow)!=0)!=(a.BombReserve>0)) return "invalid_weapon_package";
            return null;
        }

        public static IList<Archetype> Available(BiomeTier biome, PeopleFaction faction)
        {
            var result=new List<Archetype>();
            foreach(var a in Catalog) if(a.Biome==biome && a.Faction==faction) result.Add(a.Copy());
            return result;
        }

        public static PopulationPlan Select(PopulationRequest request, PopulationPlan committed)
        {
            if(request==null || !request.ServerAuthority || !request.SiteClaimPersisted ||
               !Id(request.WorldId) || !Id(request.SiteId) || !Id(request.EncounterId) ||
               request.Count<1 || request.Count>4 ||
               !Enum.IsDefined(typeof(PeopleFaction),request.Faction)) return null;
            if(committed!=null)
                return committed.CatalogVersion==PopulationPlan.CurrentCatalogVersion &&
                    committed.WorldId==request.WorldId && committed.SiteId==request.SiteId &&
                    committed.EncounterId==request.EncounterId && committed.Biome==request.Biome &&
                    committed.Faction==request.Faction && ValidCommitted(request,committed) ? committed.Copy() : null;
            IList<Archetype> candidates=Available(request.Biome,request.Faction);
            if(candidates.Count==0) return null;
            var plan=new PopulationPlan { WorldId=request.WorldId,SiteId=request.SiteId,
                EncounterId=request.EncounterId,Biome=request.Biome,Faction=request.Faction };
            uint roll=Hash32(request.WorldId+"|"+request.SiteId+"|"+request.EncounterId+"|"+request.WorldSeed);
            for(int slot=0;slot<request.Count;slot++) {
                // Avoid a duplicate role until every available role has appeared once.
                var pool=new List<Archetype>();
                foreach(var a in candidates) {
                    bool used=false; foreach(var member in plan.Members) if(member.ArchetypeId==a.Id) used=true;
                    if(!used) pool.Add(a);
                }
                if(pool.Count==0) pool.AddRange(candidates);
                roll=Next(roll);
                Archetype chosen=Weighted(pool,roll);
                plan.Members.Add(new SpawnMember { PersonId=PersonId(request,slot),ArchetypeId=chosen.Id,Kit=chosen });
            }
            return plan;
        }

        static IList<Archetype> BuildCatalog()
        {
            var result=new List<Archetype>();
            string[] armorLooks = { "meadow-hide-and-cloth", "forest-bronze-and-hide",
                "swamp-iron-and-oilcloth", "mountain-wolf-fur-and-mail",
                "plains-padded-road-mail", "mistland-carapace-and-cloak", "ashland-ash-warded-mail" };
            string[] regionalSilhouettes = { "meadow-road", "forest-road", "marsh-road",
                "high-pass", "open-plain", "mist-track", "ashen-road" };
            for(int tier=0;tier<=(int)BiomeTier.Ashlands;tier++)
                foreach(PeopleFaction faction in Enum.GetValues(typeof(PeopleFaction))) {
                    BiomeTier biome=(BiomeTier)tier;
                    string prefix=biome.ToString().ToLowerInvariant()+"."+faction.ToString().ToLowerInvariant()+".";
                    string look=armorLooks[tier];
                    string silhouette=regionalSilhouettes[tier]+(faction==PeopleFaction.Eitrherjar?
                        "-armored-warrior":"-armed-survivor");
                    Add(result,prefix+"spear-warden",biome,faction,WeaponFamily.Spear,WeaponFamily.Axe,
                        SpecialMove.SpearThrow,3,look,silhouette,0);
                    Add(result,prefix+"knife-hunter",biome,faction,WeaponFamily.Knife,WeaponFamily.Axe,
                        SpecialMove.KnifeLunge,2,look,silhouette,0);
                    Add(result,prefix+"atgeir-guard",biome,faction,WeaponFamily.Atgeir,WeaponFamily.Sword,
                        SpecialMove.AtgeirSweep,2,look,silhouette,0);
                    Add(result,prefix+"road-archer",biome,faction,WeaponFamily.Bow,WeaponFamily.Axe,
                        SpecialMove.None,2,look,silhouette,0);
                    if(tier>=(int)BiomeTier.Swamp)
                        Add(result,prefix+"spear-bombardier",biome,faction,WeaponFamily.Spear,WeaponFamily.Axe,
                            SpecialMove.SpearThrow|SpecialMove.BombThrow,1,look,silhouette,2);
                }
            foreach(var a in result) if(Validate(a)!=null) throw new InvalidOperationException("Invalid encounter catalog: "+a.Id);
            return result;
        }
        static void Add(IList<Archetype> result,string id,BiomeTier biome,PeopleFaction faction,
            WeaponFamily primary,WeaponFamily fallback,SpecialMove moves,int weight,string armor,string silhouette,int bombs)
        { result.Add(new Archetype { Id=id,Biome=biome,Faction=faction,Primary=primary,Fallback=fallback,
            Specials=moves,Weight=weight,ThreatScore=2+(int)biome*2,ArmorLook=armor,Silhouette=silhouette,BombReserve=bombs }); }
        static Archetype Weighted(IList<Archetype> pool,uint roll)
        { int total=0;foreach(var a in pool)total+=a.Weight;int n=(int)(roll%(uint)total);
          foreach(var a in pool){n-=a.Weight;if(n<0)return a;}return pool[pool.Count-1]; }
        static uint Hash32(string text)
        { unchecked { uint h=2166136261;foreach(char c in text){h^=c;h*=16777619;}return h==0?1:h; } }
        static uint Next(uint x) { x^=x<<13;x^=x>>17;x^=x<<5;return x==0?1:x; }
        static bool ValidCommitted(PopulationRequest request,PopulationPlan plan)
        {
            if(plan.Members.Count!=request.Count) return false;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            IList<Archetype> available=Available(request.Biome,request.Faction);
            for(int i=0;i<plan.Members.Count;i++) {
                SpawnMember member=plan.Members[i];
                if(member==null || member.Kit==null || member.PersonId!=PersonId(request,i) ||
                   !seen.Add(member.PersonId) || member.Kit.Id!=member.ArchetypeId) return false;
                bool matched=false;
                foreach(Archetype canonical in available) if(canonical.Id==member.ArchetypeId &&
                    SameKit(canonical,member.Kit)) { matched=true;break; }
                if(!matched) return false;
            }
            return true;
        }
        static bool SameKit(Archetype a,Archetype b)
        { return a.Id==b.Id && a.Biome==b.Biome && a.Faction==b.Faction &&
            a.Primary==b.Primary && a.Fallback==b.Fallback && a.Specials==b.Specials &&
            a.Weight==b.Weight && a.ThreatScore==b.ThreatScore && a.BombReserve==b.BombReserve &&
            a.ArmorLook==b.ArmorLook && a.Silhouette==b.Silhouette; }
        static bool Id(string value)
        { if(String.IsNullOrEmpty(value)||value.Length>96)return false;
          foreach(char c in value) if(!(Char.IsLetterOrDigit(c)||c=='.'||c=='-'||c=='_'||c==':'))return false;
          return true; }
        static string PersonId(PopulationRequest r,int slot)
        { using(var sha=SHA256.Create()) { byte[] hash=sha.ComputeHash(Encoding.UTF8.GetBytes(
              r.WorldId+"|"+r.SiteId+"|"+r.EncounterId+"|"+slot));
            return "person."+BitConverter.ToString(hash,0,16).Replace("-","").ToLowerInvariant(); } }
    }
}
