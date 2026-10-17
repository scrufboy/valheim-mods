using System;
namespace LivingVillageCore
{
    public sealed class WorkBudget
    {
        public const float Maximum=100f;
        public float Current;
        public WorkBudget(float current){Current=Single.IsNaN(current)||Single.IsInfinity(current)?Maximum:Math.Max(0,Math.Min(Maximum,current));}
        public bool Spend(float amount){if(Single.IsNaN(amount)||Single.IsInfinity(amount)||amount<0 || Current<amount)return false;Current-=amount;return true;}
        public void Rest(float seconds){if(!Single.IsNaN(seconds)&&!Single.IsInfinity(seconds)&&seconds>0)Current=Math.Min(Maximum,Current+seconds*8f);}
    }
    public enum TaskKind{AreaRepair,Supply,Production,Farming,Patrol,Rest,Social}
    public enum WorkState{Idle,Travelling,Working,Resting,Interrupted}
}
