using System;
using HarmonyLib;
using UnityEngine;

namespace CameraPlus.HarmonyPatches
{
	[HarmonyPatch(typeof(MainCamera))]
	internal class MainCameraPatch
	{
		internal static Camera gameMainCamera = null;
		[HarmonyPostfix]
		[HarmonyPatch("Awake", 0)]
		private static void OnEnablePostfix(Camera ____camera)
		{
			if (____camera.name == "MainCamera")
			{
				gameMainCamera = ____camera;
			}
		}
	}
}
