using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CameraPlus.Utilities;
using VRUIControls;

namespace CameraPlus.Behaviours
{
    internal class WebCamCalibrator : MonoBehaviour
    {
        private enum CalState
        {
            None,
            WebCamPosition,
            CenterPosition,
            LeftPosition,
            RightPosition,
            CalFinish
        }
        private CalState _state = CalState.None;
        internal Vector3 webCamPosition;
        internal Vector3 webCamRotation;
        
        private VRPointer _vrPointer = null;
        private float _triggerTime;

        private WebCamTexture _webCamTexture;
        private Canvas _webCamCanvas = null;
        private RawImage _cursorImage;
        private RectTransform _rectCursor;
        private Vector3 _camPosition;
        private Vector3 _leftPosition;
        private Vector3 _rightPosition;
        private Vector3 _centerPosition;
        private CameraPlusBehaviour _targetBehaviour;
        private bool _vrControllerTriggerd;
        private HMUI.CurvedTextMeshPro _calText;

        internal void Init()
        {
            _vrPointer = Resources.FindObjectsOfTypeAll<VRPointer>().First();
            _triggerTime = 0;
            _vrControllerTriggerd = false;
            _state = CalState.WebCamPosition;
            if (_vrPointer == null)
                Plugin.Log.Error($"VRController Null");
        }

        private void Update()
        {
            if (_vrPointer != null)
            {
                if (_vrPointer.vrController.triggerValue > 0.9f)
                {
                    _vrControllerTriggerd = true;
                    if (_triggerTime == 0)
                        _triggerTime = Time.time;

                }
                else if (_vrControllerTriggerd)
                {
                    _vrControllerTriggerd = false;
                    _triggerTime = 0;
                    switch (_state)
                    {
                        case CalState.None:
                            break;
                        case CalState.WebCamPosition:
                            _camPosition = _vrPointer.vrController.transform.position;
                            _state = CalState.CenterPosition;
                            _targetBehaviour.ThirdPersonPos = _camPosition;
                            _targetBehaviour.Config.Position = _camPosition;
                            CalibratorCenter();
                            break;
                        case CalState.CenterPosition:
                            _centerPosition = _vrPointer.vrController.transform.position;
                            _state = CalState.LeftPosition;
                            CalibratorLeft();
                            break;
                        case CalState.LeftPosition:
                            _leftPosition = _vrPointer.vrController.transform.position;
                            _state = CalState.RightPosition;
                            CalibratorRight();
                            break;
                        case CalState.RightPosition:
                            _rightPosition = _vrPointer.vrController.transform.position;

                            _state = CalState.None;

                            var normalVector = _camPosition - _centerPosition;
                            var x = _centerPosition;
                            var basePosition = _camPosition;
                            var mLeft = _leftPosition - _camPosition;
                            var mRight = _rightPosition - _camPosition;
                            var h = Vector3.Dot(normalVector, x);

                            Vector3 leftNormalize = basePosition + ((h - Vector3.Dot(normalVector, basePosition)) / (Vector3.Dot(normalVector, mLeft))) * mLeft;
                            Vector3 rightNormalize = basePosition + ((h - Vector3.Dot(normalVector, basePosition)) / (Vector3.Dot(normalVector, mRight))) * mRight;

                            float fov = Vector3.Angle(leftNormalize - _camPosition, rightNormalize - _camPosition);
                            float rot = Mathf.Atan2(leftNormalize.y - rightNormalize.y, Vector3.Distance(rightNormalize, leftNormalize)) * Mathf.Rad2Deg;

                            _targetBehaviour.ThirdPersonRot = Quaternion.LookRotation(_centerPosition - _camPosition).eulerAngles;
                            _targetBehaviour.ThirdPersonRot.z = rot;
                            _targetBehaviour.Config.Rotation = Quaternion.LookRotation(_centerPosition - _camPosition).eulerAngles;
                            _targetBehaviour.Config.RotationZ = rot;

                            _targetBehaviour.FOV = fov;
                            _targetBehaviour.Config.fov = fov;

                            Plugin.cameraController.DestroyCalScreen();
                            break;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if(_webCamTexture)
                _webCamTexture.Stop();
        }
        private void CalibratorCenter()
        {
            _rectCursor.sizeDelta = new Vector2(Screen.width / 16, Screen.height / 9);
            _rectCursor.localPosition = new Vector3(0, 0, -0.1f);
            _calText.text = "Step away from the webcam.\nAlign the controller to the crosshairs and pull the trigger.";
        }
        private void CalibratorLeft()
        {
            //-240, 135
            _rectCursor.sizeDelta = new Vector2(Screen.width / 16, Screen.height / 9);
            _rectCursor.localPosition = new Vector3(-135, 0, -0.1f);
            _calText.text = "Align the controller to the crosshairs and pull the trigger.";
        }
        private void CalibratorRight()
        {
            //+240
            _rectCursor.sizeDelta = new Vector2(Screen.width / 16, Screen.height / 9);
            _rectCursor.localPosition = new Vector3(135, 0, -0.1f);
            _calText.text = "Align the controller to the crosshairs and pull the trigger.";
        }

        internal void AddCalibrationScreen(CameraPlusBehaviour camplus, Camera camera)
        {
            _targetBehaviour = camplus;

            _webCamCanvas = new GameObject("WebCamCanvas").AddComponent<Canvas>();
            _webCamCanvas.gameObject.transform.SetParent(this.transform);
            _webCamCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _webCamCanvas.worldCamera = camera;
            _webCamCanvas.planeDistance = 1;

            CanvasScaler canvasScaler = _webCamCanvas.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);
            canvasScaler.matchWidthOrHeight = 1;

            RawImage raw = new GameObject("RawImage").AddComponent<RawImage>();
            raw.transform.SetParent(_webCamCanvas.transform);
            raw.transform.localPosition = Vector3.zero;
            raw.transform.localEulerAngles = Vector3.zero;

            var rect = raw.gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = new Vector3(-1f, 1f, 1);
            rect.anchoredPosition = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(Screen.width / 4, Screen.height / 4);
            rect.localPosition = new Vector3(0, 0, 0);

            _webCamTexture = new WebCamTexture(camplus.Config.webCamera.name);
            raw.texture = _webCamTexture;
            Material rawMaterial = new Material(Plugin.cameraController.Shaders["BeatSaber/BlitCopyWithDepth"]);
            rawMaterial.SetColor(ShaderPropertyID.Color, new Color(1.0f, 1.0f, 1.0f, 0));
            rawMaterial.SetFloat(ShaderPropertyID.CullMode, 0);
            raw.material = rawMaterial;
            _webCamTexture.Play();

            _cursorImage = new GameObject("CursorImage").AddComponent<RawImage>();
            _cursorImage.transform.SetParent(_webCamCanvas.transform);
            _cursorImage.transform.localPosition = Vector3.zero;
            _cursorImage.transform.localEulerAngles = Vector3.zero;
            _cursorImage.texture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.Xross.png");
            Material cursorMat = new Material(Plugin.cameraController.Shaders["ChromaKey/Unlit/Cutout"]);
            cursorMat.SetColor(ShaderPropertyID.Color, new Color(1.0f, 1.0f, 1.0f, 0));
            cursorMat.SetColor(ShaderPropertyID.ChromaKeyColor, Color.white);
            cursorMat.SetFloat(ShaderPropertyID.ChromaKeyHueRange, 0.5f);
            _cursorImage.material = cursorMat;
            _rectCursor = _cursorImage.GetComponent<RectTransform>();
            _rectCursor.anchorMin = new Vector2(0.5f, 0.5f);
            _rectCursor.anchorMax = new Vector2(0.5f, 0.5f);
            _rectCursor.pivot = new Vector2(0.5f, 0.5f);
            _rectCursor.localScale = new Vector3(1f, 1f, 1);
            _rectCursor.anchoredPosition = new Vector2(0, 0);
            _rectCursor.sizeDelta = new Vector2(Screen.width / 8, Screen.height / 4.5f);
            _rectCursor.localPosition = new Vector3(0, 0, -0.1f);

            _calText = new GameObject("CalibrationText").AddComponent<HMUI.CurvedTextMeshPro>();
            _calText.transform.SetParent(_webCamCanvas.transform);
            _calText.transform.localPosition = Vector3.zero;
            _calText.transform.localEulerAngles = Vector3.zero;
            _calText.alignment = TMPro.TextAlignmentOptions.Bottom;
            _calText.fontSize = 24;
            _calText.text = "Pull the trigger in front of the webcam.";
            var cakRect = _calText.gameObject.GetComponent<RectTransform>();
            cakRect.anchorMin = new Vector2(0.5f, 0.5f);
            cakRect.anchorMax = new Vector2(0.5f, 0.5f);
            cakRect.pivot = new Vector2(0.5f, 0.5f);
            cakRect.localScale = new Vector3(1f, 1f, 1);
            cakRect.anchoredPosition = new Vector2(0, 0);
            cakRect.sizeDelta = new Vector2(Screen.width / 4, Screen.height / 3);
            cakRect.localPosition = new Vector3(0, 0, -0.1f);
        }
    }
}
