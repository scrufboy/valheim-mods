using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Reuses Valheim's own Aware (?) and Alerted (!) HUD children without
    // writing a fake MonsterAI target. Awareness therefore stays presentational
    // and cannot leak into native attack selection.
    internal static class HumanAwarenessHud
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo Huds = typeof(EnemyHud).GetField("m_huds", Hidden);
        static FieldInfo hudCharacter, hudAware, hudAlerted;

        internal static void InstallPatches(Harmony harmony)
        {
            var update = typeof(EnemyHud).GetMethod("UpdateHuds", Hidden);
            if (Huds == null || update == null) throw new MissingMemberException("Native awareness HUD contract changed.");
            harmony.Patch(update, postfix: new HarmonyMethod(typeof(HumanAwarenessHud).GetMethod("AfterUpdate", BindingFlags.Static | BindingFlags.NonPublic)));
        }

        static void AfterUpdate(EnemyHud __instance)
        {
            var entries = Huds.GetValue(__instance) as IEnumerable;
            if (entries == null) return;
            foreach (object entry in entries)
            {
                var value = entry.GetType().GetProperty("Value", Hidden);
                if (value == null) continue;
                object data = value.GetValue(entry, null);
                if (data == null) continue;
                if (hudCharacter == null)
                {
                    Type type = data.GetType();
                    hudCharacter = type.GetField("m_character", Hidden);
                    hudAware = type.GetField("m_aware", Hidden);
                    hudAlerted = type.GetField("m_alerted", Hidden);
                }
                if (hudCharacter == null || hudAware == null || hudAlerted == null) return;
                var character = hudCharacter.GetValue(data) as Character;
                var companion = character == null ? null : character.GetComponent<HumanCompanion>();
                if (companion == null || companion.HasAuthorizedCombat) continue;
                var aware = hudAware.GetValue(data) as RectTransform;
                var alerted = hudAlerted.GetValue(data) as RectTransform;
                // Red is reserved for real engagement, which native MonsterAI
                // renders after authorization. Observation/concealment stays ?.
                if (aware != null) aware.gameObject.SetActive(companion.AwarenessActive);
                if (alerted != null) alerted.gameObject.SetActive(false);
            }
        }
    }
}
