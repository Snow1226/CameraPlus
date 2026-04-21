using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CameraPlus.HarmonyPatches;
using CameraPlus.Configuration;

namespace CameraPlus.Utilities
{
    internal static class MultiplayerSession
    {
    #if v1_40_8
        internal static MultiplayerSessionManager MultiplayManager { get; private set; }
    #else
        internal static BeatSaberConnectedPlayerManager MultiplayManager { get; private set; }
    #endif

        internal static List<IConnectedPlayer> ConnectedPlayers;
        internal static bool ConnectedMultiplay;
        internal static List<Transform> LobbyAvatarPlaceList;

#if v1_40_8
        internal static void Init(MultiplayerSessionManager sessionManager)
        {
            ConnectedPlayers = new List<IConnectedPlayer>();
            LobbyAvatarPlaceList = new List<Transform>();
            ConnectedMultiplay = false;
            MultiplayManager = sessionManager;

            MultiplayManager.connectedEvent += OnSessionConnected;
            MultiplayManager.disconnectedEvent += OnSessionDisconnected;
            MultiplayManager.playerConnectedEvent += OnSessionPlayerConnected;
            MultiplayManager.playerDisconnectedEvent += OnSessionPlayerDisconnected;
        }
#else
        internal static void Init(BeatSaberConnectedPlayerManager playerManager)
        {
            connectedPlayers = new List<IConnectedPlayer>();
            LobbyAvatarPlaceList = new List<Transform>();
            ConnectedMultiplay = false;
            MultiplayManager = playerManager;

            MultiplayManager.connectedEvent += OnSessionConnected;
            MultiplayManager.disconnectedEvent += OnSessionDisconnected;
            MultiplayManager.playerConnectedEvent += OnSessionPlayerConnected;
            MultiplayManager.playerDisconnectedEvent += OnSessionPlayerDisconnected;
        }
#endif
        internal static void Close()
        {
            ConnectedMultiplay = false;
            if (MultiplayManager != null)
            {
                MultiplayManager.connectedEvent -= OnSessionConnected;
                MultiplayManager.disconnectedEvent -= OnSessionDisconnected;
                MultiplayManager.playerConnectedEvent -= OnSessionPlayerConnected;
                MultiplayManager.playerDisconnectedEvent -= OnSessionPlayerDisconnected;
            }
        }

        private static void OnSessionConnected()
        {
            ConnectedMultiplay = true;
            ConnectedPlayers.Clear();
            ConnectedPlayers.Add(MultiplayManager.localPlayer);
#if DEBUG
            Plugin.Log.Info($"ConnectedPlayer---------------");
            for (int i = 0; i < ConnectedPlayers.Count; i++)
                Plugin.Log.Info($"ConnectedPlayer {ConnectedPlayers[i].userName},{ConnectedPlayers[i].sortIndex}");
#endif
            if (PluginConfig.Instance.MultiplayerProfile != "" && PluginConfig.Instance.ProfileSceneChange)
                CameraUtilities.ProfileChange(PluginConfig.Instance.MultiplayerProfile);
            LoadLobbyAvatarPlace();
        }
        private static void OnSessionDisconnected(DisconnectedReason reason)
        {
            ConnectedMultiplay = false;
            ConnectedPlayers.Clear();
            LobbyAvatarPlaceList.Clear();
            Plugin.Log.Info($"SessionManager Disconnected {reason}");
            if (PluginConfig.Instance.MenuProfile != "" && PluginConfig.Instance.ProfileSceneChange)
                CameraUtilities.ProfileChange(PluginConfig.Instance.MenuProfile);
        }
        private static void OnSessionPlayerConnected(IConnectedPlayer player)
        {
            ConnectedPlayers.Add(player);
            ConnectedPlayers = ConnectedPlayers.OrderBy(pl => pl.sortIndex)
                    .ToList();
#if DEBUG
            Plugin.Log.Info($"ConnectedPlayer---------------");
            for (int i = 0; i < ConnectedPlayers.Count; i++)
                Plugin.Log.Info($"ConnectedPlayer {ConnectedPlayers[i].userName},{ConnectedPlayers[i].sortIndex}");
#endif
        }
        private static void OnSessionPlayerDisconnected(IConnectedPlayer player)
        {
            foreach (IConnectedPlayer p in ConnectedPlayers.ToArray())
            {
                if (p.userId == player.userId)
                {
                    ConnectedPlayers.Remove(p);
                    break;
                }
            }
            Plugin.Log.Info($"SessionManager PlayerDisconnected {player.userName},{player.sortIndex}");
        }

        internal static void LoadLobbyAvatarPlace()
        {
            try
            {
                Transform lobbyOffset;
                LobbyAvatarPlaceList.Clear();
                if (!MultiplayerLobbyAvatarPlaceManagerPatch.Instance) return;
                LobbyAvatarPlaceList.Add(MultiplayerLobbyAvatarPlaceManagerPatch.Instance.transform);
                foreach (MultiplayerLobbyAvatarPlace multiLobbyAvatarPlace in MultiplayerLobbyAvatarPlaceManagerPatch.LobbyAvatarPlaces)
                {
                    lobbyOffset = multiLobbyAvatarPlace.transform;
                    LobbyAvatarPlaceList.Add(lobbyOffset);
                }
                if (LobbyAvatarPlaceList.Count <= 1)
                {
                    LobbyAvatarPlaceList.Clear();
                    return;
                }
                List<Transform> tr = ShiftLobbyPositionList(LocalPlayerSortIndex());
                if (tr != null)
                    LobbyAvatarPlaceList = tr;
                else
                    Plugin.Log.Info($"LobbyAvatarPlace SortError");
                for (int i = 0; i < LobbyAvatarPlaceList.Count; i++)
                    Plugin.Log.Notice($"Find Sorted LobbyAvatarPlace {i}: {LobbyAvatarPlaceList[i].position.x},{LobbyAvatarPlaceList[i].position.y},{LobbyAvatarPlaceList[i].position.z}");
            }
            catch
            {
                Plugin.Log.Error($"Unable to LoadLobbyAvatarPlace");
            }
        }
        private static int LocalPlayerSortIndex()
        {
            int result = 0;
            foreach (IConnectedPlayer player in ConnectedPlayers)
            {
                if (player.isMe)
                {
                    result = player.sortIndex;
                    break;
                }
            }
            return result;
        }
        private static List<Transform> ShiftLobbyPositionList(int shiftValue)
        {
            if (shiftValue < 0 || shiftValue >= LobbyAvatarPlaceList.Count) return null;

            List<Transform> result = LobbyAvatarPlaceList;
            for (int i = 0; i < shiftValue; i++)
            {
                result.Insert(0, result[result.Count - 1]);
                result.RemoveAt(result.Count - 1);
            }
            return result;
        }
    }
}
