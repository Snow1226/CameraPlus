﻿using System;
using System.IO;
using IPA.Utilities;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using CameraPlus.Behaviours;
using CameraPlus.Utilities;
using CameraPlus.Configuration;

namespace CameraPlus.Camera2Utils
{
    public static class Camera2ConfigExporter
    {
        public static string currentlyScenesSelected = "None";
        private static string cam2Path = Path.Combine(UnityGame.UserDataPath, "Camera2");
        private static List<SceneTypes> enumScenes = null;

        public static void Init()
        {
            enumScenes = Enum.GetValues(typeof(SceneTypes)).Cast<SceneTypes>().ToList();
            if (enumScenes.Count > 0)
                currentlyScenesSelected = enumScenes.First().ToString();
        }
        public static void ExportCamera2Scene()
        {
            Camera2Scenes camera2Scenes = JsonConvert.DeserializeObject<Camera2Scenes>(File.ReadAllText(Path.Combine(cam2Path, "Scenes.json")));

            var cameraList = Camera2CameraExporter(CameraUtilities.currentlySelected);
            SceneTypes selectedSceneType = enumScenes.Find(x => x.ToString() == currentlyScenesSelected);
            camera2Scenes.scenes[selectedSceneType] = cameraList;

            File.WriteAllText(Path.Combine(cam2Path, "Scenes.json"), JsonConvert.SerializeObject(camera2Scenes, Formatting.Indented));
        }

        public static void LoadCamera2Scene()
        {
            var a = enumScenes.Where(x => x.ToString() == currentlyScenesSelected);
            SceneTypes s = enumScenes.ElementAtOrDefault(enumScenes.ToList().IndexOf(a.First()));
            Camera2Scenes camera2Scenes = JsonConvert.DeserializeObject<Camera2Scenes>(File.ReadAllText(Path.Combine(cam2Path, "Scenes.json")));
            List<string> cameraList = camera2Scenes.scenes[s];
            if (cameraList.Count > 0)
            {
                string ProfileName = CameraUtilities.GetNextProfileName(s.ToString());
                if (ProfileName == string.Empty)
                {
                    Plugin.Log.Error("No ProfileName in LoadCamera2Scene");
                    return;
                }
                CameraUtilities.DirectoryCreate(Path.Combine(CameraUtilities.profilePath, ProfileName));
                for (int i = 0; i < cameraList.Count; i++)
                    Camera2ConfigLoader(Path.Combine(cam2Path,"Cameras", $"{cameraList[i]}.json"), ProfileName);
            }
            else
                Plugin.Log.Error("No Camera Data from Camera2 Scene");
        }
        public static void SetSceneNext(string now = null)
        {
            int index = 0;
            var a = enumScenes.Where(x => x.ToString() == now);
            if (a.Count() > 0)
            {
                index = enumScenes.ToList().IndexOf(a.First());
                if (index < enumScenes.Count() - 1)
                    currentlyScenesSelected = enumScenes.ElementAtOrDefault(index + 1).ToString();
                else
                    currentlyScenesSelected = enumScenes.ElementAtOrDefault(0).ToString();
            }
            else
            {
                currentlyScenesSelected = "None";
                if (enumScenes.Count > 0)
                    currentlyScenesSelected = enumScenes.First().ToString();
            }
        }
        public static void TrySceneSetLast(string now = null)
        {
            int index = 0;
            var a = enumScenes.Where(x => x.ToString() == now);
            if (a.Count() > 0)
            {
                index = enumScenes.ToList().IndexOf(a.First());
                if (index == 0 && enumScenes.Count >= 2)
                    currentlyScenesSelected = enumScenes.ElementAtOrDefault(enumScenes.Count() - 1).ToString();
                else if (index < enumScenes.Count() && enumScenes.Count >= 2)
                    currentlyScenesSelected = enumScenes.ElementAtOrDefault(index - 1).ToString();
                else
                    currentlyScenesSelected = enumScenes.ElementAtOrDefault(0).ToString();
            }
            else
            {
                currentlyScenesSelected = "None";
                if (enumScenes.Count > 0)
                    currentlyScenesSelected = enumScenes.First().ToString();
            }
        }
        private static string GetNextCameraName(string ProfileName="")
        {
            if (ProfileName == "") return string.Empty;
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(CameraUtilities.profilePath, ProfileName));
            FileInfo[] files = dir.GetFiles();
            if (files.Length <= 0)
            {
                Plugin.Log.Notice("Return MainCameraName in GetNextCamera2Name");
                return $"{Plugin.MainCamera}";
            }
            int index = 1;
            string cameraName = String.Empty;
            while (true)
            {
                cameraName = $"customcamera{index.ToString()}";
                if (!(files.Where(c=> c.Name==$"{cameraName}.json").Count()>0))
                    break;
                index++;
            }
            return cameraName;
        }
        private static void Camera2ConfigLoader(string jsonName,string ProfileName="")
        {
            string path="";
            string cameraName="";
            if (ProfileName != "")
            {
                cameraName = GetNextCameraName(ProfileName);
                path = Path.Combine(CameraUtilities.profilePath, ProfileName, $"{cameraName}.json");
            }
            else
            {
                cameraName = CameraUtilities.GetNextCameraName();
                Plugin.Log.Notice($"Adding new config with name {cameraName}.json");

                path = Path.Combine(UnityGame.UserDataPath, Plugin.Name, $"{cameraName}.json");
                if (!PluginConfig.Instance.ProfileLoadCopyMethod && Plugin.cameraController.currentProfile != null)
                    path = Path.Combine(UnityGame.UserDataPath, "." + Plugin.Name.ToLower(), "Profiles", Plugin.cameraController.currentProfile, $"{cameraName}.json");
            }
            Plugin.Log.Notice($"Try Adding {path}");

            CameraConfig config = new CameraConfig(path);
            Camera2Config config2 = null;
            string jsonConfig = File.ReadAllText(Path.Combine(cam2Path,"Cameras", jsonName));
            try
            {
                config2 = JsonConvert.DeserializeObject<Camera2Config>(jsonConfig);
            }
            catch(Exception ex)
            {
                Plugin.Log.Notice($"DeserializeObjectError {ex.Message}");
            }
            config.ConvertFromCamera2(config2);

            foreach (CameraPlusBehaviour c in Plugin.cameraController.Cameras.Values.OrderBy(i => i.Config.layer))
            {
                if (c.Config.layer > config.layer)
                    config.layer += (c.Config.layer - config.layer);
                else if (c.Config.layer == config.layer)
                    config.layer++;
            }
            //config.Save();
            CameraUtilities.ReloadCameras();
        }

        private static string GetNextCamera2Name(string cam2CameraName = "")
        {
            string cam2Name = cam2CameraName;
            if (cam2CameraName == "") 
                cam2Name = "CameraPlus-cam";
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(cam2Path, "Cameras"));
            FileInfo[] files = dir.GetFiles($"{cam2Name}*");
            int index = 1;
            string cameraName = String.Empty;
            while (true)
            {
                cameraName = $"{cam2Name}{index.ToString()}";
                if (!(files.Where(c => c.Name == $"{cameraName}.json").Count() > 0))
                    break;
                index++;
            }
            return cameraName;
        }
        private static List<string> Camera2CameraExporter(string ProfileName = "")
        {
            List<string> cameraList = new List<string>();
            string camName;
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(CameraUtilities.profilePath, ProfileName));
            FileInfo[] files = dir.GetFiles("*.json");
            if (files.Length > 0)
            {
                foreach(FileInfo file in files)
                {
                    camName = GetNextCamera2Name(ProfileName);
                    File.WriteAllText(Path.Combine(cam2Path,"Cameras", $"{camName}.json"), 
                        JsonConvert.SerializeObject(new CameraConfig(file.FullName).ConvertToCamera2(), Formatting.Indented));
                    cameraList.Add(camName);
                }
            }
            return cameraList;
        }
    }
}
