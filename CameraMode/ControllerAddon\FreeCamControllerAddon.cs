using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace LivingWorld.FreeCamController
{
    [BepInPlugin("livingworld.freecam.controller", "Living World FreeCam Controller", "1.0.0")]
    [BepInDependency("com.valheimtools.freecam", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class FreeCamControllerAddon : BaseUnityPlugin
    {
        private const KeyCode A = KeyCode.JoystickButton0;
        private const KeyCode B = KeyCode.JoystickButton1;
        private const KeyCode X = KeyCode.JoystickButton2;
        private const KeyCode Y = KeyCode.JoystickButton3;
        private const KeyCode LB = KeyCode.JoystickButton4;
        private const KeyCode RB = KeyCode.JoystickButton5;
        private const KeyCode Menu = KeyCode.JoystickButton7;
        private const KeyCode LeftStick = KeyCode.JoystickButton8;
        private const KeyCode RightStick = KeyCode.JoystickButton9;

        private Type controllerType;
        private Type hudType;
        private Type pluginType;
        private Type gameCameraType;
        private PropertyInfo activeProperty;
        private MethodInfo toggleMethod;
        private MethodInfo hudToggleMethod;
        private MethodInfo screenshotMethod;
        private PropertyInfo instanceProperty;
        private FieldInfo instanceField;
        private FieldInfo speedField;
        private FieldInfo rollField;
        private FieldInfo showHelpField;
        private bool sticksWereHeld;
        private readonly float[] speeds = { 2f, 5f, 10f, 20f, 40f };

        private void Awake()
        {
            ResolveTypes();
            Logger.LogInfo("Controller support loaded: L3+R3 camera, Menu+A HUD, Menu+B help, Menu+X capture, Menu+Y speed, Menu+LB/RB tilt.");
        }

        private void ResolveTypes()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "FreeCam")
                {
                    controllerType = assembly.GetType("FreeCam.FreeCamController");
                    hudType = assembly.GetType("FreeCam.HudToggle");
                    pluginType = assembly.GetType("FreeCam.FreeCamPlugin");
                }
                else if (assembly.GetName().Name == "assembly_valheim")
                {
                    gameCameraType = assembly.GetType("GameCamera");
                }
            }

            if (controllerType != null)
            {
                toggleMethod = controllerType.GetMethod("Toggle", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                rollField = controllerType.GetField("_roll", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (hudType != null)
                hudToggleMethod = hudType.GetMethod("Toggle", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (pluginType != null)
            {
                activeProperty = pluginType.GetProperty("ModFreeCamActive", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                showHelpField = pluginType.GetField("ShowHelpUi", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (gameCameraType != null)
            {
                instanceProperty = gameCameraType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                instanceField = gameCameraType.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                screenshotMethod = gameCameraType.GetMethod("ScreenShot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                speedField = gameCameraType.GetField("m_freeFlySpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
        }

        private bool IsActive()
        {
            return activeProperty != null && (bool)activeProperty.GetValue(null, null);
        }

        private object CameraInstance()
        {
            if (instanceProperty != null) return instanceProperty.GetValue(null, null);
            return instanceField == null ? null : instanceField.GetValue(null);
        }

        private void Update()
        {
            if (toggleMethod == null)
                ResolveTypes();

            bool sticksHeld = Input.GetKey(LeftStick) && Input.GetKey(RightStick);
            if (sticksHeld && !sticksWereHeld && toggleMethod != null)
                toggleMethod.Invoke(null, null);
            sticksWereHeld = sticksHeld;

            if (!IsActive() || !Input.GetKey(Menu))
                return;

            if (Input.GetKeyDown(A) && hudToggleMethod != null)
                hudToggleMethod.Invoke(null, null);
            if (Input.GetKeyDown(B))
                ToggleHelp();
            if (Input.GetKeyDown(X))
                Capture();
            if (Input.GetKeyDown(Y))
                CycleSpeed();
            if (Input.GetKeyDown(LB) && Input.GetKey(RB))
                SetRoll(0f);
            else if (Input.GetKeyDown(LB))
                ChangeRoll(-5f);
            else if (Input.GetKeyDown(RB))
                ChangeRoll(5f);
        }

        private void Capture()
        {
            object camera = CameraInstance();
            if (camera != null && screenshotMethod != null)
            {
                screenshotMethod.Invoke(camera, null);
                Logger.LogInfo("Controller screenshot captured.");
            }
        }

        private void CycleSpeed()
        {
            object camera = CameraInstance();
            if (camera == null || speedField == null) return;
            float current = (float)speedField.GetValue(camera);
            float next = speeds[0];
            for (int i = 0; i < speeds.Length; i++)
            {
                if (current < speeds[i] - 0.01f) { next = speeds[i]; break; }
                if (i == speeds.Length - 1) next = speeds[0];
            }
            speedField.SetValue(camera, next);
            Logger.LogInfo("FreeCam controller speed: " + next);
        }

        private void ChangeRoll(float amount)
        {
            if (rollField == null) return;
            SetRoll((float)rollField.GetValue(null) + amount);
        }

        private void SetRoll(float value)
        {
            if (rollField != null) rollField.SetValue(null, value);
        }

        private void ToggleHelp()
        {
            if (showHelpField == null) return;
            object entry = showHelpField.GetValue(null);
            if (entry == null) return;
            PropertyInfo value = entry.GetType().GetProperty("Value");
            if (value != null) value.SetValue(entry, !(bool)value.GetValue(entry, null), null);
        }
    }
}
