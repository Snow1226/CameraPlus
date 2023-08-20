using UnityEngine;
using CameraPlus.HarmonyPatches;

namespace CameraPlus.Behaviours
{
    public class CameraOrigin : MonoBehaviour
    {
        internal CameraPlusBehaviour _cameraPlus;
        private Vector3 _position = Vector3.zero;
        private Quaternion _rotation = Quaternion.identity;

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

            transform.position = _position;
            transform.rotation = _rotation;
        }
    }
}
