using CameraPlus.Utilities;
using System.Collections.Generic;
using System.Linq;
using CameraPlus.Configuration;
using Klak.Spout;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CameraPlus.Behaviours
{
    /// <summary>
    /// This is the monobehaviour that goes on the camera that handles
    /// displaying the actual feed from the camera to the screen.
    /// </summary>
    public class ScreenCameraBehaviour : MonoBehaviour
    {
        private Camera _cam;
        private List<CameraPlusBehaviour> _cameras;
        private RenderTexture _renderTexture;
        private RenderTexture _backgroundTexture;
        private RenderTexture _spoutTexture;
        
        private Material _dofMaterial = null;
        private Material _wipeMaterial = null;
        private Material _outlineMaterial = null;
        private Material _glitchMaterial = null;
        private Material _dotMateial = null;

        private Rect _screenRect;

        public SpoutReceiver ScreenSpoutReceiver;
        private Vector2 _spoutScreenOffset;
        private Material _spoutMaterial = new Material(Plugin.cameraController.Shaders["BeatSaber/Unlit/Transparent"]);
        public void SetLayer(int layer)
        {
            _cam.depth = layer;
        }

        public void RegistrationCamera(CameraPlusBehaviour cameraPlus)
        {
            if (_cameras.Find(c => c._cam.name == cameraPlus._cam.name) == null && cameraPlus.gameObject.activeInHierarchy == transform)
            {
                _cameras.Add(cameraPlus);
                _cameras = _cameras.OrderBy(c => c.Config.layer).ToList();
            }
        }
        public void UnregistrationCamera(CameraPlusBehaviour cameraPlus)
        {
            if (_cameras.Find(c => c._cam.name == cameraPlus._cam.name) != null)
            {
                _cameras.Remove(cameraPlus);
                _cameras = _cameras.OrderBy(c => c.Config.layer).ToList();
            }
        }

        public void SortCamera()
        {
            _cameras = _cameras.OrderBy(c => c.Config.layer).ToList();
        }

        public void ClearScreenCamera()
        {
            _cameras.Clear();
        }

        public void Awake()
        {
#if DEBUG
            Plugin.Log.Notice("Created new screen camera behaviour component!");
#endif
            _cam = gameObject.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.Nothing;
            _cam.cullingMask = 0;
            _cam.stereoTargetEye = StereoTargetEyeMask.None;
            _cam.depth = -1000;

            _cameras = new List<CameraPlusBehaviour>();

            _screenRect = new Rect(0, 0, Screen.width, Screen.height);
            _renderTexture = new RenderTexture(Screen.width, Screen.height, 24)
            {
                useMipMap = false,
                anisoLevel = 1,
                useDynamicScale = false
            };
            _backgroundTexture = new RenderTexture(Screen.width, Screen.height, 24)
            {
                useMipMap = false,
                anisoLevel = 1,
                useDynamicScale = false
            };
            
            _spoutScreenOffset = Vector2.zero;
            if(PluginConfig.Instance.AvatarSpoutAutoConnect)
                CreateSpoutScreen(PluginConfig.Instance.AvatarSpoutName);
        }

        public void CreateSpoutScreen(string spoutName)
        {
            if (!ScreenSpoutReceiver)
            {
                _spoutMaterial = new Material(Plugin.cameraController.Shaders["BeatSaber/Unlit/Transparent"]);
                var spoutResources = SpoutResources.CreateInstance<SpoutResources>();
                spoutResources.blitShader = Plugin.cameraController.Shaders["Hidden/Klak/Spout/Blit"];
                
                ScreenSpoutReceiver = this.gameObject.AddComponent<SpoutReceiver>();
                ScreenSpoutReceiver.SetResources(spoutResources);
                ScreenSpoutReceiver.sourceName = spoutName;
                _spoutTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                ScreenSpoutReceiver.targetTexture = _spoutTexture;
                ScreenSpoutReceiver.targetMaterialProperty = "_MainTex";
            }
        }

        public void DisposeSpoutScreen()
        {
            if(!ScreenSpoutReceiver) return;
            Destroy(ScreenSpoutReceiver);
            ScreenSpoutReceiver = null;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (_renderTexture == null) return;

            _cam.pixelRect = _screenRect;
            Graphics.Blit(_backgroundTexture, dest);

            foreach (CameraPlusBehaviour c in _cameras)
            {
                if (c.renderScreen)
                {
                    _cam.pixelRect = c.ScreenRect;

                    if (c.effectElements.enableDOF) PostEffect.DepthOfField(c, c._camRenderTexture, _dofMaterial);
                    if (c.effectElements.enableGlitch) PostEffect.Glitch(c, c._camRenderTexture, _glitchMaterial);
                    if (c.effectElements.enableOutline) PostEffect.Outline(c, c._camRenderTexture, _outlineMaterial);
                    if (c.effectElements.enableDot) PostEffect.Dot(c, c._camRenderTexture, _dotMateial);
                    if (c.effectElements.wipeProgress > 0)
                        PostEffect.Wipe(c, c._camRenderTexture, dest, _wipeMaterial);
                    else
                        Graphics.Blit(c._camRenderTexture, dest);
                }
            }

            if (ScreenSpoutReceiver)
            {
                if ((SceneManager.GetActiveScene().name == "MainMenu" && PluginConfig.Instance.AvatarSpoutInMenu) ||
                    (SceneManager.GetActiveScene().name == "GameCore" && PluginConfig.Instance.AvatarSpoutInGame))
                {
                    _cam.pixelRect = _screenRect;

                    _spoutMaterial.SetFloat("_Threshold", PluginConfig.Instance.SpoutCameraAlpha);
                    _spoutScreenOffset.x = PluginConfig.Instance.AvatarSpoutPositionOffsetX / Screen.width;
                    _spoutScreenOffset.y = PluginConfig.Instance.AvatarSpoutPositionOffsetY / Screen.height;
                    Graphics.Blit(_spoutTexture, dest, _spoutMaterial);
                }
            }
        }
    }
}
