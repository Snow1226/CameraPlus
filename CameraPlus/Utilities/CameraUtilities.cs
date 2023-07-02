using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using IPA.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;
using CameraPlus.Configuration;
using CameraPlus.Behaviours;
using CameraPlus.HarmonyPatches;
using static CameraPlus.Behaviours.CameraMovement;
using System.Text.RegularExpressions;

namespace CameraPlus.Utilities
{
    public static class CameraUtilities
    {
        public static string ProfilePath = Path.Combine(UnityGame.UserDataPath, Plugin.Name, "Profiles");
        public static string ConfigPath = Path.Combine(UnityGame.UserDataPath, Plugin.Name);
        public static string ScriptPath = Path.Combine(UnityGame.UserDataPath, Plugin.Name, "Scripts");
        public static string CurrentlySelected = "None";
        public static string RootProfile = "__Root__";

        internal static float[] s_mouseMoveSpeed = { -0.01f, -0.01f };//x, y
        internal static float s_mouseScrollSpeed = 0.5f;
        internal static float[] s_mouseRotateSpeed = { -0.05f, 0.05f, 1f };//x, y, z

        public static int BaseCullingMask = 0;

        private static bool CameraExists(string cameraName)
        {
            string dictKey = (Plugin.cameraController.CurrentProfile == string.Empty ? $"{Plugin.Name}" : $"{Plugin.cameraController.CurrentProfile}");
            return Plugin.cameraController.Cameras.Keys.Where(c => c == $"{dictKey}_{cameraName}.json").Count() > 0;
        }

        internal static void AddNewCamera(string cameraName, CameraConfig CopyConfig = null, string cameraPath = "")
        {
            string path = Path.Combine(UnityGame.UserDataPath, Plugin.Name, $"{cameraName}.json");
            if (cameraPath != string.Empty)
                path = Path.Combine(cameraPath, $"{cameraName}.json");
            else if (Plugin.cameraController.CurrentProfile !=  string.Empty)
                path = Path.Combine(ProfilePath, Plugin.cameraController.CurrentProfile, $"{cameraName}.json");

            Plugin.Log.Notice($"Add New Camera : {path}");
            if (!File.Exists(path))
            {
                CameraConfig config = null;
                if (CopyConfig != null)
                    File.Copy(CopyConfig.FilePath, path, true);

                config = new CameraConfig(path);
                foreach (CameraPlusBehaviour c in Plugin.cameraController.Cameras.Values.OrderBy(i => i.Config.layer))
                {
                    if (c.Config.rawLayer > config.rawLayer)
                        config.rawLayer += (c.Config.rawLayer - config.rawLayer);
                    else if (c.Config.rawLayer == config.rawLayer)
                        config.rawLayer++;
                }

                if (cameraName == Plugin.MainCamera)
                    config.fitToCanvas = true;

                if (CopyConfig == null && cameraName != Plugin.MainCamera)
                {
                    config.screenHeight /= 4;
                    config.screenWidth /= 4;
                }

                config.Position = config.DefaultPosition;
                config.Rotation = config.DefaultRotation;
                config.FirstPersonPositionOffset = config.DefaultFirstPersonPositionOffset;
                config.FirstPersonRotationOffset = config.DefaultFirstPersonRotationOffset;
                config.Save();
                Plugin.Log.Notice($"Success creating new camera \"{cameraName}\"");
            }
            else
            {
                Plugin.Log.Notice($"Camera \"{cameraName}\" already exists!");
            }
        }

        internal static string GetNextCameraName()
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

        internal static bool RemoveCamera(CameraPlusBehaviour instance, bool delete = true)
        {
            try
            {
                if (Path.GetFileName(instance.Config.FilePath) != $"{Plugin.MainCamera}.json")
                {
                    if (Plugin.cameraController.Cameras.TryRemove(Plugin.cameraController.Cameras.Where(c => c.Value == instance && c.Key != $"{Plugin.MainCamera}.json")?.First().Key, out var removedEntry))
                    {
                        GL.Clear(false, true, Color.black, 0);
                        GameObject.Destroy(removedEntry.gameObject);
                        if (delete)
                        {
                            if (File.Exists(removedEntry.Config.FilePath))
                                File.Delete(removedEntry.Config.FilePath);
                        }
                        return true;
                    }
                }
                else
                {
                    Plugin.Log.Warn("One does not simply remove the main camera!");
                }
            }
            catch (Exception ex)
            {
                string msg
                    = ((instance != null && instance.Config != null && instance.Config.FilePath != null)
                    ? $"Could not remove camera with configuration: '{Path.GetFileName(instance.Config.FilePath)}'."
                    : $"Could not remove camera.");

                Plugin.Log.Error($"{msg} CameraUtilities.RemoveCamera() threw an exception:" +
                    $" {ex.Message}\n{ex.StackTrace}");
            }
            return false;
        }

        internal static bool RemoveProfileCameras(string profile)
        {
            try
            {
                string dictKey = (profile == string.Empty ? $"{Plugin.Name}_" : $"{profile}_");
                CameraPlusBehaviour _cameraPlus;
                foreach (var cam in Plugin.cameraController.Cameras)
                {
                    if (cam.Key.StartsWith(dictKey))
                    {
                        Plugin.cameraController.Cameras.TryRemove(cam.Key, out _cameraPlus);
                        if (File.Exists(_cameraPlus.Config.FilePath))
                            File.Delete(_cameraPlus.Config.FilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"CameraUtilities.RemoveProfileCameras() threw an exception:" +
                    $" {ex.Message}\n{ex.StackTrace}");
            }
            return false;
        }

        internal static void SetAllCameraCulling()
        {
            try
            {
                foreach (CameraPlusBehaviour c in Plugin.cameraController.Cameras.Values.ToArray())
                {
                    c.Config.SetCullingMask(c.Config.visibleObject);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Exception cameras culling! Exception:" +
                    $" {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static GameObject GetMainCamera()
        {
            var cam = Camera.main;
            if (cam == null)
                return GameObject.FindGameObjectsWithTag("MainCamera")[0];
            return cam.gameObject;
        }

        public static void LoadProfile(string profileName)
        {
            string workProfile;
            GameObject profileObject;

            workProfile = (profileName != string.Empty ? profileName : RootProfile);

            Plugin.Log.Notice($"Loading profile : {workProfile}");
            if (!Plugin.cameraController.LoadedProfile.ContainsKey(workProfile))
            {
                profileObject = new GameObject(workProfile);
                Plugin.cameraController.LoadedProfile.TryAdd(workProfile, profileObject);
                profileObject.name = workProfile;
                profileObject.transform.SetParent(Plugin.cameraController.gameObject.transform);
                profileObject.transform.localPosition = Vector3.zero;
                profileObject.transform.localRotation = Quaternion.identity;
            }
            else
            {
                profileObject = Plugin.cameraController.LoadedProfile[workProfile];
            }

            try
            {
                if (!Directory.Exists(ConfigPath))
                    Directory.CreateDirectory(ConfigPath);

                string[] files = (profileName != string.Empty ? Directory.GetFiles(Path.Combine(ProfilePath, profileName)) : Directory.GetFiles(ConfigPath));
                string fileName, dictKey;
                foreach (string filePath in files)
                {
                    Plugin.Log.Notice($"Loading profile cameras : {Path.GetFileName(filePath)}");

                    fileName = Path.GetFileName(filePath);
                    dictKey = (profileName == string.Empty ? $"{Plugin.Name}_{fileName}" : $"{profileName}_{fileName}");
                    if (fileName.EndsWith(".json") && !Plugin.cameraController.Cameras.ContainsKey(dictKey))
                    {
                        Plugin.Log.Notice($"Found new CameraConfig {filePath}!");

                        CameraConfig Config = new CameraConfig(filePath);
                        if (Config.configLoaded)
                        {
                            var cam = new GameObject(dictKey).AddComponent<CameraPlusBehaviour>();
                            cam.transform.SetParent(profileObject.transform);
                            cam.transform.localPosition = Vector3.zero;
                            cam.transform.localRotation = Quaternion.identity;
                            cam.Init(Config);
                            Plugin.cameraController.Cameras.TryAdd(dictKey, cam);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Exception while loading profile cameras! Exception:" +
                    $" {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static CameraPlusBehaviour TargetCameraPlus(string cameraName,string profileName = "")
        {
            string dictKey = (profileName == string.Empty ? $"{Plugin.Name}_{cameraName}" : $"{profileName}_{cameraName}");
            if(Plugin.cameraController.Cameras.ContainsKey(dictKey))
                return Plugin.cameraController.Cameras[dictKey] as CameraPlusBehaviour;
            else
                return null;
        }
        public static void TurnOffCameras()
        {
            foreach(var obj in Plugin.cameraController.LoadedProfile.Values)
                obj.SetActive(false);
        }

        public static string[] MovementScriptList()
        {
            string[] spath = Directory.GetFiles(Path.Combine(UnityGame.UserDataPath, Plugin.Name, "Scripts"), "*.json");
            List<string> list = new List<string>();
            list.Add("[ MovementScript Off ]");
            for (int i = 0; i < spath.Length; i++)
                list.Add(Path.GetFileName(spath[i]));
            return list.ToArray();
        }

        internal static string CurrentMovementScript(string scriptPath)
        {
            return Path.GetFileName(scriptPath);
        }

        public static string[] CameraSettingList(string profileName)
        {
            string[] files = (profileName != string.Empty ? Directory.GetFiles(Path.Combine(ProfilePath, profileName)) : Directory.GetFiles(ConfigPath));
            List<string> configList = new List<string>();
            foreach (string filePath in files)
                configList.Add(Path.GetFileName(filePath));
            return configList.ToArray();
        }

        public static string[] ProfileList()
        {
            List<string> profileList = new List<string>();
            profileList.Add(string.Empty);
            DirectoryInfo di = new DirectoryInfo(ProfilePath);
            DirectoryInfo[] profileDirs = di.GetDirectories();
            foreach(DirectoryInfo dir in profileDirs)
                profileList.Add(dir.Name);
            return profileList.ToArray();
        }

        internal static void ProfileChange(String ProfileName)
        {
            string profile = (ProfileName == string.Empty ? RootProfile : ProfileName);
            string currentprofile = (Plugin.cameraController.CurrentProfile == string.Empty ? RootProfile : Plugin.cameraController.CurrentProfile);
            Plugin.Log.Notice($"ProfileChange {currentprofile} to {profile}");
            if (!Plugin.cameraController.LoadedProfile.ContainsKey(profile))
            {
                LoadProfile(ProfileName);
            }
            Plugin.cameraController.LoadedProfile[currentprofile].SetActive(false);
            Plugin.cameraController.LoadedProfile[profile].SetActive(true);
            Plugin.cameraController.CurrentProfile = ProfileName;
        }

        public static void CreateExampleScript()
        {
            if (!Directory.Exists(ScriptPath))
                Directory.CreateDirectory(ScriptPath);
            string defaultScript = Path.Combine(ScriptPath, "ExampleMovementScript.json");
            if (!File.Exists(defaultScript))
                File.WriteAllBytes(defaultScript, CustomUtils.GetResource(Assembly.GetExecutingAssembly(), "CameraPlus.Resources.ExampleMovementScript.json"));
        }

        internal static void CreateMainDirectory()
        {
            if(!Directory.Exists(ProfilePath))
                Directory.CreateDirectory(ProfilePath);
            
            var a = new DirectoryInfo(ProfilePath).GetDirectories();
            if (a.Length > 0)
                CurrentlySelected = a.First().Name;
        }

        public static string SaveNewProfile()
        {
            string profileName = GetNextProfileName();
            string path = Path.Combine(ProfilePath, profileName);
            if (!Directory.Exists(path))
            {
                var di = Directory.CreateDirectory(path);
                CameraUtilities.AddNewCamera(Plugin.MainCamera, null, path);
                return profileName;
            }
            return string.Empty;
        }

        internal static string SaveAsCurrentProfile()
        {
            string cPath = ConfigPath;
            string profileName = GetNextProfileName();
            if (Plugin.cameraController.CurrentProfile != string.Empty)
            {
                cPath = Path.Combine(ProfilePath, Plugin.cameraController.CurrentProfile);
            }
            DirectoryCopy(cPath, Path.Combine(ProfilePath, profileName), false);
            return profileName;
        }


        internal static void DeleteProfile(string name)
        {
            if(name == string.Empty)
            {
                Plugin.Log.Notice("The default profile cannot be deleted.");
                return;
            }
            if (name == Plugin.cameraController.CurrentProfile)
                ProfileChange(string.Empty);
            if (Directory.Exists(Path.Combine(ProfilePath, name)))
            {
                RemoveProfileCameras(name);
                Plugin.cameraController.RemoveProfile(name);
                Directory.Delete(Path.Combine(ProfilePath, name), true);
            }
        }

        internal static string GetNextProfileName(string BaseName = "")
        {
            int max = 1;
            string folName = "CameraPlusProfile";
            string bname;
            if (BaseName == "")
                bname = "CameraPlusProfile";
            else
                bname = BaseName;
            DirectoryInfo dir = new DirectoryInfo(ProfilePath);
            DirectoryInfo[] dirs = dir.GetDirectories($"{bname}*");
            foreach (var dire in dirs)
            {
                var regex = Regex.Match(dire.Name, "([0-9]*$)");
                if (regex.Success)
                {
                    var no = int.Parse(regex.Groups[1].Value);
                    if (no >= max)
                        max = no + 1;
                }
            }
            folName = $"{folName}{max.ToString()}";
            Plugin.Log.Notice($"Next ProfileName : {folName}");
            return folName;
        }

        internal static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
                return;

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
        internal static void DirectoryCreate(string sourceDirName)
        {
            if (!Directory.Exists(sourceDirName))
                Directory.CreateDirectory(sourceDirName);
        }

        public static CameraPlusBehaviour GetTopmostInstanceAtCursorPos(Vector2 mousePos)
        {
            foreach (CameraPlusBehaviour c in Plugin.cameraController.Cameras.Values.ToArray())
            {
                if (c.IsTopmostRenderAreaAtPos(mousePos) && c.gameObject.activeInHierarchy)
                    return c;
            }
            return null;
        }

        public static string GetFullPath(this GameObject obj)
        {
            return GetFullPath(obj.transform);
        }

        public static string GetFullPath(this Transform t)
        {
            string path = t.name;
            var parent = t.parent;
            while (parent)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }
            return path;
        }
    }
}
