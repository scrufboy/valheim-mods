using System;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.Studio
{
    // Native local-player environment zone. No forced environment, debug time,
    // global shader setting, original EnvSetup mutation or companion state.
    public sealed class StudioEnvironment : IDisposable
    {
        const string EnvironmentName = "LWC_Studio_Neutral_v1";
        static EnvMan registeredManager;
        static EnvSetup registeredEnvironment;
        GameObject zoneObject;
        EnvZone zone;

        StudioEnvironment() { }

        // center is the geometric center of the 32 x 12 x 32 metre studio volume,
        // not its floor origin. The room remains responsible for its solid shell.
        public static IDisposable Attach(Vector3 center)
        {
            if (!Finite(center.x) || !Finite(center.y) || !Finite(center.z))
                throw new ArgumentException("Studio environment center must be finite.");
            var manager = EnvMan.instance;
            if (manager == null) throw new InvalidOperationException("Native environment manager is unavailable.");
            EnsureEnvironment(manager);
            var scope = new StudioEnvironment();
            try
            {
                scope.zoneObject = new GameObject("LWC_StudioEnvironmentVolume");
                scope.zoneObject.SetActive(false);
                scope.zoneObject.transform.position = center;
                var bounds = scope.zoneObject.AddComponent<BoxCollider>();
                bounds.isTrigger = true;
                bounds.size = new Vector3(32f, 12f, 32f);
                scope.zone = scope.zoneObject.AddComponent<EnvZone>();
                scope.zone.m_environment = EnvironmentName;
                scope.zone.m_force = false;
                scope.zone.m_exteriorMesh = null;
                scope.zoneObject.SetActive(true);
                return scope;
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        static void EnsureEnvironment(EnvMan manager)
        {
            if (registeredManager == manager && registeredEnvironment != null &&
                manager.m_environments.Contains(registeredEnvironment)) return;
            foreach (var existing in manager.m_environments)
                if (existing != null && existing.m_name == EnvironmentName)
                    throw new InvalidOperationException("Studio environment name is already registered by another scope.");
            var source = manager.GetCurrentEnvironment();
            if (source == null) throw new InvalidOperationException("Native source environment is not ready.");
            var copy = new EnvSetup();
            // Public native configuration only. Referenced resources are read-only;
            // mutable arrays used by this scope are replaced below, never edited.
            foreach (var field in typeof(EnvSetup).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (!field.IsInitOnly) field.SetValue(copy, field.GetValue(source));
            copy.m_name = EnvironmentName;
            copy.m_default = false;
            copy.m_isWet = false;
            copy.m_isFreezing = false;
            copy.m_isFreezingAtNight = false;
            copy.m_isCold = false;
            copy.m_isColdAtNight = false;
            copy.m_alwaysDark = false;
            copy.m_snowBuildup = 0f;
            copy.m_ambColorNight = copy.m_ambColorDay = new Color(.5f, .5f, .5f, 1f);
            copy.m_fogColorNight = copy.m_fogColorMorning = copy.m_fogColorDay = copy.m_fogColorEvening = Color.white;
            copy.m_fogColorSunNight = copy.m_fogColorSunMorning = copy.m_fogColorSunDay = copy.m_fogColorSunEvening = Color.white;
            copy.m_fogDensityNight = copy.m_fogDensityMorning = copy.m_fogDensityDay = copy.m_fogDensityEvening = 0f;
            copy.m_sunColorNight = copy.m_sunColorMorning = copy.m_sunColorDay = copy.m_sunColorEvening = Color.white;
            copy.m_lightIntensityDay = copy.m_lightIntensityNight = 0f;
            copy.m_windMin = copy.m_windMax = 0f;
            copy.m_auroraIntensityNight = copy.m_auroraIntensityMorning = copy.m_auroraIntensityDay = copy.m_auroraIntensityEvening = 0f;
            copy.m_cloudOpacityNight = copy.m_cloudOpacityMorning = copy.m_cloudOpacityDay = copy.m_cloudOpacityEvening = 0f;
            copy.m_envObject = null;
            copy.m_psystems = new GameObject[0];
            copy.m_psystemsOutsideOnly = true;
            copy.m_rainCloudAlpha = 0f;
            copy.m_ambientOcclusionColor = Color.white;
            copy.m_aoIntensityNight = copy.m_aoIntensityMorning = copy.m_aoIntensityDay = copy.m_aoIntensityEvening = 0f;
            copy.m_ambientLoop = null;
            copy.m_ambientVol = 0f;
            copy.m_ambientList = "";
            copy.m_musicMorning = copy.m_musicEvening = copy.m_musicDay = copy.m_musicNight = "";
            manager.AppendEnvironment(copy);
            registeredManager = manager;
            registeredEnvironment = copy;
        }

        public void Dispose()
        {
            if (zoneObject == null) return;
            try
            {
                // Native OnTriggerExit guards that s_triggered is this zone and
                // collider's Player is local. It restores normal selection only
                // when we own the zone; m_force=false never clears forced state.
                var player = Player.m_localPlayer;
                var collider = player == null ? null : player.GetComponent<Collider>();
                if (zone != null && collider != null)
                    zone.SendMessage("OnTriggerExit", collider, SendMessageOptions.DontRequireReceiver);
            }
            finally
            {
                zoneObject.SetActive(false);
                UnityEngine.Object.Destroy(zoneObject);
                zoneObject = null;
                zone = null;
            }
            // Keep only our neutral registration for this EnvMan session. Removing
            // environment entries during native transitions can invalidate lookups.
            // A destroyed EnvZone also compares Unity-null in GetEnvironment.
        }

        static bool Finite(float value) { return !Single.IsNaN(value) && !Single.IsInfinity(value); }
    }
}
