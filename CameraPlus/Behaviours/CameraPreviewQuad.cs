using UnityEngine;
using UnityEngine.EventSystems;
using CameraPlus.Configuration;
using CameraPlus.Utilities;

namespace CameraPlus.Behaviours
{
    internal class CameraPreviewQuad : MonoBehaviour, IPointerClickHandler
    {
        internal CameraPlusBehaviour _cameraPlus { get; private set; }
        private GameObject _cameraCube;
        private GameObject _cameraQuad;

        internal Material _previewMaterial = new Material(Plugin.cameraController.Shaders["BeatSaber/BlitCopyWithDepth"]);
        internal Material _cubeMaterial = new Material(Plugin.cameraController.Shaders["BeatSaber/BlitCopyWithDepth"]);

        public bool IsDisplayMaterialVROnly
        {
            get
            {
                return _previewMaterial.GetFloat(ShaderPropertyID.VRCameraOnly) == 1;
            }
            set
            {
                if (value)
                {
                    _previewMaterial.SetFloat(ShaderPropertyID.VRCameraOnly, 1);
                    _cubeMaterial.SetFloat(ShaderPropertyID.VRCameraOnly, 1);
                }
                else
                {
                    _previewMaterial.SetFloat(ShaderPropertyID.VRCameraOnly, 0);
                    _cubeMaterial.SetFloat(ShaderPropertyID.VRCameraOnly, 0);
                }
            }
        }

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        public void Init(CameraPlusBehaviour camera)
        {
            _cameraPlus = camera;
            _cameraCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cameraCube.GetComponent<MeshRenderer>().material = _cubeMaterial;
            SetCameraCubeSize(PluginConfig.Instance.CameraCubeSize);
            Plugin.Log.Notice($"Camera Aspect {_cameraPlus._cam.aspect}");
            _cameraCube.name = "CameraCube";
            _cameraCube.transform.SetParent(transform);
            _cameraCube.transform.localPosition = Vector3.zero;
            _cameraCube.transform.localEulerAngles = Vector3.zero;

            _cameraQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DestroyImmediate(_cameraQuad.GetComponent<Collider>());
            _cameraQuad.GetComponent<MeshRenderer>().material = _previewMaterial;

            if (_cameraPlus.Config.cameraExtensions.previewQuadSeparate)
            {
                _cameraQuad.transform.SetParent(_cameraPlus._cameraOrigin.transform);
                _cameraQuad.transform.position = _cameraPlus.Config.PreviewQuadPosition;
                _cameraQuad.transform.eulerAngles = _cameraPlus.Config.PreviewQuadRotation;
            }
            else
            {
                _cameraQuad.transform.SetParent(transform);
                SetCameraQuadPosition(PluginConfig.Instance.CameraQuadPosition, PluginConfig.Instance.CameraCubeSize);
            }
            IsDisplayMaterialVROnly = _cameraPlus.Config.PreviewCameraVROnly;
            CubeDisplay(!_cameraPlus.Config.PreviewCameraQuadOnly);
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!(eventData.currentInputModule is VRUIControls.VRInputModule)) return;
            if (_cameraPlus.Config.cameraLock.lockCamera) return;
            CameraMoverPointer.BeginDragCamera(_cameraPlus);
        }
        public void CubeDisplay(bool isEnable)
        {
            _cameraCube.SetActive(isEnable);
        }
        public void SetCameraQuadPosition(string quadPosition, float cubeScale = 1)
        {
            if (!_cameraPlus.Config.cameraExtensions.previewQuadSeparate)
            {
                _cameraQuad.transform.localEulerAngles = new Vector3(0, 180, 0);
                SetCameraQuadSize(_cameraPlus.Config.cameraExtensions.previewCameraQuadScale, _cameraPlus.Config.cameraExtensions.previewCameraMirrorMode);
                if (quadPosition == "Top")
                    _cameraQuad.transform.localPosition = new Vector3(0, _cameraQuad.transform.localScale.y / 2 + _cameraCube.transform.localScale.y / 2, 0.05f * cubeScale);
                else if (quadPosition == "Bottom")
                    _cameraQuad.transform.localPosition = new Vector3(0, -_cameraQuad.transform.localScale.y / 2 - _cameraCube.transform.localScale.y / 2, 0.05f * cubeScale);
                else if (quadPosition == "Left")
                    _cameraQuad.transform.localPosition = new Vector3(Mathf.Abs(_cameraQuad.transform.localScale.x) / 2 + _cameraCube.transform.localScale.x / 2, 0, 0.05f * cubeScale);
                else if (quadPosition == "Center")
                    _cameraQuad.transform.localPosition = new Vector3(0, 0, 0.15f * cubeScale);
                else
                    _cameraQuad.transform.localPosition = new Vector3(-Mathf.Abs(_cameraQuad.transform.localScale.x) / 2 - _cameraCube.transform.localScale.x / 2, 0, 0.05f * cubeScale);
            }
            else
            {
                SetCameraQuadSize(_cameraPlus.Config.cameraExtensions.previewCameraQuadScale, _cameraPlus.Config.cameraExtensions.previewCameraMirrorMode);
            }
        }
        public void SetCameraCubeSize(float cubeScale)
        {
            _cameraCube.transform.localScale = new Vector3(0.15f * cubeScale, 0.15f * cubeScale, 0.22f * cubeScale);
        }

        public void SetCameraQuadSize(float quadScale, bool isMirror = false)
        {
            if (isMirror)
                _cameraQuad.transform.localScale = new Vector3(-1 * 0.15f * (PluginConfig.Instance.CameraQuadStretch ? 1.7778f : _cameraPlus._cam.aspect) * quadScale, 0.15f * 1 * quadScale, 1); //1.7778f = 16 / 9
            else
                _cameraQuad.transform.localScale = new Vector3(0.15f * (PluginConfig.Instance.CameraQuadStretch ? 1.7778f : _cameraPlus._cam.aspect) * quadScale, 0.15f * 1 * quadScale, 1); //1.7778f = 16 / 9
        }

        public void SeparateQuad()
        {
            _cameraPlus.Config.PreviewQuadPosition = _cameraQuad.transform.position;
            _cameraPlus.Config.PreviewQuadRotation = _cameraQuad.transform.eulerAngles;
            _cameraQuad.transform.SetParent(_cameraPlus._cameraOrigin.transform);
        }
        public void CombineQuad()
        {
            _cameraQuad.transform.SetParent(transform);
            SetCameraQuadPosition(PluginConfig.Instance.CameraQuadPosition, PluginConfig.Instance.CameraCubeSize);
            _cameraPlus.Config.PreviewQuadPosition = _cameraQuad.transform.position;
            _cameraPlus.Config.PreviewQuadRotation = _cameraQuad.transform.eulerAngles;
        }

        public void SetCameraQuadSeparatePosition()
        {
            _cameraQuad.transform.position = _cameraPlus.Config.PreviewQuadPosition;
            _cameraQuad.transform.eulerAngles = _cameraPlus.Config.PreviewQuadRotation;

        }
    }
}
