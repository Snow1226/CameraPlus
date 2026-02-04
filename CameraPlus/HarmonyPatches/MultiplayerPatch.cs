using System;
using System.Collections.Generic;
using HarmonyLib;
using CameraPlus.Utilities;

namespace CameraPlus.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatSaberConnectedPlayerManager), nameof(BeatSaberConnectedPlayerManager.SetLocalPlayerAvatar))]
    internal class BeatSaberConnectedPlayerManagerPatch
    {
        public static BeatSaberConnectedPlayerManager Instance { get; private set; }
        static void Postfix(BeatSaberConnectedPlayerManager __instance)
        {
            MultiplayerSession.Init(__instance);
        }
    }

    [HarmonyPatch(typeof(MultiplayerLobbyAvatarPlaceManager), nameof(MultiplayerLobbyAvatarPlaceManager.Activate))]
    internal class MultiplayerLobbyAvatarPlaceManagerPatch
    {
        public static MultiplayerLobbyAvatarPlaceManager Instance { get; private set; }
        public static List<MultiplayerLobbyAvatarPlace> LobbyAvatarPlaces = null;
        static void Postfix(MultiplayerLobbyAvatarPlaceManager __instance, List<MultiplayerLobbyAvatarPlace> ____allPlaces)
        {
            Instance = __instance;
            LobbyAvatarPlaces = ____allPlaces;
#if DEBUG
            Plugin.Log.Notice("Got MultiplayerLobbyAvatarPlaceManager");
#endif
        }
    }

    [HarmonyPatch(typeof(MultiplayerLobbyController), "ActivateMultiplayerLobby")]
    internal class MultiplayerLobbyControllerPatch
    {
        public static MultiplayerLobbyController Instance { get; private set; }
        static void Postfix(MultiplayerLobbyController __instance)
        {
            Instance = __instance;
#if DEBUG
            Plugin.Log.Notice("Got MultiplayerLobbyController");
#endif
        }
    }
    [HarmonyPatch(typeof(MultiplayerPlayersManager), "BindPlayerFactories")]
    internal class MultiplayerPlayersManagerPatch
    {
        public static MultiplayerPlayersManager Instance { get; private set; }
        static void Postfix(MultiplayerPlayersManager __instance)
        {
            Instance = __instance;
#if DEBUG
            Plugin.Log.Notice("Got MultiplayerPlayersManager");
#endif
        }
    }
    [HarmonyPatch(typeof(MultiplayerScoreProvider),"Update")]
    internal class MultiplayerScoreProviderPatch
    {
        public static MultiplayerScoreProvider Instance = null;
        static void Postfix(MultiplayerScoreProvider __instance)
        {
            if (Instance == null)
            {
#if DEBUG
                Plugin.Log.Notice("Got MultiplayerScoreProvider");
#endif
                Instance = __instance;
            }
        }
    }
}