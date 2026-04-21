using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CameraPlus.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelSelectionNavigationController), "HandleLevelCollectionNavigationControllerDidPressActionButton")]
    internal class LevelDataPatch
	{
        public static bool is360Level = false;

        static void Prefix(LevelSelectionNavigationController __instance)
        {
			is360Level = __instance.beatmapKey.beatmapCharacteristic.containsRotationEvents;
        }
    }
}
