using UnityEngine;
using CameraPlus.HarmonyPatches;
using CameraPlus.Utilities;
using IPA.Config;
using System;
using UnityEngine.SceneManagement;

namespace CameraPlus.Behaviours
{
    public class CameraOrigin : MonoBehaviour
    {
        internal CameraPlusBehaviour _cameraPlus;
        private Vector3 _position = Vector3.zero;
        private Quaternion _rotation = Quaternion.identity;
        private float _yAngle;
        private Vector3 _multiOffsetPosition;
        private Quaternion _multiOffsetRotation;

        protected virtual void LateUpdate()
        {
            if (_cameraPlus.Config.cameraExtensions.followNoodlePlayerTrack && Plugin.cameraController.origin)
            {
                transform.position = Plugin.cameraController.origin.position;
                transform.rotation = Plugin.cameraController.origin.rotation * Quaternion.Inverse(RoomAdjustPatch.rotation);

                _cameraPlus._originOffset.transform.localPosition = Vector3.zero - RoomAdjustPatch.position;
                _cameraPlus._originOffset.transform.localRotation = Quaternion.identity;

                _position = _cameraPlus._originOffset.transform.position;
                _rotation = _cameraPlus._originOffset.transform.rotation;
            }
            else
            {
                _position = Vector3.zero;
                _rotation = Quaternion.identity;
            }

            HandleMultiPlayerLobby();
            HandleMultiPlayerGame();

            transform.localPosition = _position + _multiOffsetPosition;
            transform.localRotation = _rotation * _multiOffsetRotation;

            if (_cameraPlus.Config.cameraExtensions.follow360map)
            {
                _yAngle = Mathf.LerpAngle(_yAngle, CameraPlusController.instance._beatLineManagerYAngle, Mathf.Clamp(Time.deltaTime * _cameraPlus.Config.cameraExtensions.rotation360Smooth, 0f, 1f));

                transform.localRotation *= Quaternion.AngleAxis(_yAngle, transform.up);
            }

        }

        private void HandleMultiPlayerLobby()
        {
            try
            {
                if (!MultiplayerLobbyAvatarPlaceManagerPatch.Instance || !MultiplayerLobbyControllerPatch.Instance.isActiveAndEnabled || _cameraPlus.Config.multiplayer.targetPlayerNumber == 0) return;
                if (MultiplayerSession.LobbyAvatarPlaceList.Count == 0) MultiplayerSession.LoadLobbyAvatarPlace();

                for (int i = 0; i < MultiplayerSession.LobbyAvatarPlaceList.Count; i++)
                {
                    if (i == _cameraPlus.Config.multiplayer.targetPlayerNumber - 1)
                    {
                        _multiOffsetPosition = MultiplayerSession.LobbyAvatarPlaceList[i].position;
                        _multiOffsetRotation = MultiplayerSession.LobbyAvatarPlaceList[i].rotation;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"HandleMultiPlayerLobby Error {ex.Message}");
            }
        }
        private void HandleMultiPlayerGame()
        {
            try
            {
                if (SceneManager.GetActiveScene().name == "GameCore" && MultiplayerSession.ConnectedMultiplay)
                {
                    MultiplayerConnectedPlayerFacade player = null;
                    bool TryPlayerFacade;
                    if (MultiplayerPlayersManagerPatch.Instance && _cameraPlus.Config.multiplayer.targetPlayerNumber != 0)
                        foreach (IConnectedPlayer connectedPlayer in MultiplayerSession.connectedPlayers)
                            if (_cameraPlus.Config.multiplayer.targetPlayerNumber - 1 == connectedPlayer.sortIndex)
                            {
                                TryPlayerFacade = MultiplayerPlayersManagerPatch.Instance.TryGetConnectedPlayerController(connectedPlayer.userId, out player);
                                if (TryPlayerFacade && player != null)
                                {
                                    _multiOffsetPosition = player.transform.position;
                                    _multiOffsetRotation = player.transform.rotation;
                                }
                                break;
                            }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"{this.name} HandleMultiPlayerGame Error {ex.Message}");
            }
        }
    }
}
