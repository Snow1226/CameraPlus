﻿using System;
using System.Reflection;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using HarmonyLib;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;
using CameraPlus.Configuration;

namespace CameraPlus
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin instance { get; private set; }
        public static IPALogger Log { get; private set; }

        internal static string Name => "CameraPlus";
        public static string MainCamera => "cameraplus";

        private Harmony _harmony;
        internal static CameraPlusController cameraController;
        [Init]

        public void Init(Config conf, IPALogger logger)
        {
            instance = this;
            PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Log = logger;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            _harmony = new Harmony("com.brian91292.beatsaber.cameraplus");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            try
            {
                if (HarmonyPatches.FPFCToggleEnable.TargetMethod() != null)
                {
                    _harmony.PatchAll(typeof(HarmonyPatches.FPFCToggleEnable));
                    _harmony.PatchAll(typeof(HarmonyPatches.FPFCToggleDisbale));
                }else
                    Log.Warn("SiraUtil Not Detected.");
            }
            catch(Exception ex)
            {
                Log.Warn($"HarmonyPatches Error {ex}");
            }
            cameraController= new GameObject("CameraPlusController").AddComponent<CameraPlusController>();
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            if(cameraController)
                GameObject.Destroy(cameraController);
            _harmony.UnpatchSelf();
        }
    }
}
