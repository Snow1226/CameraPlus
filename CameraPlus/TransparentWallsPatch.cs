using HarmonyLib;
using UnityEngine;

namespace CameraPlus
{
    [HarmonyPatch(typeof(ObstacleController))]
    [HarmonyPatch("Init", MethodType.Normal)]
    public class TransparentWallsPatch
    {
        public static int WallLayerMask = 25;
        private static void Postfix(ref ObstacleController __instance)
        {
            Renderer mesh = __instance.gameObject?.GetComponentInChildren<Renderer>(false);
            if (mesh?.gameObject)
            {
                mesh.gameObject.layer = WallLayerMask;
            }
        }
    }
}
