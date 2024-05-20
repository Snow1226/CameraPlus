using IPA.Loader;
using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CameraPlus.HarmonyPatches
{
    internal class BeatLeaderReplayerCameraControllerStartPatch
    {
        internal static MethodBase TargetMethod()
        {
            PluginMetadata beatLeader = PluginManager.GetPluginFromId("BeatLeader");
            if (beatLeader != null)
                return beatLeader.Assembly.GetType("BeatLeader.Replayer.ReplayerCameraController").GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            else
                return null;
        }
        internal static void Prefix()
        {
            Plugin.cameraController._isBeatLeaderReplay = true;
            Plugin.cameraController.ScreenCamera.enabled = false;
        }
    }

    internal class BeatLeaderReplayerCameraControllerOnDestroyPatch
    {
        internal static MethodBase TargetMethod()
        {
            PluginMetadata beatLeader = PluginManager.GetPluginFromId("BeatLeader");
            if (beatLeader != null)
                return beatLeader.Assembly.GetType("BeatLeader.Replayer.ReplayerCameraController").GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
            else
                return null;
        }
        internal static void Prefix()
        {
            Plugin.cameraController._isBeatLeaderReplay = false;
            Plugin.cameraController.ScreenCamera.enabled = true;
        }
    }

}
