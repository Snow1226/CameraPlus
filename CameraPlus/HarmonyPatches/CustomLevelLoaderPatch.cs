using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using CameraPlus.Utilities;

namespace CameraPlus.HarmonyPatches
{

    [HarmonyPatch(typeof(CustomLevelLoader), "Awake")]
    public static class CustomLevelLoaderPatch
    {
        public static CustomLevelLoader Instance { get; set; } = null;
        public static Dictionary<string, CustomLevelLoader.LoadedSaveData> LoadedData { get; set; } = null;
        static void Postfix(CustomLevelLoader __instance, Dictionary<string, CustomLevelLoader.LoadedSaveData> ____loadedBeatmapSaveData)
        {
            Instance = __instance;
            LoadedData = ____loadedBeatmapSaveData;
#if DEBUG
            Plugin.Log.Notice($"CustomLevelLoader Loaded");
#endif
        }

    }
}
