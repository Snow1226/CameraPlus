using System;
using System.IO;
using UnityEngine;
using IPA.Utilities;
using CameraPlus.Configuration;

namespace CameraPlus.Utilities
{
    internal static class ConfigConverter
    {
        private static string previousPath = Path.Combine(UnityGame.UserDataPath, $".{Plugin.Name.ToLower()}", "Profiles");
        private static string profilePath = Path.Combine(UnityGame.UserDataPath, Plugin.Name, "Profiles");
        private static string backupPath = Path.Combine(UnityGame.UserDataPath, Plugin.Name,"OldProfiles");

        internal static void ProfileConverter()
        {
            PreviousConfig previousConfig;
            CameraConfig cameraConfig;
#if DEBUG
            // ここでDirectoryNameを取得すると作ろうとしているDirectoryの名前を取得してしまうため、
            // 本来作ろうとしているDirectoryの一個上までしかDirectoryが作られない。
            Logger.Debug(Path.GetDirectoryName(previousPath));
            Logger.Debug(previousPath);
#endif
            // Directory作っちゃダメ？
            // ここでreturnしちゃうとDefaultConfigConverterでフォルダないなってて詰む
            if (!Directory.Exists(previousPath))
                Directory.CreateDirectory(previousPath);

#if DEBUG
            Logger.Debug(Path.GetDirectoryName(profilePath));
            Logger.Debug(profilePath);
#endif
            if (!Directory.Exists(profilePath))
                Directory.CreateDirectory(profilePath);
#if DEBUG
            Logger.Debug(Path.GetDirectoryName(backupPath));
            Logger.Debug(backupPath);
#endif
            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);

            DirectoryInfo dir = new DirectoryInfo(previousPath);
            foreach(var dirInfo in dir.GetDirectories())
            {
                if (!Directory.Exists(Path.Combine(backupPath, dirInfo.Name))){
                    foreach (FileInfo fi in dirInfo.GetFiles("*.cfg"))
                    {
                        previousConfig = new PreviousConfig(fi.FullName);
                        cameraConfig = PreviousConfigToCameraConfig(previousConfig,
                                Path.Combine(profilePath, dirInfo.Name,
                                $"{fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)}.json"));
                    }
                    Logger.log.Notice($"Profile Convert : {dirInfo.Name}");
                    if (!Directory.Exists(Path.Combine(backupPath, dirInfo.Name)))
                    {
                        CameraUtilities.DirectoryCopy(dirInfo.FullName, Path.Combine(backupPath, dirInfo.Name), true);
                        DirecrtoryDelete(dirInfo.FullName);
                    }
                }
            }
        }

        internal static void DefaultConfigConverter()
        {
            PreviousConfig pc;
            CameraConfig cc;
            DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(UnityGame.UserDataPath, Plugin.Name));
            foreach (FileInfo fi in dirInfo.GetFiles("*.cfg"))
            {
                pc = new PreviousConfig(fi.FullName);
                cc = PreviousConfigToCameraConfig(pc,
                        Path.Combine(UnityGame.UserDataPath, Plugin.Name,
                        $"{fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)}.json"));
                File.Copy(fi.FullName, Path.Combine(UnityGame.UserDataPath, Plugin.Name, "OldProfiles", fi.Name),true);
                File.Delete(fi.FullName);
                Logger.log.Notice($"Profile Convert : {fi.Name}");
            }
        }
        internal static CameraConfig PreviousConfigToCameraConfig(PreviousConfig cfg,string CameraConfigPath)
        {
            CameraConfig config = new CameraConfig(CameraConfigPath);
            config.thirdPerson = cfg.thirdPerson;
            config.fov = cfg.fov;
            config.layer = cfg.layer;
            config.antiAliasing = cfg.antiAliasing;
            config.renderScale = cfg.renderScale;

            config.fitToCanvas = cfg.fitToCanvas;
            config.screenPosX = cfg.screenPosX;
            config.screenPosY = cfg.screenPosY;
            config.screenWidth = cfg.screenWidth;
            config.screenHeight = cfg.screenHeight;

            config.Position = new Vector3(cfg.posx, cfg.posy, cfg.posz);
            config.Rotation = new Vector3(cfg.angx, cfg.angy, cfg.angz);
            config.FirstPersonPositionOffset = new Vector3(cfg.firstPersonPosOffsetX, cfg.firstPersonPosOffsetY, cfg.firstPersonPosOffsetZ);
            config.FirstPersonRotationOffset = new Vector3(cfg.firstPersonRotOffsetX, cfg.firstPersonRotOffsetY, cfg.firstPersonRotOffsetZ);
            config.TurnToHeadOffset = new Vector3(cfg.turnToHeadOffsetX, cfg.turnToHeadOffsetY, cfg.turnToHeadOffsetZ);

            config.layerSetting.previewCamera = cfg.showThirdPersonCamera;
            config.layerSetting.avatar = cfg.avatar;
            config.layerSetting.ui = !cfg.HideUI;
            config.layerSetting.wall = !cfg.transparentWalls;
            config.layerSetting.wallFrame = cfg.WallFrame;
            config.layerSetting.saber = cfg.Saber;
            config.layerSetting.notes = cfg.Notes;
            if (cfg.debri == "link")
                config.Debris = DebriVisibility.Link;
            else if(cfg.debri == "show")
                config.Debris = DebriVisibility.Visible;
            else
                config.Debris = DebriVisibility.Hidden;

            config.movementScript.movementScript = Path.GetFileName(cfg.movementScriptPath);
            config.movementScript.useAudioSync = cfg.movementAudioSync;
            config.movementScript.songSpecificScript = cfg.songSpecificScript;

            config.multiplayer.targetPlayerNumber = cfg.MultiPlayerNumber;
            config.multiplayer.displayPlayerInfo = cfg.DisplayMultiPlayerNameInfo;

            config.cameraExtensions.positionSmooth = cfg.positionSmooth;
            config.cameraExtensions.rotationSmooth = cfg.rotationSmooth;
            config.cameraExtensions.rotation360Smooth = cfg.cam360Smoothness;
            config.cameraExtensions.firstPersonCameraForceUpRight = cfg.forceFirstPersonUpRight;
            config.cameraExtensions.follow360map = cfg.use360Camera;
            config.cameraExtensions.follow360mapUseLegacyProcess = !cfg.cam360RotateControlNew;
            config.cameraExtensions.followNoodlePlayerTrack = cfg.NoodleTrack;
            config.cameraExtensions.turnToHead = cfg.turnToHead;

            if (cfg.VMCProtocolMode == "sender")
                config.vmcProtocol.mode = VMCProtocolMode.Sender;
            else if (cfg.VMCProtocolMode == "receiver")
                config.vmcProtocol.mode = VMCProtocolMode.Receiver;
            else
                config.vmcProtocol.mode = VMCProtocolMode.Disable;
            config.vmcProtocol.address = cfg.VMCProtocolAddress;
            config.vmcProtocol.port = cfg.VMCProtocolPort;

            config.cameraLock.lockScreen = cfg.LockScreen;
            config.cameraLock.lockCamera = cfg.LockCamera;
            config.cameraLock.dontSaveDrag = cfg.LockCameraDrag;
            config.Save();
            return config;
        }

        private static void DirecrtoryDelete(string targetDirectoryPath)
        {
            if (!Directory.Exists(targetDirectoryPath))
                return;

            string[] filePaths = Directory.GetFiles(targetDirectoryPath);
            foreach (string filePath in filePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }

            string[] directoryPaths = Directory.GetDirectories(targetDirectoryPath);
            foreach (string directoryPath in directoryPaths)
            {
                DirecrtoryDelete(directoryPath);
            }
            Directory.Delete(targetDirectoryPath, false);
        }
    }
}
