using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using CameraPlus.Behaviours;
using CameraPlus.HarmonyPatches;
using CameraPlus.Utilities;
using CameraPlus.Camera2Utils;

namespace CameraPlus.Configuration
{
    internal enum CameraType
    {
        FirstPerson,
        ThirdPerson
    }
    public enum DebriVisibility
    {
        Visible,
        Hidden,
        Link
    }
    internal enum VMCProtocolMode
    {
        Disable,
        Sender,
        Receiver
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class CameraConfig
    {
        internal CameraPlusBehaviour cam = null;
        internal string FilePath { get; }

        [JsonConverter(typeof(StringEnumConverter)), JsonProperty("CameraType")]
        private CameraType _cameraType = CameraType.ThirdPerson;
        [JsonProperty("FieldOfView")]
        private float _fieldOfView = 60;
        [JsonProperty("VisibleObject")]
        private visibleObjectsElements _visibleObject = new visibleObjectsElements();
        [JsonProperty("Layer")]
        private int _layer = -1000;
        [JsonProperty("AntiAliasing")]
        private int _antiAliasing = 2;
        [JsonProperty("RenderScale")]
        private float _renderScale = 1;
        [JsonProperty("WindowRect")]
        private windowRectElement _windowRect = new windowRectElement();

        [JsonProperty("ThirdPersonPos")]
        private targetTransformElements _thirdPersonPos = new targetTransformElements();
        [JsonProperty("ThirdPersonRot")]
        private targetTransformElements _thirdPersonRot = new targetTransformElements();

        [JsonProperty("FirstPersonPos")]
        private targetTransformElements _firstPersonPos = new targetTransformElements();
        [JsonProperty("FirstPersonRot")]
        private targetTransformElements _firstPersonRot = new targetTransformElements();

        [JsonProperty("PreviewQuadPos")]
        private targetTransformElements _previewQuadPos = new targetTransformElements();
        [JsonProperty("PreviewQuadRot")]
        private targetTransformElements _previewQuadRot = new targetTransformElements();

        [JsonProperty("TurnToHeadOffset")]
        private targetTransformElements _turnToHeadOffset = new targetTransformElements();
        [JsonProperty("MovementScript")]
        private movementScriptElements _movementScript = new movementScriptElements();
        [JsonProperty("CameraLock")]
        private cameraLockElements _cameraLock = new cameraLockElements();
        [JsonProperty("CameraExtensions")]
        private cameraExtensionsElements _cameraExtensions = new cameraExtensionsElements();
        [JsonProperty("Multiplayer")]
        private multiplayerElemetns _multiplayer = new multiplayerElemetns();
        [JsonProperty("VMCProtocol")]
        private vmcProtocolElements _vmcProtocol = new vmcProtocolElements();
        [JsonProperty("WebCamera")]
        private webCameraElements _webCameraElements = new webCameraElements();
        [JsonProperty("CameraEffect")]
        private CameraEffectStruct _cameraEffect = new CameraEffectStruct
        {
            enableDOF = false,
            dofFocusDistance = 0,
            dofFocusRange = 1,
            dofBlurRadius = 5,
            dofAutoDistance = false,

            wipeType = "Circle",
            wipeCircleCenter = new Vector4(0, 0, 0, 0),
            wipeProgress = 0,

            enableOutline = false,
            outlineOnly = 0,
            outlineColor = Color.black,
            outlineBGColor = Color.white,

            enableGlitch = false,
            glitchLineSpeed = 5,
            glitchLineSize = 0.01f,
            glitchColorGap = 0.01f,
            glitchFrameRate = 15,
            glitchFrequency = 0.1f,
            glitchScale = 1
        };
        [JsonProperty("Spout")]
        private SpoutCameraElements _spout = new SpoutCameraElements();
        //public string spoutReceiverName = "";

        public bool thirdPerson
        {
            get
            {
                return _cameraType == CameraType.ThirdPerson;
            }
            set
            {
                if (value)
                    _cameraType = CameraType.ThirdPerson;
                else
                    _cameraType = CameraType.FirstPerson;
                if (cam != null)
                {
                    cam._quad.gameObject.SetActive(thirdPerson && PreviewCamera);
                    SetCullingMask(visibleObject);
                }
            }
        }
        public float fov { get => _fieldOfView; set { _fieldOfView = value; } }
        public int layer
        {
            get
            {
                if (FPFCPatch.instance != null)
                    return _layer + 1002;
                else
                    return _layer;
            }
            set
            {
                _layer = value;
                Plugin.cameraController.ScreenCamera.SortCamera();
            }
        }
        public int rawLayer
        {
            get => _layer;
            set
            {
                _layer = value;
                Plugin.cameraController.ScreenCamera.SortCamera();
            }
        }
        public int antiAliasing { get => _antiAliasing; set { _antiAliasing = value; } }
        public float renderScale { get => _renderScale; set { _renderScale = value; } }
        public bool fitToCanvas { get => _windowRect.fitToCanvas; set { _windowRect.fitToCanvas = value; } }

        private Rect _rect = new Rect(0, 0, 0, 0);
        public Rect rect
        {
            get
            {
                _rect.x = _windowRect.x;
                _rect.y = _windowRect.y;
                _rect.width = _windowRect.width;
                _rect.height = _windowRect.height;
                return _rect;
            }
        }
        public int screenPosX { get => _windowRect.x; set { _windowRect.x = value; } }
        public int screenPosY { get => _windowRect.y; set { _windowRect.y = value; } }
        public int screenWidth { get => _windowRect.width; set { _windowRect.width = value; } }
        public int screenHeight { get => _windowRect.height; set { _windowRect.height = value; } }
        public float posx { get => _thirdPersonPos.x; set { _thirdPersonPos.x = value; } }
        public float posy { get => _thirdPersonPos.y; set { _thirdPersonPos.y = value; } }
        public float posz { get => _thirdPersonPos.z; set { _thirdPersonPos.z = value; } }
        public targetTransformElements thirdPersonPos { get => _thirdPersonPos; set { _thirdPersonPos = value; } }
        public targetTransformElements thirdPersonRot { get => _thirdPersonRot; set { _thirdPersonRot = value; } }
        public targetTransformElements firstPersonPos { get => _firstPersonPos; set { _firstPersonPos = value; } }
        public targetTransformElements firstPersonRot { get => _firstPersonRot; set { _firstPersonRot = value; } }
        public targetTransformElements previewQuadPos { get => _previewQuadPos; set { _previewQuadPos = value; } }
        public targetTransformElements previewQuadRot { get => _previewQuadRot; set { _previewQuadRot = value; } }
        public targetTransformElements turnToHeadOffsetTransform { get => _turnToHeadOffset; set { _turnToHeadOffset = value; } }
        public visibleObjectsElements layerSetting { get => _visibleObject; set { _visibleObject = value; } }

        public VisibleObject visibleObject { get
            {
                VisibleObject v = new VisibleObject();
                v.avatar = _visibleObject.avatar;
                v.ui = _visibleObject.ui;
                v.notes = _visibleObject.notes;
                v.wall = _visibleObject.wall;
                v.wallFrame = _visibleObject.wallFrame;
                v.saber = _visibleObject.saber;
                if (_visibleObject.debris == DebriVisibility.Link && PlayerSettingPatch.playerSetting != null)
                    v.debris = !PlayerSettingPatch.playerSetting.reduceDebris;
                else
                    v.debris = _visibleObject.debris == DebriVisibility.Visible ? true : false;
                return v;   
            }
        }
        public movementScriptElements movementScript { get => _movementScript; set { _movementScript = value; } }
        public cameraLockElements cameraLock { get => _cameraLock; set { _cameraLock = value; } }
        public cameraExtensionsElements cameraExtensions { get => _cameraExtensions; set { _cameraExtensions = value; } }
        public multiplayerElemetns multiplayer { get => _multiplayer; set { _multiplayer = value; } }
        public vmcProtocolElements vmcProtocol { get => _vmcProtocol; set { _vmcProtocol = value; } }
        public webCameraElements webCamera { get => _webCameraElements; set { _webCameraElements = value; } }
        public SpoutCameraElements spout { get => _spout; set { _spout = value; } }
        public CameraEffectStruct cameraEffect { get => _cameraEffect; set { _cameraEffect = value; } }

        public bool DoFEnable { get => _cameraEffect.enableDOF; set { _cameraEffect.enableDOF = value; } }
        public float DoFFocusDistance { get => _cameraEffect.dofFocusDistance; set { _cameraEffect.dofFocusDistance = value; } }
        public float DoFFocusRange { get => _cameraEffect.dofFocusRange; set { _cameraEffect.dofFocusRange = value; } }
        public float DoFBlurRadius { get => _cameraEffect.dofBlurRadius; set { _cameraEffect.dofBlurRadius = value; } }
        public bool DoFAutoDistance { get => _cameraEffect.dofAutoDistance; set { _cameraEffect.dofAutoDistance = value; } }

        public float WipeProgress { get => _cameraEffect.wipeProgress; set { _cameraEffect.wipeProgress = value; } }
        public string WipeType { get => _cameraEffect.wipeType; set { _cameraEffect.wipeType = value; } }
        public Vector4 WipeCircleCenter { get => _cameraEffect.wipeCircleCenter; set { _cameraEffect.wipeCircleCenter = value; } }
        public float[] WipeCircleCenterValue { get => _cameraEffect.wipeCircleCenterValue; set => _cameraEffect.wipeCircleCenterValue = value; }

        public bool OutlineEnable { get => _cameraEffect.enableOutline; set { _cameraEffect.enableOutline = value; } }
        public float OutlineOnly { get => _cameraEffect.outlineOnly; set { _cameraEffect.outlineOnly = value; } }
        public Color OutlineColor { get => _cameraEffect.outlineColor; set { _cameraEffect.outlineColor = value; } }
        public Color OutlineBackgroundColor { get => _cameraEffect.outlineBGColor; set { _cameraEffect.outlineBGColor = value; } }
        public float[] OutlineColorValue { get => _cameraEffect.outlineColorValue; set { _cameraEffect.outlineColorValue = value; } }
        public float[] OutlineBGColorValue { get => _cameraEffect.outlineBGColorValue; set { _cameraEffect.outlineBGColorValue = value; } }

        public bool GlitchEnable { get => _cameraEffect.enableGlitch; set { _cameraEffect.enableGlitch = value; } }
        public float GlitchLineSpeed { get => _cameraEffect.glitchLineSpeed; set { _cameraEffect.glitchLineSpeed = value; } }
        public float GlitchLineSize { get => _cameraEffect.glitchLineSize; set { _cameraEffect.glitchLineSize = value; } }
        public float GlitchColorGap { get => _cameraEffect.glitchColorGap; set { _cameraEffect.glitchColorGap = value; } }
        public float GlitchFrameRate { get => _cameraEffect.glitchFrameRate; set { _cameraEffect.glitchFrameRate = value; } }
        public float GlitchFrequency { get => _cameraEffect.glitchFrequency; set { _cameraEffect.glitchFrequency = value; } }
        public float GlitchScale { get => _cameraEffect.glitchScale; set { _cameraEffect.glitchScale = value; } }
        public float[] GlitchValue { get => _cameraEffect.glitchValue; set { _cameraEffect.glitchValue = value; } }
        public bool PreviewCamera
        {
            get => _cameraExtensions.previewCamera;
            set
            {
                _cameraExtensions.previewCamera = value;
                cam?._quad.gameObject.SetActive(thirdPerson && PreviewCamera);
            }
        }
        public bool PreviewCameraVROnly
        {
            get => _cameraExtensions.previewCameraVROnly;
            set
            {
                _cameraExtensions.previewCameraVROnly = value;
                cam._quad.IsDisplayMaterialVROnly = value;
            }
        }
        public bool PreviewCameraQuadOnly
        {
            get => _cameraExtensions.previewCameraQuadOnly;
            set
            {
                _cameraExtensions.previewCameraQuadOnly = value;
                cam?._quad.CubeDisplay(!value);
            }
        }
        public bool DontDrawDesktop
        {
            get => _cameraExtensions.dontDrawDesktop;
            set
            {
                _cameraExtensions.dontDrawDesktop = value;
                if (!value)
                    Plugin.cameraController.ScreenCamera.RegistrationCamera(cam);
                else
                    Plugin.cameraController.ScreenCamera.UnregistrationCamera(cam);
            }
        }
        public bool Avatar { get => _visibleObject.avatar; set { _visibleObject.avatar = value; SetCullingMask(visibleObject); } }
        public bool UI { get => _visibleObject.ui; set { _visibleObject.ui = value; SetCullingMask(visibleObject); } }
        public bool Wall { get => _visibleObject.wall; set { _visibleObject.wall = value; SetCullingMask(visibleObject); } }
        public bool WallFrame { get => _visibleObject.wallFrame; set { _visibleObject.wallFrame = value; SetCullingMask(visibleObject); } }
        public bool Saber { get => _visibleObject.saber; set { _visibleObject.saber = value; SetCullingMask(visibleObject); } }
        public bool CutParticles { get => _visibleObject.cutParticles; set { _visibleObject.cutParticles = value; SetCullingMask(visibleObject); } }
        public bool Notes { get => _visibleObject.notes; set { _visibleObject.notes = value; SetCullingMask(visibleObject); } }
        public DebriVisibility Debris { get => _visibleObject.debris; set { _visibleObject.debris = value; SetCullingMask(visibleObject); } }

        private bool _saving = false;
        public event Action<CameraConfig> ConfigChangedEvent;
        private readonly FileSystemWatcher _configWatcher;
        internal bool configLoaded = false;
        public Vector2 ScreenPosition
        {
            get
            {
                return new Vector2(_windowRect.x, _windowRect.y);
            }
        }
        public Vector2 ScreenSize
        {
            get
            {
                return new Vector2(_windowRect.width, _windowRect.height);
            }
        }
        public Vector3 Position
        {
            get
            {
                return new Vector3(_thirdPersonPos.x, _thirdPersonPos.y, _thirdPersonPos.z);
            }
            set
            {
                _thirdPersonPos.x = value.x;
                _thirdPersonPos.y = value.y;
                _thirdPersonPos.z = value.z;
            }
        }
        public float[] ThirdPersonPositionFloat
        {
            get
            {
                float[] result = new float[3];
                result[0] = _thirdPersonPos.x;
                result[1] = _thirdPersonPos.y;
                result[2] = _thirdPersonPos.z;
                return result;
            }
            set
            {
                _thirdPersonPos.x = value[0];
                _thirdPersonPos.y = value[1];
                _thirdPersonPos.z = value[2];
            }
        }
        public Vector3 Rotation
        {
            get
            {
                return new Vector3(_thirdPersonRot.x, _thirdPersonRot.y, _thirdPersonRot.z);
            }
            set
            {
                _thirdPersonRot.x = value.x;
                _thirdPersonRot.y = value.y;
                _thirdPersonRot.z = value.z;
            }
        }
        public float[] ThirdPersonRotationFloat
        {
            get
            {
                float[] result = new float[3];
                result[0] = _thirdPersonRot.x;
                result[1] = _thirdPersonRot.y;
                result[2] = _thirdPersonRot.z;
                return result;
            }
            set
            {
                _thirdPersonRot.x = value[0];
                _thirdPersonRot.y = value[1];
                _thirdPersonRot.z = value[2];
            }
        }
        public float RotationZ
        {
            get { return _thirdPersonRot.z; }
            set { _thirdPersonRot.z = value; }
        }
        public Vector3 DefaultPosition
        {
            get
            {
                return new Vector3(0f, 2f, -1.2f);
            }
        }
        public Vector3 DefaultRotation
        {
            get
            {
                return new Vector3(15f, 0f, 0f);
            }
        }

        public Vector3 FirstPersonPositionOffset
        {
            get
            {
                return new Vector3(_firstPersonPos.x, _firstPersonPos.y, _firstPersonPos.z);
            }
            set
            {
                _firstPersonPos.x = value.x;
                _firstPersonPos.y = value.y;
                _firstPersonPos.z = value.z;
            }
        }
        public float[] FirstPersonPositionOffsetFloat
        {
            get
            {
                float[] result = new float[3];
                result[0] = _firstPersonPos.x;
                result[1] = _firstPersonPos.y;
                result[2] = _firstPersonPos.z;
                return result;
            }
            set
            {
                _firstPersonPos.x = value[0];
                _firstPersonPos.y = value[1];
                _firstPersonPos.z = value[2];
            }
        }
        public Vector3 FirstPersonRotationOffset
        {
            get
            {
                return new Vector3(_firstPersonRot.x, _firstPersonRot.y, _firstPersonRot.z);
            }
            set
            {
                _firstPersonRot.x = value.x;
                _firstPersonRot.y = value.y;
                _firstPersonRot.z = value.z;
            }
        }
        public float[] FirstPersonRotationOffsetFloat
        {
            get
            {
                float[] result = new float[3];
                result[0] = _firstPersonRot.x;
                result[1] = _firstPersonRot.y;
                result[2] = _firstPersonRot.z;
                return result;
            }
            set
            {
                _firstPersonRot.x = value[0];
                _firstPersonRot.y = value[1];
                _firstPersonRot.z = value[2];
            }
        }
        public Vector3 DefaultFirstPersonPositionOffset
        {
            get
            {
                return new Vector3(0, 0, 0);
            }
        }
        public Vector3 DefaultFirstPersonRotationOffset
        {
            get
            {
                return new Vector3(0, 0, 0);
            }
        }

        public Vector3 PreviewQuadPosition
        {
            get
            {
                return new Vector3(_previewQuadPos.x, _previewQuadPos.y, _previewQuadPos.z);
            }
            set
            {
                _previewQuadPos.x = value.x;
                _previewQuadPos.y = value.y;
                _previewQuadPos.z = value.z;
            }
        }
        public float[] PreviewQuadPositionFloat
        {
            get
            {
                float[] result = new float[3];
                result[0] = _previewQuadPos.x;
                result[1] = _previewQuadPos.y;
                result[2] = _previewQuadPos.z;
                return result;
            }
            set
            {
                _previewQuadPos.x = value[0];
                _previewQuadPos.y = value[1];
                _previewQuadPos.z = value[2];
            }
        }
        public Vector3 PreviewQuadRotation
        {
            get
            {
                return new Vector3(_previewQuadRot.x, _previewQuadRot.y, _previewQuadRot.z);
            }
            set
            {
                _previewQuadRot.x = value.x;
                _previewQuadRot.y = value.y;
                _previewQuadRot.z = value.z;
            }
        }
        public float[] PreviewQuadRotationFloat
        {
            get
            {
                float[] result = new float[3];
                result[0] = _previewQuadRot.x;
                result[1] = _previewQuadRot.y;
                result[2] = _previewQuadRot.z;
                return result;
            }
            set
            {
                _previewQuadRot.x = value[0];
                _previewQuadRot.y = value[1];
                _previewQuadRot.z = value[2];
            }
        }

        public Vector3 TurnToHeadOffset
        {
            get
            {
                return new Vector3(_turnToHeadOffset.x, _turnToHeadOffset.y, _turnToHeadOffset.z);
            }
            set
            {
                _turnToHeadOffset.x = value.x;
                _turnToHeadOffset.y = value.y;
                _turnToHeadOffset.z = value.z;
            }
        }
        public float[] TurnToHeadOffsetFloat
        {
            get
            {
                float[] result = new float[3];
                result[0] = _turnToHeadOffset.x;
                result[1] = _turnToHeadOffset.y;
                result[2] = _turnToHeadOffset.z;
                return result;
            }
            set
            {
                _turnToHeadOffset.x = value[0];
                _turnToHeadOffset.y = value[1];
                _turnToHeadOffset.z = value[2];
            }
        }
        public CameraConfig(string configPath)
        {
            FilePath = configPath;
            if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            if (File.Exists(FilePath))
            {
                if (Load())
                    configLoaded = true;
                else
                {
                    configLoaded = false;
                    return;
                }
            }
            else
            {
                _thirdPersonPos.y = 2.0f;
                _thirdPersonPos.z = -3.0f;
                _thirdPersonRot.x = 15.0f;
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
        ~CameraConfig()
        {
            _configWatcher.Changed -= ConfigWatcherOnChanged;
        }

        public void Save()
        {
            string json;
            _saving = true;
            try
            {
                json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to save {cam.name}\n{ex}");
            }
            _saving = false;
        }
        public bool Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var jsondata = File.ReadAllText(FilePath);
                    jsondata = jsondata.Replace("FirstPesron", "FirstPerson");//Old version Typo correction process
                    JsonConvert.PopulateObject(jsondata, this);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Config json read error.\n{ex.Message}");
                return false;
            }
            return true;
        }

        private void ConfigWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (_saving)
            {
                //_saving = false;
                return;
            }

            Load();

            if (ConfigChangedEvent != null)
            {
                ConfigChangedEvent(this);
            }
        }
        internal virtual void SetCullingMask(VisibleObject visibleObject = null)
        {
            if (!cam) return;
            int builder = CameraUtilities.BaseCullingMask;
            if (visibleObject == null) visibleObject = new VisibleObject();
#if DEBUG
            Plugin.Log.Notice($"SetCuttingMask Avatar{_visibleObject.avatar}, UI{visibleObject.ui}, Notes{visibleObject.notes}, Debri{visibleObject.debris}, Saber{visibleObject.saber}");
#endif
            if (visibleObject.avatar.HasValue ? visibleObject.avatar.Value : layerSetting.avatar)
            {
                if (thirdPerson)
                {
                    builder |= 1 << Layer.OnlyInThirdPerson;
                    builder &= ~(1 << Layer.OnlyInFirstPerson);
                }
                else
                {
                    builder |= 1 << Layer.OnlyInFirstPerson;
                    builder &= ~(1 << Layer.OnlyInThirdPerson);
                }
                builder |= 1 << Layer.AlwaysVisible;
            }
            else
            {
                builder &= ~(1 << Layer.OnlyInThirdPerson);
                builder &= ~(1 << Layer.OnlyInFirstPerson);
                builder &= ~(1 << Layer.AlwaysVisible);
            }
            if (visibleObject.debris.HasValue)
            {
                if (visibleObject.debris.Value)
                    builder |= (1 << Layer.NotesDebriLayer);
                else
                    builder &= ~(1 << Layer.NotesDebriLayer);
            }
            else
            {
                if (layerSetting.debris != DebriVisibility.Link)
                {
                    if (layerSetting.debris == DebriVisibility.Visible)
                        builder |= (1 << Layer.NotesDebriLayer);
                    else
                        builder &= ~(1 << Layer.NotesDebriLayer);
                }
            }
            if (visibleObject.ui.HasValue ? visibleObject.ui.Value : layerSetting.ui)
                builder |= (1 << Layer.UI);
            else
                builder &= ~(1 << Layer.UI);

            if (visibleObject.saber.HasValue ? visibleObject.saber.Value : layerSetting.saber)
                builder |= 1 << Layer.Saber;
            else
                builder &= ~(1 << Layer.Saber);

            if (visibleObject.wall.HasValue ? visibleObject.wall.Value : layerSetting.wall)
                builder |= 1 << TransparentWallsPatch.WallLayerMask;
            else
                builder &= ~(1 << TransparentWallsPatch.WallLayerMask);

            if (visibleObject.wallFrame.HasValue ? visibleObject.wallFrame.Value : layerSetting.wallFrame)
                builder |= 1 << Layer.Obstracle;
            else
                builder &= ~(1 << Layer.Obstracle);

            if (visibleObject.notes.HasValue ? visibleObject.notes.Value : layerSetting.notes)
            {
                builder &= ~(1 << Layer.CustomNoteLayer);
                builder |= 1 << Layer.Notes;
            }
            else
            {
                builder &= ~(1 << Layer.CustomNoteLayer);
                builder &= ~(1 << Layer.Notes);
            }

            cam._cam.cullingMask = builder;
        }

        internal void ConvertFromCamera2(Camera2Config config2)
        {
            fov = config2.FOV;
            antiAliasing = config2.antiAliasing;
            renderScale = config2.renderScale;
            cameraExtensions.rotation360Smooth = config2.follow360.smoothing;
            if (config2.type == Camera2Utils.CameraType.FirstPerson)
                thirdPerson = false;
            else
                thirdPerson = true;
            if (config2.worldCamVisibility != WorldCamVisibility.Hidden)
                PreviewCamera = true;
            else
                PreviewCamera = false;
            cameraExtensions.follow360map = config2.follow360.enabled;
            thirdPersonPos.x = firstPersonPos.x = config2.targetPos.x;
            thirdPersonPos.y = firstPersonPos.y = config2.targetPos.y;
            thirdPersonPos.z = firstPersonPos.z = config2.targetPos.z;
            thirdPersonRot.x = firstPersonRot.x = config2.targetRot.x;
            thirdPersonRot.y = firstPersonRot.y = config2.targetRot.y;
            thirdPersonRot.z = firstPersonRot.z = config2.targetRot.z;
            cameraExtensions.followNoodlePlayerTrack = config2.modmapExtensions.moveWithMap;
            screenWidth = (int)Math.Round(config2.viewRect.width * Screen.width);
            if (screenWidth <= 0) screenWidth = Screen.width;
            screenHeight = (int)Math.Round(config2.viewRect.height * Screen.height);
            if (screenHeight <= 0) screenHeight = Screen.height;
            screenPosX = (int)Math.Round(config2.viewRect.x * Screen.width);
            screenPosY = (int)Math.Round(config2.viewRect.y * Screen.height);
            layer = config2.layer;
            fitToCanvas = false;
            if (config2.visibleObject.Walls == WallVisiblity.Hidden)
                layerSetting.wall = false;
            else
                layerSetting.wall = true;
            layerSetting.avatar = config2.visibleObject.Avatar == Camera2Utils.AvatarVisibility.Hidden ? false : true;
            layerSetting.debris = config2.visibleObject.Debris ? DebriVisibility.Visible : DebriVisibility.Hidden;
            layerSetting.ui = config2.visibleObject.UI;
            cameraExtensions.firstPersonCameraForceUpRight = config2.Smoothfollow.forceUpright;
        }

        internal Camera2Config ConvertToCamera2()
        {
            Camera2Config config2 = new Camera2Config();
            config2.visibleObject = new visibleObjectsElement();
            config2.viewRect = new viewRectElement();
            config2.Smoothfollow = new SmoothfollowElement();
            config2.follow360 = new Follow360Element();
            config2.modmapExtensions = new ModmapExtensionsElement();
            config2.targetPos = new targetPosElement();
            config2.targetRot = new targetRotElement();
            config2.visibleObject.Walls = layerSetting.wall ? Camera2Utils.WallVisiblity.Visible : Camera2Utils.WallVisiblity.Hidden;
            config2.visibleObject.Debris = layerSetting.debris == DebriVisibility.Hidden ? false : true;
            config2.visibleObject.UI = layerSetting.ui;
            config2.visibleObject.Avatar = layerSetting.avatar ? Camera2Utils.AvatarVisibility.Visible : Camera2Utils.AvatarVisibility.Hidden;
            config2.type = thirdPerson ? Camera2Utils.CameraType.Positionable : Camera2Utils.CameraType.FirstPerson;
            config2.FOV = fov;
            config2.layer = layer + 1000;
            config2.renderScale = (renderScale >= 0.99f) ? Math.Max(1.2f, renderScale) : renderScale;
            config2.antiAliasing = (renderScale >= 0.99f) ? Math.Max(antiAliasing, 2) : antiAliasing;
            config2.viewRect.x = fitToCanvas ? 0 : (float)screenPosX / Screen.width;
            config2.viewRect.y = fitToCanvas ? 0 : (float)screenPosY / Screen.height;
            config2.viewRect.width = fitToCanvas ? 1 : (float)screenWidth / Screen.width;
            config2.viewRect.height = fitToCanvas ? 1 : (float)screenHeight / Screen.height;
            config2.Smoothfollow.position = cameraExtensions.positionSmooth;
            config2.Smoothfollow.rotation = cameraExtensions.rotationSmooth;
            config2.Smoothfollow.forceUpright = cameraExtensions.firstPersonCameraForceUpRight;
            config2.follow360.enabled = cameraExtensions.follow360map;
            config2.follow360.smoothing = cameraExtensions.rotation360Smooth;
            config2.modmapExtensions.moveWithMap = cameraExtensions.followNoodlePlayerTrack;
            config2.targetPos.x = thirdPerson ? thirdPersonPos.x : FirstPersonPositionOffset.x;
            config2.targetPos.y = thirdPerson ? thirdPersonPos.y : FirstPersonPositionOffset.y;
            config2.targetPos.z = thirdPerson ? thirdPersonPos.z : FirstPersonPositionOffset.z;
            config2.targetRot.x = thirdPerson ? thirdPersonRot.x : FirstPersonRotationOffset.x;
            config2.targetRot.y = thirdPerson ? thirdPersonRot.y : FirstPersonRotationOffset.y;
            config2.targetRot.z = thirdPerson ? thirdPersonRot.z : FirstPersonRotationOffset.z;
            return config2;
        }

        public CameraEffectStruct InitializeCameraEffect()
        {
            CameraEffectStruct cameraEffectStruct = new CameraEffectStruct
            {
                enableDOF = cameraEffect.enableDOF,
                dofFocusDistance = cameraEffect.dofFocusDistance,
                dofFocusRange = cameraEffect.dofFocusRange,
                dofBlurRadius = cameraEffect.dofBlurRadius,
                dofAutoDistance = cameraEffect.dofAutoDistance,

                wipeType = cameraEffect.wipeType,
                wipeCircleCenter = cameraEffect.wipeCircleCenter,
                wipeProgress = cameraEffect.wipeProgress,

                enableOutline = cameraEffect.enableOutline,
                outlineOnly = cameraEffect.outlineOnly,
                outlineColor = cameraEffect.outlineColor,
                outlineBGColor = cameraEffect.outlineBGColor
            };
            return cameraEffectStruct;
        }

    }

    public class cameraLockElements
    {
        [JsonProperty("LockScreen")]
        public bool lockScreen = false;
        [JsonProperty("LockCamera")]
        public bool lockCamera = false;
        [JsonProperty("DontSaveCameraDrag")]
        public bool dontSaveDrag = false;
    }
    public class visibleObjectsElements
    {
        [JsonProperty("Avatar")]
        public bool avatar = true;
        [JsonProperty("UI")]
        public bool ui = true;
        [JsonProperty("ForceUI")]
        public bool forceUi = true;
        [JsonProperty("Wall")]
        public bool wall = true;
        [JsonProperty("WallFrame")]
        public bool wallFrame = true;
        [JsonProperty("Saber")]
        public bool saber = true;
        [JsonProperty("CutParticles")]
        public bool cutParticles = true;
        [JsonProperty("Notes")]
        public bool notes = true;
        [JsonConverter(typeof(StringEnumConverter)), JsonProperty("Debris")]
        internal DebriVisibility debris = DebriVisibility.Link;
    }
    public class cameraExtensionsElements
    {
        [JsonProperty("PreviewCamera")]
        public bool previewCamera = true;
        [JsonProperty("PreviewCameraVROnly")]
        public bool previewCameraVROnly = true;
        [JsonProperty("PreviewCameraQuadScale")]
        public float previewCameraQuadScale = 1.0f;
        [JsonProperty("PreviewCameraMirrorMode")]
        public bool previewCameraMirrorMode = false;
        [JsonProperty("PreviewQuadSeparate")]
        public bool previewQuadSeparate = false;
        [JsonProperty("PreviewCameraQuadOnly")]
        public bool previewCameraQuadOnly = false;

        [JsonProperty("OrthographicMode")]
        public bool orthographicMode = false;
        [JsonProperty("OrthographicSize")]
        public float orthographicSize = 1.0f;
        [JsonProperty("NearClip")]
        public float nearClip = 0.1f;
        [JsonProperty("FarClip")]
        public float farClip = 1000.0f;

        [JsonProperty("PositionSmooth")]
        public float positionSmooth = 10.0f;
        [JsonProperty("RotationSmooth")]
        public float rotationSmooth = 5.0f;
        [JsonProperty("FirstPersonCameraForceUpRight")]
        public bool firstPersonCameraForceUpRight = false;

        [JsonProperty("Rotation360Smooth")]
        public float rotation360Smooth = 2.0f;
        [JsonProperty("Follow360Map")]
        public bool follow360map = false;

        [JsonProperty("FollowNoodlePlayerTrack")]
        public bool followNoodlePlayerTrack = true;

        [JsonProperty("TurnToHead")]
        public bool turnToHead = false;
        [JsonProperty("TurnToHeadHorizontal")]
        public bool turnToHeadHorizontal = false;

        [JsonProperty("DontDrawDesktop")]
        public bool dontDrawDesktop = false;
    }
    public class windowRectElement
    {
        [JsonProperty("FitToCanvas")]
        public bool fitToCanvas = false;
        public int x = 0;
        public int y = 0;
        public int width = Screen.width;
        public int height = Screen.height;
    }
    public class targetTransformElements
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
    public class movementScriptElements
    {
        [JsonProperty("MovementScript")]
        public string movementScript = string.Empty;
        [JsonProperty("UseAudioSync")]
        public bool useAudioSync = true;
        [JsonProperty("SongSpecificScript")]
        public bool songSpecificScript = false;
        [JsonProperty("IgnoreScriptUIDisplay")]
        public bool ignoreScriptUIDisplay = false;
    }
    public class multiplayerElemetns
    {
        [JsonProperty("TargetPlayerNumber")]
        public int targetPlayerNumber = 0;
        [JsonProperty("DisplayPlayerInfo")]
        public bool displayPlayerInfo = false;
    }

    public class vmcProtocolElements
    {
        [JsonConverter(typeof(StringEnumConverter)), JsonProperty("Mode")]
        internal VMCProtocolMode mode = VMCProtocolMode.Disable;
        [JsonProperty("Address")]
        public string address = "127.0.0.1";
        [JsonProperty("Sender Port")]
        public int port = 39540;
        [JsonProperty("Receiver Port")]
        public int receiverPort = 39540;
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class webCameraElements
    {
        [JsonProperty("WebCameraName")]
        public string name = string.Empty;
        [JsonProperty("AutoConnect")]
        public bool autoConnect = false;
        [JsonProperty("ChromaKeyColor")]
        private float[] color = new float[] { 0f, 0f, 0f };
        [JsonProperty("ChromaKeyHue")]
        public float hue = 0f;
        [JsonProperty("ChromaKeySaturation")]
        public float saturation = 0f;
        [JsonProperty("ChromaKeyBrightness")]
        public float brightness = 0f;
        public Color chromaKeyColor
        {
            get
            {
                return new Color(color[0], color[1], color[2], 0);
            }
            set
            {
                color[0] = value.r;
                color[1] = value.g;
                color[2] = value.b;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class SpoutCameraElements
    {
        [JsonProperty("SpoutReceiverName")]
        public string reciverName = string.Empty;
        [JsonProperty("AutoConnect")]
        public bool reciverAutoConnect = false;
        [JsonProperty("SpoutSenderName")]
        public string senderName = string.Empty;
        [JsonProperty("SpoutSenderWidth")]
        public int senderWidth = 1920;
        [JsonProperty("SpoutSenderHeight")]
        public int senderHeight = 1080;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public struct CameraEffectStruct
    {
        //Depth of Field
        [JsonProperty("EnableDOF")]
        public bool enableDOF;
        [JsonProperty("DOFFocusDistance")]
        public float dofFocusDistance;
        [JsonProperty("DOFFocusRange")]
        public float dofFocusRange;
        [JsonProperty("DOFBlurRadius")]
        public float dofBlurRadius;
        [JsonProperty("DOFAutoDistance")]
        public bool dofAutoDistance;

        //Wipe
        [JsonProperty("WipeProgress")]
        public float wipeProgress;
        [JsonProperty("WipeType")]
        public string wipeType;
        [JsonProperty("WipeCircleCenter")]
        private float[] _wipeCircleCenter;
        //←↑-0.5,-0.5 / →↓+0.5, +0.5
        public Vector4 wipeCircleCenter
        {
            get
            {
                if (_wipeCircleCenter == null) _wipeCircleCenter = new float[] { 0f, 0f };
                return new Color(_wipeCircleCenter[0], _wipeCircleCenter[1], 0, 0);
            }
            set
            {
                if (_wipeCircleCenter == null) _wipeCircleCenter = new float[] { 0f, 0f };
                _wipeCircleCenter[0] = value.x;
                _wipeCircleCenter[1] = value.y;
            }
        }
        public float[] wipeCircleCenterValue { get => _wipeCircleCenter; set => _wipeCircleCenter = value; }
        //OutlineEffect
        [JsonProperty("EnableOutline")]
        public bool enableOutline;
        [JsonProperty("OutlineOnly")]
        public float outlineOnly;
        [JsonProperty("OutlineColor")]
        private float[] _outlineColor;
        public Color outlineColor
        {
            get
            {
                if (_outlineColor == null) _outlineColor = new float[] { 0f, 0f, 0f };
                return new Color(_outlineColor[0], _outlineColor[1], _outlineColor[2], 0);
            }
            set
            {
                if (_outlineColor == null) _outlineColor = new float[] { 0f, 0f, 0f };
                _outlineColor[0] = value.r;
                _outlineColor[1] = value.g;
                _outlineColor[2] = value.b;
            }
        }
        public float[] outlineColorValue { get { return _outlineColor; } set { _outlineColor = value; } }

        [JsonProperty("OutlineBGColor")]
        private float[] _outlineBGColor;
        public Color outlineBGColor
        {
            get
            {
                if (_outlineBGColor == null) _outlineBGColor = new float[] { 0f, 0f, 0f };
                return new Color(_outlineBGColor[0], _outlineBGColor[1], _outlineBGColor[2], 0);
            }
            set
            {
                if (_outlineBGColor == null) _outlineBGColor = new float[] { 0f, 0f, 0f };
                _outlineBGColor[0] = value.r;
                _outlineBGColor[1] = value.g;
                _outlineBGColor[2] = value.b;
            }
        }
        public float[] outlineBGColorValue { get { return _outlineBGColor; } set { _outlineBGColor = value; } }

        //GlitchEffect
        [JsonProperty("EnableGlitch")]
        public bool enableGlitch;
        [JsonProperty("GlitchLineSpeed")]
        public float glitchLineSpeed;
        [JsonProperty("GlitchLineSize")]
        public float glitchLineSize;
        [JsonProperty("GlitchColorGap")]
        public float glitchColorGap;
        [JsonProperty("GlitchFrameRate")]
        public float glitchFrameRate;
        [JsonProperty("GlitchFrequency")]
        public float glitchFrequency;
        [JsonProperty("GlitchScale")]
        public float glitchScale;

        public float[] glitchValue
        {
            get
            {
                return new float[] { glitchLineSpeed, glitchLineSize, glitchColorGap, glitchFrameRate, glitchFrequency, glitchScale };
            }
            set
            {
                glitchLineSpeed = value[0];
                glitchLineSize = value[1];
                glitchColorGap = value[2];
                glitchFrameRate = value[3];
                glitchFrequency = value[4];
                glitchScale = value[5];
            }
        }
    }
}
