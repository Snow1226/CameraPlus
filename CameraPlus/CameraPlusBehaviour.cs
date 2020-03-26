using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using IPA.Utilities;
using LogLevel = IPA.Logging.Logger.Level;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using VRUIControls;
using Screen = UnityEngine.Screen;

namespace CameraPlus
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
        private readonly WaitForSecondsRealtime _waitForSecondsRealtime = new WaitForSecondsRealtime(1f);
        protected const int OnlyInThirdPerson = 3;
        protected const int OnlyInFirstPerson = 6; //Moved to an empty layer because layer 4 overlapped the floor

        public bool ThirdPerson {
            get { return _thirdPerson; }
            set {
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
        protected static ContextMenu _contextMenu = null;
        public static CursorType currentCursor = CursorType.None;
        public static bool wasWithinBorder = false;
        public static bool anyInstanceBusy = false;
        private static bool _contextMenuEnabled = true;

        public virtual void Init(Config config)
        {
            DontDestroyOnLoad(gameObject);
            Logger.Log("Created new camera plus behaviour component!");

            Config = config;
            _isMainCamera = Path.GetFileName(Config.FilePath) == $"{Plugin.MainCamera}.cfg";
            _contextMenuEnabled = !Environment.CommandLine.Contains("fpfc");

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
                _contextMenu = MenuObj.AddComponent<ContextMenu>();
            }
            XRSettings.showDeviceView = false;

            Config.ConfigChangedEvent += PluginOnConfigChangedEvent;

            var gameObj = Instantiate(_mainCamera.gameObject);

            gameObj.SetActive(false);
            gameObj.name = "Camera Plus";
            gameObj.tag = "Untagged";
            while (gameObj.transform.childCount > 0) DestroyImmediate(gameObj.transform.GetChild(0).gameObject);
            //DestroyImmediate(gameObj.GetComponent(typeof(CameraRenderCallbacksManager)));
            DestroyImmediate(gameObj.GetComponent("AudioListener"));
            DestroyImmediate(gameObj.GetComponent("MeshCollider"));

            _cam = gameObj.GetComponent<Camera>();
            _cam.stereoTargetEye = StereoTargetEyeMask.None;
            _cam.enabled = true;
            _cam.name = Path.GetFileName(Config.FilePath);

            var _liv = _cam.GetComponent<LIV.SDK.Unity.LIV>();
            if (_liv)
                Destroy(_liv);

            _screenCamera = new GameObject("Screen Camera").AddComponent<ScreenCameraBehaviour>();

            if (_previewMaterial == null)
                _previewMaterial = new Material(Shader.Find("Hidden/BlitCopyWithDepth"));

            gameObj.SetActive(true);

            var camera = _mainCamera.transform;
            transform.position = camera.position;
            transform.rotation = camera.rotation;

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

            _quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DontDestroyOnLoad(_quad);
            DestroyImmediate(_quad.GetComponent<Collider>());
            _quad.GetComponent<MeshRenderer>().material = _previewMaterial;
            _quad.transform.parent = _cameraCube;
            _quad.transform.localPosition = new Vector3(-1f * ((_cam.aspect - 1) / 2 + 1), 0, 0.22f);
            _quad.transform.localEulerAngles = new Vector3(0, 180, 0);
            _quad.transform.localScale = new Vector3(_cam.aspect, 1, 1);
            _cameraPreviewQuad = _quad;

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
            if (Config.movementScriptPath != String.Empty)
                AddMovementScript();

            SetCullingMask();

            CameraMovement.CreateExampleScript();

            Plugin.Instance.ActiveSceneChanged += SceneManager_activeSceneChanged;

            //      FirstPersonOffset = Config.FirstPersonPositionOffset;
            //       FirstPersonRotationOffset = Config.FirstPersonRotationOffset;
            SceneManager_activeSceneChanged(new Scene(), new Scene());
            Logger.Log($"Camera \"{Path.GetFileName(Config.FilePath)}\" successfully initialized!");
        }

        protected virtual void OnDestroy()
        {
            Config.ConfigChangedEvent -= PluginOnConfigChangedEvent;
            Plugin.Instance.ActiveSceneChanged -= SceneManager_activeSceneChanged;

            _cameraMovement?.Shutdown();

            // Close our context menu if its open, and destroy all associated controls, otherwise the game will lock up
            CloseContextMenu();

            _camRenderTexture.Release();

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
                //      FirstPersonOffset = Config.FirstPersonPositionOffset;
                //      FirstPersonRotationOffset = Config.FirstPersonRotationOffset;
            }

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

                if (!replace)
                {
                    //Logger.Log("Don't need to replace");
                    return;
                }

                if (Config.fitToCanvas)
                {
                    Config.screenPosX = 0;
                    Config.screenPosY = 0;
                    Config.screenWidth = Screen.width;
                    Config.screenHeight = Screen.height;
                }

                _lastRenderUpdate = DateTime.Now;
                //GetScaledScreenResolution(Config.renderScale, out var scaledWidth, out var scaledHeight);
                _camRenderTexture.width = Mathf.Clamp(Mathf.RoundToInt(Config.screenWidth * Config.renderScale), 1, int.MaxValue);
                _camRenderTexture.height = Mathf.Clamp(Mathf.RoundToInt(Config.screenHeight * Config.renderScale), 1, int.MaxValue);

                _camRenderTexture.useDynamicScale = false;
                _camRenderTexture.antiAliasing = Config.antiAliasing;
                _camRenderTexture.Create();

                _cam.targetTexture = _camRenderTexture;
                _previewMaterial.SetTexture("_MainTex", _camRenderTexture);
                _screenCamera.SetRenderTexture(_camRenderTexture);
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
            StartCoroutine(GetMainCamera());
            StartCoroutine(Get360Managers());
            var vrPointers = to.name == "GameCore" ? Resources.FindObjectsOfTypeAll<VRPointer>() : Resources.FindObjectsOfTypeAll<VRPointer>();
            if (vrPointers.Count() == 0)
            {
                Logger.Log("Failed to get VRPointer!", LogLevel.Warning);
                return;
            }

            var pointer = to.name != "GameCore" ? vrPointers.First() : vrPointers.Last();
            if (_moverPointer) Destroy(_moverPointer);
            _moverPointer = pointer.gameObject.AddComponent<CameraMoverPointer>();
            _moverPointer.Init(this, _cameraCube);
        }

        [DllImport("user32.dll")]
        static extern System.IntPtr GetActiveWindow();

        protected void OnApplicationFocus(bool hasFocus)
        {
            //      if(!hasFocus && GetActiveWindow() == IntPtr.Zero)
            //         CloseContextMenu();
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
                        //          FirstPersonOffset = Config.FirstPersonPositionOffset;
                        //            FirstPersonRotationOffset = Config.FirstPersonRotationOffset;
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
                var camera = _mainCamera.transform;

                if (ThirdPerson)
                {

                    HandleThirdPerson360();
                    transform.position = ThirdPersonPos;
                    transform.eulerAngles = ThirdPersonRot;
                    _cameraCube.position = ThirdPersonPos;
                    _cameraCube.eulerAngles = ThirdPersonRot;
                    return;
                }
                //     Console.WriteLine(Config.FirstPersonPositionOffset.ToString());
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

            if (!_beatLineManager || !Config.use360Camera || !_environmentSpawnRotation) return;

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

            _yAngle = Mathf.Lerp(_yAngle, b, Mathf.Clamp(Time.deltaTime * Config.cam360Smoothness, 0f, 1f));

 

            ThirdPersonRot = new Vector3(Config.cam360XTilt, _yAngle + Config.cam360YTilt, Config.cam360ZTilt);

            ThirdPersonPos = (transform.forward * Config.cam360ForwardOffset) + (transform.right * Config.cam360RightOffset);
            ThirdPersonPos = new Vector3(ThirdPersonPos.x, Config.cam360UpOffset, ThirdPersonPos.z);
        }

        protected void AddMovementScript()
        {
            if (Config.movementScriptPath != String.Empty)
            {
                if (_cameraMovement)
                    _cameraMovement.Shutdown();

                if (Config.movementScriptPath == "SongMovementScript")
                    _cameraMovement = _cam.gameObject.AddComponent<SongCameraMovement>();
                else if (File.Exists(Config.movementScriptPath))
                    _cameraMovement = _cam.gameObject.AddComponent<CameraMovement>();
                else
                    return;

                if (_cameraMovement.Init(this))
                {
                    ThirdPersonPos = Config.Position;
                    ThirdPersonRot = Config.Rotation;
                    Config.thirdPerson = true;
                    ThirdPerson = true;
                    CreateScreenRenderTexture();
                }
            }
        }

        protected IEnumerator GetMainCamera()
        {
            yield return _waitForMainCamera;
            _mainCamera = Camera.main;
        }

        protected IEnumerator Get360Managers() {
            yield return new WaitForSeconds(0.5f);

            _beatLineManager = null;
            _environmentSpawnRotation = null;

            var testList = Resources.FindObjectsOfTypeAll<BeatLineManager>();

            if (testList.Length > 0)
            {
                _beatLineManager = testList.FirstOrDefault();

                _environmentSpawnRotation = Resources.FindObjectsOfTypeAll<EnvironmentSpawnRotation>().FirstOrDefault();
            }

            if (_beatLineManager)
            {
                this._yAngle = _beatLineManager.midRotation;
            }

        }

        internal virtual void SetFOV()
        {
            if (_cam == null) return;
            _cam.fieldOfView = Config.fov;
        }

        internal virtual void SetCullingMask()
        {
            if (Config.transparentWalls)
                _cam.cullingMask &= ~(1 << TransparentWallsPatch.WallLayerMask);
            else
                _cam.cullingMask |= (1 << TransparentWallsPatch.WallLayerMask);
            if (Config.avatar)
            {
                if(Config.thirdPerson || Config.use360Camera)
                    _cam.cullingMask |= (1 << DisplayAvatarPatch.OnlyInThirdPerson);
                else
                    _cam.cullingMask |= (1 << DisplayAvatarPatch.OnlyInFirstPerson);
                _cam.cullingMask |= (1 << DisplayAvatarPatch.AlwaysVisible);
            }
            else
            {
                if(Config.thirdPerson || Config.use360Camera)
                    _cam.cullingMask &= ~(1 << DisplayAvatarPatch.OnlyInThirdPerson);
                else
                    _cam.cullingMask &= ~(1 << DisplayAvatarPatch.OnlyInFirstPerson);
                _cam.cullingMask &= ~(1 << DisplayAvatarPatch.AlwaysVisible);
            }
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
            foreach (CameraPlusInstance c in Plugin.Instance.Cameras.Values.ToArray())
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
            foreach (CameraPlusInstance c in Plugin.Instance.Cameras.Values.ToArray())
            {
                if (c.Instance._isTopmostAtCursorPos)
                    return c.Instance;
            }
            return null;
        }

        internal void CloseContextMenu()
        {
            _contextMenu.DisableMenu();
            Destroy(MenuObj);
            /*
            if (_menuStrip != null)
            {
                _menuStrip.Close();
                _menuStrip.Items.Clear();
                foreach (ToolStripItem item in _controlTracker)
                {
                    if (item is ToolStripMenuItem)
                        (item as ToolStripMenuItem).DropDownItems.Clear();
                    item.Dispose();
                }
                _menuStrip.Dispose();
                _menuStrip = null;
            }
            */
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
                        texture = Utils.LoadTextureFromResources("CameraPlus.Resources.Resize_Horiz.png");
                        break;
                    case CursorType.Vertical:
                        texture = Utils.LoadTextureFromResources("CameraPlus.Resources.Resize_Vert.png");
                        break;
                    case CursorType.DiagonalRight:
                        texture = Utils.LoadTextureFromResources("CameraPlus.Resources.Resize_DiagRight.png");
                        break;
                    case CursorType.DiagonalLeft:
                        texture = Utils.LoadTextureFromResources("CameraPlus.Resources.Resize_DiagLeft.png");
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

            Vector3 mousePos = Input.mousePosition;

            // Close the context menu when we click anywhere within the bounds of the application
            if (!_mouseHeld && (holdingLeftClick || holdingRightClick))
            {
                if (/*_menuStrip != null &&*/ mousePos.x > 0 && mousePos.x < Screen.width && mousePos.y > 0 && mousePos.y < Screen.height)
                {
                    //          CloseContextMenu();
                }
            }

            _isTopmostAtCursorPos = IsTopmostRenderAreaAtPos(mousePos);
            // Only evaluate mouse events for the topmost render target at the mouse position
            if (!_mouseHeld && !_isTopmostAtCursorPos) return;

            int tolerance = 5;
            bool cursorWithinBorder = Utils.WithinRange((int)mousePos.x, -tolerance, tolerance) || Utils.WithinRange((int)mousePos.y, -tolerance, tolerance) ||
                Utils.WithinRange((int)mousePos.x, Config.screenPosX + Config.screenWidth - tolerance, Config.screenPosX + Config.screenWidth + tolerance) ||
                Utils.WithinRange((int)mousePos.x, Config.screenPosX - tolerance, Config.screenPosX + tolerance) ||
                Utils.WithinRange((int)mousePos.y, Config.screenPosY + Config.screenHeight - tolerance, Config.screenPosY + Config.screenHeight + tolerance) ||
                Utils.WithinRange((int)mousePos.y, Config.screenPosY - tolerance, Config.screenPosY + tolerance);

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
                    _xAxisLocked = Utils.WithinRange((int)mousePos.x, centerX - offsetX + 1, centerX + offsetX - 1);
                    _yAxisLocked = Utils.WithinRange((int)mousePos.y, centerY - offsetY + 1, centerY + offsetY - 1);

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

            if (holdingLeftClick && !Config.fitToCanvas)
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
                //       if (_menuStrip == null)
                //      {
                DisplayContextMenu();
                _contextMenuOpen = true;
                //       }
                //       _menuStrip.SetBounds(Cursor.Position.X, Cursor.Position.Y, 0, 0);
                //       if (!_menuStrip.Visible)
                //           _menuStrip.Show();
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

        void DisplayContextMenu()
        {
            if (_contextMenu == null)
            {
                MenuObj = new GameObject("CameraPlusMenu");
                _contextMenu = MenuObj.AddComponent<ContextMenu>();
            }
            _contextMenu.EnableMenu(Input.mousePosition, this);
            /*
            _menuStrip = new ContextMenuStrip();
            // Adds a new camera into the scene
            _menuStrip.Items.Add("Add New Camera", null, (p1, p2) =>
            {
                lock (Plugin.Instance.Cameras)
                {
                    string cameraName = CameraUtilities.GetNextCameraName();
                    Logger.Log($"Adding new config with name {cameraName}.cfg");
                    CameraUtilities.AddNewCamera(cameraName);
                    CameraUtilities.ReloadCameras();
                    CloseContextMenu();
                }
            });

            // Instantiates an exact copy of the currently selected camera
            _menuStrip.Items.Add("Duplicate Selected Camera", null, (p1, p2) =>
            {
                lock (Plugin.Instance.Cameras)
                {
                    string cameraName = CameraUtilities.GetNextCameraName();
                    Logger.Log($"Adding {cameraName}", LogLevel.Notice);
                    CameraUtilities.AddNewCamera(cameraName, Config);
                    CameraUtilities.ReloadCameras();
                    CloseContextMenu();
                }
            });

            // Removes the selected camera from the scene, and deletes the config associated with it
            _menuStrip.Items.Add("Remove Selected Camera", null, (p1, p2) =>
            {
                lock (Plugin.Instance.Cameras)
                {
                    if (CameraUtilities.RemoveCamera(this))
                    {
                        _isCameraDestroyed = true;
                        CreateScreenRenderTexture();
                        CloseContextMenu();
                        Logger.Log("Camera removed!", LogLevel.Notice);
                    }
                    else
                    {
                        MessageBox.Show("Cannot remove main camera!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            });
            _menuStrip.Items.Add(new ToolStripSeparator());

            // Toggles between third/first person
            _menuStrip.Items.Add(Config.thirdPerson ? "First Person" : "Third Person", null, (p1, p2) =>
            {
                Config.thirdPerson = !Config.thirdPerson;
                ThirdPerson = Config.thirdPerson;
                ThirdPersonPos = Config.Position;
                ThirdPersonRot = Config.Rotation;
                //FirstPersonOffset = Config.FirstPersonPositionOffset;
          //     FirstPersonRotationOffset = Config.FirstPersonRotationOffset;
                CreateScreenRenderTexture();
                CloseContextMenu();
                Config.Save();
            });
            if (Config.thirdPerson)
            {
                // Hides/unhides the third person camera that appears when a camera is in third person mode
                _menuStrip.Items.Add(Config.showThirdPersonCamera ? "Hide Third Person Camera" : "Show Third Person Camera", null, (p1, p2) =>
                {
                    Config.showThirdPersonCamera = !Config.showThirdPersonCamera;
                    Config.Save();
                    CreateScreenRenderTexture();
                    CloseContextMenu();
                });

                // Hides/unhides the third person camera that appears when a camera is in third person mode
                _menuStrip.Items.Add("Reset Camera Position/Rotation", null, (p1, p2) =>
                {
                    Config.Position = Config.DefaultPosition;
                    Config.Rotation = Config.DefaultRotation;
                    Config.FirstPersonPositionOffset = Config.DefaultFirstPersonPositionOffset;
                    Config.FirstPersonRotationOffset = Config.DefaultFirstPersonRotationOffset;
                    ThirdPersonPos = Config.DefaultPosition;
                    ThirdPersonRot = Config.DefaultRotation;
                    //FirstPersonOffset = Config.FirstPersonPositionOffset;
             //       FirstPersonRotationOffset = Config.FirstPersonRotationOffset;
                    Config.Save();
                    CloseContextMenu();
                });
            }
            _menuStrip.Items.Add(new ToolStripSeparator());

            // Toggle transparent walls
            _menuStrip.Items.Add(Config.transparentWalls ? "Solid Walls" : "Transparent Walls", null, (p1, p2) =>
            {
                Config.transparentWalls = !Config.transparentWalls;
                SetCullingMask();
                CloseContextMenu();
                Config.Save();
            });

            _menuStrip.Items.Add(Config.forceFirstPersonUpRight ? "Don't Force Camera Upright" : "Force Camera Upright", null, (p1, p2) =>
            {
                Config.forceFirstPersonUpRight = !Config.forceFirstPersonUpRight;
                Config.Save();
                CloseContextMenu();
            });

            _menuStrip.Items.Add(new ToolStripSeparator());

            var _layoutMenu = new ToolStripMenuItem("Layout");
            _controlTracker.Add(_layoutMenu);

            // Sets the layer associated with the current camera
            _layoutMenu.DropDownItems.Add(new ToolStripLabel("Layer"));
            var _layerBox = new ToolStripNumberControl();
            _controlTracker.Add(_layerBox);
            _layerBox.Maximum = int.MaxValue;
            _layerBox.Minimum = int.MinValue;
            _layerBox.Value = Config.layer;
            _layerBox.ValueChanged += (sender, args) =>
            {
                Config.layer = (int)_layerBox.Value;
                CreateScreenRenderTexture();
                Config.Save();
            };
            _layoutMenu.DropDownItems.Add(_layerBox);

            // FOV
            _layoutMenu.DropDownItems.Add(new ToolStripLabel("FOV"));
            var _fov = new ToolStripNumberControl();
            _controlTracker.Add(_fov);
            _fov.Maximum = 179;
            _fov.Minimum = 0;
            _fov.Value = (decimal)Config.fov;

            _fov.ValueChanged += (sender, args) =>
            {
                Config.fov = (int)_fov.Value;
                SetFOV();
                CreateScreenRenderTexture();
                Config.Save();
            };
            _layoutMenu.DropDownItems.Add(_fov);

            // Render Scale
            _layoutMenu.DropDownItems.Add(new ToolStripLabel("Render Scale"));
            var _renderScale = new ToolStripNumberControl();
            _controlTracker.Add(_renderScale);
            _renderScale.Maximum = 4;
            _renderScale.Minimum = 0.1M;
            _renderScale.Increment = 0.1M;
            _renderScale.DecimalPlaces = 1;
            _renderScale.Value = (decimal)Config.renderScale;

            _renderScale.ValueChanged += (sender, args) =>
            {
                Config.renderScale = (float)_renderScale.Value;
                CreateScreenRenderTexture();
                Config.Save();
            };
            _layoutMenu.DropDownItems.Add(_renderScale);
            

            // Sets the size of the current cameras pixelrect
            _layoutMenu.DropDownItems.Add(new ToolStripLabel("Size"));
            var _widthBox = new ToolStripNumberControl();
            _controlTracker.Add(_widthBox);
            _widthBox.Maximum = Screen.width;
            _widthBox.Minimum = 0;
            _widthBox.Value = Config.screenWidth;
            _widthBox.ValueChanged += (sender, args) =>
            {
                Config.screenWidth = (int)_widthBox.Value;
                GL.Clear(false, true, Color.black, 0);
                CreateScreenRenderTexture();
                Config.Save();
            };
            _layoutMenu.DropDownItems.Add(_widthBox);
            var _heightBox = new ToolStripNumberControl();
            _controlTracker.Add(_heightBox);
            _heightBox.Maximum = Screen.height;
            _heightBox.Minimum = 0;
            _heightBox.Value = Config.screenHeight;
            _heightBox.ValueChanged += (sender, args) =>
            {
                Config.screenHeight = (int)_heightBox.Value;
                GL.Clear(false, true, Color.black, 0);
                CreateScreenRenderTexture();
                Config.Save();
            };
            _layoutMenu.DropDownItems.Add(_heightBox);

            // Set the location of the current cameras pixelrect
            _layoutMenu.DropDownItems.Add(new ToolStripLabel("Position"));
            var _xBox = new ToolStripNumberControl();
            _controlTracker.Add(_xBox);
            _xBox.Maximum = Screen.width;
            _xBox.Minimum = 0;
            _xBox.Value = Config.screenPosX;
            _xBox.ValueChanged += (sender, args) =>
            {
                Config.screenPosX = (int)_xBox.Value;
                CreateScreenRenderTexture();
                Config.Save();
            };
            _layoutMenu.DropDownItems.Add(_xBox);
            var _yBox = new ToolStripNumberControl();
            _controlTracker.Add(_yBox);
            _yBox.Maximum = Screen.height;
            _yBox.Minimum = 0;
            _yBox.Value = Config.screenPosY;
            _yBox.ValueChanged += (sender, args) =>
            {
                Config.screenPosY = (int)_yBox.Value;
                CreateScreenRenderTexture();
                Config.Save();
            };
            _layoutMenu.DropDownItems.Add(_yBox);

            
            // Fit to canvas checkbox
            var _fitToCanvasBox = new ToolStripCheckBox("Fit to Canvas");
            _controlTracker.Add(_fitToCanvasBox);
            _fitToCanvasBox.Checked = Config.fitToCanvas;
            _fitToCanvasBox.CheckedChanged += (sender, args) =>
            {
                Config.fitToCanvas = _fitToCanvasBox.Checked;
                _widthBox.Enabled = !Config.fitToCanvas;
                _heightBox.Enabled = !Config.fitToCanvas;
                _xBox.Enabled = !Config.fitToCanvas;
                _yBox.Enabled = !Config.fitToCanvas;
                CreateScreenRenderTexture();
                Config.Save();
            };
            _layoutMenu.DropDownItems.Add(_fitToCanvasBox);

            // Finally, add our layout menu to the main menustrip
            _menuStrip.Items.Add(_layoutMenu);

            // Set the initial state for our width/height boxes depending on whether or not fitToCanvas is enabled
            _widthBox.Enabled = !Config.fitToCanvas;
            _heightBox.Enabled = !Config.fitToCanvas;
            _xBox.Enabled = !Config.fitToCanvas;
            _yBox.Enabled = !Config.fitToCanvas;

            // Scripts submenu
            var _scriptsMenu = new ToolStripMenuItem("Scripts");
            _controlTracker.Add(_scriptsMenu);

            // Add menu
            var _addMenu = new ToolStripMenuItem("Add");
            _controlTracker.Add(_addMenu);

            // Add camera movement script
            ToolStripItem _addCameraMovement = _addMenu.DropDownItems.Add("Camera Movement", null, (p1, p2) =>
            {
                OpenFileDialog ofd = new OpenFileDialog();
                string path = Path.Combine(BeatSaber.UserDataPath, Plugin.Name, "Scripts");
                CameraMovement.CreateExampleScript();
                ofd.InitialDirectory = path;
                ofd.Title = "Select a script";
                ofd.FileOk += (sender, e) => {
                    string file = ((OpenFileDialog)sender).FileName;
                    if (File.Exists(file))
                    {
                        Config.movementScriptPath = file;
                        Config.Save();
                        AddMovementScript();
                    }
                };
                ofd.ShowDialog();
                CloseContextMenu();
            });
            _addCameraMovement.Enabled = !File.Exists(Config.movementScriptPath) || (Config.movementScriptPath == "SongMovementScript" || Config.movementScriptPath == String.Empty);

            // Add song camera movement script
            ToolStripItem _addSongMovement = _addMenu.DropDownItems.Add("Song Camera Movement", null, (p1, p2) =>
            {
                Config.movementScriptPath = "SongMovementScript";
                Config.Save();
                AddMovementScript();
                CloseContextMenu();
            });
            _addSongMovement.Enabled = Config.movementScriptPath != "SongMovementScript";
            _scriptsMenu.DropDownItems.Add(_addMenu);
            
            // Remove menu
            var _removeMenu = new ToolStripMenuItem("Remove");
            _controlTracker.Add(_removeMenu);

            // Remove camera movement script
            ToolStripItem _removeCameraMovement = _removeMenu.DropDownItems.Add("Camera Movement", null, (p1, p2) =>
            {
                Config.movementScriptPath = String.Empty;
                if (_cameraMovement)
                    _cameraMovement.Shutdown();
                Config.Save();
                CloseContextMenu();
            });
            _removeCameraMovement.Enabled = !_addCameraMovement.Enabled;

            // Remove song camera movement script
            ToolStripItem _removeSongMovement = _removeMenu.DropDownItems.Add("Song Camera Movement", null, (p1, p2) =>
            {
                Config.movementScriptPath = String.Empty;
                if (_cameraMovement)
                    _cameraMovement.Shutdown();
                Config.Save();
                CloseContextMenu();
            });
            _removeSongMovement.Enabled = !_addSongMovement.Enabled;
            _scriptsMenu.DropDownItems.Add(_removeMenu);
            _menuStrip.Items.Add(_scriptsMenu);

            // Extras submenu
            var _extrasMenu = new ToolStripMenuItem("Extras");
            _controlTracker.Add(_extrasMenu);
            // Just the right number...
            _extrasMenu.DropDownItems.Add("Spawn 38 Cameras", null, (p1, p2) =>
            {
                StartCoroutine(CameraUtilities.Spawn38Cameras());
                CloseContextMenu();
            });
            _menuStrip.Items.Add(_extrasMenu);
            */
        }

        private void FixedUpdate() {
            // Fix the cursor when the user resizes the main camera to be smaller than the canvas size and they hover over the black portion of the canvas
            if (CameraPlusBehaviour.currentCursor != CameraPlusBehaviour.CursorType.None && !CameraPlusBehaviour.anyInstanceBusy && 
                CameraPlusBehaviour.wasWithinBorder && CameraPlusBehaviour.GetTopmostInstanceAtCursorPos() == null)
            {
                CameraPlusBehaviour.SetCursor(CameraPlusBehaviour.CursorType.None);
                CameraPlusBehaviour.wasWithinBorder = false;
            }
        }
    }
}
