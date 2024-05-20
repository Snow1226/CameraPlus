using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using IPA.Utilities;
using CameraPlus.HarmonyPatches;

namespace CameraPlus.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelSelectionNavigationController), nameof(LevelSelectionNavigationController.HandleLevelCollectionNavigationControllerDidChangeLevelDetailContent))]
    internal static class SongScriptBeatmapPatch
    {
        private static string _customLevelRoot = CustomLevelPathHelper.customLevelsDirectoryPath;

        private static string _latestSelectedLevelPath = string.Empty;
        public static string customLevelPath = string.Empty;
        static void Postfix(LevelSelectionNavigationController __instance)
        {
            if(CustomLevelLoaderPatch.Instance._loadedBeatmapSaveData != null && __instance.beatmapLevel != null)
            {
                if (CustomLevelLoaderPatch.Instance._loadedBeatmapSaveData.ContainsKey(__instance.beatmapLevel.levelID))
                {
                    string currentLevelPath = CustomLevelLoaderPatch.Instance._loadedBeatmapSaveData[__instance.beatmapLevel.levelID].customLevelFolderInfo.folderPath;
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
