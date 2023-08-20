using UnityEngine;
using System;
using CameraPlus.Camera2Utils;
using CameraPlus.Behaviours;
using CameraPlus.Utilities;
using CameraPlus.Configuration;
using System.IO;
using System.Text.RegularExpressions;

namespace CameraPlus.UI
{
    public class ContextMenu : MonoBehaviour
    {
        public enum MenuState
        {
            MenuTop,
            CameraSetting,
            PreviewQuad,
            Layout,
            Effect,
            Profile,
            MovementScript,
            SettingConverter,
            ExternalLink,
            ChromaKey
        }
        public enum CameraSettingState
        {
            Setting,
            Position,
            Visibility
        }
        public enum LayoutState
        {
            Layout,
            Template
        }

        public enum ExternalLinkState
        {
            VMCProtocol,
            WebCamera
        }
        public enum EffectSettingState
        {
            DoF,
            Wipe,
            Outline,
            Glitch
        }

        private bool _showMenu;
        private float _menuWidth = 300;
        private float _menuHeight = 470;

        public bool IsMenuEnter
        {
            get
            {
                if (_showMenu &&
                    MenuUI.ScaleMousePos.x >= MenuUI.MenuPos.x && MenuUI.ScaleMousePos.x <= MenuUI.MenuPos.x + _menuWidth &&
                    MenuUI.ScaleMousePos.y >= MenuUI.MenuPos.y && MenuUI.ScaleMousePos.y <= MenuUI.MenuPos.y + _menuHeight
                    )
                    return true;
                else
                    return false;
            }
        }

        private MenuState _menuMode = MenuState.MenuTop;
        private CameraSettingState _settingState = CameraSettingState.Setting;
        private LayoutState _layoutState = LayoutState.Layout;
        private ExternalLinkState _externalLinkState = ExternalLinkState.VMCProtocol;
        private EffectSettingState _effectState = EffectSettingState.DoF;

        internal CameraPlusBehaviour _cameraPlus;

        private int _selectedConfig = 0;
        private string[] _configList;
        private int _currentConfigPage;

        private int _selectedProfile = 0;
        private string[] _profileNameList;
        private int _currentProfilePage = 0;

        private int _selectedScript = 0;
        private string[] _scriptList;
        private int _currentScriptPage;

        private int _selectedWebCam = 0;
        private string[] _webCamList;
        private int _currentWebCamPage = 0;

        private int _selectedCam2Scene = 0;
        private int _currentCam2ScenePage = 0;

        private int _selectedMultiplayerNum = 0;
        private string[] _multiplayerList = new string[] { "Player1", "Player2", "Player3", "Player4", "Player5" };

        private int _selectedAmountPosition = 1;
        private string[] _amountPosList = new string[] { "0.01", "0.10", "1.00", "2.00", "5.00" };
        private float _amountPosition = 0.1f;
        private float[] _pos = new float[3];
        private int _selectedAmountRotation = 2;
        private string[] _amountRotList = new string[] { "0.01", "0.1", "1.0", "10", "45", "90" };
        private float _amountRotation = 1.0f;
        private float[] _rot = new float[3];

        private string ipNum = @"(25[0-5]|2[0-4]\d|[0-1]\d{2}|[1-9]?\d)";

        private bool _useSlider = false;
        public void EnableMenu(Vector2 mousePos, CameraPlusBehaviour parentBehaviour)
        {
            this.enabled = true;
            MenuUI.MousePosition = mousePos;
            _showMenu = true;
            this._cameraPlus = parentBehaviour;

            _profileNameList = CameraUtilities.ProfileList();
            _scriptList = CameraUtilities.MovementScriptList();
            _selectedScript = SelectedListNumber(_scriptList, _cameraPlus.Config.movementScript.movementScript);

            _configList = CameraUtilities.CameraSettingList(Plugin.cameraController.CurrentProfile);
            _selectedConfig = SelectedListNumber(_configList, Path.GetFileName(_cameraPlus.Config.FilePath));

            _webCamList = Plugin.cameraController.WebCameraList();
        }
        public void DisableMenu()
        {
            if (!this) return;
            this.enabled = false;
            _showMenu = false;
            if (_cameraPlus && !_cameraPlus._isCameraDestroyed)
                _cameraPlus?.Config.Save();
        }

        private int SelectedListNumber(string[] list, string selectedName)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == selectedName)
                    return i;
            }
            return 0;
        }

        void OnGUI()
        {
            if (!MenuUI.UIInitialize) MenuUI.Initialize();

            if (_showMenu && _cameraPlus)
            {
                Vector3 scale;
                float originalWidth = 1600f;
                float originalHeight = 900f;

                scale.x = Screen.width / originalWidth;
                scale.y = Screen.height / originalHeight;
                scale.z = 1;
                Matrix4x4 originalMatrix = GUI.matrix;
                GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, scale);
                //Layer boxes for Opacity
                for (int i = 0; i < 3; i++)
                    GUI.Box(new Rect(MenuUI.MenuPos.x - 5, MenuUI.MenuPos.y, _menuWidth + 10, _menuHeight), _cameraPlus.name);

                switch (_menuMode)
                {
                    /////////////////////////////////////////////////////////////////////////////////////
                    case MenuState.MenuTop:
                        MenuUI.SetGrid(6, 43);

                        if (MenuUI.ToggleSwitch(0, 0, "Lock\nWindow", _cameraPlus.Config.cameraLock.lockScreen, 2, 3, 2))
                            _cameraPlus.Config.cameraLock.lockScreen = !_cameraPlus.Config.cameraLock.lockScreen;
                        if (MenuUI.ToggleSwitch(2, 0, "Grab\nCamera", !_cameraPlus.Config.cameraLock.lockCamera, 2, 3, 2))
                            _cameraPlus.Config.cameraLock.lockCamera = !_cameraPlus.Config.cameraLock.lockCamera;
                        if (MenuUI.ToggleSwitch(4, 0, "Save\nGrabPos", !_cameraPlus.Config.cameraLock.dontSaveDrag, 2, 3, 2))
                            _cameraPlus.Config.cameraLock.dontSaveDrag = !_cameraPlus.Config.cameraLock.dontSaveDrag;

                        if (MenuUI.Button(0, 4, "Add New Camera", 3, 4))
                        {
                            lock (Plugin.cameraController.Cameras)
                            {
                                string cameraName = CameraUtilities.GetNextCameraName();
                                Plugin.Log.Notice($"Adding new config with name {cameraName}.json");
                                CameraUtilities.AddNewCamera(cameraName);
                                CameraUtilities.LoadProfile(Plugin.cameraController.CurrentProfile);
                                Plugin.cameraController.CloseContextMenu();
                                _configList = CameraUtilities.CameraSettingList(Plugin.cameraController.CurrentProfile);
                            }
                        }
                        if (MenuUI.Button(3, 4, "Duplicate\nSelected Camera", 3, 4))
                        {
                            lock (Plugin.cameraController.Cameras)
                            {
                                string cameraName = CameraUtilities.GetNextCameraName();
                                Plugin.Log.Notice($"Adding {cameraName}");
                                CameraUtilities.AddNewCamera(cameraName, _cameraPlus.Config);
                                CameraUtilities.LoadProfile(Plugin.cameraController.CurrentProfile);
                                Plugin.cameraController.CloseContextMenu();
                                _configList = CameraUtilities.CameraSettingList(Plugin.cameraController.CurrentProfile);
                            }
                        }
                        if (MenuUI.Button(3, 8, "Remove\nSelected Camera", 3, 4))
                        {
                            if (CameraUtilities.RemoveCamera(_cameraPlus))
                            {
                                _cameraPlus._isCameraDestroyed = true;
                                Plugin.cameraController.CloseContextMenu();
                                Plugin.Log.Notice("Camera removed!");
                                _configList = CameraUtilities.CameraSettingList(Plugin.cameraController.CurrentProfile);
                            }
                        }

                        if (MenuUI.Button(0, 13, "Camera Setting", 3, 3))
                            _menuMode = MenuState.CameraSetting;
                        if (MenuUI.Button(3, 13, "Preview Camera", 3, 3))
                            _menuMode = MenuState.PreviewQuad;
                        if (MenuUI.Button(0, 16, "Window Layout", 3, 3))
                            _menuMode = MenuState.Layout;
                        if (MenuUI.Button(3, 16, "External linkage", 3, 3))
                            _menuMode = MenuState.ExternalLink;
                        if (MenuUI.Button(0, 19, "MovementScript", 3, 3))
                            _menuMode = MenuState.MovementScript;
                        if (MenuUI.Button(3, 19, "Effect", 3, 3))
                            _menuMode = MenuState.Effect;

                        if (MenuUI.Button(0, 23, "Profile", 6, 3))
                            _menuMode = MenuState.Profile;

                        _selectedConfig = MenuUI.SelectionGrid(0, 27, _selectedConfig, ref _currentConfigPage, _configList, Path.GetFileName(_cameraPlus.Config.FilePath), 4, 12);
                        if (MenuUI.Button(4, 27, "Change\ntarget\nconfig", 2, 5))
                        {
                            foreach (var c in Plugin.cameraController.Cameras.Values)
                            {
                                if (c.gameObject.activeInHierarchy && Path.GetFileName(c.Config.FilePath) == _configList[_selectedConfig])
                                    _cameraPlus = c;
                            }
                        }

                        if (MenuUI.Button(4, 35, "Setting\nConverter", 2, 4))
                            _menuMode = MenuState.SettingConverter;

                        if (MenuUI.Button(0, 40, "Close Menu", 6, 3))
                        {
                            Plugin.cameraController.CloseContextMenu();
                            _cameraPlus.Config.Save();
                        }
                        break;
                    /////////////////////////////////////////////////////////////////////////////////////
                    case MenuState.CameraSetting:
                        MenuUI.SetGrid(12, 38);
                        _settingState = (CameraSettingState)Enum.ToObject(typeof(CameraSettingState), MenuUI.Toolbar(0, 0, (int)_settingState, Enum.GetNames(typeof(CameraSettingState)), 12, 2));
                        switch (_settingState)
                        {
                            case CameraSettingState.Setting:
                                if (MenuUI.ToggleSwitch(0, 3, "Third person", _cameraPlus.Config.thirdPerson, 6, 2, 1.5f))
                                    _cameraPlus.Config.thirdPerson = !_cameraPlus.Config.thirdPerson;
                                if (MenuUI.ToggleSwitch(6, 3, "Desktop screen", !_cameraPlus.Config.cameraExtensions.dontDrawDesktop, 6, 2, 1.5f))
                                    _cameraPlus.Config.DontDrawDesktop = !_cameraPlus.Config.DontDrawDesktop;

                                if (_cameraPlus.Config.thirdPerson)
                                {
                                    if (MenuUI.ToggleSwitch(0, 5, "Follow Noodle", _cameraPlus.Config.cameraExtensions.followNoodlePlayerTrack, 6, 2, 1.5f))
                                        _cameraPlus.Config.cameraExtensions.followNoodlePlayerTrack = !_cameraPlus.Config.cameraExtensions.followNoodlePlayerTrack;
                                    if (MenuUI.ToggleSwitch(6, 5, "Follow 90/360", _cameraPlus.Config.cameraExtensions.follow360map, 6, 2, 1.5f))
                                        _cameraPlus.Config.cameraExtensions.follow360map = !_cameraPlus.Config.cameraExtensions.follow360map;
                                    MenuUI.Label(6, 7, "90/360 Rotation smooth", 6, 2);
                                    var rotationSmoothValue = _cameraPlus.Config.cameraExtensions.rotation360Smooth;
                                    if (MenuUI.DoubleSpinBox(6, 9, ref rotationSmoothValue, 0.1f, 1, 0.1f, 10, 1, 6, 2))
                                        _cameraPlus.Config.cameraExtensions.rotation360Smooth = rotationSmoothValue;
                                }
                                else
                                {
                                    if (MenuUI.ToggleSwitch(0, 5, "Force Upright", _cameraPlus.Config.cameraExtensions.firstPersonCameraForceUpRight, 6, 2, 1.5f))
                                        _cameraPlus.Config.cameraExtensions.firstPersonCameraForceUpRight = !_cameraPlus.Config.cameraExtensions.firstPersonCameraForceUpRight;
                                    MenuUI.Label(0, 7, "Position smooth", 6, 2);
                                    MenuUI.Label(6, 7, "Rotation smooth", 6, 2);
                                    var positionSmoothValue = _cameraPlus.Config.cameraExtensions.positionSmooth;
                                    var rotationSmoothValue = _cameraPlus.Config.cameraExtensions.rotationSmooth;
                                    if (MenuUI.DoubleSpinBox(0, 9, ref positionSmoothValue, 0.1f, 1, 0.1f, 10, 1, 6, 2))
                                        _cameraPlus.Config.cameraExtensions.positionSmooth = positionSmoothValue;
                                    if (MenuUI.DoubleSpinBox(6, 9, ref rotationSmoothValue, 0.1f, 1, 0.1f, 10, 1, 6, 2))
                                        _cameraPlus.Config.cameraExtensions.rotationSmooth = rotationSmoothValue;

                                }
                                MenuUI.Label(0, 11, "Render scale", 6, 2);
                                var renderScale = _cameraPlus.Config.renderScale;
                                if (MenuUI.SpinBox(0, 13, ref renderScale, 0.1f, 0.1f, 2, 1, 6, 2))
                                {
                                    _cameraPlus.Config.renderScale = renderScale;
                                    _cameraPlus.CreateScreenRenderTexture();
                                }
                                MenuUI.Label(6, 11, "FoV", 6, 2);
                                var fov = _cameraPlus.Config.fov;
                                if (MenuUI.DoubleSpinBox(6, 13, ref fov, 1, 10, 0.1f, 200, 1, 6, 2))
                                    _cameraPlus.Config.fov = _cameraPlus.FOV = fov;
                                MenuUI.Label(0, 15, "Layer", 6, 2);
                                var screenLayer = (float)_cameraPlus.Config.rawLayer;
                                if (MenuUI.DoubleSpinBox(0, 17, ref screenLayer, 1, 10, -1000, 1000, 0, 6, 2))
                                {
                                    _cameraPlus.Config.rawLayer = (int)screenLayer;
                                    _cameraPlus._screenCamera.SetLayer(_cameraPlus.Config.layer);
                                }

                                MenuUI.Label(0, 19, "Near clip plane", 6, 2);
                                var nearClip = _cameraPlus.Config.cameraExtensions.nearClip;
                                var neardellta = nearClip >= 100 ? 10 : 1;
                                if (MenuUI.DoubleSpinBox(0, 21, ref nearClip, 0.1f, neardellta, 0.1f, 1000, 1, 6, 2))
                                    _cameraPlus.Config.cameraExtensions.nearClip = _cameraPlus._cam.nearClipPlane = nearClip;
                                MenuUI.Label(6, 19, "Far clip plane", 6, 2);
                                var farClip = _cameraPlus.Config.cameraExtensions.farClip;
                                var fardelta = farClip >= 100 ? 10 : 1;
                                if (MenuUI.DoubleSpinBox(6, 21, ref farClip, 0.1f, fardelta, 0.1f, 1000, 1, 6, 2))
                                    _cameraPlus.Config.cameraExtensions.farClip = _cameraPlus._cam.farClipPlane = farClip;


                                if (MenuUI.ToggleSwitch(0, 24, "Orthographic", _cameraPlus.Config.cameraExtensions.orthographicMode, 6, 2, 1.5f))
                                    _cameraPlus.Config.cameraExtensions.orthographicMode = _cameraPlus._cam.orthographic = !_cameraPlus.Config.cameraExtensions.orthographicMode;
                                MenuUI.Label(6, 24, "Orthographic scale", 6, 2);
                                var orthographicSize = _cameraPlus.Config.cameraExtensions.orthographicSize;
                                if (MenuUI.DoubleSpinBox(6, 26, ref orthographicSize, 0.1f, 1, 0.1f, 100, 1, 6, 2))
                                    _cameraPlus.Config.cameraExtensions.orthographicSize = _cameraPlus._cam.orthographicSize = orthographicSize;

                                if (MenuUI.ToggleSwitch(0, 29, "Multiplayer", _cameraPlus.Config.multiplayer.targetPlayerNumber != 0, 6, 2, 1.5f))
                                {
                                    if (_cameraPlus.Config.multiplayer.targetPlayerNumber == 0)
                                        _cameraPlus.Config.multiplayer.targetPlayerNumber = 1;
                                    else
                                        _cameraPlus.Config.multiplayer.targetPlayerNumber = 0;
                                }
                                if (MenuUI.ToggleSwitch(6, 29, "Display info", _cameraPlus.Config.multiplayer.displayPlayerInfo, 6, 2, 1.5f))
                                    _cameraPlus.Config.multiplayer.displayPlayerInfo = !_cameraPlus.Config.multiplayer.displayPlayerInfo;

                                _selectedMultiplayerNum = _cameraPlus.Config.multiplayer.targetPlayerNumber - 1;
                                if (MenuUI.HorizontalSelection(0, 31, ref _selectedMultiplayerNum, _multiplayerList, 12, 2))
                                    _cameraPlus.Config.multiplayer.targetPlayerNumber = _selectedMultiplayerNum + 1;
                                MenuUI.Box(0, 33, "Number Extension", 6, 2);
                                if (MenuUI.Button(6, 33, "<", 2, 2))
                                {
                                    if (_cameraPlus.Config.multiplayer.targetPlayerNumber > 0)
                                        _cameraPlus.Config.multiplayer.targetPlayerNumber -= 1;
                                }
                                MenuUI.Box(8, 33, _cameraPlus.Config.multiplayer.targetPlayerNumber > 0 ? _cameraPlus.Config.multiplayer.targetPlayerNumber.ToString() : "-", 2, 2);
                                if (MenuUI.Button(10, 33, ">", 2, 2))
                                {
                                    if (_cameraPlus.Config.multiplayer.targetPlayerNumber <= 100)
                                        _cameraPlus.Config.multiplayer.targetPlayerNumber += 1;
                                }

                                break;
                            case CameraSettingState.Position:

                                if (MenuUI.Button(1, 3, "Reset Camera Position and Rotation", 10, 2))
                                {
                                    _cameraPlus.Config.Position = _cameraPlus.Config.DefaultPosition;
                                    _cameraPlus.Config.Rotation = _cameraPlus.Config.DefaultRotation;
                                    _cameraPlus.Config.FirstPersonPositionOffset = _cameraPlus.Config.DefaultFirstPersonPositionOffset;
                                    _cameraPlus.Config.FirstPersonRotationOffset = _cameraPlus.Config.DefaultFirstPersonRotationOffset;
                                    _cameraPlus.ThirdPersonPos = _cameraPlus.Config.DefaultPosition;
                                    _cameraPlus.ThirdPersonRot = _cameraPlus.Config.DefaultRotation;
                                }

                                if (_cameraPlus.Config.thirdPerson)
                                {
                                    if (MenuUI.ToggleSwitch(0, 6, "Mouse drag", _cameraPlus.mouseMoveCamera, 6, 2, 1.5f))
                                    {
                                        _cameraPlus.mouseMoveCamera = !_cameraPlus.mouseMoveCamera;
                                        _cameraPlus.mouseMoveCameraSave = !_cameraPlus.mouseMoveCameraSave;
                                    }
                                    if (MenuUI.ToggleSwitch(0, 8, "Turn to Head", _cameraPlus.Config.cameraExtensions.turnToHead, 6, 2, 1.5f))
                                        _cameraPlus.Config.cameraExtensions.turnToHead = _cameraPlus.turnToHead = !_cameraPlus.Config.cameraExtensions.turnToHead;
                                    if (MenuUI.ToggleSwitch(6, 8, "Horizontal Only", _cameraPlus.Config.cameraExtensions.turnToHeadHorizontal, 6, 2, 1.5f))
                                        _cameraPlus.Config.cameraExtensions.turnToHeadHorizontal = _cameraPlus.turnToHeadHorizontal = !_cameraPlus.Config.cameraExtensions.turnToHeadHorizontal;

                                    MenuUI.Box(0, 11, "Third person position", 12, 10);
                                    MenuUI.Label(0, 13, "Amount position", 12, 2);
                                    if (MenuUI.HorizontalSelection(0, 15, ref _selectedAmountPosition, _amountPosList, 12, 2))
                                        _amountPosition = float.Parse(_amountPosList[_selectedAmountPosition]);
                                    _pos = _cameraPlus.Config.ThirdPersonPositionFloat;

                                    if (MenuUI.AxizEdit(0, 17, ref _pos, _amountPosition, 12, 4))
                                    {
                                        _cameraPlus.Config.ThirdPersonPositionFloat = _pos;
                                        _cameraPlus.ThirdPersonPos = _cameraPlus.Config.Position;
                                    }

                                    if (!_cameraPlus.Config.cameraExtensions.turnToHead)
                                    {
                                        MenuUI.Box(0, 22, "Third person rotation", 12, 10);
                                        MenuUI.Label(0, 24, "Amount rotation", 12, 2);
                                        if (MenuUI.HorizontalSelection(0, 26, ref _selectedAmountRotation, _amountRotList, 12, 2))
                                            _amountRotation = float.Parse(_amountRotList[_selectedAmountRotation]);
                                        _rot = _cameraPlus.Config.ThirdPersonRotationFloat;

                                        if (MenuUI.AxizEdit(0, 28, ref _rot, _amountRotation, 12, 4, true))
                                        {
                                            _cameraPlus.Config.ThirdPersonRotationFloat = _rot;
                                            _cameraPlus.ThirdPersonRot = _cameraPlus.Config.Rotation;
                                        }
                                    }
                                    else
                                    {
                                        MenuUI.Box(0, 22, "Turn to Head Offset", 12, 10);
                                        _pos = _cameraPlus.Config.TurnToHeadOffsetFloat;

                                        if (MenuUI.AxizEdit(0, 24, ref _pos, _amountPosition, 12, 4))
                                        {
                                            _cameraPlus.Config.TurnToHeadOffsetFloat = _pos;
                                            _cameraPlus.turnToHeadOffset = _cameraPlus.Config.TurnToHeadOffset;
                                        }
                                    }
                                }
                                else
                                {
                                    MenuUI.Box(0, 6, "First person offset position", 12, 10);
                                    MenuUI.Label(0, 8, "Amount position", 12, 2);
                                    if (MenuUI.HorizontalSelection(0, 10, ref _selectedAmountPosition, _amountPosList, 12, 2))
                                        _amountPosition = float.Parse(_amountPosList[_selectedAmountPosition]);
                                    _pos = _cameraPlus.Config.FirstPersonPositionOffsetFloat;

                                    if (MenuUI.AxizEdit(0, 12, ref _pos, _amountPosition, 12, 4))
                                        _cameraPlus.Config.FirstPersonPositionOffsetFloat = _pos;

                                    MenuUI.Box(0, 17, "First person offset rotation", 12, 10);
                                    MenuUI.Label(0, 19, "Amount rotation", 12, 2);
                                    if (MenuUI.HorizontalSelection(0, 21, ref _selectedAmountRotation, _amountRotList, 12, 2))
                                        _amountRotation = float.Parse(_amountRotList[_selectedAmountRotation]);
                                    _rot = _cameraPlus.Config.FirstPersonRotationOffsetFloat;

                                    if (MenuUI.AxizEdit(0, 23, ref _rot, _amountRotation, 12, 4, true))
                                        _cameraPlus.Config.FirstPersonRotationOffsetFloat = _rot;
                                }
                                break;
                            case CameraSettingState.Visibility:
                                if (MenuUI.ToggleSwitch(0, 3, "Avatar", _cameraPlus.Config.Avatar, 6, 3, 1.5f))
                                    _cameraPlus.Config.Avatar = !_cameraPlus.Config.Avatar;
                                if (MenuUI.ToggleSwitch(0, 6, "Saber", _cameraPlus.Config.Saber, 6, 3, 1.5f))
                                    _cameraPlus.Config.Saber = !_cameraPlus.Config.Saber;
                                if (MenuUI.ToggleSwitch(0, 9, "Cut Particle", _cameraPlus.Config.CutParticles, 6, 3, 1.5f))
                                    _cameraPlus.Config.CutParticles = !_cameraPlus.Config.CutParticles;
                                if (MenuUI.ToggleSwitch(0, 12, "Notes", _cameraPlus.Config.Notes, 6, 3, 1.5f))
                                    _cameraPlus.Config.Notes = !_cameraPlus.Config.Notes;
                                if (MenuUI.ToggleSwitch(0, 15, "Debri - LinkGame", _cameraPlus.Config.Debris == DebriVisibility.Link, 6, 3, 1.5f))
                                {
                                    if (_cameraPlus.Config.Debris == DebriVisibility.Link)
                                        _cameraPlus.Config.Debris = DebriVisibility.Visible;
                                    else
                                        _cameraPlus.Config.Debris = DebriVisibility.Link;
                                }
                                if (MenuUI.ToggleSwitch(6, 15, "Debri - AlwaysOn", _cameraPlus.Config.Debris == DebriVisibility.Visible, 6, 3, 1.5f))
                                {
                                    if (_cameraPlus.Config.Debris == DebriVisibility.Visible)
                                        _cameraPlus.Config.Debris = DebriVisibility.Hidden;
                                    else
                                        _cameraPlus.Config.Debris = DebriVisibility.Visible;
                                }
                                if (MenuUI.ToggleSwitch(0, 18, "Wall inside", _cameraPlus.Config.Wall, 6, 3, 1.5f))
                                    _cameraPlus.Config.Wall = !_cameraPlus.Config.Wall;
                                if (MenuUI.ToggleSwitch(6, 18, "Wall Frame", _cameraPlus.Config.WallFrame, 6, 3, 1.5f))
                                    _cameraPlus.Config.WallFrame = !_cameraPlus.Config.WallFrame;
                                if (MenuUI.ToggleSwitch(0, 21, "UI", _cameraPlus.Config.UI, 6, 3, 1.5f))
                                    _cameraPlus.Config.UI = !_cameraPlus.Config.UI;

                                if (MenuUI.ToggleSwitch(0, 25, "PreviewCamera", _cameraPlus.Config.PreviewCamera, 6, 3, 1.5f))
                                    _cameraPlus.Config.PreviewCamera = !_cameraPlus.Config.PreviewCamera;
                                if (_cameraPlus.Config.PreviewCamera)
                                {
                                    if (MenuUI.ToggleSwitch(0, 28, "Hide Cube", _cameraPlus.Config.PreviewCameraQuadOnly, 6, 3, 1.5f))
                                        _cameraPlus.Config.PreviewCameraQuadOnly = !_cameraPlus.Config.PreviewCameraQuadOnly;
                                    if (MenuUI.ToggleSwitch(6, 28, "Show VR only", _cameraPlus.Config.PreviewCameraVROnly, 6, 3, 1.5f))
                                        _cameraPlus.Config.PreviewCameraVROnly = !_cameraPlus.Config.PreviewCameraVROnly;
                                }

                                break;
                        }
                        if (MenuUI.Button(0, 36, "Back top menu", 12, 2))
                        {
                            _menuMode = MenuState.MenuTop;
                            _cameraPlus.Config.Save();
                        }
                        break;
                    /////////////////////////////////////////////////////////////////////////////////////
                    case MenuState.PreviewQuad:
                        MenuUI.SetGrid(12, 41);

                        MenuUI.Label(0, 0, "Preview Position", 6, 2);
                        var quadPos = PluginConfig.Instance.CameraQuadPosition == string.Empty ? "Right" : PluginConfig.Instance.CameraQuadPosition;
                        if (MenuUI.CrossSelection(0, 2, ref quadPos, 7, 7))
                        {
                            PluginConfig.Instance.CameraQuadPosition = quadPos;
                            foreach (CameraPlusBehaviour cam in Plugin.cameraController.Cameras.Values)
                                cam._quad.SetCameraQuadPosition(PluginConfig.Instance.CameraQuadPosition, PluginConfig.Instance.CameraCubeSize);
                        }
                        if (MenuUI.ToggleSwitch(7, 2, "Preview\nstretch", PluginConfig.Instance.CameraQuadStretch, 5, 3, 1.5f))
                        {
                            PluginConfig.Instance.CameraQuadStretch = !PluginConfig.Instance.CameraQuadStretch;
                            foreach (CameraPlusBehaviour cam in Plugin.cameraController.Cameras.Values)
                                cam._quad.SetCameraQuadPosition(PluginConfig.Instance.CameraQuadPosition, PluginConfig.Instance.CameraCubeSize);
                        }

                        MenuUI.Label(0, 9, "Cube Scale", 6, 2);
                        var cubeSize = PluginConfig.Instance.CameraCubeSize;
                        if (MenuUI.SpinBox(0, 11, ref cubeSize, 0.1f, 0.1f, 1, 1, 6, 2))
                        {
                            PluginConfig.Instance.CameraCubeSize = cubeSize;
                            foreach (CameraPlusBehaviour cam in Plugin.cameraController.Cameras.Values)
                            {
                                cam._quad.SetCameraCubeSize(PluginConfig.Instance.CameraCubeSize);
                                cam._quad.SetCameraQuadPosition(PluginConfig.Instance.CameraQuadPosition, PluginConfig.Instance.CameraCubeSize);
                            }
                        }

                        MenuUI.Label(6, 9, "Preview Scale", 6, 2);
                        var quadScale = _cameraPlus.Config.cameraExtensions.previewCameraQuadScale;
                        if (MenuUI.DoubleSpinBox(6, 11, ref quadScale, 0.1f, 1.0f, 0.1f, 100, 1, 6, 2))
                        {
                            _cameraPlus.Config.cameraExtensions.previewCameraQuadScale = quadScale;
                            _cameraPlus._quad.SetCameraQuadPosition(PluginConfig.Instance.CameraQuadPosition, PluginConfig.Instance.CameraCubeSize);
                        }

                        if (MenuUI.ToggleSwitch(0, 14, "Preview camera", _cameraPlus.Config.PreviewCamera, 6, 2, 1.5f))
                            _cameraPlus.Config.PreviewCamera = !_cameraPlus.Config.PreviewCamera;
                        if (MenuUI.ToggleSwitch(6, 14, "Mirror mode", _cameraPlus.Config.cameraExtensions.previewCameraMirrorMode, 6, 2, 1.5f))
                        {
                            _cameraPlus.Config.cameraExtensions.previewCameraMirrorMode = !_cameraPlus.Config.cameraExtensions.previewCameraMirrorMode;
                            _cameraPlus._quad.SetCameraQuadPosition(PluginConfig.Instance.CameraQuadPosition, PluginConfig.Instance.CameraCubeSize);
                        }
                        if (MenuUI.ToggleSwitch(0, 16, "Hide Cube", _cameraPlus.Config.PreviewCameraQuadOnly, 6, 2, 1.5f))
                            _cameraPlus.Config.PreviewCameraQuadOnly = !_cameraPlus.Config.PreviewCameraQuadOnly;
                        if (MenuUI.ToggleSwitch(6, 16, "Show VR only", _cameraPlus.Config.PreviewCameraVROnly, 6, 2, 1.5f))
                            _cameraPlus.Config.PreviewCameraVROnly = !_cameraPlus.Config.PreviewCameraVROnly;

                        if (MenuUI.ToggleSwitch(0, 19, "Preview separate", _cameraPlus.Config.cameraExtensions.previewQuadSeparate, 6, 2, 1.5f))
                        {
                            _cameraPlus.Config.cameraExtensions.previewQuadSeparate = !_cameraPlus.Config.cameraExtensions.previewQuadSeparate;
                            if (_cameraPlus.Config.cameraExtensions.previewQuadSeparate)
                                _cameraPlus._quad.SeparateQuad();
                            else
                                _cameraPlus._quad.CombineQuad();
                        }
                        if (_cameraPlus.Config.cameraExtensions.previewQuadSeparate)
                        {
                            MenuUI.Label(0, 22, "Amount position", 12, 2);
                            if (MenuUI.HorizontalSelection(0, 24, ref _selectedAmountPosition, _amountPosList, 12, 2))
                                _amountPosition = float.Parse(_amountPosList[_selectedAmountPosition]);
                            _pos = _cameraPlus.Config.PreviewQuadPositionFloat;

                            if (MenuUI.AxizEdit(0, 26, ref _pos, _amountPosition, 12, 4))
                            {
                                _cameraPlus.Config.PreviewQuadPositionFloat = _pos;
                                _cameraPlus._quad.SetCameraQuadSeparatePosition();
                            }

                            MenuUI.Label(0, 30, "Amount rotation", 12, 2);
                            if (MenuUI.HorizontalSelection(0, 32, ref _selectedAmountRotation, _amountRotList, 12, 2))
                                _amountRotation = float.Parse(_amountRotList[_selectedAmountRotation]);
                            _rot = _cameraPlus.Config.PreviewQuadRotationFloat;

                            if (MenuUI.AxizEdit(0, 34, ref _rot, _amountRotation, 12, 4, true))
                            {
                                _cameraPlus.Config.PreviewQuadRotationFloat = _rot;
                                _cameraPlus._quad.SetCameraQuadSeparatePosition();
                            }
                        }

                        if (MenuUI.Button(0, 39, "Back top menu", 12, 2))
                        {
                            _menuMode = MenuState.MenuTop;
                            _cameraPlus.Config.Save();
                        }
                        break;
                    /////////////////////////////////////////////////////////////////////////////////////
                    case MenuState.Layout:
                        MenuUI.SetGrid(12, 34);
                        _layoutState = (LayoutState)Enum.ToObject(typeof(LayoutState), MenuUI.Toolbar(0, 0, (int)_layoutState, Enum.GetNames(typeof(LayoutState)), 12, 2));

                        switch (_layoutState)
                        {
                            case LayoutState.Layout:
                                if (MenuUI.ToggleSwitch(0, 3, "Fit to Canvas", _cameraPlus.Config.fitToCanvas, 6, 2, 1.5f))
                                {
                                    _cameraPlus.Config.fitToCanvas = !_cameraPlus.Config.fitToCanvas;
                                    _cameraPlus.CreateScreenRenderTexture();
                                }

                                MenuUI.Box(0, 5, "Window size", 12, 6);
                                MenuUI.Label(0, 7, "Width", 6, 2);
                                var screenWidth = (float)_cameraPlus.Config.screenWidth;
                                if (MenuUI.DoubleSpinBox(0, 9, ref screenWidth, 1, 20, 1, Screen.width, 0, 6, 2))
                                {
                                    _cameraPlus.Config.screenWidth = (int)screenWidth;
                                    _cameraPlus.CreateScreenRenderTexture();
                                }
                                MenuUI.Label(6, 7, "Height", 6, 2);
                                var screenHeight = (float)_cameraPlus.Config.screenHeight;
                                if (MenuUI.DoubleSpinBox(6, 9, ref screenHeight, 1, 20, 1, Screen.height, 0, 6, 2))
                                {
                                    _cameraPlus.Config.screenHeight = (int)screenHeight;
                                    _cameraPlus.CreateScreenRenderTexture();
                                }

                                MenuUI.Box(0, 12, "Screen position", 12, 6);
                                MenuUI.Label(0, 14, "X", 6, 2);
                                var screenX = (float)_cameraPlus.Config.screenPosX;
                                if (MenuUI.DoubleSpinBox(0, 16, ref screenX, 1, 20, 0, Screen.width - _cameraPlus.Config.screenWidth, 0, 6, 2))
                                {
                                    _cameraPlus.Config.screenPosX = (int)screenX;
                                    _cameraPlus._screenCamera.SetPosition(new Vector2(_cameraPlus.Config.screenPosX, _cameraPlus.Config.screenPosY));
                                }
                                MenuUI.Label(6, 14, "Y", 6, 2);
                                var screenY = (float)_cameraPlus.Config.screenPosY;
                                if (MenuUI.DoubleSpinBox(6, 16, ref screenY, 1, 20, 0, Screen.height - _cameraPlus.Config.screenHeight, 0, 6, 2))
                                {
                                    _cameraPlus.Config.screenPosY = (int)screenY;
                                    _cameraPlus._screenCamera.SetPosition(new Vector2(_cameraPlus.Config.screenPosX, _cameraPlus.Config.screenPosY));
                                }


                                if (MenuUI.Button(0, 20, MenuUI.IconTexture[2], 3, 6))
                                {
                                    _cameraPlus.Config.screenPosX = 0;
                                    _cameraPlus.Config.screenPosY = 0;
                                    _cameraPlus.Config.screenWidth = Screen.width / 3;
                                    _cameraPlus.Config.screenHeight = Screen.height;
                                    _cameraPlus.CreateScreenRenderTexture();
                                }
                                if (MenuUI.Button(3, 20, MenuUI.IconTexture[3], 3, 3))
                                {
                                    _cameraPlus.Config.screenPosX = 0;
                                    _cameraPlus.Config.screenPosY = Screen.height - (Screen.height / 2);
                                    _cameraPlus.Config.screenWidth = Screen.width / 3;
                                    _cameraPlus.Config.screenHeight = Screen.height / 2;
                                    _cameraPlus.CreateScreenRenderTexture();
                                }
                                if (MenuUI.Button(3, 23, MenuUI.IconTexture[4], 3, 3))
                                {
                                    _cameraPlus.Config.screenPosX = 0;
                                    _cameraPlus.Config.screenPosY = 0;
                                    _cameraPlus.Config.screenWidth = Screen.width / 3;
                                    _cameraPlus.Config.screenHeight = Screen.height / 2;
                                    _cameraPlus.CreateScreenRenderTexture();
                                }
                                if (MenuUI.Button(6, 20, MenuUI.IconTexture[5], 3, 3))
                                {
                                    _cameraPlus.Config.screenPosX = Screen.width - (Screen.width / 3);
                                    _cameraPlus.Config.screenPosY = Screen.height - (Screen.height / 2);
                                    _cameraPlus.Config.screenWidth = Screen.width / 3;
                                    _cameraPlus.Config.screenHeight = Screen.height / 2;
                                    _cameraPlus.CreateScreenRenderTexture();
                                }
                                if (MenuUI.Button(6, 23, MenuUI.IconTexture[6], 3, 3))
                                {
                                    _cameraPlus.Config.screenPosX = Screen.width - (Screen.width / 3);
                                    _cameraPlus.Config.screenPosY = 0;
                                    _cameraPlus.Config.screenWidth = Screen.width / 3;
                                    _cameraPlus.Config.screenHeight = Screen.height / 2;
                                    _cameraPlus.CreateScreenRenderTexture();
                                }
                                if (MenuUI.Button(9, 20, MenuUI.IconTexture[7], 3, 6))
                                {
                                    _cameraPlus.Config.screenPosX = Screen.width - (Screen.width / 3);
                                    _cameraPlus.Config.screenPosY = 0;
                                    _cameraPlus.Config.screenWidth = Screen.width / 3;
                                    _cameraPlus.Config.screenHeight = Screen.height;
                                    _cameraPlus.CreateScreenRenderTexture();
                                }

                                break;
                            case LayoutState.Template:
                                if (MenuUI.Button(0, 3, "Side by side\n\n for multiplayer\nlayout", 6, 6))
                                {
                                    int count = _configList.Length;
                                    for (int i = 0; i < count; i++)
                                    {
                                        var cam = CameraUtilities.TargetCameraPlus(_configList[i], Plugin.cameraController.CurrentProfile);
                                        if (cam != null)
                                        {
                                            cam.Config.fitToCanvas = false;
                                            cam.Config.screenWidth = i == count - 1 ? Screen.width - Mathf.CeilToInt(Screen.width / count) * (count - 1) : Mathf.CeilToInt(Screen.width / count);
                                            cam.Config.screenHeight = Screen.height;
                                            cam.Config.screenPosX = Mathf.CeilToInt(Screen.width / count) * i;
                                            cam.Config.screenPosY = 0;
                                            cam.Config.cameraLock.lockScreen = true;
                                            cam.Config.multiplayer.targetPlayerNumber = i;
                                            cam.CreateScreenRenderTexture();
                                            cam.Config.Save();
                                        }
                                    }
                                }
                                if (MenuUI.Button(6, 3, "side by side\nin two rows\n for multiplayer\nlayout", 6, 6))
                                {
                                    int count = _configList.Length;
                                    int col = (count % 2 == 0) ? count / 2 : (count + 1) / 2;
                                    for (int i = 0; i < count; i++)
                                    {
                                        var cam = CameraUtilities.TargetCameraPlus(_configList[i], Plugin.cameraController.CurrentProfile);
                                        if (cam != null)
                                        {
                                            cam.Config.fitToCanvas = false;
                                            cam.Config.screenWidth = i == col - 1 || i - col == col - 1 ? Screen.width - Mathf.CeilToInt(Screen.width / col) * (col - 1) : Mathf.CeilToInt(Screen.width / col);
                                            cam.Config.screenHeight = Screen.height / 2;
                                            cam.Config.screenPosX = Mathf.CeilToInt(Screen.width / col) * (i < col ? i : i - col);
                                            cam.Config.screenPosY = i < col ? Screen.height / 2 : 0;
                                            cam.Config.cameraLock.lockScreen = true;
                                            cam.Config.multiplayer.targetPlayerNumber = i;
                                            cam.CreateScreenRenderTexture();
                                            cam.Config.Save();
                                        }
                                    }
                                }
                                break;
                        }

                        if (MenuUI.Button(0, 32, "Back top menu", 12, 2))
                        {
                            _menuMode = MenuState.MenuTop;
                            _cameraPlus.Config.Save();
                        }
                        break;
                    /////////////////////////////////////////////////////////////////////////////////////
                    case MenuState.ExternalLink:
                        MenuUI.SetGrid(12, 34);
                        _externalLinkState = (ExternalLinkState)Enum.ToObject(typeof(ExternalLinkState), MenuUI.Toolbar(0, 0, (int)_externalLinkState, Enum.GetNames(typeof(ExternalLinkState)), 12, 2));

                        switch (_externalLinkState)
                        {
                            case ExternalLinkState.VMCProtocol:
                                int _vmcProtocolState = (int)_cameraPlus.Config.vmcProtocol.mode;
                                if (MenuUI.HorizontalSelection(0, 3, ref _vmcProtocolState, Enum.GetNames(typeof(VMCProtocolMode)), 12, 2))
                                {
                                    _cameraPlus.Config.vmcProtocol.mode = (VMCProtocolMode)Enum.ToObject(typeof(VMCProtocolMode), _vmcProtocolState);
                                    _cameraPlus.DestoryVMCProtocolObject();
                                    if (_cameraPlus.Config.vmcProtocol.mode == VMCProtocolMode.Sender)
                                        _cameraPlus.InitExternalSender();
                                    else if (_cameraPlus.Config.vmcProtocol.mode == VMCProtocolMode.Receiver)
                                        _cameraPlus.InitExternalReceiver();
                                }
                                MenuUI.Label(0, 5, "Address", 4, 2);
                                MenuUI.Label(4, 5, "Port", 4, 2);
                                var senderAddr = MenuUI.TextField(0, 7, _cameraPlus.Config.vmcProtocol.address, 4, 2);
                                var senderPort = MenuUI.TextField(4, 7, _cameraPlus.Config.vmcProtocol.port.ToString(), 4, 2);
                                if (Regex.IsMatch(senderAddr, ("^" + ipNum + "\\." + ipNum + "\\." + ipNum + "\\." + ipNum + "$")))
                                    _cameraPlus.Config.vmcProtocol.address = senderAddr;
                                if (int.TryParse(senderPort, out int result))
                                    _cameraPlus.Config.vmcProtocol.port = result;

                                if (MenuUI.Button(8, 7, "Save", 4, 2))
                                    _cameraPlus.Config.Save();
                                break;
                            case ExternalLinkState.WebCamera:
                                _selectedWebCam = MenuUI.SelectionGrid(0, 3, _selectedWebCam, ref _currentWebCamPage, _webCamList, _cameraPlus.Config.webCamera.name, 8, 10);

                                if (MenuUI.ToggleSwitch(8, 3, "Auto\nConnect", _cameraPlus.Config.webCamera.autoConnect, 4, 3, 1.5f))
                                    _cameraPlus.Config.webCamera.autoConnect = !_cameraPlus.Config.webCamera.autoConnect;
                                if (!Plugin.cameraController.inProgressCalibration())
                                {
                                    if (!_cameraPlus.webCamScreen)
                                    {
                                        if (MenuUI.Button(8, 6, "Connect", 4, 3))
                                            _cameraPlus.CreateWebCamScreen();
                                    }
                                    else
                                    {
                                        if (MenuUI.Button(8, 6, "Disconnect", 4, 3))
                                            _cameraPlus.DisableWebCamScreen();
                                    }
                                }
                                if (!_cameraPlus.webCamScreen)
                                {
                                    if (!Plugin.cameraController.inProgressCalibration())
                                    {
                                        if (MenuUI.Button(8, 9, "Calibration", 4, 3))
                                            Plugin.cameraController.WebCameraCalibration(_cameraPlus);
                                    }
                                    else
                                    {
                                        if (MenuUI.Button(8, 9, "Abort Cal", 4, 3))
                                            Plugin.cameraController.DestroyCalScreen();
                                    }
                                }
                                if (_cameraPlus.webCamScreen)
                                {
                                    MenuUI.Label(0, 13, "Chromakey", 6, 2);
                                    if (MenuUI.Button(6, 13, "Pick Chromakey Color", 6, 2))
                                    {
                                        _cameraPlus.webCamScreen.colorPickState = true;
                                        UnityEngine.Cursor.SetCursor(MenuUI.MouseCursorTexture[4], new Vector2(0, 0), CursorMode.Auto);
                                    }
                                    var r = _cameraPlus.webCamScreen.ChromakeyR;
                                    var g = _cameraPlus.webCamScreen.ChromakeyG;
                                    var b = _cameraPlus.webCamScreen.ChromakeyB;
                                    MenuUI.Label(0, 15, "R", 6, 2);
                                    MenuUI.Label(6, 15, "G", 6, 2);
                                    MenuUI.Label(0, 19, "B", 6, 2);
                                    if (MenuUI.DoubleSpinBox(0, 17, ref r, 0.01f, 0.1f, 0, 1, 2, 6, 2))
                                        _cameraPlus.webCamScreen.ChromakeyR = r;
                                    if (MenuUI.DoubleSpinBox(6, 17, ref g, 0.01f, 0.1f, 0, 1, 2, 6, 2))
                                        _cameraPlus.webCamScreen.ChromakeyG = g;
                                    if (MenuUI.DoubleSpinBox(0, 21, ref b, 0.01f, 0.1f, 0, 1, 2, 6, 2))
                                        _cameraPlus.webCamScreen.ChromakeyB = b;

                                    var hue = _cameraPlus.webCamScreen.ChromakeyHue;
                                    var sat = _cameraPlus.webCamScreen.ChromakeySaturation;
                                    var bri = _cameraPlus.webCamScreen.ChromakeyBrightness;
                                    MenuUI.Label(0, 23, "Hue", 6, 2);
                                    MenuUI.Label(6, 23, "Saturation", 6, 2);
                                    MenuUI.Label(0, 27, "Brightness", 6, 2);
                                    if (MenuUI.DoubleSpinBox(0, 25, ref hue, 0.01f, 0.1f, 0, 1, 2, 6, 2))
                                        _cameraPlus.webCamScreen.ChromakeyHue = hue;
                                    if (MenuUI.DoubleSpinBox(6, 25, ref sat, 0.01f, 0.1f, 0, 1, 2, 6, 2))
                                        _cameraPlus.webCamScreen.ChromakeySaturation = sat;
                                    if (MenuUI.DoubleSpinBox(0, 29, ref bri, 0.01f, 0.1f, 0, 1, 2, 6, 2))
                                        _cameraPlus.webCamScreen.ChromakeyBrightness = bri;
                                }
                                break;
                        }

                        if (MenuUI.Button(0, 32, "Back top menu", 12, 2))
                        {
                            _menuMode = MenuState.MenuTop;
                            _cameraPlus.Config.Save();
                        }
                        break;
                    /////////////////////////////////////////////////////////////////////////////////////
                    case MenuState.MovementScript:
                        MenuUI.SetGrid(6, 34);
                        if (MenuUI.ToggleSwitch(0, 0, "Use audio sync", _cameraPlus.Config.movementScript.useAudioSync, 6, 2, 1.5f))
                            _cameraPlus.Config.movementScript.useAudioSync = !_cameraPlus.Config.movementScript.useAudioSync;
                        if (MenuUI.ToggleSwitch(0, 2, "Song specific script", _cameraPlus.Config.movementScript.songSpecificScript, 6, 2, 1.5f))
                            _cameraPlus.Config.movementScript.songSpecificScript = !_cameraPlus.Config.movementScript.songSpecificScript;

                        _selectedScript = MenuUI.SelectionGrid(0, 5, _selectedScript, ref _currentScriptPage, _scriptList,
                            (_cameraPlus.Config.movementScript.movementScript == string.Empty ? "[ MovementScript Off ]" : Path.GetFileName(_cameraPlus.Config.movementScript.movementScript)), 6, 12);

                        if (MenuUI.Button(0, 18, "Set MovementScript", 6, 2))
                        {
                            if (_selectedScript == 0)
                            {
                                _cameraPlus.Config.movementScript.movementScript = string.Empty;
                                _cameraPlus.ClearMovementScript();
                            }
                            else
                            {
                                _cameraPlus.Config.movementScript.movementScript = _scriptList[_selectedScript];
                                _cameraPlus.AddMovementScript();
                            }
                        }
                        MenuUI.Box(0, 21, "Ignore script visibility setting", 6, 10);
                        if (MenuUI.ToggleSwitch(0, 23, "UI", _cameraPlus.Config.movementScript.ignoreScriptUIDisplay, 3, 2, 1.5f))
                            _cameraPlus.Config.movementScript.ignoreScriptUIDisplay = !_cameraPlus.Config.movementScript.ignoreScriptUIDisplay;

                        if (MenuUI.Button(0, 32, "Back top menu", 6, 2))
                        {
                            _menuMode = MenuState.MenuTop;
                            _cameraPlus.Config.Save();
                        }
                        break;
                    /////////////////////////////////////////////////////////////////////////////////////
                    case MenuState.Effect:
                        MenuUI.SetGrid(12, 34);
                        _effectState = (EffectSettingState)Enum.ToObject(typeof(EffectSettingState), MenuUI.Toolbar(0, 0, (int)_effectState, Enum.GetNames(typeof(EffectSettingState)), 12, 2));

                        switch (_effectState)
                        {
                            case EffectSettingState.DoF:
                                if (MenuUI.ToggleSwitch(0, 3, "Depth of field", _cameraPlus.Config.DoFEnable, 6, 2, 1.5f))
                                    _cameraPlus.Config.DoFEnable = _cameraPlus.effectElements.enableDOF = !_cameraPlus.Config.DoFEnable;
                                if (MenuUI.ToggleSwitch(6, 3, "Use slider", _useSlider, 6, 2, 1.5f))
                                    _useSlider = !_useSlider;

                                if (MenuUI.ToggleSwitch(0, 6, "Auto distance", _cameraPlus.Config.DoFAutoDistance, 6, 2, 1.5f))
                                    _cameraPlus.Config.DoFAutoDistance = _cameraPlus.effectElements.dofAutoDistance = !_cameraPlus.Config.DoFAutoDistance;

                                MenuUI.Box(0, 8, $"Focus distance : {_cameraPlus.effectElements.dofFocusDistance.ToString("F2")}", 12, 4);
                                var dist = _cameraPlus.Config.DoFFocusDistance;
                                if (MenuUI.SwitchableSlider(0, 10, ref dist, 0, 100, _useSlider, 12, 2))
                                    _cameraPlus.Config.DoFFocusDistance = _cameraPlus.effectElements.dofFocusDistance = dist;

                                MenuUI.Box(0, 12, $"Focus range : {_cameraPlus.Config.DoFFocusRange.ToString("F2")}", 12, 4);
                                var rng = _cameraPlus.Config.DoFFocusRange;
                                if (MenuUI.SwitchableSlider(0, 14, ref rng, 0, 100, _useSlider, 12, 2))
                                    _cameraPlus.Config.DoFFocusRange = _cameraPlus.effectElements.dofFocusRange = rng;

                                MenuUI.Box(0, 16, $"Blur size : {_cameraPlus.Config.DoFBlurRadius.ToString("F2")}", 12, 4);
                                var blur = _cameraPlus.Config.DoFBlurRadius;
                                if (MenuUI.SwitchableSlider(0, 18, ref blur, 0, 50, _useSlider, 12, 2))
                                    _cameraPlus.Config.DoFBlurRadius = _cameraPlus.effectElements.dofBlurRadius = blur;

                                break;
                            case EffectSettingState.Outline:
                                if (MenuUI.ToggleSwitch(0, 3, "Outline effect", _cameraPlus.Config.OutlineEnable, 6, 2, 1.5f))
                                    _cameraPlus.Config.OutlineEnable = _cameraPlus.effectElements.enableOutline = !_cameraPlus.Config.OutlineEnable;
                                if (MenuUI.ToggleSwitch(6, 3, "Use slider", _useSlider, 6, 2, 1.5f))
                                    _useSlider = !_useSlider;

                                MenuUI.Label(0, 5, $"Outline Only : {_cameraPlus.Config.OutlineOnly.ToString("F2")}", 6, 2);
                                var outlineState = _cameraPlus.Config.OutlineOnly;
                                if (MenuUI.SwitchableSliderShort(0, 7, ref outlineState, 0, 1, _useSlider, 6, 2))
                                    _cameraPlus.Config.OutlineOnly = _cameraPlus.effectElements.outlineOnly = outlineState;

                                float[] lineColor = _cameraPlus.Config.OutlineColorValue;
                                MenuUI.Label(0, 9, "Outline color", 12, 2);
                                MenuUI.Label(0, 11, $"R : {lineColor[0].ToString("F2")}", 4, 2);
                                MenuUI.Label(4, 11, $"G : {lineColor[1].ToString("F2")}", 4, 2);
                                MenuUI.Label(8, 11, $"B : {lineColor[2].ToString("F2")}", 4, 2);
                                if (MenuUI.SwitchableSliderShort(0, 13, ref lineColor[0], 0, 1, _useSlider, 4, 2))
                                    _cameraPlus.Config.OutlineColorValue = _cameraPlus.effectElements.outlineColorValue = lineColor;
                                if (MenuUI.SwitchableSliderShort(4, 13, ref lineColor[1], 0, 1, _useSlider, 4, 2))
                                    _cameraPlus.Config.OutlineColorValue = _cameraPlus.effectElements.outlineColorValue = lineColor;
                                if (MenuUI.SwitchableSliderShort(8, 13, ref lineColor[2], 0, 1, _useSlider, 4, 2))
                                    _cameraPlus.Config.OutlineColorValue = _cameraPlus.effectElements.outlineColorValue = lineColor;

                                float[] lineBGColor = _cameraPlus.Config.OutlineBGColorValue;
                                MenuUI.Label(0, 15, "Background color", 12, 2);
                                MenuUI.Label(0, 17, $"R : {lineBGColor[0].ToString("F2")}", 4, 2);
                                MenuUI.Label(4, 17, $"G : {lineBGColor[1].ToString("F2")}", 4, 2);
                                MenuUI.Label(8, 17, $"B : {lineBGColor[2].ToString("F2")}", 4, 2);
                                if (MenuUI.SwitchableSliderShort(0, 19, ref lineBGColor[0], 0, 1, _useSlider, 4, 2))
                                    _cameraPlus.Config.OutlineBGColorValue = _cameraPlus.effectElements.outlineBGColorValue = lineBGColor;
                                if (MenuUI.SwitchableSliderShort(4, 19, ref lineBGColor[1], 0, 1, _useSlider, 4, 2))
                                    _cameraPlus.Config.OutlineBGColorValue = _cameraPlus.effectElements.outlineBGColorValue = lineBGColor;
                                if (MenuUI.SwitchableSliderShort(8, 19, ref lineBGColor[2], 0, 1, _useSlider, 4, 2))
                                    _cameraPlus.Config.OutlineBGColorValue = _cameraPlus.effectElements.outlineBGColorValue = lineBGColor;
                                break;
                            case EffectSettingState.Wipe:
                                if (MenuUI.ToggleSwitch(0, 3, "Wipe effect", _cameraPlus.Config.WipeProgress != 0, 6, 2, 1.5f))
                                {
                                    if (_cameraPlus.Config.WipeProgress != 0)
                                        _cameraPlus.Config.WipeProgress = _cameraPlus.effectElements.wipeProgress = 0;
                                    else
                                        _cameraPlus.Config.WipeProgress = _cameraPlus.effectElements.wipeProgress = 0.5f;

                                }
                                if (MenuUI.ToggleSwitch(6, 3, "Use slider", _useSlider, 6, 2, 1.5f))
                                    _useSlider = !_useSlider;

                                MenuUI.Label(0, 5, $"Wipe progress : {_cameraPlus.Config.WipeProgress.ToString("F2")}", 6, 2);
                                var wipeProgress = _cameraPlus.Config.WipeProgress;
                                if (MenuUI.SwitchableSliderShort(0, 7, ref wipeProgress, 0, 1, _useSlider, 6, 2))
                                    _cameraPlus.Config.WipeProgress = _cameraPlus.effectElements.wipeProgress = wipeProgress;

                                float[] wipeOffset = _cameraPlus.Config.WipeCircleCenterValue;
                                MenuUI.Label(0, 9, $"Center offset", 12, 2);
                                MenuUI.Label(0, 11, "x", 6, 2);
                                MenuUI.Label(6, 11, "y", 6, 2);
                                if (MenuUI.DoubleSpinBox(0, 13, ref wipeOffset[0], 0.01f, 0.1f, -0.5f, 0.5f, 2, 6, 2))
                                    _cameraPlus.Config.WipeCircleCenterValue = _cameraPlus.effectElements.wipeCircleCenterValue = wipeOffset;
                                if (MenuUI.DoubleSpinBox(6, 13, ref wipeOffset[1], 0.01f, 0.1f, -0.5f, 0.5f, 2, 6, 2))
                                    _cameraPlus.Config.WipeCircleCenterValue = _cameraPlus.effectElements.wipeCircleCenterValue = wipeOffset;

                                MenuUI.Label(0, 16, "Wipe type", 6, 2);
                                if (MenuUI.ToggleButton(0, 18, "Circle", _cameraPlus.Config.WipeType == "Circle", 4, 6))
                                    _cameraPlus.Config.WipeType = _cameraPlus.effectElements.wipeType = "Circle";
                                if (MenuUI.ToggleButton(6, 18, "Top to Bottom", _cameraPlus.Config.WipeType == "Top", 4, 2))
                                    _cameraPlus.Config.WipeType = _cameraPlus.effectElements.wipeType = "Top";
                                if (MenuUI.ToggleButton(4, 20, "Left to Right", _cameraPlus.Config.WipeType == "Left", 4, 2))
                                    _cameraPlus.Config.WipeType = _cameraPlus.effectElements.wipeType = "Left";
                                if (MenuUI.ToggleButton(8, 20, "Right to Left", _cameraPlus.Config.WipeType == "Right", 4, 2))
                                    _cameraPlus.Config.WipeType = _cameraPlus.effectElements.wipeType = "Right";
                                if (MenuUI.ToggleButton(6, 22, "Bottom to Top", _cameraPlus.Config.WipeType == "Bottom", 4, 2))
                                    _cameraPlus.Config.WipeType = _cameraPlus.effectElements.wipeType = "Bottom";
                                break;
                            case EffectSettingState.Glitch:
                                if (MenuUI.ToggleSwitch(0, 3, "Glitch", _cameraPlus.Config.GlitchEnable, 6, 2, 1.5f))
                                    _cameraPlus.Config.GlitchEnable = _cameraPlus.effectElements.enableGlitch = !_cameraPlus.Config.GlitchEnable;
                                float[] glitchValue = _cameraPlus.Config.GlitchValue;
                                MenuUI.Label(0, 6, "Line speed", 6, 2);
                                if (MenuUI.DoubleSpinBox(0, 8, ref glitchValue[0], 0.1f, 1, 0, 10, 1, 6, 2))
                                    _cameraPlus.Config.GlitchLineSpeed = _cameraPlus.effectElements.glitchLineSpeed = glitchValue[0];
                                MenuUI.Label(6, 6, "Line size", 6, 2);
                                if (MenuUI.DoubleSpinBox(6, 8, ref glitchValue[1], 0.01f, 0.1f, 0, 1, 2, 6, 2))
                                    _cameraPlus.Config.GlitchLineSize = _cameraPlus.effectElements.glitchLineSize = glitchValue[1];
                                MenuUI.Label(0, 10, "Color gap", 6, 2);
                                if (MenuUI.DoubleSpinBox(0, 12, ref glitchValue[2], 0.01f, 0.1f, 0, 1, 2, 6, 2))
                                    _cameraPlus.Config.GlitchColorGap = _cameraPlus.effectElements.glitchColorGap = glitchValue[2];
                                MenuUI.Label(0, 14, "Frame rate", 6, 2);
                                if (MenuUI.SpinBox(0, 16, ref glitchValue[3], 1, 0, 30, 0, 6, 2))
                                    _cameraPlus.Config.GlitchFrameRate = _cameraPlus.effectElements.glitchFrameRate = glitchValue[3];
                                MenuUI.Label(6, 14, "Frequency", 6, 2);
                                if (MenuUI.DoubleSpinBox(6, 16, ref glitchValue[4], 0.01f, 0.1f, 0, 1, 2, 6, 2))
                                    _cameraPlus.Config.GlitchFrequency = _cameraPlus.effectElements.glitchFrequency = glitchValue[4];
                                MenuUI.Label(0, 18, "Scale", 6, 2);
                                if (MenuUI.SpinBox(0, 20, ref glitchValue[5], 1, 1, 10, 0, 6, 2))
                                    _cameraPlus.Config.GlitchScale = _cameraPlus.effectElements.glitchScale = glitchValue[5];
                                break;
                        }
                        if (MenuUI.Button(0, 32, "Back top menu", 12, 2))
                        {
                            _menuMode = MenuState.MenuTop;
                            _cameraPlus.Config.Save();
                        }
                        break;
                    /////////////////////////////////////////////////////////////////////////////////////
                    case MenuState.Profile:
                        MenuUI.SetGrid(12, 34);

                        if (MenuUI.Button(0, 0, "Create\nnew profile", 4, 3))
                        {
                            var profileName = CameraUtilities.SaveNewProfile();
                            _selectedProfile = 0;
                            _profileNameList = CameraUtilities.ProfileList();
                            if (profileName != string.Empty)
                                CameraUtilities.ProfileChange(profileName);
                        }
                        if (MenuUI.Button(4, 0, "Save as\ncurrent profile", 4, 3))
                        {
                            var profileName = CameraUtilities.SaveAsCurrentProfile();
                            _selectedProfile = 0;
                            _profileNameList = CameraUtilities.ProfileList();
                            CameraUtilities.ProfileChange(profileName);
                        }
                        if (MenuUI.Button(8, 0, "Delete\nselected profile", 4, 3))
                        {
                            CameraUtilities.DeleteProfile(_profileNameList[_selectedProfile]);
                            _selectedProfile = 0;
                            _profileNameList = CameraUtilities.ProfileList();
                            _configList = CameraUtilities.CameraSettingList(Plugin.cameraController.CurrentProfile);
                        }

                        _selectedProfile = MenuUI.SelectionGrid(0, 4, _selectedProfile, ref _currentProfilePage, _profileNameList, string.Empty, 12, 10);

                        if (MenuUI.Button(1, 15, "Load Profile", 10, 2))
                        {
                            CameraUtilities.ProfileChange(_profileNameList[_selectedProfile]);
                            _configList = CameraUtilities.CameraSettingList(Plugin.cameraController.CurrentProfile);
                            Plugin.cameraController.CloseContextMenu();
                        }

                        if (MenuUI.ToggleSwitch(0, 18, "Load profile on scene change", PluginConfig.Instance.ProfileSceneChange, 12, 2, 1.5f))
                            PluginConfig.Instance.ProfileSceneChange = !PluginConfig.Instance.ProfileSceneChange;

                        if (MenuUI.Button(0, 21, "Menu Scene", 4, 2))
                            PluginConfig.Instance.MenuProfile = _profileNameList[_selectedProfile];
                        MenuUI.Box(4, 21, PluginConfig.Instance.MenuProfile, MenuUI.CustomStyle[2], 7, 2);
                        if (MenuUI.Button(11, 21, "x", 1, 2))
                            PluginConfig.Instance.MenuProfile = string.Empty;

                        if (MenuUI.Button(0, 23, "Game Scene", 4, 2))
                            PluginConfig.Instance.GameProfile = _profileNameList[_selectedProfile];
                        MenuUI.Box(4, 23, PluginConfig.Instance.GameProfile, MenuUI.CustomStyle[2], 7, 2);
                        if (MenuUI.Button(11, 23, "x", 1, 2))
                            PluginConfig.Instance.GameProfile = string.Empty;

                        if (MenuUI.Button(0, 25, "Game 90/360", 4, 2))
                            PluginConfig.Instance.RotateProfile = _profileNameList[_selectedProfile];
                        MenuUI.Box(4, 25, PluginConfig.Instance.RotateProfile, MenuUI.CustomStyle[2], 7, 2);
                        if (MenuUI.Button(11, 25, "x", 1, 2))
                            PluginConfig.Instance.RotateProfile = string.Empty;

                        if (MenuUI.Button(0, 27, "Multiplayer", 4, 2))
                            PluginConfig.Instance.MultiplayerProfile = _profileNameList[_selectedProfile];
                        MenuUI.Box(4, 27, PluginConfig.Instance.MultiplayerProfile, MenuUI.CustomStyle[2], 7, 2);
                        if (MenuUI.Button(11, 27, "x", 1, 2))
                            PluginConfig.Instance.MultiplayerProfile = string.Empty;

                        if (MenuUI.Button(0, 29, "SongScript", 4, 2))
                            PluginConfig.Instance.SongSpecificScriptProfile = _profileNameList[_selectedProfile];
                        MenuUI.Box(4, 29, PluginConfig.Instance.SongSpecificScriptProfile, MenuUI.CustomStyle[2], 7, 2);
                        if (MenuUI.Button(11, 29, "x", 1, 2))
                            PluginConfig.Instance.SongSpecificScriptProfile = string.Empty;

                        if (MenuUI.Button(0, 32, "Back top menu", 12, 2))
                        {
                            _menuMode = MenuState.MenuTop;
                            _cameraPlus.Config.Save();
                        }
                        break;
                    /////////////////////////////////////////////////////////////////////////////////////
                    case MenuState.SettingConverter:
                        MenuUI.SetGrid(12, 34);
                        MenuUI.Label(0, 0, "Import from Camera2 Setting", 12, 2);
                        _selectedCam2Scene = MenuUI.SelectionGrid(0, 2, _selectedCam2Scene, ref _currentCam2ScenePage, Enum.GetNames(typeof(Camera2Utils.SceneTypes)), string.Empty, 8, 10);
                        if (MenuUI.Button(8, 8, "Import", 4, 4))
                        {
                            Camera2ConfigExporter.LoadCamera2Scene(Enum.GetNames(typeof(Camera2Utils.SceneTypes))[_selectedCam2Scene].ToString());
                        }

                        MenuUI.Label(0, 15, "Export to Camera2 Setting", 12, 2);
                        _selectedProfile = MenuUI.SelectionGrid(0, 17, _selectedProfile, ref _currentProfilePage, _profileNameList, string.Empty, 8, 10);
                        if (MenuUI.Button(8, 23, "Export", 4, 4))
                        {
                            Camera2ConfigExporter.ExportCamera2Scene(_profileNameList[_selectedProfile]);
                        }

                        if (MenuUI.Button(0, 32, "Back top menu", 12, 2))
                            _menuMode = MenuState.MenuTop;
                        break;
                        /////////////////////////////////////////////////////////////////////////////////////
                }
                GUI.matrix = originalMatrix;
            }
        }

    }
}
