﻿using HarmonyLib;
using IPA.Loader;
using System;
using System.Reflection;
using UnityEngine;

namespace CameraPlus.HarmonyPatches
{
    [HarmonyPatch]
    internal class FPFCToggleEnable
    {
        //Temporarily monitor FPFCToggle of SiraUtil due to FPFC change in 1.29.4
        private static MethodBase TargetMethod()
        {
            PluginMetadata siraUtil = PluginManager.GetPluginFromId("SiraUtil");
            if (siraUtil != null)
            {
                return siraUtil.Assembly.GetType("SiraUtil.Tools.FPFC.FPFCToggle").GetMethod("EnableFPFC", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            else return null;
        }
        private static void Postfix()
        {
            Plugin.cameraController.isFPFC = true;
#if DEBUG
            Plugin.Log.Notice("SiraUtil FPFC Toggle Enable");
#endif
            Plugin.cameraController.OnFPFCToggleEvent.Invoke();
        }
    }

    [HarmonyPatch]
    internal class FPFCToggleDisbale
    {
        private static MethodBase TargetMethod()
        {
            PluginMetadata siraUtil = PluginManager.GetPluginFromId("SiraUtil");
            if (siraUtil != null)
            {
                return siraUtil.Assembly.GetType("SiraUtil.Tools.FPFC.FPFCToggle").GetMethod("DisableFPFC", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            else return null;
        }
        private static void Postfix()
        {
            Plugin.cameraController.isFPFC = false;
#if DEBUG
            Plugin.Log.Notice("SiraUtil FPFC Toggle False");
#endif
            Plugin.cameraController.OnFPFCToggleEvent.Invoke();
        }
    }
}
