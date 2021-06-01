using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using Screen = UnityEngine.Screen;
using CameraPlus.Configuration;
using CameraPlus.HarmonyPatches;
using CameraPlus.VMCProtocol;
using CameraPlus.Utilities;
using CameraPlus.UI;

namespace CameraPlus.Behaviours
{
    public class CameraPlusBehaviour : MonoBehaviour
    {
        public enum CursorType
        {
            None,
            Horizontal,
            Vertical,
            DiagonalLeft,
            DiagonalRight
        }

        protected readonly WaitUntil _waitForMainCamera = new WaitUntil(() => Camera.main);
        protected const int OnlyInThirdPerson = 3;
        protected const int UILayer = 5;
        protected const int OnlyInFirstPerson = 6; //Moved to an empty layer because layer 4 overlapped the floor
        protected const int NotesDebriLayer = 9;
        protected const int AlwaysVisible = 10;
        protected const int NoteLayer = 8;
        protected const int CustomNoteLayer = 24;
        protected const int SaberLayer = 12;

        public bool ThirdPerson
        {
            get { return _thirdPerson; }
            set
            {
                _thirdPerson = value;
                _cameraCube.gameObject.SetActive(_thirdPerson && Config.showThirdPersonCamera);
                _cameraPreviewQuad.gameObject.SetActive(_thirdPerson && Config.showThirdPersonCamera);

                if (value)
                {
                    _cam.cullingMask &= ~(1 << OnlyInFirstPerson);
                    _cam.cullingMask |= 1 << OnlyInThirdPerson;

                }
                else
                {
                    _cam.cullingMask &= ~(1 << OnlyInThirdPerson);
                    _cam.cullingMask |= 1 << OnlyInFirstPerson;
                }
            }
        }

        protected bool _thirdPerson;
        public Vector3 ThirdPersonPos;
        public Vector3 ThirdPersonRot;
        public Vector3 OffsetPosition;
        public Vector3 OffsetAngle;
        public Config Config;

        protected RenderTexture _camRenderTexture;
        protected Material _previewMaterial;
        protected Camera _cam;
        protected Transform _cameraCube;
        protected ScreenCameraBehaviour _screenCamera;
        protected GameObject _cameraPreviewQuad;
        protected Camera _mainCamera = null;
        protected CameraMoverPointer _moverPointer = null;
        protected GameObject _cameraCubeGO;
        protected GameObject _quad;
        protected CameraMovement _cameraMovement = null;
        protected BeatLineManager _beatLineManager;
        protected EnvironmentSpawnRotation _environmentSpawnRotation;

        protected int _prevScreenWidth;
        protected int _prevScreenHeight;
        protected int _prevAA;
        protected float _prevRenderScale;
        protected int _prevLayer;
        protected int _prevScreenPosX, _prevScreenPosY;
        protected bool _prevFitToCanvas;
        protected float _aspectRatio;
        protected float _yAngle;

        protected bool _wasWindowActive = false;
        protected bool _mouseHeld = false;
        protected bool _isResizing = false;
        protected bool _isMoving = false;
        protected bool _xAxisLocked = false;
        protected bool _yAxisLocked = false;
        protected bool _contextMenuOpen = false;
        internal bool _isCameraDestroyed = false;
        protected bool _isMainCamera = false;
        protected bool _isTopmostAtCursorPos = false;
        protected DateTime _lastRenderUpdate;
        protected Vector2 _initialOffset = new Vector2(0, 0);
        protected Vector2 _lastGrabPos = new Vector2(0, 0);
        protected Vector2 _lastScreenPos;
        protected bool _isBottom = false, _isLeft = false;
        protected static GameObject MenuObj = null;
        protected static UI.ContextMenu _contextMenu = null;
        private BehaviourScriptEdit _scriptEditMenu = null;
        public static CursorType currentCursor = CursorType.None;
        public static bool wasWithinBorder = false;
        public static bool anyInstanceBusy = false;
        private static bool _contextMenuEnabled = true;
        private GameObject adjustOffset;
        private GameObject adjustParent;
        private ExternalSender externalSender = null;
        private bool replaceFPFC = false;
        private bool isFPFC = false;
        private GUIStyle _multiplayerGUIStyle = null;
        private Vector3 prevMousePos = Vector3.zero;
        private Vector3 mouseRightDownPos = Vector3.zero;
        internal bool mouseMoveCamera = false;
        internal bool mouseMoveCameraSave = false;
        internal bool scriptEditMode = false;
        private Transform turnToTarget;
        internal bool turnToHead = false;
        internal Vector3 turnToHeadOffset = Vector3.zero;

#if WithVMCAvatar
        private VMCProtocol.VMCAvatarMarionette marionette = null;
#endif
        public virtual void Init(Config config)
        {
            DontDestroyOnLoad(gameObject);
            Logger.log.Notice("Created new camera plus behaviour component!");

            Config = config;
            _isMainCamera = Path.GetFileName(Config.FilePath) == $"{Plugin.MainCamera}.cfg";
            _contextMenuEnabled = Array.IndexOf(Environment.GetCommandLineArgs(), "fpfc") == -1;

            StartCoroutine(DelayedInit());
        }

        protected IEnumerator DelayedInit()
        {
            yield return _waitForMainCamera;

            _mainCamera = Camera.main;
            //      _menuStrip = null;
            if (_contextMenu == null)
            {
                MenuObj = new GameObject("CameraPlusMenu");
                _contextMenu = MenuObj.AddComponent<UI.ContextMenu>();
            }
            XRSettings.showDeviceView = false;

            var gameObj = Instantiate(_mainCamera.gameObject);

            Config.ConfigChangedEvent += PluginOnConfigChangedEvent;

            gameObj.SetActive(false);
            gameObj.name = "Camera Plus";
            gameObj.tag = "Untagged";

            _cam = gameObj.GetComponent<Camera>();
            _cam.stereoTargetEye = StereoTargetEyeMask.None;
            _cam.enabled = true;
            _cam.name = Path.GetFileName(Config.FilePath);

            foreach (var child in _cam.transform.Cast<Transform>())
                Destroy(child.gameObject);
            var destroyList = new string[] { "AudioListener", "LIV", "MainCamera", "MeshCollider" };
            foreach (var component in _cam.GetComponents<Behaviour>())
                if (destroyList.Contains(component.GetType().Name)) Destroy(component);

            _screenCamera = new GameObject("Screen Camera").AddComponent<ScreenCameraBehaviour>();
            
            
            Shader shader = Resources.Load<Shader>("CustomBlitCopyWithDepth");
            if (_previewMaterial == null)
                _previewMaterial = new Material(Plugin.cameraController.Shaders["BeatSaber/BlitCopyWithDepth"]);
                //_previewMaterial = new Material(Shader.Find("Hidden/BlitCopyWithDepth"));
            gameObj.SetActive(true);

            var camera = _mainCamera.transform;
            transform.position = camera.position;
            transform.rotation = camera.rotation;
            //Logger.log.Notice($"near clipplane \"{Camera.main.nearClipPlane}");

            gameObj.transform.parent = transform;
            gameObj.transform.localPosition = Vector3.zero;
            gameObj.transform.localRotation = Quaternion.identity;
            gameObj.transform.localScale = Vector3.one;

            _cameraCubeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DontDestroyOnLoad(_cameraCubeGO);
            _cameraCubeGO.SetActive(ThirdPerson);
            _cameraCube = _cameraCubeGO.transform;
            _cameraCube.localScale = new Vector3(0.15f, 0.15f, 0.22f);
            _cameraCube.name = "CameraCube";
            _cameraCubeGO.layer = Plugin.cameraController.rootConfig.CameraQuadLayer;

            _quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DontDestroyOnLoad(_quad);
            DestroyImmediate(_quad.GetComponent<Collider>());
            _quad.GetComponent<MeshRenderer>().material = _previewMaterial;
            _quad.transform.parent = _cameraCube;
            _quad.transform.localPosition = new Vector3(-1f * ((_cam.aspect - 1) / 2 + 1), 0, 0.22f);
            _quad.transform.localEulerAngles = new Vector3(0, 180, 0);
            _quad.transform.localScale = new Vector3(_cam.aspect, 1, 1);
            _cameraPreviewQuad = _quad;
            _quad.layer = Plugin.cameraController.rootConfig.CameraQuadLayer;

            ReadConfig();

            if (ThirdPerson)
            {
                ThirdPersonPos = Config.Position;
                ThirdPersonRot = Config.Rotation;

                transform.position = ThirdPersonPos;
                transform.eulerAngles = ThirdPersonRot;

                _cameraCube.position = ThirdPersonPos;
                _cameraCube.eulerAngles = ThirdPersonRot;
            }

            // Add our camera movement script if the movement script path is set
            if (Config.movementScriptPath != String.Empty || Config.songSpecificScript)
                AddMovementScript();

            SetCullingMask();

            Plugin.cameraController.ActiveSceneChanged += SceneManager_activeSceneChanged;
            SceneManager_activeSceneChanged(new Scene(), new Scene());
            Logger.log.Notice($"Camera \"{Path.GetFileName(Config.FilePath)}\" successfully initialized! {Convert.ToString(_cam.cullingMask, 16)}");

            if (Config.VMCProtocolMode == "sender")
                InitExternalSender();

            if (CameraUtilities.seekBar == null)
                CameraUtilities.CreatSeekbarTexture();
        }

        public void InitExternalSender()
        {
            if (Config.VMCProtocolMode == "sender" && Config.fitToCanvas)
            {
                if (CameraUtilities.vmcPortList == null) CameraUtilities.vmcPortList = new List<int>();
                if (CameraUtilities.vmcPortList.Find(p => p == Config.VMCProtocolPort) == Config.VMCProtocolPort)
                {
                    Logger.log.Notice($"Camera \"{Path.GetFileName(Config.FilePath)}\" already use port {Config.VMCProtocolPort}");
                    return;
                }
                externalSender = new GameObject("VMCProtocolCamera").AddComponent<ExternalSender>();
                externalSender.SendCameraData(Config.VMCProtocolAddress, Config.VMCProtocolPort);
                externalSender.camera = _cam;
                CameraUtilities.vmcPortList.Add(Config.VMCProtocolPort);
            }
        }
        public void InitExternalReceiver()
        {
#if WithVMCAvatar
            if (Config.VMCProtocolMode == "receiver" && Plugin.cameraController.existsVMCAvatar)
            {
                marionette = this.gameObject.AddComponent<VMCProtocol.VMCAvatarMarionette>();
                ClearMovementScript();
            }
#endif
        }
        public void DestoryVMCProtocolObject()
        {
#if WithVMCAvatar
            if (marionette)
                Destroy(marionette);
#endif
            if (CameraUtilities.vmcPortList != null)
                if (CameraUtilities.vmcPortList.Find(p => p == Config.VMCProtocolPort) == Config.VMCProtocolPort)
                    CameraUtilities.vmcPortList.Remove(Config.VMCProtocolPort);
            if (externalSender)
                Destroy(externalSender);
            if (Config.movementScriptPath != String.Empty || Config.songSpecificScript)
                AddMovementScript();
        }


        protected virtual void OnDestroy()
        {
            Config.ConfigChangedEvent -= PluginOnConfigChangedEvent;
            Plugin.cameraController.ActiveSceneChanged -= SceneManager_activeSceneChanged;

            _cameraMovement?.Shutdown();
            // Close our context menu if its open, and destroy all associated controls, otherwise the game will lock up
            CloseContextMenu();

            _camRenderTexture?.Release();

#if WithVMCAvatar
            if (marionette)
                Destroy(marionette);
#endif
            if (CameraUtilities.vmcPortList != null)
                if (CameraUtilities.vmcPortList.Find(p => p == Config.VMCProtocolPort) == Config.VMCProtocolPort)
                    CameraUtilities.vmcPortList.Remove(Config.VMCProtocolPort);
            if (externalSender)
                Destroy(externalSender);

            if (_screenCamera)
                Destroy(_screenCamera.gameObject);
            if (_cameraCubeGO)
                Destroy(_cameraCubeGO);
            if (_quad)
                Destroy(_quad);
        }

        protected virtual void PluginOnConfigChangedEvent(Config config)
        {
            ReadConfig();
        }

        protected virtual void ReadConfig()
        {
            ThirdPerson = Config.thirdPerson;

            if (!ThirdPerson)
            {
                transform.position = _mainCamera.transform.position;
                transform.rotation = _mainCamera.transform.rotation;
            }
            else
            {
                ThirdPersonPos = Config.Position;
                ThirdPersonRot = Config.Rotation;
            }
            turnToHead = Config.turnToHead;
            turnToHeadOffset = Config.TurnToHeadOffset;
            SetCullingMask();
            CreateScreenRenderTexture();
            SetFOV();
        }

        internal virtual void CreateScreenRenderTexture()
        {
            HMMainThreadDispatcher.instance.Enqueue(delegate
            {
                var replace = false;
                if (_camRenderTexture == null)
                {
                    _camRenderTexture = new RenderTexture(1, 1, 24);
                    replace = true;
                }
                else
                {
                    if (Config.fitToCanvas != _prevFitToCanvas || Config.antiAliasing != _prevAA || Config.screenPosX != _prevScreenPosX || Config.screenPosY != _prevScreenPosY || Config.renderScale != _prevRenderScale || Config.screenHeight != _prevScreenHeight || Config.screenWidth != _prevScreenWidth || Config.layer != _prevLayer)
                    {
                        replace = true;

                        _cam.targetTexture = null;
                        _screenCamera.SetRenderTexture(null);
                        _screenCamera.SetCameraInfo(new Vector2(0, 0), new Vector2(0, 0), -1000);

                        _camRenderTexture.Release();

                    }
                }
                if (replaceFPFC)
                {
                    replaceFPFC = false;
                    if (FPFCPatch.isInstanceFPFC && !FPFCPatch.instance.isActiveAndEnabled)
                        _screenCamera.SetCameraInfo(Config.ScreenPosition, Config.ScreenSize, Config.layer + 1000);
                    else
                        _screenCamera.SetCameraInfo(Config.ScreenPosition, Config.ScreenSize, Config.layer);
                }

                if (!replace)
                    return;

                if (Config.fitToCanvas)
                {
                    Config.screenPosX = 0;
                    Config.screenPosY = 0;
                    Config.screenWidth = Screen.width;
                    Config.screenHeight = Screen.height;
                }
                _lastRenderUpdate = DateTime.Now;
                _camRenderTexture.width = Mathf.Clamp(Mathf.RoundToInt(Config.screenWidth * Config.renderScale), 1, int.MaxValue);
                _camRenderTexture.height = Mathf.Clamp(Mathf.RoundToInt(Config.screenHeight * Config.renderScale), 1, int.MaxValue);

                _camRenderTexture.useDynamicScale = false;
                _camRenderTexture.antiAliasing = Config.antiAliasing;
                //_camRenderTexture.Create();

                _cam.targetTexture = _camRenderTexture;
                _previewMaterial.SetTexture("_MainTex", _camRenderTexture);
                _screenCamera.SetRenderTexture(_camRenderTexture);

                if (FPFCPatch.isInstanceFPFC && !FPFCPatch.instance.isActiveAndEnabled)
                    _screenCamera.SetCameraInfo(Config.ScreenPosition, Config.ScreenSize, Config.layer + 1000);
                else
                    _screenCamera.SetCameraInfo(Config.ScreenPosition, Config.ScreenSize, Config.layer);

                _prevFitToCanvas = Config.fitToCanvas;
                _prevAA = Config.antiAliasing;
                _prevRenderScale = Config.renderScale;
                _prevScreenHeight = Config.screenHeight;
                _prevScreenWidth = Config.screenWidth;
                _prevLayer = Config.layer;
                _prevScreenPosX = Config.screenPosX;
                _prevScreenPosY = Config.screenPosY;
            });
        }

        public virtual void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            CloseContextMenu();

            StartCoroutine(GetMainCamera());
            StartCoroutine(Get360Managers());

            var pointer = VRPointerPatch.Instance;
            if (_moverPointer) Destroy(_moverPointer);
            _moverPointer = pointer.gameObject.AddComponent<CameraMoverPointer>();
            _moverPointer.Init(this, _cameraCube);

            if (to.name == "GameCore")
                SharedCoroutineStarter.instance.StartCoroutine(Delayed_activeSceneChanged(from, to));
            else
                if (Config.movementAudioSync || (!Config.movementAudioSync && Config.movementScriptPath == string.Empty))
                ClearMovementScript();
        }

        private IEnumerator Delayed_activeSceneChanged(Scene from, Scene to)
        {
            yield return new WaitForSeconds(0.05f);
            if(!_cameraMovement || Config.movementAudioSync)
            {
                string scriptpath = AddMovementScript();
                Logger.log.Notice($"{this.name} Add MoveScript \"{Path.GetFileName(scriptpath)}\" successfully initialized! {Convert.ToString(_cam.cullingMask, 16)}");
            }
        }

        protected virtual void Update()
        {
            // Only toggle the main camera in/out of third person with f1, not any extra cams
            if (_isMainCamera)
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    ThirdPerson = !ThirdPerson;
                    if (!ThirdPerson)
                    {
                        transform.position = _mainCamera.transform.position;
                        transform.rotation = _mainCamera.transform.rotation;
                    }
                    else
                    {
                        ThirdPersonPos = Config.Position;
                        ThirdPersonRot = Config.Rotation;
                    }

                    Config.thirdPerson = ThirdPerson;
                    Config.Save();
                }
            }
            HandleMouseEvents();
        }

        protected virtual void LateUpdate()
        {
            try
            {
                OffsetPosition = Vector3.zero;
                OffsetAngle = Vector3.zero;

                var camera = _mainCamera.transform;

                if (ThirdPerson)
                {
                    if (FPFCPatch.isInstanceFPFC)
                    {
                        if (isFPFC != FPFCPatch.instance.isActiveAndEnabled)
                        {
                            isFPFC = FPFCPatch.instance.enabled;
                            turnToHead = false;
                            replaceFPFC = true;
                            CreateScreenRenderTexture();
                        }
                        else
                            turnToHead = Config.turnToHead;
                    }
#if WithVMCAvatar
                    if (Plugin.cameraController.existsVMCAvatar)
                        if (Config.VMCProtocolMode == "receiver" && marionette)
                            if (marionette.receivedData)
                            {
                                transform.position = marionette.position;
                                transform.rotation = marionette.rotate;
                                _cam.fieldOfView = marionette.fov > 0 ? marionette.fov : Config.fov;
                                Logger.log.Notice($"Receive Marionette");
                                return;
                            }
#endif
                    HandleMultiPlayerLobby();
                    HandleMultiPlayerGame();
                    HandleThirdPerson360();

                    if (Config.NoodleTrack && SceneManager.GetActiveScene().name == "GameCore")
                    {
                        if (adjustOffset == null)
                        {
                            adjustOffset = new GameObject("OriginTarget");
                            adjustParent = new GameObject("OriginParent");
                            adjustOffset.transform.SetParent(adjustParent.transform);
                            Plugin.cameraController.origin = new GameObject("OriginParent").transform;
                        }
                        adjustParent.transform.position = Plugin.cameraController.origin.position - RoomAdjustPatch.position;
                        adjustParent.transform.localRotation = Plugin.cameraController.origin.localRotation * Quaternion.Inverse(RoomAdjustPatch.rotation);

                        adjustOffset.transform.localPosition = ThirdPersonPos;
                        adjustOffset.transform.localEulerAngles = ThirdPersonRot;

                        transform.position = adjustOffset.transform.position;
                        transform.eulerAngles = adjustOffset.transform.eulerAngles;
                    }
                    else
                    {
                        transform.position = ThirdPersonPos;
                        transform.eulerAngles = ThirdPersonRot;
                    }

                    if (OffsetPosition != Vector3.zero && OffsetAngle != Vector3.zero)
                    {
                        transform.position = ThirdPersonPos + OffsetPosition;
                        transform.eulerAngles = ThirdPersonRot + OffsetAngle;
                        Quaternion angle = Quaternion.AngleAxis(OffsetAngle.y, Vector3.up);
                        transform.position -= OffsetPosition;
                        transform.position = angle * transform.position;
                        transform.position += OffsetPosition;

                    }
                    if (turnToHead)
                    {
                        turnToTarget = Camera.main.transform;
                        turnToTarget.transform.position += turnToHeadOffset;
                        transform.LookAt(turnToTarget);
                    }

                    _cameraCube.position = transform.position;
                    _cameraCube.eulerAngles = transform.eulerAngles;

                    if (externalSender != null & Config.VMCProtocolMode == "sender")
                    {
                        externalSender.position = ThirdPersonPos;
                        externalSender.rotation = Quaternion.Euler(ThirdPersonRot);
                        externalSender.update = true;
                    }
                    return;
                }
                transform.position = Vector3.Lerp(transform.position, camera.position + Config.FirstPersonPositionOffset,
                    Config.positionSmooth * Time.unscaledDeltaTime);

                if (!Config.forceFirstPersonUpRight)
                    transform.rotation = Quaternion.Slerp(transform.rotation, camera.rotation * Quaternion.Euler(Config.FirstPersonRotationOffset),
                        Config.rotationSmooth * Time.unscaledDeltaTime);
                else

                {
                    Quaternion rot = Quaternion.Slerp(transform.rotation, camera.rotation * Quaternion.Euler(Config.FirstPersonRotationOffset),
                        Config.rotationSmooth * Time.unscaledDeltaTime);
                    transform.rotation = rot * Quaternion.Euler(0, 0, -(rot.eulerAngles.z));
                }
            }
            catch { }
        }

        private void HandleThirdPerson360()
        {
            if (!_beatLineManager || !Config.use360Camera || !_environmentSpawnRotation)
            {
                _beatLineManager = BeatLineManagerPatch.Instance;
                _environmentSpawnRotation = EnvironmentSpawnRotationPatch.Instance;
                return;
            }
            float b;
            if (_beatLineManager.isMidRotationValid)
            {
                double midRotation = (double)this._beatLineManager.midRotation;
                float num1 = Mathf.DeltaAngle((float)midRotation, this._environmentSpawnRotation.targetRotation);
                float num2 = (float)(-(double)this._beatLineManager.rotationRange * 0.5);
                float num3 = this._beatLineManager.rotationRange * 0.5f;
                if ((double)num1 > (double)num3)
                    num3 = num1;
                else if ((double)num1 < (double)num2)
                    num2 = num1;
                b = (float)(midRotation + ((double)num2 + (double)num3) * 0.5);
            }
            else
                b = this._environmentSpawnRotation.targetRotation;

            if (Config.cam360RotateControlNew)
                _yAngle = Mathf.LerpAngle(_yAngle, b, Mathf.Clamp(Time.deltaTime * Config.cam360Smoothness, 0f, 1f));
            else
                _yAngle = Mathf.Lerp(_yAngle, b, Mathf.Clamp(Time.deltaTime * Config.cam360Smoothness, 0f, 1f));

            ThirdPersonRot = new Vector3(Config.angx, _yAngle + Config.angy, Config.angz);

            ThirdPersonPos = (transform.forward * Config.posz) + (transform.right * Config.posx);
            ThirdPersonPos = new Vector3(ThirdPersonPos.x, Config.posy, ThirdPersonPos.z);
        }

        private void HandleMultiPlayerLobby()
        {
            try
            {
                if (!MultiplayerLobbyAvatarPlaceManagerPatch.Instance || !MultiplayerLobbyControllerPatch.Instance.isActiveAndEnabled || Config.MultiPlayerNumber == 0) return;
                if (MultiplayerSession.LobbyAvatarPlaceList.Count == 0) MultiplayerSession.LoadLobbyAvatarPlace();

                for (int i = 0; i < MultiplayerSession.LobbyAvatarPlaceList.Count; i++)
                {
                    if (i == Config.MultiPlayerNumber - 1)
                    {
                        OffsetPosition = MultiplayerSession.LobbyAvatarPlaceList[i].position;
                        OffsetAngle = MultiplayerSession.LobbyAvatarPlaceList[i].eulerAngles;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error($"HandleMultiPlayerLobby Error {ex.Message}");
            }
        }
        private void HandleMultiPlayerGame()
        {
            try
            {
                if (SceneManager.GetActiveScene().name == "GameCore" && MultiplayerSession.ConnectedMultiplay)
                {
                    MultiplayerConnectedPlayerFacade player = null;
                    bool TryPlayerFacade;
                    if (MultiplayerPlayersManagerPatch.Instance && Config.MultiPlayerNumber != 0)
                        foreach (IConnectedPlayer connectedPlayer in MultiplayerSession.connectedPlayers)
                            if (Config.MultiPlayerNumber - 1 == connectedPlayer.sortIndex)
                            {
                                TryPlayerFacade = MultiplayerPlayersManagerPatch.Instance.TryGetConnectedPlayerController(connectedPlayer.userId, out player);
                                if (TryPlayerFacade && player != null)
                                {
                                    OffsetPosition = player.transform.position;
                                    OffsetAngle = player.transform.eulerAngles;
                                }
                                break;
                            }
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error($"{this.name} HandleMultiPlayerGame Error {ex.Message}");
            }
        }

        public string AddMovementScript()
        {
            string songScriptPath = String.Empty;
            if (Config.VMCProtocolMode == "receiver") return "ExternalReceiver Enabled";

            if (Config.movementScriptPath != String.Empty || Config.songSpecificScript)
            {
                if (_cameraMovement)
                    _cameraMovement.Shutdown();

                if (CustomPreviewBeatmapLevelPatch.customLevelPath != String.Empty && Config.songSpecificScript)
                    songScriptPath = CustomPreviewBeatmapLevelPatch.customLevelPath;
                else if (File.Exists(Path.Combine(CameraUtilities.scriptPath, Path.GetFileName(Config.movementScriptPath))))
                    songScriptPath = Path.Combine(CameraUtilities.scriptPath, Path.GetFileName(Config.movementScriptPath));
                else
                    return "Not Find Script";

                _cameraMovement = _cam.gameObject.AddComponent<CameraMovement>();

                if (_cameraMovement.Init(this, songScriptPath))
                {
                    ThirdPersonPos = Config.Position;
                    ThirdPersonRot = Config.Rotation;
                    Config.thirdPerson = true;
                    ThirdPerson = true;
                    CreateScreenRenderTexture();
                }
                else
                    return "Fail CameraMovement Initialize";
                return songScriptPath;
            }
            return string.Empty;
        }
        public void ClearMovementScript()
        {
            if (_cameraMovement)
                _cameraMovement.Shutdown();
            _cameraMovement = null;
            ThirdPersonPos = Config.Position;
            ThirdPersonRot = Config.Rotation;
            SetFOV();
            CreateScreenRenderTexture();
        }

        protected IEnumerator GetMainCamera()
        {
            yield return _waitForMainCamera;
            _mainCamera = Camera.main;
        }

        protected IEnumerator Get360Managers()
        {
            yield return _waitForMainCamera;

            _beatLineManager = null;
            _environmentSpawnRotation = null;

            if (BeatLineManagerPatch.Instance)
            {
                _beatLineManager = BeatLineManagerPatch.Instance;
                _environmentSpawnRotation = EnvironmentSpawnRotationPatch.Instance;
            }

            if (_beatLineManager)
                this._yAngle = _beatLineManager.midRotation;
        }

        internal virtual void SetFOV()
        {
            if (_cam == null) return;
            _cam.fieldOfView = Config.fov;
        }

        internal virtual void FOV(float FOV)
        {
            _cam.fieldOfView = FOV;
        }

        internal virtual float GetFOV()
        {
            return _cam.fieldOfView;
        }

        internal virtual void SetCullingMask()
        {
            int builder = Camera.main.cullingMask;

            Debug.Log("Greenscreen Mode: " + Config.GreenScreenMode);
            _cam.backgroundColor = Color.black;
            _cam.clearFlags = CameraClearFlags.Color;

            if (Config.GreenScreenMode != "off")
            {
                builder = 0;
                if (Config.GreenScreenMode == "transparent")
                {
                    _cam.backgroundColor = Color.clear;
                } else if(Config.GreenScreenMode == "greenscreen")
                {
                    _cam.backgroundColor = Color.green;
                }

                if (Config.GreenScreenAvatar)
                {
                    if (Config.thirdPerson || Config.use360Camera)
                    {
                        builder |= 1 << OnlyInThirdPerson;
                        builder &= ~(1 << OnlyInFirstPerson);
                    }
                    else
                    {
                        builder |= 1 << OnlyInFirstPerson;
                        builder &= ~(1 << OnlyInThirdPerson);
                    }
                    builder |= 1 << AlwaysVisible;
                }
                else
                {
                    builder &= ~(1 << OnlyInThirdPerson);
                    builder &= ~(1 << OnlyInFirstPerson);
                    builder &= ~(1 << AlwaysVisible);
                }
                if (Config.GreenScreenDebri != "link")
                {
                    if (Config.GreenScreenDebri == "show")
                        builder |= (1 << NotesDebriLayer);
                    else
                        builder &= ~(1 << NotesDebriLayer);
                }
                if (Config.GreenScreenNotes)
                {
                    builder &= ~(1 << CustomNoteLayer);
                    builder |= 1 << NoteLayer;
                }
                else
                {
                    builder &= ~(1 << CustomNoteLayer);
                    builder &= ~(1 << NoteLayer);
                }
                if (Config.GreenScreenSaber)
                {
                    builder |= 1 << SaberLayer;
                } else
                {
                    builder &= ~(1 << SaberLayer);
                }

                _cam.cullingMask = builder;
                return;
            }

            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Color.black;

            if (Config.transparentWalls)
                builder &= ~(1 << TransparentWallsPatch.WallLayerMask);
            else
                builder |= (1 << TransparentWallsPatch.WallLayerMask);
            if (Config.avatar)
            {
                if (Config.thirdPerson || Config.use360Camera)
                {
                    builder |= 1 << OnlyInThirdPerson;
                    builder &= ~(1 << OnlyInFirstPerson);
                }
                else
                {
                    builder |= 1 << OnlyInFirstPerson;
                    builder &= ~(1 << OnlyInThirdPerson);
                }
                builder |= 1 << AlwaysVisible;
            }
            else
            {
                builder &= ~(1 << OnlyInThirdPerson);
                builder &= ~(1 << OnlyInFirstPerson);
                builder &= ~(1 << AlwaysVisible);
            }
            if (Config.debri != "link")
            {
                if (Config.debri == "show")
                    builder |= (1 << NotesDebriLayer);
                else
                    builder &= ~(1 << NotesDebriLayer);
            }
            if (Config.HideUI)
                builder &= ~(1 << UILayer);
            else
                builder |= (1 << UILayer);
            if (Config.Notes)
            {
                builder &= ~(1 << CustomNoteLayer);
                builder |= 1 << NoteLayer;
            }
            else
            {
                builder &= ~(1 << CustomNoteLayer);
                builder &= ~(1 << NoteLayer);
            }

            _cam.cullingMask = builder;
        }

        public bool IsWithinRenderArea(Vector2 mousePos, Config c)
        {
            if (mousePos.x < c.screenPosX) return false;
            if (mousePos.x > c.screenPosX + c.screenWidth) return false;
            if (mousePos.y < c.screenPosY) return false;
            if (mousePos.y > c.screenPosY + c.screenHeight) return false;
            return true;
        }

        public bool IsTopmostRenderAreaAtPos(Vector2 mousePos)
        {
            if (!IsWithinRenderArea(mousePos, Config)) return false;
            foreach (CameraPlusInstance c in Plugin.cameraController.Cameras.Values.ToArray())
            {
                if (c.Instance == this) continue;
                if (!IsWithinRenderArea(mousePos, c.Config) && !c.Instance._mouseHeld) continue;
                if (c.Config.layer > Config.layer)
                {
                    return false;
                }

                if (c.Config.layer == Config.layer &&
                    c.Instance._lastRenderUpdate > _lastRenderUpdate)
                {
                    return false;
                }

                if (c.Instance._mouseHeld && (c.Instance._isMoving ||
                    c.Instance._isResizing || c.Instance._contextMenuOpen))
                {
                    return false;
                }
            }
            return true;
        }

        public static CameraPlusBehaviour GetTopmostInstanceAtCursorPos()
        {
            foreach (CameraPlusInstance c in Plugin.cameraController.Cameras.Values.ToArray())
            {
                if (c.Instance._isTopmostAtCursorPos)
                    return c.Instance;
            }
            return null;
        }

        internal void CloseContextMenu()
        {
            if (_contextMenu != null)
                _contextMenu.DisableMenu();
            Destroy(MenuObj);
            _contextMenuOpen = false;
        }

        public static void SetCursor(CursorType type)
        {
            if (type != currentCursor)
            {
                Texture2D texture = null;
                switch (type)
                {
                    case CursorType.Horizontal:
                        texture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.Resize_Horiz.png");
                        break;
                    case CursorType.Vertical:
                        texture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.Resize_Vert.png");
                        break;
                    case CursorType.DiagonalRight:
                        texture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.Resize_DiagRight.png");
                        break;
                    case CursorType.DiagonalLeft:
                        texture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.Resize_DiagLeft.png");
                        break;
                }
                UnityEngine.Cursor.SetCursor(texture, texture ? new Vector2(texture.width / 2, texture.height / 2) : new Vector2(0, 0), CursorMode.Auto);
                currentCursor = type;
            }
        }

        protected void HandleMouseEvents()
        {
            bool holdingLeftClick = Input.GetMouseButton(0);
            bool holdingRightClick = Input.GetMouseButton(1);
            bool holdingMiddleClick = Input.GetMouseButton(2);

            Vector3 mousePos = Input.mousePosition;

            _isTopmostAtCursorPos = IsTopmostRenderAreaAtPos(mousePos);
            if (_isTopmostAtCursorPos && mouseMoveCamera)
            {
                if (Input.GetMouseButtonDown(2))
                    prevMousePos = mousePos;

                if (holdingLeftClick)
                {
                    float scroll = Input.mouseScrollDelta.y;
                    if (scroll != 0)
                        ThirdPersonRot.z += scroll * CameraUtilities.mouseRotateSpeed[2];
                }

                if (holdingMiddleClick)
                    if (mousePos != prevMousePos)
                    {
                        Vector3 up = transform.TransformDirection(Vector3.up);
                        Vector3 right = transform.TransformDirection(Vector3.right);

                        ThirdPersonPos += right * (mousePos.x - prevMousePos.x) * CameraUtilities.mouseMoveSpeed[0] +
                                            up * (mousePos.y - prevMousePos.y) * CameraUtilities.mouseMoveSpeed[1];
                    }

                if (holdingRightClick)
                {
                    float scroll = Input.mouseScrollDelta.y;
                    if (scroll != 0)
                        ThirdPersonPos += transform.forward * scroll * CameraUtilities.mouseScrollSpeed;

                    if (mousePos != prevMousePos)
                    {
                        ThirdPersonRot.x += (mousePos.y - prevMousePos.y) * CameraUtilities.mouseRotateSpeed[0];
                        ThirdPersonRot.y += (mousePos.x - prevMousePos.x) * CameraUtilities.mouseRotateSpeed[1];
                    }
                }

                if (Input.GetMouseButtonDown(1))
                {
                    mouseRightDownPos = mousePos;
                    holdingRightClick = false;
                }
                else if (Input.GetMouseButtonUp(1))
                    holdingRightClick = (Vector3.Distance(mouseRightDownPos, mousePos) < 4);
                else
                    holdingRightClick = false;

                if (mouseMoveCameraSave && (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2)))
                {
                    Config.Position = ThirdPersonPos;
                    Config.Rotation = ThirdPersonRot;
                    Config.Save();
                }

                prevMousePos = mousePos;
            }

            // Only evaluate mouse events for the topmost render target at the mouse position
            if (!_mouseHeld && !_isTopmostAtCursorPos) return;

            int tolerance = 5;
            bool cursorWithinBorder = CustomUtils.WithinRange((int)mousePos.x, -tolerance, tolerance) || CustomUtils.WithinRange((int)mousePos.y, -tolerance, tolerance) ||
                CustomUtils.WithinRange((int)mousePos.x, Config.screenPosX + Config.screenWidth - tolerance, Config.screenPosX + Config.screenWidth + tolerance) ||
                CustomUtils.WithinRange((int)mousePos.x, Config.screenPosX - tolerance, Config.screenPosX + tolerance) ||
                CustomUtils.WithinRange((int)mousePos.y, Config.screenPosY + Config.screenHeight - tolerance, Config.screenPosY + Config.screenHeight + tolerance) ||
                CustomUtils.WithinRange((int)mousePos.y, Config.screenPosY - tolerance, Config.screenPosY + tolerance);

            float currentMouseOffsetX = mousePos.x - Config.screenPosX;
            float currentMouseOffsetY = mousePos.y - Config.screenPosY;
            if (!_mouseHeld)
            {
                if (cursorWithinBorder)
                {
                    var isLeft = currentMouseOffsetX <= Config.screenWidth / 2;
                    var isBottom = currentMouseOffsetY <= Config.screenHeight / 2;
                    var centerX = Config.screenPosX + (Config.screenWidth / 2);
                    var centerY = Config.screenPosY + (Config.screenHeight / 2);
                    var offsetX = Config.screenWidth / 2 - tolerance;
                    var offsetY = Config.screenHeight / 2 - tolerance;
                    _xAxisLocked = CustomUtils.WithinRange((int)mousePos.x, centerX - offsetX + 1, centerX + offsetX - 1);
                    _yAxisLocked = CustomUtils.WithinRange((int)mousePos.y, centerY - offsetY + 1, centerY + offsetY - 1);

                    if (!Config.fitToCanvas)
                    {
                        if (_xAxisLocked)
                            SetCursor(CursorType.Vertical);
                        else if (_yAxisLocked)
                            SetCursor(CursorType.Horizontal);
                        else if (isLeft && isBottom || !isLeft && !isBottom)
                            SetCursor(CursorType.DiagonalLeft);
                        else if (isLeft && !isBottom || !isLeft && isBottom)
                            SetCursor(CursorType.DiagonalRight);
                    }
                    wasWithinBorder = true;
                }
                else if (!cursorWithinBorder && wasWithinBorder)
                {
                    SetCursor(CursorType.None);
                    wasWithinBorder = false;
                }
            }

            if (holdingLeftClick && !Config.fitToCanvas && !Config.LockScreen)
            {
                if (!_mouseHeld)
                {
                    _initialOffset.x = currentMouseOffsetX;
                    _initialOffset.y = currentMouseOffsetY;

                    _lastScreenPos = Config.ScreenPosition;
                    _lastGrabPos = new Vector2(mousePos.x, mousePos.y);

                    _isLeft = _initialOffset.x <= Config.screenWidth / 2;
                    _isBottom = _initialOffset.y <= Config.screenHeight / 2;
                    anyInstanceBusy = true;
                }
                _mouseHeld = true;

                if (!_isMoving && (_isResizing || cursorWithinBorder))
                {
                    _isResizing = true;
                    if (!_xAxisLocked)
                    {
                        int changeX = _isLeft ? (int)(_lastGrabPos.x - mousePos.x) : (int)(mousePos.x - _lastGrabPos.x);
                        Config.screenWidth += changeX;
                        Config.screenPosX = ((int)_lastScreenPos.x - (_isLeft ? changeX : 0));
                    }
                    if (!_yAxisLocked)
                    {
                        int changeY = _isBottom ? (int)(mousePos.y - _lastGrabPos.y) : (int)(_lastGrabPos.y - mousePos.y);
                        Config.screenHeight -= changeY;
                        Config.screenPosY = ((int)_lastScreenPos.y + (_isBottom ? changeY : 0));
                    }
                    _lastGrabPos = mousePos;
                    _lastScreenPos = Config.ScreenPosition;
                }
                else
                {
                    _isMoving = true;
                    Config.screenPosX = (int)mousePos.x - (int)_initialOffset.x;
                    Config.screenPosY = (int)mousePos.y - (int)_initialOffset.y;
                }
                Config.screenWidth = Mathf.Clamp(Config.screenWidth, 100, Screen.width);
                Config.screenHeight = Mathf.Clamp(Config.screenHeight, 100, Screen.height);
                Config.screenPosX = Mathf.Clamp(Config.screenPosX, 0, Screen.width - Config.screenWidth);
                Config.screenPosY = Mathf.Clamp(Config.screenPosY, 0, Screen.height - Config.screenHeight);

                CreateScreenRenderTexture();
            }
            else if (holdingRightClick && _contextMenuEnabled)
            {
                if (_mouseHeld) return;
                DisplayContextMenu();
                _contextMenuOpen = true;
                anyInstanceBusy = true;
                _mouseHeld = true;
            }
            else if (_isResizing || _isMoving || _mouseHeld)
            {
                if (!_contextMenuOpen)
                {
                    if (!_isCameraDestroyed)
                    {
                        Config.Save();
                    }
                }
                _isResizing = false;
                _isMoving = false;
                _mouseHeld = false;
                anyInstanceBusy = false;
            }
        }
        void OnGUI()
        {
            if (MultiplayerSession.connectedPlayers != null && Config.DisplayMultiPlayerNameInfo)
                foreach (IConnectedPlayer connectedPlayer in MultiplayerSession.connectedPlayers)
                    if (Config.MultiPlayerNumber - 1 == connectedPlayer.sortIndex)
                    {
                        int size = 0;
                        var offsetY = Screen.height / 2;

                        GUI.skin.label.fontSize = Config.screenWidth / 8;
                        size = GUI.skin.label.fontSize + 15;

                        GUI.Label(new Rect(Config.screenPosX, Screen.height - Config.screenPosY - Config.screenHeight, Config.screenWidth, GUI.skin.label.fontSize + 15), connectedPlayer.userName);

                        if (SceneManager.GetActiveScene().name == "GameCore" && MultiplayerSession.ConnectedMultiplay)
                        {
                            if (MultiplayerScoreProviderPatch.Instance)
                            {
                                foreach (MultiplayerScoreProvider.RankedPlayer rankedPlayer in MultiplayerScoreProviderPatch.Instance.rankedPlayers)
                                    if (rankedPlayer.userId == connectedPlayer.userId)
                                    {
                                        if (_multiplayerGUIStyle == null)
                                            _multiplayerGUIStyle = new GUIStyle(GUI.skin.label);
                                        if (rankedPlayer.isFailed)
                                            _multiplayerGUIStyle.normal.textColor = Color.red;
                                        else
                                            _multiplayerGUIStyle.normal.textColor = Color.white;
                                        _multiplayerGUIStyle.fontSize = 30;
                                        GUI.Label(new Rect(Config.screenPosX, Screen.height - Config.screenPosY - Config.screenHeight + size + 45, Config.screenWidth, 40), String.Format("{0:#,0}", rankedPlayer.score), _multiplayerGUIStyle);
                                        GUI.Label(new Rect(Config.screenPosX, Screen.height - Config.screenPosY - Config.screenHeight + size + 5, Config.screenWidth, 40), "Rank " + MultiplayerScoreProviderPatch.Instance.GetPositionOfPlayer(connectedPlayer.userId).ToString(), _multiplayerGUIStyle);
                                        break;
                                    }
                                break;
                            }
                        }
                    }
            if (scriptEditMode)
            {
                if (_scriptEditMenu == null) _scriptEditMenu = new BehaviourScriptEdit();
                _scriptEditMenu.DisplayUI(this);
            }
        }
        void DisplayContextMenu()
        {
            if (scriptEditMode) return;
            if (_contextMenu == null)
            {
                MenuObj = new GameObject("CameraPlusMenu");
                _contextMenu = MenuObj.AddComponent<UI.ContextMenu>();
            }
            _contextMenu.EnableMenu(Input.mousePosition, this);
        }
    }
}
