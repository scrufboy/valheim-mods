using System.Threading.Tasks;
namespace LivingVillageCore
{
    public sealed class ResidentIdentity
    {
        public string EntityId;
        public string DisplayName;
        public bool Female;
        public bool NamingCompleted;
    }
    // Provider intentionally not selected/enabled. Persist validated results
    // on game thread; naming must never block simulation or expose credentials.
    public interface IResidentNameProvider
    {Task<string> RequestNameAsync(string entityId,bool female);}
    public enum VillageRole{Commoner,Worker,Steward,Jarl,Matron,Housecarl,Guard,Archer,Cleric,Cook,Smith,Farmer}
}
