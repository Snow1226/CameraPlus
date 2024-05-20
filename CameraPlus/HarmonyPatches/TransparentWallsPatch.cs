using HarmonyLib;
using UnityEngine;

namespace CameraPlus.HarmonyPatches
{
    [HarmonyPatch(typeof(StretchableObstacle), nameof(StretchableObstacle.SetSizeAndColor))]
    internal class TransparentWallsPatch
    {
        public static int WallLayerMask = 25;
        static void Postfix(Transform ____obstacleCore, ref ParametricBoxFakeGlowController ____obstacleFakeGlow)
        {
            if(Camera.main != null)
                Camera.main.cullingMask |= (1 << TransparentWallsPatch.WallLayerMask);//Enables HMD bits because layer 25 is masked by default
            if (____obstacleCore != null)
            {
                ____obstacleCore.gameObject.layer = WallLayerMask;

                if (____obstacleFakeGlow.enabled)
                    ____obstacleCore.GetChild(0).gameObject.layer = WallLayerMask;
            }
        }
    }
}
