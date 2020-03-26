using System;
using System.Reflection;
using System.Collections;
using System.Collections.Concurrent;
using IPA;
using IPA.Loader;
using IPALogger = IPA.Logging.Logger;
using LogLevel = IPA.Logging.Logger.Level;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CameraPlus
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private bool _init;
        private Harmony _harmony;

        public Action<Scene, Scene> ActiveSceneChanged;
        public ConcurrentDictionary<string, CameraPlusInstance> Cameras = new ConcurrentDictionary<string, CameraPlusInstance>();

        public static Plugin Instance { get; private set; }
        public static string Name => "CameraPlus";
        public static string MainCamera => "cameraplus";

        [Init]
        public void Init(IPALogger logger)
        {
            Logger.log = logger;
            Logger.Log("Logger prepared", LogLevel.Debug);
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            if (_init) return;
            _init = true;
            Instance = this;

            _harmony = new Harmony("com.brian91292.beatsaber.cameraplus");
            try
            {
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to apply harmony patches! {ex}", LogLevel.Error);
            }
            
            // Add our default cameraplus camera
            CameraUtilities.AddNewCamera(Plugin.MainCamera);

            Logger.Log($"{Plugin.Name} has started", LogLevel.Notice);
        }

        public void OnActiveSceneChanged(Scene from, Scene to)
        {
            SharedCoroutineStarter.instance.StartCoroutine(DelayedActiveSceneChanged(from, to));
        }

        private IEnumerator DelayedActiveSceneChanged(Scene from, Scene to)
        {
            yield return new WaitForSeconds(0.5f);
            // If any new cameras have been added to the config folder, render them
           // if(to.name == )
            CameraUtilities.ReloadCameras();

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
                        Logger.Log($"Exception while invoking ActiveSceneChanged:" +
                            $" {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
                    }
                }
            }
        }

        [OnStart]
        public void OnApplicationQuit()
        {
            _harmony.UnpatchAll("com.brian91292.beatsaber.cameraplus");
        }
    }
}
