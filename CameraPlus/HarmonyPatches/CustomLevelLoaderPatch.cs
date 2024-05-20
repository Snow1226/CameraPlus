using HarmonyLib;
using UnityEngine;
using CameraPlus.Utilities;

namespace CameraPlus.HarmonyPatches
{

    [HarmonyPatch(typeof(CustomLevelLoader), nameof(CustomLevelLoader.Awake))]
    internal static class CustomLevelLoaderPatch
    {
        public static CustomLevelLoader Instance { get; set; } = null;
        static void Postfix(CustomLevelLoader __instance)
        {
            Instance = __instance;
#if DEBUG
            Plugin.Log.Notice($"CustomLevelLoader Loaded");
#endif
        }

    }
}
