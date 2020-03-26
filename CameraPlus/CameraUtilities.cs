using System;
using System.IO;
using System.Linq;
using System.Collections;
using IPA.Utilities;
using LogLevel = IPA.Logging.Logger.Level;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CameraPlus
{
    public class CameraUtilities
    {
        public static bool CameraExists(string cameraName)
        {
            return Plugin.Instance.Cameras.Keys.Where(c => c == cameraName + ".cfg").Count() > 0;
        }
        
        public static void AddNewCamera(string cameraName, Config CopyConfig = null, bool meme = false)
        {
            string path = Path.Combine(UnityGame.UserDataPath, Plugin.Name, $"{cameraName}.cfg");
            if (!File.Exists(path))
            {
                // Try to copy their old config file into the new camera location
                if(cameraName == Plugin.MainCamera)
                {
                    string oldPath = Path.Combine(Environment.CurrentDirectory, $"{Plugin.MainCamera}.cfg");
                    if (File.Exists(oldPath))
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(path)))
                            Directory.CreateDirectory(Path.GetDirectoryName(path));

                        File.Move(oldPath, path);
                        Logger.Log($"Copied old {Plugin.MainCamera}.cfg into new {Plugin.Name} folder in UserData");
                    }
                }

                Config config = null;
                if (CopyConfig != null)
                    File.Copy(CopyConfig.FilePath, path, true);

                config = new Config(path);
                foreach (CameraPlusInstance c in Plugin.Instance.Cameras.Values.OrderBy(i => i.Config.layer))
                {
                    if (c.Config.layer > config.layer)
                        config.layer += (c.Config.layer - config.layer);
                    else if (c.Config.layer == config.layer)
                        config.layer++;
                }

                if (cameraName == Plugin.MainCamera)
                    config.fitToCanvas = true;

                if (meme)
                {
                    config.screenWidth = (int)Random.Range(200, Screen.width / 1.5f);
                    config.screenHeight = (int)Random.Range(200, Screen.height / 1.5f);
                    config.screenPosX = Random.Range(-200, Screen.width - config.screenWidth + 200);
                    config.screenPosY = Random.Range(-200, Screen.height - config.screenHeight + 200);
                    config.thirdPerson = Random.Range(0, 2) == 0;
                    config.renderScale = Random.Range(0.1f, 1.0f);
                    config.posx += Random.Range(-5, 5);
                    config.posy += Random.Range(-2, 2);
                    config.posz += Random.Range(-5, 5);
                    config.angx = Random.Range(0, 360);
                    config.angy = Random.Range(0, 360);
                    config.angz = Random.Range(0, 360);
                }
                else if (CopyConfig == null && cameraName != Plugin.MainCamera)
                {
                    config.screenHeight /= 4;
                    config.screenWidth /= 4;
                }
                
                config.Position = config.DefaultPosition;
                config.Rotation = config.DefaultRotation;
                config.FirstPersonPositionOffset = config.DefaultFirstPersonPositionOffset;
                config.FirstPersonRotationOffset = config.DefaultFirstPersonRotationOffset;
                config.Save();
                Logger.Log($"Success creating new camera \"{cameraName}\"");
            }
            else
            {
                Logger.Log($"Camera \"{cameraName}\" already exists!");
            }
        }

        public static string GetNextCameraName()
        {
            int index = 1;
            string cameraName = String.Empty;
            while (true)
            {
                cameraName = $"customcamera{index.ToString()}";
                if (!CameraUtilities.CameraExists(cameraName))
                    break;

                index++;
            }
            return cameraName;
        }

        public static bool RemoveCamera(CameraPlusBehaviour instance, bool delete = true)
        {
            try
            {
                if (Path.GetFileName(instance.Config.FilePath) != $"{Plugin.MainCamera}.cfg")
                {
                    if (Plugin.Instance.Cameras.TryRemove(Plugin.Instance.Cameras.Where(c => c.Value.Instance == instance && c.Key != $"{Plugin.MainCamera}.cfg")?.First().Key, out var removedEntry))
                    {
                        if (delete)
                        {
                            if (File.Exists(removedEntry.Config.FilePath))
                                File.Delete(removedEntry.Config.FilePath);
                        }

                        GL.Clear(false, true, Color.black, 0);
                        GameObject.Destroy(removedEntry.Instance.gameObject);
                        return true;
                    }
                }
                else
                {
                    Logger.Log("One does not simply remove the main camera!", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                string msg
                    = ((instance != null && instance.Config != null && instance.Config.FilePath != null)
                    ? $"Could not remove camera with configuration: '{Path.GetFileName(instance.Config.FilePath)}'."
                    : $"Could not remove camera.");

                Logger.Log($"{msg} CameraUtilities.RemoveCamera() threw an exception:" +
                    $" {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
            return false;
        }

        public static void ReloadCameras()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(UnityGame.UserDataPath, Plugin.Name)))
                    Directory.CreateDirectory(Path.Combine(UnityGame.UserDataPath, Plugin.Name));
                string[] files = Directory.GetFiles(Path.Combine(UnityGame.UserDataPath, Plugin.Name));
                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    if (fileName.EndsWith(".cfg") && !Plugin.Instance.Cameras.ContainsKey(fileName))
                    {
                        Logger.Log($"Found config {filePath}!");
                        Plugin.Instance.Cameras.TryAdd(fileName, new CameraPlusInstance(filePath));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception while reloading cameras! Exception:" +
                    $" {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        public static IEnumerator Spawn38Cameras()
        {
            lock (Plugin.Instance.Cameras)
            {
                for (int i = 0; i < 38; i++)
                {
                    AddNewCamera(GetNextCameraName(), null, true);
                    ReloadCameras();

                    yield return null;
                }
            }
        }
    }
}
