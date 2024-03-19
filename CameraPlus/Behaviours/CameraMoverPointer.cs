using System.Linq;
using UnityEngine;
using VRUIControls;

namespace CameraPlus.Behaviours
{
    public class CameraMoverPointer : MonoBehaviour
    {
        protected const float MinScrollDistance = 0.25f;
        protected const float MaxLaserDistance = 50;

        private static CameraPlusBehaviour _targetCamera;
        private static Transform _targetTransform;
        private static VRController _grabbingController;
        private static Vector3 _grabPos;
        private static Quaternion _grabRot;
        private static Vector3 _realPos;
        private static Quaternion _realRot;

        public static void BeginDragCamera(CameraPlusBehaviour camera)
        {
            if (_targetCamera != null)
                EndDragCamera();
            _grabbingController = Resources.FindObjectsOfTypeAll<VRLaserPointer>()
                .LastOrDefault(x => x.gameObject.activeInHierarchy)?.GetComponentInParent<VRController>();
            if (_grabbingController == null)
                return;
            _targetCamera = camera;
            _targetTransform = camera._cam.transform;
            _realPos = _targetCamera.Config.Position;
            _realRot = Quaternion.Euler(_targetCamera.Config.Rotation);
            _grabPos = _grabbingController.transform.InverseTransformPoint(_targetTransform.position);
            _grabRot = Quaternion.Inverse(_grabbingController.rotation) * _targetTransform.rotation;

        }

        private static void EndDragCamera()
        {
            if (_targetCamera == null) return;

            _targetCamera.Config.Position = _targetCamera._cam.transform.localPosition;
            _targetCamera.Config.Rotation = _targetCamera._cam.transform.localRotation.eulerAngles;

            if (!_targetCamera.Config.cameraLock.dontSaveDrag)
                _targetCamera.Config.Save();
            _targetCamera = null;
        }

        protected virtual void Update()
        {
            if (_targetCamera != null)
            {
                if (_grabbingController != null && _targetCamera._cam.isActiveAndEnabled)
                {
                    if (_grabbingController.triggerValue > 0.5f)
                        return;
                }
                EndDragCamera();
            }
        }

        protected virtual void LateUpdate()
        {
            if (_targetCamera != null)
            {
                if (_grabbingController != null)
                {
                    var diff = _grabbingController.verticalAxisValue * Time.unscaledDeltaTime;
                    if (_grabPos.magnitude > MinScrollDistance)
                        _grabPos -= Vector3.forward * diff;
                    else
                        _grabPos -= Vector3.forward * Mathf.Clamp(diff, float.MinValue, 0);

                    _realPos = _grabbingController.transform.TransformPoint(_grabPos);
                    _realRot = _grabbingController.transform.rotation * _grabRot;
                }
                else return;

                _targetCamera.ThirdPersonPos = Vector3.Lerp(_targetCamera._quad.transform.position, _realPos,
                    _targetCamera.Config.cameraExtensions.positionSmooth * Time.unscaledDeltaTime);

                _targetCamera.ThirdPersonRot = Quaternion.Slerp(_targetCamera._quad.transform.rotation, _realRot,
                    _targetCamera.Config.cameraExtensions.rotationSmooth * Time.unscaledDeltaTime).eulerAngles;

            }
        }
    }
}
