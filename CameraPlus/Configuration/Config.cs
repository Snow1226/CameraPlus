using System;
using System.IO;
using UnityEngine;
using CameraPlus.Camera2Utils;

namespace CameraPlus.Configuration
{
    public class Config
    {
        public string FilePath { get; }
        public bool LockScreen = false;
        public bool LockCamera = false;
        public bool LockCameraDrag = false;
        public float fov = 50;
        public int antiAliasing = 2;
        public float renderScale = 1;
        public float positionSmooth = 10;
        public float rotationSmooth = 5;
        public float cam360Smoothness = 2;

        public bool cam360RotateControlNew = true;

        public bool thirdPerson = false;
        public bool showThirdPersonCamera = true;
        public bool use360Camera = false;
        public bool turnToHead = false;

        public float posx;
        public float posy = 2;
        public float posz = -3.0f;
        public float angx = 15;
        public float angy;
        public float angz;

        public float firstPersonPosOffsetX;
        public float firstPersonPosOffsetY;
        public float firstPersonPosOffsetZ;
        public float firstPersonRotOffsetX;
        public float firstPersonRotOffsetY;
        public float firstPersonRotOffsetZ;

        public float turnToHeadOffsetX;
        public float turnToHeadOffsetY;
        public float turnToHeadOffsetZ;

        public bool NoodleTrack = false;

        public int screenWidth = Screen.width;
        public int screenHeight = Screen.height;
        public int screenPosX;
        public int screenPosY;

        public int MultiPlayerNumber = 0;
        public bool DisplayMultiPlayerNameInfo = false;

        public int layer = -1000;

        public bool fitToCanvas = false;
        public bool transparentWalls = false;
        public bool forceFirstPersonUpRight = false;
        public bool avatar = true;
        public string debri = "link";
        public bool HideUI = false;
        public bool Notes = true;
        public bool Saber = true;
        public string movementScriptPath = String.Empty;
        public bool movementAudioSync = true;
        public bool songSpecificScript = false;
        public string VMCProtocolMode = "disable";
        public string VMCProtocolAddress = "127.0.0.1";
        public int VMCProtocolPort = 39540;
        //public int maxFps = 90;

        // Greenscreen Settings
        public string GreenScreenMode = "off";
        public bool GreenScreenSaber = true;
        public bool GreenScreenNotes = false;
        public bool GreenScreenAvatar = true;
        public string GreenScreenDebri = "link";

        public event Action<Config> ConfigChangedEvent;

        private readonly FileSystemWatcher _configWatcher;
        private bool _saving;

        public Vector2 ScreenPosition {
            get {
                return new Vector2(screenPosX, screenPosY);
            }
        }

        public Vector2 ScreenSize {
            get {
                return new Vector2(screenWidth, screenHeight);
            }
        }

        public Vector3 Position {
            get {
                return new Vector3(posx, posy, posz);
            }
            set {
                posx = value.x;
                posy = value.y;
                posz = value.z;
            }
        }

        public Vector3 DefaultPosition {
            get {
                return new Vector3(0f, 2f, -1.2f);
            }
        }

        public Vector3 Rotation {
            get {
                return new Vector3(angx, angy, angz);
            }
            set {
                angx = value.x;
                angy = value.y;
                angz = value.z;
            }
        }

        public Vector3 DefaultRotation {
            get {
                return new Vector3(15f, 0f, 0f);
            }
        }

        public Vector3 FirstPersonPositionOffset {
            get {
                return new Vector3(firstPersonPosOffsetX, firstPersonPosOffsetY, firstPersonPosOffsetZ);
            }
            set {
                firstPersonPosOffsetX = value.x;
                firstPersonPosOffsetY = value.y;
                firstPersonPosOffsetZ = value.z;
            }
        }
        public Vector3 FirstPersonRotationOffset {
            get {
                return new Vector3(firstPersonRotOffsetX, firstPersonRotOffsetY, firstPersonRotOffsetZ);
            }
            set {
                firstPersonRotOffsetX = value.x;
                firstPersonRotOffsetY = value.y;
                firstPersonRotOffsetZ = value.z;
            }
        }

        public Vector3 DefaultFirstPersonPositionOffset {
            get {
                return new Vector3(0, 0, 0);
            }
        }
        public Vector3 DefaultFirstPersonRotationOffset {
            get {
                return new Vector3(0, 0, 0);
            }
        }

        public Vector3 TurnToHeadOffset
        {
            get
            {
                return new Vector3(turnToHeadOffsetX, turnToHeadOffsetY, turnToHeadOffsetZ);
            }
            set
            {
                turnToHeadOffsetX = value.x;
                turnToHeadOffsetY = value.y;
                turnToHeadOffsetZ = value.z;
            }
        }

        public Config(string filePath)
        {
            FilePath = filePath;

            if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            if (File.Exists(FilePath))
            {
                Load();
                var text = File.ReadAllText(FilePath);
                if (!text.Contains("fitToCanvas") && Path.GetFileName(FilePath) == $"{Plugin.MainCamera}.cfg")
                {
                    fitToCanvas = true;
                }
                if (text.Contains("rotx"))
                {
                    var oldRotConfig = new OldRotConfig();
                    ConfigSerializer.LoadConfig(oldRotConfig, FilePath);

                    var euler = new Quaternion(oldRotConfig.rotx, oldRotConfig.roty, oldRotConfig.rotz, oldRotConfig.rotw).eulerAngles;
                    angx = euler.x;
                    angy = euler.y;
                    angz = euler.z;
                }
            }
            Save();

            _configWatcher = new FileSystemWatcher(Path.GetDirectoryName(FilePath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(FilePath),
                EnableRaisingEvents = true
            };
            _configWatcher.Changed += ConfigWatcherOnChanged;
        }

        ~Config()
        {
            _configWatcher.Changed -= ConfigWatcherOnChanged;
        }

        public void Save()
        {
            _saving = true;
            ConfigSerializer.SaveConfig(this, FilePath);
            // sets saving back to false cause SaveConfig wont write the file at all if nothing has changed
            // and if so the FileWatcher would not get triggered so saving would stuck at true
            _saving = false;
        }

        public void Load()
        {
            ConfigSerializer.LoadConfig(this, FilePath);
        }

        private void ConfigWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (_saving)
            {
                _saving = false;
                return;
            }

            Load();

            if (ConfigChangedEvent != null)
            {
                ConfigChangedEvent(this);
            }
        }

        public class OldRotConfig
        {
            public float rotx;
            public float roty;
            public float rotz;
            public float rotw;
        }

        public void ConvertFromCamera2(Camera2Config config2)
        {
            fov = config2.FOV;
            antiAliasing = config2.antiAliasing;
            renderScale = config2.renderScale;
            cam360Smoothness = config2.follow360.smoothing;
            if (config2.type == "FirstPerson")
                thirdPerson = false;
            else
                thirdPerson = true;
            if (config2.worldCamVisibility != "Hidden")
                showThirdPersonCamera = true;
            else
                showThirdPersonCamera = false;
            use360Camera = config2.follow360.enabled;
            posx = firstPersonPosOffsetX = config2.targetPos.x;
            posy = firstPersonPosOffsetY = config2.targetPos.y;
            posz = firstPersonPosOffsetZ = config2.targetPos.z;
            angx = firstPersonRotOffsetX = config2.targetRot.x;
            angy = firstPersonRotOffsetY = config2.targetRot.y;
            angz = firstPersonRotOffsetZ = config2.targetRot.z;
            NoodleTrack = config2.modmapExtensions.moveWithMap;
            screenWidth = (int)config2.viewRect.width;
            if (screenWidth <= 0) screenWidth = Screen.width;
            screenHeight = (int)config2.viewRect.height;
            if (screenHeight <= 0) screenHeight = Screen.height;
            screenPosX = (int)config2.viewRect.x;
            screenPosY = (int)config2.viewRect.y;
            layer = config2.layer;
            fitToCanvas = false;
            if (config2.visibleObject.Walls == "Visible")
                transparentWalls = false;
            else
                transparentWalls = true;
            avatar = config2.visibleObject.Avatar;
            if (!config2.visibleObject.Debris)
                debri = "show";
            else
                debri = "hide";
            HideUI = config2.visibleObject.UI;
            forceFirstPersonUpRight = config2.Smoothfollow.forceUpright;
        }

        public Camera2Config ConvertToCamera2()
        {
            Camera2Config config2 = new Camera2Config();
            config2.visibleObject = new visibleObjectsElement();
            config2.viewRect = new viewRectElement();
            config2.Smoothfollow = new SmoothfollowElement();
            config2.follow360 = new Follow360Element();
            config2.modmapExtensions = new ModmapExtensionsElement();
            config2.targetPos = new targetPosElement();
            config2.targetRot = new targetRotElement();
            if (transparentWalls)
                config2.visibleObject.Walls = Camera2Utils.WallVisiblity.Transparent.ToString();
            else
                config2.visibleObject.Walls = Camera2Utils.WallVisiblity.Visible.ToString();
            if (debri == "show")
                config2.visibleObject.Debris = true;
            else
                config2.visibleObject.Debris = false;
            config2.visibleObject.UI = HideUI;
            config2.visibleObject.Avatar = avatar;
            if (thirdPerson)
                config2.type = Camera2Utils.CameraType.Positionable.ToString();
            else
                config2.type = Camera2Utils.CameraType.FirstPerson.ToString();
            config2.FOV = fov;
            config2.layer = layer;
            config2.renderScale = (renderScale >= 0.99f) ? Math.Max(1.2f, renderScale) : renderScale;
            config2.antiAliasing = (renderScale >= 0.99f) ? Math.Max(antiAliasing, 2) : antiAliasing;
            config2.viewRect.x = screenPosX;
            config2.viewRect.y = screenPosY;
            config2.viewRect.width = fitToCanvas ? -1 : screenWidth;
            config2.viewRect.height = fitToCanvas ? -1 : screenHeight;
            config2.Smoothfollow.position = positionSmooth;
            config2.Smoothfollow.rotation = rotationSmooth;
            config2.Smoothfollow.forceUpright = forceFirstPersonUpRight;
            config2.follow360.enabled = use360Camera;
            config2.follow360.smoothing = cam360Smoothness;
            config2.modmapExtensions.moveWithMap = NoodleTrack;
            config2.targetPos.x = thirdPerson ? posx : firstPersonPosOffsetX;
            config2.targetPos.y = thirdPerson ? posy : firstPersonPosOffsetY;
            config2.targetPos.z = thirdPerson ? posz : firstPersonPosOffsetZ;
            config2.targetRot.x = thirdPerson ? angx : firstPersonRotOffsetX;
            config2.targetRot.y = thirdPerson ? angy : firstPersonRotOffsetY;
            config2.targetRot.z = thirdPerson ? angz : firstPersonRotOffsetZ;
            return config2;
        }
    }
}