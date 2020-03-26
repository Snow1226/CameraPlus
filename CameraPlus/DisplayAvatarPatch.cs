using HarmonyLib;
using UnityEngine;

namespace CameraPlus
{
    [HarmonyPatch(typeof(ObstacleController))]
    [HarmonyPatch("Init", MethodType.Normal)]
    public class DisplayAvatarPatch
    {
        public static int OnlyInThirdPerson = 3;
        public static int OnlyInFirstPerson = 6;
        public static int AlwaysVisible = 10;
    }
}