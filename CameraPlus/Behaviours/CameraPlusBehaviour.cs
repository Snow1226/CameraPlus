#define WITH_VMCA
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

        public bool ThirdPerson { get => Config.thirdPerson; set { Config.thirdPerson = value; }}
        internal float FOV { get => _cam.fieldOfView; set { _cam.fieldOfView = value; } }

        public Vector3 ThirdPersonPos;
        public Vector3 ThirdPersonRot;
        public Vector3 OffsetPosition;
        public Vector3 OffsetAngle;
        public CameraConfig Config = null;
        public bool RunCullingMask = false;

        public Rect ScreenRect = new Rect();
        public Vector2 ScreenPosition
        {
            get
            {
                return new Vector2(ScreenRect.x, ScreenRect.y);
            }
            set
            {
                ScreenRect.x = value.x;
                ScreenRect.y = value.y;
            }
        }

        internal Camera _cam;
        internal CameraPreviewQuad _quad;
        internal RenderTexture _camRenderTexture;
        internal CameraOrigin _cameraOrigin;
        internal GameObject _originOffset;
        protected Camera _mainCamera = null;
        protected CameraMovement _cameraMovement = null;

        protected int _prevLayer;
        protected int _prevScreenPosX, _prevScreenPosY;

        protected bool _wasWindowActive = false;
        protected bool _mouseHeld = false;
        protected bool _isResizing = false;
        protected bool _isMoving = false;
        protected bool _xAxisLocked = false;
        protected bool _yAxisLocked = false;
        internal bool _isCameraDestroyed = false;
        internal bool _isMainCamera = false;
        internal bool _isTopmostAtCursorPos = false;
        protected DateTime _lastRenderUpdate;
        protected Vector2 _initialOffset = new Vector2(0, 0);
        protected Vector2 _lastGrabPos = new Vector2(0, 0);
        protected Vector2 _lastScreenPos;
        protected bool _isBottom = false, _isLeft = false;
        protected static GameObject MenuObj = null;
        public static CursorType currentCursor = CursorType.None;
        public static bool wasWithinBorder = false;
        public static bool anyInstanceBusy = false;
        private GUIStyle _multiplayerGUIStyle = null;
        private Vector3 prevMousePos = Vector3.zero;
        private Vector3 mouseRightDownPos = Vector3.zero;
        internal bool mouseMoveCamera = false;
        internal bool mouseMoveCameraSave = false;
        internal bool scriptEditMode = false;
        private Transform turnToTarget;
        internal bool turnToHead = false;
        internal Vector3 turnToHeadOffset = Vector3.zero;
        internal bool turnToHeadHorizontal = false;
        internal WebCamScreen webCamScreen = null;
        internal CameraEffectStruct effectElements = new CameraEffectStruct();
        private bool _initializeExternalSender = false;
        internal bool renderScreen = true;

#if WITH_VMCA
        private VMCAvatarMarionette marionette = null;
#endif
        public virtual void Init(CameraConfig config)
        {
            Config = config;
            Config.cam = this;
            _isMainCamera = Path.GetFileName(Config.FilePath) == $"{Plugin.MainCamera}.json";

            XRSettings.showDeviceView = false;

            Config.ConfigChangedEvent += PluginOnConfigChangedEvent;

            _cameraOrigin = new GameObject("CameraPlusOrigin").AddComponent<CameraOrigin>();
            _cameraOrigin.transform.SetParent(transform);
            _cameraOrigin._cameraPlus = this;

            _originOffset = new GameObject("OriginOffset");
            _originOffset.transform.SetParent(_cameraOrigin.transform);

            var gameObj = Instantiate(CameraUtilities.GetMainCamera(), Vector3.zero, Quaternion.identity, _cameraOrigin.gameObject.transform);
            gameObj.transform.localScale = Vector3.one;

            gameObj.SetActive(false);
            gameObj.name = "Camera Plus";
            gameObj.tag = "Untagged";

            _cam = gameObj.GetComponent<Camera>();
            _cam.stereoTargetEye = StereoTargetEyeMask.None;
            _cam.enabled = true;
            _cam.name = Path.GetFileName(Config.FilePath);

            foreach (var child in _cam.transform.Cast<Transform>())
                Destroy(child.gameObject);
            var destroyList = new string[] { "AudioListener", "LIV", "MainCamera", "MeshCollider", "TrackedPoseDriver", "DepthTextureController" };
            foreach (var component in _cam.GetComponents<Behaviour>())
                if (destroyList.Contains(component.GetType().Name)) Destroy(component);

            gameObj.SetActive(true);

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            _quad = new GameObject("PreviewQuad").AddComponent<CameraPreviewQuad>();
            _quad.transform.SetParent(_cam.transform);
            _quad.transform.localPosition = Vector3.zero;
            _quad.transform.localEulerAngles = Vector3.zero;
            _quad.Init(this);

            ReadConfig();

            if (ThirdPerson)
            {
                ThirdPersonPos = Config.Position;
                ThirdPersonRot = Config.Rotation;

                _cam.transform.position = ThirdPersonPos;
                _cam.transform.eulerAngles = ThirdPersonRot;
            }

            AddMovementScript();

            Plugin.cameraController.ActiveSceneChanged += SceneManager_activeSceneChanged;
            SceneManager_activeSceneChanged(new Scene(), new Scene());
            Plugin.Log.Notice($"Camera \"{Path.GetFileName(Config.FilePath)}\" successfully initialized! {Convert.ToString(_cam.cullingMask, 16)}");

            if (Config.vmcProtocol.mode == VMCProtocolMode.Sender && !_initializeExternalSender)
                InitExternalSender();

            Plugin.cameraController.OnSetCullingMask.AddListener(OnCullingMaskChangeEvent);
            Plugin.cameraController.OnFPFCToggleEvent.AddListener(OnFPFCToglleEvent);
            if (Config.webCamera.autoConnect)
                CreateWebCamScreen();
        }

        internal void CreateWebCamScreen()
        {
            if (webCamScreen)
            {
                ChangeWebCamScreen();
                return;
            }
            webCamScreen = new GameObject("WebCamScreen").AddComponent<WebCamScreen>();
            webCamScreen.transform.SetParent(transform);
            webCamScreen.AddWebCamScreen(Config.webCamera.name, this);
        }
        internal void ChangeWebCamScreen()
        {
            if (!webCamScreen)
            {
                CreateWebCamScreen();
                return;
            }
            webCamScreen.ChangeCamera(Config.webCamera.name);
        }
        internal void DisableWebCamScreen()
        {
            if (!webCamScreen) return;
            webCamScreen.DisconnectWebCam();
            Destroy(webCamScreen.gameObject);
            webCamScreen = null;
        }

        public void InitExternalSender()
        {
            if (Config.vmcProtocol.mode == VMCProtocolMode.Sender)
            {
                Plugin.cameraController.externalSender.AddSendTask(this, Config.vmcProtocol.address, Config.vmcProtocol.port);
                _initializeExternalSender = true;
            }
        }
        public void InitExternalReceiver()
        {
#if WITH_VMCA
            if (Config.vmcProtocol.mode == VMCProtocolMode.Receiver && Plugin.cameraController.existsVMCAvatar)
            {
                marionette = this.gameObject.AddComponent<VMCProtocol.VMCAvatarMarionette>();
                ClearMovementScript();
            }
#endif
        }
        public void DestoryVMCProtocolObject()
        {
#if WITH_VMCA
            if (marionette)
                Destroy(marionette);
#endif
            Plugin.cameraController.externalSender.RemoveTask(this);

            if (!string.IsNullOrWhiteSpace(Config.movementScript.movementScript) || Config.movementScript.songSpecificScript)
                AddMovementScript();
        }

        public void OnEnable()
        {
            if (Config != null)
            {
                if (Config.vmcProtocol.mode == VMCProtocolMode.Sender)
                    InitExternalSender();
                if (!string.IsNullOrWhiteSpace(Config.movementScript.movementScript) || Config.movementScript.songSpecificScript)
                    AddMovementScript();
                if (!Config.cameraExtensions.dontDrawDesktop)
                    Plugin.cameraController.ScreenCamera.RegistrationCamera(this);
            }
        }

        public void OnDisable()
        {
            Plugin.cameraController.externalSender.RemoveTask(this);
            _initializeExternalSender = false;
            Plugin.cameraController.ScreenCamera.UnregistrationCamera(this);
        }

        protected virtual void OnDestroy()
        {
            Config.ConfigChangedEvent -= PluginOnConfigChangedEvent;
            Plugin.cameraController.ActiveSceneChanged -= SceneManager_activeSceneChanged;

            _cameraMovement?.Shutdown();

            _camRenderTexture?.Release();

#if WITH_VMCA
            if (marionette)
                Destroy(marionette);
#endif
            Plugin.cameraController.externalSender.RemoveTask(this);
            _initializeExternalSender = false;

            Plugin.cameraController.OnSetCullingMask.RemoveListener(OnCullingMaskChangeEvent);

            if (webCamScreen)
                DisableWebCamScreen();

            if (_quad)
                Destroy(_quad);
        }

        protected virtual void PluginOnConfigChangedEvent(CameraConfig config)
        {
            ReadConfig();
        }

        protected virtual void ReadConfig()
        {
            if (!ThirdPerson && _cam != null && _mainCamera != null)
            {
                _cam.transform.position = _mainCamera.transform.position;
                _cam.transform.rotation = _mainCamera.transform.rotation;
            }
            else
            {
                ThirdPersonPos = Config.Position;
                ThirdPersonRot = Config.Rotation;
            }

            if (!Config.cameraExtensions.dontDrawDesktop)
                Plugin.cameraController.ScreenCamera.RegistrationCamera(this);
            else
                Plugin.cameraController.ScreenCamera.UnregistrationCamera(this);

            turnToHead = Config.cameraExtensions.turnToHead;
            turnToHeadOffset = Config.TurnToHeadOffset;
            turnToHeadHorizontal = Config.cameraExtensions.turnToHeadHorizontal;
            Config.SetCullingMask(Config.visibleObject);
            _quad.gameObject.SetActive(ThirdPerson && Config.PreviewCamera);
            _cam.fieldOfView = Config.fov;
            _cam.orthographic = Config.cameraExtensions.orthographicMode;
            _cam.orthographicSize = Config.cameraExtensions.orthographicSize;
            _cam.nearClipPlane = Config.cameraExtensions.nearClip;
            _cam.farClipPlane = Config.cameraExtensions.farClip;

            effectElements = Config.cameraEffect;
            ScreenRect = Config.rect;
            renderScreen = !Config.cameraExtensions.dontDrawDesktop;
            CreateScreenRenderTexture();
            _quad.SetCameraQuadPosition(PluginConfig.Instance.CameraQuadPosition);
        }

        internal virtual void CreateScreenRenderTexture()
        {
            try
            {
                if (Config.fitToCanvas)
                {
                    Config.screenPosX = 0;
                    Config.screenPosY = 0;
                    Config.screenWidth = Screen.width;
                    Config.screenHeight = Screen.height;
                }
                var w = (int)Math.Round(Config.screenWidth * Config.renderScale);
                var h = (int)Math.Round(Config.screenHeight * Config.renderScale);

                var changed = _camRenderTexture?.width != w || _camRenderTexture?.height != h ||
                            _camRenderTexture?.antiAliasing != _camRenderTexture.antiAliasing;

                if (changed)
                {
                    _camRenderTexture?.Release();
                    _camRenderTexture = new RenderTexture(w, h, 24)
                    {
                        useMipMap = false,
                        antiAliasing = Config.antiAliasing,
                        anisoLevel = 1,
                        useDynamicScale = false
                    };
                    _cam.targetTexture = _camRenderTexture;
                    _quad._previewMaterial.SetTexture("_MainTex", _camRenderTexture);
                }

                _prevLayer = Config.layer;
                _prevScreenPosX = Config.screenPosX;
                _prevScreenPosY = Config.screenPosY;

            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Fail CreateScreenRenderTexture {ex}");
            }
        }
        
        public virtual void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            OnFPFCToglleEvent();
            if (this.gameObject.activeInHierarchy)
            {
                StartCoroutine(GetMainCamera());
                Config.SetCullingMask(Config.visibleObject);
            }
        }

        public void OnCullingMaskChangeEvent()
        {
            if (this.gameObject.activeInHierarchy)
            {
                StartCoroutine(GetMainCamera());
                Config.SetCullingMask(Config.visibleObject);
                OnFPFCToglleEvent();
            }
        }

        private void OnFPFCToglleEvent()
        {
            if (Plugin.cameraController.isFPFC)
                turnToHead = false;
            else
                turnToHead = Config.cameraExtensions.turnToHead;
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
                        _cam.transform.position = _mainCamera.transform.position;
                        _cam.transform.rotation = _mainCamera.transform.rotation;
                    }
                    else
                    {
                        ThirdPersonPos = Config.Position;
                        ThirdPersonRot = Config.Rotation;
                    }
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

                if (!_mainCamera || _cam==null) return;
                var camera = _mainCamera?.transform;

                if (ThirdPerson)
                {
#if WITH_VMCA
                    if (Plugin.cameraController.existsVMCAvatar)
                        if (Config.vmcProtocol.mode == VMCProtocolMode.Receiver && marionette)
                            if (marionette.receivedData)
                            {
                                transform.position = marionette.position;
                                transform.rotation = marionette.rotate;
                                _cam.fieldOfView = marionette.fov > 0 ? marionette.fov : Config.fov;
                                return;
                            }
#endif

                    HandleMultiPlayerLobby();
                    HandleMultiPlayerGame();

                    _cam.transform.localPosition = ThirdPersonPos;
                    _cam.transform.localEulerAngles = ThirdPersonRot;

                    if (OffsetPosition != Vector3.zero && OffsetAngle != Vector3.zero)
                    {
                        _cam.transform.localPosition = ThirdPersonPos + OffsetPosition;
                        _cam.transform.localEulerAngles = ThirdPersonRot + OffsetAngle;
                        Quaternion angle = Quaternion.AngleAxis(OffsetAngle.y, Vector3.up);
                        _cam.transform.localPosition -= OffsetPosition;
                        _cam.transform.localPosition = angle * transform.position;
                        _cam.transform.localPosition += OffsetPosition;

                    }
                    if (Camera.main && effectElements.dofAutoDistance)
                    {
                        effectElements.dofFocusDistance = (Camera.main.transform.position - _cam.transform.position).magnitude;
                    }
                    if (Camera.main && turnToHead && !Plugin.cameraController.isFPFC && !Config.cameraExtensions.follow360map)
                    {
                        turnToTarget = Camera.main.transform;
                        turnToTarget.transform.position += turnToHeadOffset;
                        var direction = turnToTarget.position - _cam.transform.position;
                        var lookRotation = Quaternion.LookRotation(direction);
                        if (turnToHeadHorizontal)
                            _cam.transform.localEulerAngles = new Vector3(_cam.transform.eulerAngles.x,lookRotation.eulerAngles.y, _cam.transform.eulerAngles.z);
                        else
                            _cam.transform.localRotation = lookRotation;
                        //transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Config.cameraExtensions.rotationSmooth);
                        turnToTarget.transform.localPosition -= turnToHeadOffset;
                    }
                    return;
                }
                _cam.transform.position = Vector3.Lerp(_cam.transform.position, camera.position + Config.FirstPersonPositionOffset,
                    Config.cameraExtensions.positionSmooth * Time.unscaledDeltaTime);

                if (!Config.cameraExtensions.firstPersonCameraForceUpRight)
                    _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, camera.rotation * Quaternion.Euler(Config.FirstPersonRotationOffset),
                        Config.cameraExtensions.rotationSmooth * Time.unscaledDeltaTime);
                else

                {
                    Quaternion rot = Quaternion.Slerp(_cam.transform.rotation, camera.rotation * Quaternion.Euler(Config.FirstPersonRotationOffset),
                        Config.cameraExtensions.rotationSmooth * Time.unscaledDeltaTime);
                    _cam.transform.rotation = rot * Quaternion.Euler(0, 0, -(rot.eulerAngles.z));
                }
            }
            catch(Exception ex)
            {
                Plugin.Log.Error($"CameraPlus {this.name}, Error in LateUpdate : {ex}");
            }
        }

        private void HandleMultiPlayerLobby()
        {
            try
            {
                if (!MultiplayerLobbyAvatarPlaceManagerPatch.Instance || !MultiplayerLobbyControllerPatch.Instance.isActiveAndEnabled || Config.multiplayer.targetPlayerNumber == 0) return;
                if (MultiplayerSession.LobbyAvatarPlaceList.Count == 0) MultiplayerSession.LoadLobbyAvatarPlace();

                for (int i = 0; i < MultiplayerSession.LobbyAvatarPlaceList.Count; i++)
                {
                    if (i == Config.multiplayer.targetPlayerNumber - 1)
                    {
                        OffsetPosition = MultiplayerSession.LobbyAvatarPlaceList[i].position;
                        OffsetAngle = MultiplayerSession.LobbyAvatarPlaceList[i].eulerAngles;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"HandleMultiPlayerLobby Error {ex.Message}");
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
                    if (MultiplayerPlayersManagerPatch.Instance && Config.multiplayer.targetPlayerNumber != 0)
                        foreach (IConnectedPlayer connectedPlayer in MultiplayerSession.connectedPlayers)
                            if (Config.multiplayer.targetPlayerNumber - 1 == connectedPlayer.sortIndex)
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
                Plugin.Log.Error($"{this.name} HandleMultiPlayerGame Error {ex.Message}");
            }
        }

        public string AddMovementScript()
        {
            string songScriptPath = String.Empty;
            if (Config.vmcProtocol.mode == VMCProtocolMode.Receiver) return "ExternalReceiver Enabled";
            if (!ThirdPerson) return "Camera Mode is First Person";

            if (!string.IsNullOrWhiteSpace( Config.movementScript.movementScript) || Config.movementScript.songSpecificScript)
            {
                ClearMovementScript();

                if (!string.IsNullOrWhiteSpace( SongScriptBeatmapPatch.customLevelPath) && Config.movementScript.songSpecificScript)
                    songScriptPath = SongScriptBeatmapPatch.customLevelPath;
                else if (File.Exists(Path.Combine(CameraUtilities.ScriptPath, Path.GetFileName(Config.movementScript.movementScript))))
                    songScriptPath = Path.Combine(CameraUtilities.ScriptPath, Path.GetFileName(Config.movementScript.movementScript));
                else
                    return "Not Find Script";

                _cameraMovement = _cam.gameObject.AddComponent<CameraMovement>();
                if (_cameraMovement.Init(this, songScriptPath))
                {
                    ThirdPersonPos = Config.Position;
                    ThirdPersonRot = Config.Rotation;
                    Config.thirdPerson = true;
                    ThirdPerson = true;
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
            {
                _cameraMovement.Shutdown();
                _cameraMovement = null;
                ThirdPersonPos = Config.Position;
                ThirdPersonRot = Config.Rotation;
                _cam.fieldOfView = Config.fov;
            }
        }

        protected IEnumerator GetMainCamera()
        {
            if (SceneManager.GetActiveScene().name == "GameCore")
            {
                while (CameraUtilities.GetMainCamera() == null)
                    yield return null;
                CameraUtilities.BaseCullingMask = Camera.main.cullingMask;
            }
            else
            {
                while (Camera.main == null)
                    yield return null;
            }
            _mainCamera = Camera.main;
        }

        public bool IsWithinRenderArea(Vector2 mousePos, CameraConfig c)
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
            foreach (CameraPlusBehaviour c in Plugin.cameraController.Cameras.Values)
            {
                if (c != this && c.gameObject.activeInHierarchy)
                {
                    if (!IsWithinRenderArea(mousePos, c.Config) && !c._mouseHeld) continue;

                    if (c.Config.rawLayer > Config.rawLayer)
                        return false;
                    if (c._mouseHeld && (c._isMoving || c._isResizing))
                        return false;
                }
            }
            return true;
        }

        public static void SetCursor(CursorType type)
        {
            if (type != currentCursor)
            {
                Texture2D texture = null;
                switch (type)
                {
                    case CursorType.Horizontal:
                        texture = MenuUI.MouseCursorTexture[0];
                        break;
                    case CursorType.Vertical:
                        texture = MenuUI.MouseCursorTexture[1];
                        break;
                    case CursorType.DiagonalRight:
                        texture = MenuUI.MouseCursorTexture[2];
                        break;
                    case CursorType.DiagonalLeft:
                        texture = MenuUI.MouseCursorTexture[3];
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
                        ThirdPersonRot.z += scroll * CameraUtilities.s_mouseRotateSpeed[2];
                }

                if (holdingMiddleClick)
                    if (mousePos != prevMousePos)
                    {
                        Vector3 up = transform.TransformDirection(Vector3.up);
                        Vector3 right = transform.TransformDirection(Vector3.right);

                        ThirdPersonPos += right * (mousePos.x - prevMousePos.x) * CameraUtilities.s_mouseMoveSpeed[0] +
                                            up * (mousePos.y - prevMousePos.y) * CameraUtilities.s_mouseMoveSpeed[1];
                    }

                if (holdingRightClick)
                {
                    float scroll = Input.mouseScrollDelta.y;
                    if (scroll != 0)
                        ThirdPersonPos += transform.forward * scroll * CameraUtilities.s_mouseScrollSpeed;

                    if (mousePos != prevMousePos)
                    {
                        ThirdPersonRot.x += (mousePos.y - prevMousePos.y) * CameraUtilities.s_mouseRotateSpeed[0];
                        ThirdPersonRot.y += (mousePos.x - prevMousePos.x) * CameraUtilities.s_mouseRotateSpeed[1];
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
            if (Plugin.cameraController._contextMenu.IsMenuEnter) return;

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

            if (holdingLeftClick && !Config.fitToCanvas && !Config.cameraLock.lockScreen)
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
                ScreenRect = Config.rect;
                webCamScreen?.ChangeWebCamRectScale(Config.screenHeight);

                CreateScreenRenderTexture();
            }
            else if (_isResizing || _isMoving || _mouseHeld)
            {
                if (!_isCameraDestroyed)
                {
                    Config.Save();
                }
                _isResizing = false;
                _isMoving = false;
                _mouseHeld = false;
                anyInstanceBusy = false;
            }
        }
        void OnGUI()
        {
            if (MultiplayerSession.connectedPlayers != null && Config.multiplayer.displayPlayerInfo)
            {
                foreach (IConnectedPlayer connectedPlayer in MultiplayerSession.connectedPlayers)
                {
                    if (Config.multiplayer.targetPlayerNumber - 1 == connectedPlayer.sortIndex)
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

                }
            }
        }
    }
}
