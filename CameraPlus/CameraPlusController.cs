using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.IO;
using System.Reflection;
using CameraPlus.HarmonyPatches;
using CameraPlus.Configuration;
using CameraPlus.Behaviours;
using CameraPlus.Utilities;
using CameraPlus.VMCProtocol;

namespace CameraPlus
{
    public class CameraPlusController : MonoBehaviour
    {
        public Action<Scene, Scene> ActiveSceneChanged;

        internal static CameraPlusController instance { get; private set; }
        public ConcurrentDictionary<string, GameObject> LoadedProfile = new ConcurrentDictionary<string, GameObject>();
        public ConcurrentDictionary<string, CameraPlusBehaviour> Cameras = new ConcurrentDictionary<string, CameraPlusBehaviour>();

        public string CurrentProfile = string.Empty;

        public bool MultiplayerSessionInit;
        internal bool existsVMCAvatar = false;
        internal Transform origin;

        internal Dictionary<string, Shader> Shaders = new Dictionary<string, Shader>();
        private RenderTexture _renderTexture;
        private ScreenCameraBehaviour _fillBlackScreen;
        private CameraMoverPointer _cameraMovePointer;
        public bool Initialized = false;

        internal UnityEvent OnFPFCToggleEvent = new UnityEvent();
        internal UnityEvent OnSetCullingMask = new UnityEvent();
        public bool isFPFC = false;

        internal ExternalSender externalSender = null;

        private WebCamTexture _webCamTexture = null;
        internal WebCamDevice[] webCamDevices;
        private WebCamCalibrator _webCamCal;

        internal UI.ContextMenu _contextMenu = null;
        protected bool _contextMenuOpen = false;
        protected bool _mouseHeld = false;

        private void Awake()
        {
            if (instance != null)
            {
                Plugin.Log?.Warn($"Instance of {this.GetType().Name} already exists, destroying.");
                GameObject.DestroyImmediate(this);
                return;
            }
            GameObject.DontDestroyOnLoad(this);
            instance = this;

            SceneManager.activeSceneChanged += this.OnActiveSceneChanged;
            CameraUtilities.CreateMainDirectory();
            CameraUtilities.CreateExampleScript();
        }
        private void Start()
        {
            _renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            _fillBlackScreen = this.gameObject.AddComponent<ScreenCameraBehaviour>();
            _fillBlackScreen.SetCameraInfo(new Vector2(0, 0), new Vector2(Screen.width, Screen.height), -2000);
            _fillBlackScreen.SetRenderTexture(_renderTexture);

            ShaderLoad();
            _cameraMovePointer = this.gameObject.AddComponent<CameraMoverPointer>();

            CameraUtilities.AddNewCamera(Plugin.MainCamera);
            MultiplayerSessionInit = false;

            externalSender = new GameObject("ExternalSender").AddComponent<ExternalSender>();
            externalSender.transform.SetParent(transform);

            _contextMenu = this.gameObject.AddComponent<UI.ContextMenu>();

            OnFPFCToggleEvent.AddListener(OnFPFCToglleEvent);

            if (CustomUtils.IsModInstalled("VMCAvatar","0.99.0"))
                existsVMCAvatar = true;
            _webCamTexture = new WebCamTexture();
            webCamDevices = WebCamTexture.devices;
        }

        private void Update()
        {
            if (Input.GetMouseButton(1))
            {
                if (!_mouseHeld)
                {
                    DisplayContextMenu();
                    _contextMenuOpen = true;
                }
                _mouseHeld = true;
            }
            else
                _mouseHeld = false;
        }

        private void ShaderLoad()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("CameraPlus.Resources.Shader.customshader"));
            Shaders = assetBundle.LoadAllAssets<Shader>().ToDictionary(x => x.name);
            assetBundle.Unload(false);
        }

        private void OnDestroy()
        {
            MultiplayerSession.Close();
            SceneManager.activeSceneChanged -= this.OnActiveSceneChanged;
            Plugin.Log?.Debug($"{name}: OnDestroy()");
            instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.
        }

        public void OnActiveSceneChanged(Scene from, Scene to)
        {
            if (Initialized || (!Initialized && (to.name == "MainMenu")))
                SharedCoroutineStarter.instance.StartCoroutine(DelayedActiveSceneChanged(from, to));
#if DEBUG
            Plugin.Log.Info($"Scene Change {from.name} to {to.name}");
#endif
        }

        private IEnumerator DelayedActiveSceneChanged(Scene from, Scene to)
        {
            bool isLevelEditor = false;
            yield return waitMainCamera();

            if (PluginConfig.Instance.ProfileSceneChange)
            {
                if (!MultiplayerSession.ConnectedMultiplay || PluginConfig.Instance.MultiplayerProfile == "")
                {
                    if (to.name == "GameCore" && PluginConfig.Instance.SongSpecificScriptProfile != "" && CustomPreviewBeatmapLevelPatch.customLevelPath != "")
                        CameraUtilities.ProfileChange(PluginConfig.Instance.SongSpecificScriptProfile);
                    else if (to.name == "GameCore" && PluginConfig.Instance.RotateProfile != "" && LevelDataPatch.is360Level)
                        CameraUtilities.ProfileChange(PluginConfig.Instance.RotateProfile);
                    else if (to.name == "GameCore" && PluginConfig.Instance.GameProfile != "")
                        CameraUtilities.ProfileChange(PluginConfig.Instance.GameProfile);
                    else if ((to.name == "MainMenu" || to.name == "MenuCore" || to.name == "HealthWarning") && PluginConfig.Instance.MenuProfile != "")
                        CameraUtilities.ProfileChange(PluginConfig.Instance.MenuProfile);
                    else if (to.name == "BeatmapEditor3D" || to.name == "BeatmapLevelEditorWorldUi")
                    {
                        CameraUtilities.TurnOffCameras();
                        isLevelEditor = true;
                    }
                }
            }
            if (!isLevelEditor)
            {
                if (ActiveSceneChanged != null)
                {
                    // Invoke each activeSceneChanged event
                    foreach (var func in ActiveSceneChanged?.GetInvocationList())
                    {
                        try
                        {
                            func?.DynamicInvoke(from, to);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.Error($"Exception while invoking ActiveSceneChanged:" +
                                $" {ex.Message}\n{ex.StackTrace}");
                        }
                    }
                }
                else if (PluginConfig.Instance.ProfileSceneChange && to.name == "HealthWarning" && PluginConfig.Instance.MenuProfile != "")
                    CameraUtilities.ProfileChange(PluginConfig.Instance.MenuProfile);

                if (to.name == "GameCore")
                    origin = GameObject.Find("LocalPlayerGameCore/Origin")?.transform;
                if (to.name == "MainMenu")
                {
                    var chat = GameObject.Find("ChatDisplay");
                    if (chat)
                        chat.layer = Layer.UI;
                }
            }
            CloseContextMenu();

        }

        internal IEnumerator waitMainCamera()
        {
            if (SceneManager.GetActiveScene().name == "GameCore")
            {
                while (!MainCameraPatch.isGameCameraEnable)
                    yield return null;
            }
            else
            {
                while (Camera.main == null)
                    yield return null;
            }
        }

        private void OnFPFCToglleEvent()
        {
            if (isFPFC)
                _fillBlackScreen.enabled = false;
            else
                _fillBlackScreen.enabled = true;
        }
        internal void SetBackScreenLayer(int layer)
        {
            _fillBlackScreen.SetLayer(layer);
        }

        internal string[] WebCameraList()
        {
            string[] webcamera = new string[] { };
            List<string> list = new List<string>();
            if (_webCamTexture)
            {
                for (int i = 0; i < webCamDevices.Length; i++)
                    list.Add(webCamDevices[i].name);
                webcamera = list.ToArray();
            }
            return webcamera;
        }

        internal void WebCameraCalibration(CameraPlusBehaviour camplus)
        {
            _webCamCal = new GameObject("WebCamCalScreen").AddComponent<WebCamCalibrator>();
            _webCamCal.transform.SetParent(this.transform);
            _webCamCal.Init();
            _webCamCal.AddCalibrationScreen(camplus, Camera.main);
        }
        internal bool inProgressCalibration()
        {
            if (_webCamCal)
                return true;
            return false;
        }
        internal void DestroyCalScreen()
        {
            if (_webCamCal)
                Destroy(_webCamCal.gameObject);
        }

        internal void RemoveProfile(string profile)
        {
            GameObject obj;
            if (LoadedProfile.ContainsKey(profile))
            {
                LoadedProfile.TryRemove(profile, out obj);
                Destroy(obj);
            }
        }

        internal void DisplayContextMenu()
        {
            var c = CameraUtilities.GetTopmostInstanceAtCursorPos(Input.mousePosition);
            if (c != null)
                _contextMenu.EnableMenu(Input.mousePosition, c);
        }

        internal void CloseContextMenu()
        {
            _contextMenu?.DisableMenu();
            _contextMenuOpen = false;
        }
    }
}
