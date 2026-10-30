using UnityEngine;

namespace LivingWorld.Core
{
    public enum IntentReader { FormidableActor, NativeMonsterAI, NativeBaseAI, VisibleOnly }

    /// <summary>
    /// Brian 2026-10-30: aggro is aggro. Awareness reads intent the same way whoever generates it - vanilla MonsterAI,
    /// another mod's character, or one of our own FormidableAI actors (companions now; hostile and friendly camp NPCs
    /// later). Each reader reports the actor's target and alert; anything without a reader is judged only by what is
    /// visible (facing, charging, attacking). A reported target still counts as evidence only when it is perceptible
    /// (ContactBook rule), so no reader grants hidden knowledge. Read-only.
    /// </summary>
    public static class ActorIntent
    {
        public static IntentReader Read(Character c, out Character target, out bool alerted)
        {
            target = null; alerted = false;
            NpcKernel k = c.GetComponent<NpcKernel>();
            if (k != null && k.Frame != null)
            {
                target = k.Frame.Senses.Target;
                NpcPhase p = k.Frame.Phase;
                alerted = p == NpcPhase.Readiness || p == NpcPhase.Combat || p == NpcPhase.CombatClear || p == NpcPhase.CombatRegroup;
                return IntentReader.FormidableActor;
            }
            BaseAI ai = c.GetBaseAI();
            MonsterAI monster = ai as MonsterAI;
            if (monster != null) { target = monster.GetTargetCreature(); alerted = monster.IsAlerted(); return IntentReader.NativeMonsterAI; }
            if (ai != null) { alerted = ai.IsAlerted(); return IntentReader.NativeBaseAI; }
            return IntentReader.VisibleOnly;
        }
    }
}
