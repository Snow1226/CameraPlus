using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using IPA.Utilities;
using CameraPlus.HarmonyPatches;

namespace CameraPlus.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelSelectionNavigationController), "HandleLevelCollectionNavigationControllerDidChangeLevelDetailContent")]
    internal static class SongScriptBeatmapPatch
    {
        private static string _customLevelRoot = CustomLevelPathHelper.customLevelsDirectoryPath;

        private static string _latestSelectedLevelPath = string.Empty;
        public static string customLevelPath = string.Empty;
        static void Postfix(LevelSelectionNavigationController __instance)
        {
            if(CustomLevelLoaderPatch.LoadedData != null && __instance.beatmapLevel != null)
            {
                if (CustomLevelLoaderPatch.LoadedData.ContainsKey(__instance.beatmapLevel.levelID))
                {
                    string currentLevelPath = CustomLevelLoaderPatch.LoadedData[__instance.beatmapLevel.levelID].customLevelFolderInfo.folderPath;
                    if (currentLevelPath != _latestSelectedLevelPath)
                    {
                        _latestSelectedLevelPath = currentLevelPath;
#if DEBUG
                        Plugin.Log.Notice($"Selected CustomLevel Path :\n {currentLevelPath}");
#endif
                        if (File.Exists(Path.Combine(currentLevelPath, "SongScript.json")))
                        {
                            customLevelPath = Path.Combine(currentLevelPath, "SongScript.json");
                            Plugin.Log.Notice($"Found SongScript path : \n{currentLevelPath}");
                        }
                        else
                        {
                            customLevelPath = string.Empty;
                        }
                    }
                }
                else
                    customLevelPath = string.Empty;
            }
        }
    }
}
