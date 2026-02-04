using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CameraPlus.HarmonyPatches;
using CameraPlus.Configuration;

namespace CameraPlus.Utilities
{
    internal static class MultiplayerSession
    {
        //internal static BeatSaberMultiplayerSessionManager SessionManager { get; private set; }
        internal static BeatSaberConnectedPlayerManager ConnectedPlayerManager { get; private set; }

        internal static List<IConnectedPlayer> connectedPlayers;
        internal static bool ConnectedMultiplay;
        internal static MultiplayerPlayersManager playersManager = null;
        internal static List<Transform> LobbyAvatarPlaceList;
        internal static void Init(BeatSaberConnectedPlayerManager playerManager)
        {
            connectedPlayers = new List<IConnectedPlayer>();
            LobbyAvatarPlaceList = new List<Transform>();
            ConnectedMultiplay = false;
            ConnectedPlayerManager = playerManager;

            ConnectedPlayerManager.connectedEvent += OnSessionConnected;
            ConnectedPlayerManager.disconnectedEvent += OnSessionDisconnected;
            ConnectedPlayerManager.playerConnectedEvent += OnSessionPlayerConnected;
            ConnectedPlayerManager.playerDisconnectedEvent += OnSessionPlayerDisconnected;
        }

        internal static void Close()
        {
            ConnectedMultiplay = false;
            if (ConnectedPlayerManager != null)
            {
                ConnectedPlayerManager.connectedEvent -= OnSessionConnected;
                ConnectedPlayerManager.disconnectedEvent -= OnSessionDisconnected;
                ConnectedPlayerManager.playerConnectedEvent -= OnSessionPlayerConnected;
                ConnectedPlayerManager.playerDisconnectedEvent -= OnSessionPlayerDisconnected;
            }
        }

        private static void OnSessionConnected()
        {
            ConnectedMultiplay = true;
            connectedPlayers.Clear();
            connectedPlayers.Add(ConnectedPlayerManager.localPlayer);
#if DEBUG
            Plugin.Log.Info($"ConnectedPlayer---------------");
            for (int i = 0; i < connectedPlayers.Count; i++)
                Plugin.Log.Info($"ConnectedPlayer {connectedPlayers[i].userName},{connectedPlayers[i].sortIndex}");
#endif
            if (PluginConfig.Instance.MultiplayerProfile != "" && PluginConfig.Instance.ProfileSceneChange)
                CameraUtilities.ProfileChange(PluginConfig.Instance.MultiplayerProfile);
            LoadLobbyAvatarPlace();
        }
        private static void OnSessionDisconnected(DisconnectedReason reason)
        {
            ConnectedMultiplay = false;
            connectedPlayers.Clear();
            LobbyAvatarPlaceList.Clear();
            Plugin.Log.Info($"SessionManager Disconnected {reason}");
            if (PluginConfig.Instance.MenuProfile != "" && PluginConfig.Instance.ProfileSceneChange)
                CameraUtilities.ProfileChange(PluginConfig.Instance.MenuProfile);
        }
        private static void OnSessionPlayerConnected(IConnectedPlayer player)
        {
            connectedPlayers.Add(player);
            connectedPlayers = connectedPlayers.OrderBy(pl => pl.sortIndex)
                    .ToList();
#if DEBUG
            Plugin.Log.Info($"ConnectedPlayer---------------");
            for (int i = 0; i < connectedPlayers.Count; i++)
                Plugin.Log.Info($"ConnectedPlayer {connectedPlayers[i].userName},{connectedPlayers[i].sortIndex}");
#endif
        }
        private static void OnSessionPlayerDisconnected(IConnectedPlayer player)
        {
            foreach (IConnectedPlayer p in connectedPlayers.ToArray())
            {
                if (p.userId == player.userId)
                {
                    connectedPlayers.Remove(p);
                    break;
                }
            }
            Plugin.Log.Info($"SessionManager PlayerDisconnected {player.userName},{player.sortIndex}");
        }

        internal static void LoadLobbyAvatarPlace()
        {
            try
            {
                Transform LobbyOffset;
                LobbyAvatarPlaceList.Clear();
                if (!MultiplayerLobbyAvatarPlaceManagerPatch.Instance) return;
                LobbyAvatarPlaceList.Add(MultiplayerLobbyAvatarPlaceManagerPatch.Instance.transform);
                foreach (MultiplayerLobbyAvatarPlace multiLobbyAvatarPlace in MultiplayerLobbyAvatarPlaceManagerPatch.LobbyAvatarPlaces)
                {
                    LobbyOffset = multiLobbyAvatarPlace.transform;
                    LobbyAvatarPlaceList.Add(LobbyOffset);
                }
                if (LobbyAvatarPlaceList.Count <= 1)
                {
                    LobbyAvatarPlaceList.Clear();
                    return;
                }
                List<Transform> Tr = ShiftLobbyPositionList(LocalPlayerSortIndex());
                if (Tr != null)
                    LobbyAvatarPlaceList = Tr;
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
            foreach (IConnectedPlayer player in connectedPlayers)
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
