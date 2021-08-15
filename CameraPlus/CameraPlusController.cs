using System;
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

namespace CameraPlus
{
    public class CameraPlusController : MonoBehaviour
    {
        internal Action<Scene, Scene> ActiveSceneChanged;
        internal static CameraPlusController instance { get; private set; }
        internal ConcurrentDictionary<string, CameraPlusBehaviour> Cameras = new ConcurrentDictionary<string, CameraPlusBehaviour>();

        internal string currentProfile;
        internal bool MultiplayerSessionInit;
        internal bool existsVMCAvatar = false;
        internal bool isRestartingSong = false;
        internal Transform origin;
        private UnityEngine.Object[] Assets;
        internal Dictionary<string, Shader> Shaders = new Dictionary<string, Shader>();
        private RenderTexture _renderTexture;
        private ScreenCameraBehaviour _screenCameraBehaviour;
        private CameraMoverPointer _cameraMovePointer;
        private bool initialized = false;

        internal UnityEvent OnFPFCToggleEvent = new UnityEvent();

        private void Awake()
        {
            if (instance != null)
            {
                Logger.log?.Warn($"Instance of {this.GetType().Name} already exists, destroying.");
                GameObject.DestroyImmediate(this);
                return;
            }
            GameObject.DontDestroyOnLoad(this);
            instance = this;

            string path = Path.Combine(UnityGame.UserDataPath, $"{Plugin.Name}.ini");
            string backupPath = backupPath = Path.Combine(UnityGame.UserDataPath, Plugin.Name, "OldProfiles");
            
            if (File.Exists(path))
            {
                RootConfig rootConfig = new RootConfig(path);
                // Directory?
                if (!Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);
                File.Copy(path, Path.Combine(backupPath, $"{Plugin.Name}.ini"), true);
                File.Delete(path);
            }
            
            ConfigConverter.ProfileConverter();

            SceneManager.activeSceneChanged += this.OnActiveSceneChanged;
            CameraUtilities.CreateMainDirectory();
            CameraUtilities.CreateExampleScript();

            ConfigConverter.DefaultConfigConverter();
        }
        private void Start()
        {
            if (PluginConfig.Instance.ScreenFillBlack)
            {
                _renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
                _screenCameraBehaviour = this.gameObject.AddComponent<ScreenCameraBehaviour>();
                _screenCameraBehaviour.SetCameraInfo(new Vector2(0, 0), new Vector2(Screen.width, Screen.height), -2000);
                _screenCameraBehaviour.SetRenderTexture(_renderTexture);
            }

            ShaderLoad();
            _cameraMovePointer = this.gameObject.AddComponent<CameraMoverPointer>();
            CameraUtilities.AddNewCamera(Plugin.MainCamera);
            MultiplayerSessionInit = false;
            Logger.log.Notice($"{Plugin.Name} has started");

            if (CustomUtils.IsModInstalled("VMCAvatar"))
                existsVMCAvatar = true;
        }
        private void ShaderLoad()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("CameraPlus.Resources.Shader.customshader"));
            Assets = assetBundle.LoadAllAssets();
            UnityEngine.Object[] assets = Assets;
            for (int i = 0; i < assets.Length; i++)
            {
                Shader shader = assets[i] as Shader;
                if (shader != null)
                    Shaders[shader.name] = shader;
            }
        }

        private void OnDestroy()
        {
            MultiplayerSession.Close();
            SceneManager.activeSceneChanged -= this.OnActiveSceneChanged;
            Logger.log?.Debug($"{name}: OnDestroy()");
            instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.
        }

        public void OnActiveSceneChanged(Scene from, Scene to)
        {
            if (isRestartingSong && to.name != "GameCore") return;
            if (initialized || (!initialized && (to.name == "HealthWarning" || to.name == "MainMenu")))
                SharedCoroutineStarter.instance.StartCoroutine(DelayedActiveSceneChanged(from, to));
#if DEBUG
            Logger.log.Info($"Scene Change {from.name} to {to.name}");
#endif
        }

        private IEnumerator DelayedActiveSceneChanged(Scene from, Scene to)
        {
            initialized = true;
            bool isRestart = isRestartingSong;
            isRestartingSong = false;

            yield return waitMainCamera();

            if (!isRestart)
                CameraUtilities.ReloadCameras();

            IEnumerator waitForcam()
            {
                yield return new WaitForSeconds(0.1f);
                while (Camera.main == null) yield return new WaitForSeconds(0.05f);
            }

            if (PluginConfig.Instance.ProfileSceneChange && !isRestart)
            {
                if (!MultiplayerSession.ConnectedMultiplay || PluginConfig.Instance.MultiplayerProfile == "")
                {
                    if (to.name == "GameCore" && PluginConfig.Instance.RotateProfile != "" && LevelDataPatch.is360Level)
                        CameraUtilities.ProfileChange(PluginConfig.Instance.RotateProfile);
                    else if (to.name == "GameCore" && PluginConfig.Instance.GameProfile != "")
                        CameraUtilities.ProfileChange(PluginConfig.Instance.GameProfile);
                    else if ((to.name == "MainMenu" || to.name == "MenuCore" || to.name == "HealthWarning") && PluginConfig.Instance.MenuProfile != "")
                        CameraUtilities.ProfileChange(PluginConfig.Instance.MenuProfile);
                }
            }

            if (ActiveSceneChanged != null)
            {
                yield return waitForcam();
                // Invoke each activeSceneChanged event
                foreach (var func in ActiveSceneChanged?.GetInvocationList())
                {
                    try
                    {
                        func?.DynamicInvoke(from, to);
                    }
                    catch (Exception ex)
                    {
                        Logger.log.Error($"Exception while invoking ActiveSceneChanged:" +
                            $" {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
            else if (PluginConfig.Instance.ProfileSceneChange && to.name == "HealthWarning" && PluginConfig.Instance.MenuProfile != "")
                CameraUtilities.ProfileChange(PluginConfig.Instance.MenuProfile);

            yield return waitForcam();

            if (to.name == "GameCore")
                origin = GameObject.Find("LocalPlayerGameCore/Origin")?.transform;
        }

        protected IEnumerator waitMainCamera()
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
    }
}
