using UnityEngine;
using CameraPlus.Camera2Utils;
using CameraPlus.Behaviours;
using CameraPlus.Utilities;

namespace CameraPlus.UI
{
    public class ContextMenu : MonoBehaviour
    {
        internal enum MenuState
        {
            MenuTop,
            DisplayObject,
            Layout,
            Multiplayer,
            Profile,
            MovementScript,
            Camera2Converter,
            ExternalLink,
            GreenScreen
        }
        internal Vector2 menuPos
        {
            get
            {
                return new Vector2(
                   Mathf.Min(mousePosition.x / (Screen.width / 1600f), (Screen.width * ( 0.806249998f / (Screen.width / 1600f)))),
                   Mathf.Min((Screen.height - mousePosition.y) / (Screen.height / 900f), (Screen.height * (0.555555556f / (Screen.height / 900f))))
                    );
            }
        }
        internal Vector2 mousePosition;
        internal bool showMenu;
        internal MenuState MenuMode = MenuState.MenuTop;
        internal CameraPlusBehaviour parentBehaviour;
        internal string[] scriptName;
        internal int scriptPage;
        internal Texture2D texture = null;
        internal Texture2D Cameratexture = null;
        internal GUIStyle CustomEnableStyle = null;
        internal GUIStyle CustomDisableStyle = null;
        internal GUIStyle ProfileStyle = null;

        private MenuDisplayObject menuDisplayObject;
        private MenuLayout menuLayout;
        private MenuMultiplayer menuMultiplayer;
        private MenuProfile menuProfile;
        private MenuMovementScript menuMovementScript;
        private MenuCamera2 menuCamera2Converter;
        private MenuExternalLink menuExternalLink;
        private MenuGreenScreen menuGreenScreen;

        public void EnableMenu(Vector2 mousePos, CameraPlusBehaviour parentBehaviour)
        {
            this.enabled = true;
            mousePosition = mousePos;
            showMenu = true;
            this.parentBehaviour = parentBehaviour;
            MenuMode = 0;
            scriptName = null;
            scriptPage = 0;

            menuDisplayObject = new MenuDisplayObject();
            menuLayout = new MenuLayout();
            menuMultiplayer = new MenuMultiplayer();
            menuProfile = new MenuProfile();
            menuMovementScript = new MenuMovementScript();
            menuCamera2Converter = new MenuCamera2();
            menuExternalLink = new MenuExternalLink();
            menuGreenScreen = new MenuGreenScreen();

            if (this.parentBehaviour.Config.LockScreen)
                texture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.Lock.png");
            else
                texture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.UnLock.png");
            if (this.parentBehaviour.Config.LockCamera || this.parentBehaviour.Config.LockCameraDrag)
                Cameratexture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.CameraLock.png");
            else
                Cameratexture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.CameraUnlock.png");
        }
        public void DisableMenu()
        {
            if (!this) return;
            this.enabled = false;
            showMenu = false;
        }
        void OnGUI()
        {

            if (showMenu)
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
                GUI.Box(new Rect(menuPos.x - 5, menuPos.y, 310, 470), $"CameraPlus {parentBehaviour.name}");
                GUI.Box(new Rect(menuPos.x - 5, menuPos.y, 310, 470), $"CameraPlus {parentBehaviour.name}");
                GUI.Box(new Rect(menuPos.x - 5, menuPos.y, 310, 470), $"CameraPlus {parentBehaviour.name}");

                CustomEnableStyle = new GUIStyle(GUI.skin.button);
                CustomEnableStyle.normal.background = CustomEnableStyle.active.background;
                CustomEnableStyle.hover.background = CustomEnableStyle.active.background;
                CustomDisableStyle = new GUIStyle(GUI.skin.button);
                ProfileStyle = new GUIStyle(GUI.skin.box);
                ProfileStyle.alignment = UnityEngine.TextAnchor.MiddleLeft;

                if (MenuMode == MenuState.MenuTop)
                {
                    if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 25, 30, 30), texture))
                    {
                        parentBehaviour.Config.LockScreen = !parentBehaviour.Config.LockScreen;
                        parentBehaviour.Config.Save();
                        if (this.parentBehaviour.Config.LockScreen)
                            texture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.Lock.png");
                        else
                            texture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.UnLock.png");
                    }
                    GUI.Box(new Rect(menuPos.x + 35, menuPos.y + 25, 115, 30), new GUIContent(parentBehaviour.Config.LockScreen ? "Locked Screen" : "Unlocked Screen"), ProfileStyle);

                    if (GUI.Button(new Rect(menuPos.x + 150, menuPos.y + 25, 30, 30), Cameratexture))
                    {
                        if (!parentBehaviour.Config.LockCamera && !parentBehaviour.Config.LockCameraDrag)
                        {
                            parentBehaviour.Config.LockCamera = true;
                            parentBehaviour.Config.LockCameraDrag = false;
                            Cameratexture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.CameraLock.png");
                        }
                        else if (parentBehaviour.Config.LockCamera && !parentBehaviour.Config.LockCameraDrag)
                        {
                            parentBehaviour.Config.LockCamera = false;
                            parentBehaviour.Config.LockCameraDrag = true;
                            Cameratexture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.CameraLock.png");
                        }
                        else
                        {
                            parentBehaviour.Config.LockCamera = false;
                            parentBehaviour.Config.LockCameraDrag = false;
                            Cameratexture = CustomUtils.LoadTextureFromResources("CameraPlus.Resources.CameraUnlock.png");
                        }
                        parentBehaviour.Config.Save();
                    }
                    GUI.Box(new Rect(menuPos.x + 185, menuPos.y + 25, 115, 30), new GUIContent(parentBehaviour.Config.LockCameraDrag ? "ResetDrag Camera" : (parentBehaviour.Config.LockCamera ? "Locked Camera" : "Unlocked Camera")), ProfileStyle);

                    if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 60, 145, 60), new GUIContent("Add New Camera")))
                    {
                        lock (Plugin.cameraController.Cameras)
                        {
                            string cameraName = CameraUtilities.GetNextCameraName();
                            Logger.log.Notice($"Adding new config with name {cameraName}.cfg");
                            CameraUtilities.AddNewCamera(cameraName);
                            CameraUtilities.ReloadCameras();
                            parentBehaviour.CloseContextMenu();
                        }
                    }
                    if (GUI.Button(new Rect(menuPos.x + 150, menuPos.y + 60, 145, 60), new GUIContent("Duplicate\nSelected Camera")))
                    {
                        lock (Plugin.cameraController.Cameras)
                        {
                            string cameraName = CameraUtilities.GetNextCameraName();
                            Logger.log.Notice($"Adding {cameraName}");
                            CameraUtilities.AddNewCamera(cameraName, parentBehaviour.Config);
                            CameraUtilities.ReloadCameras();
                            parentBehaviour.CloseContextMenu();
                        }
                    }
                    if (GUI.Button(new Rect(menuPos.x + 150, menuPos.y + 130, 145, 50), new GUIContent("Remove\nSelected Camera")))
                    {
                        lock (Plugin.cameraController.Cameras)
                        {
                            if (CameraUtilities.RemoveCamera(parentBehaviour))
                            {
                                parentBehaviour._isCameraDestroyed = true;
                                parentBehaviour.CreateScreenRenderTexture();
                                parentBehaviour.CloseContextMenu();
                                Logger.log.Notice("Camera removed!");
                            }
                        }
                    }

                    //First Person, Third Person, 360degree
                    GUI.Box(new Rect(menuPos.x, menuPos.y + 190, 300, 55), "Camera Mode");
                    if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 210, 95, 30), new GUIContent("First Person"), !parentBehaviour.Config.thirdPerson ? CustomEnableStyle : CustomDisableStyle))
                    {
                        parentBehaviour.Config.thirdPerson = false;
                        parentBehaviour.Config.use360Camera = false;
                        parentBehaviour.ThirdPerson = parentBehaviour.Config.thirdPerson;
                        parentBehaviour.ThirdPersonPos = parentBehaviour.Config.Position;
                        parentBehaviour.ThirdPersonRot = parentBehaviour.Config.Rotation;

                        parentBehaviour.CreateScreenRenderTexture();
                        parentBehaviour.Config.Save();
                    }
                    if (GUI.Button(new Rect(menuPos.x + 105, menuPos.y + 210, 95, 30), new GUIContent("Third Person"), (parentBehaviour.Config.thirdPerson && !parentBehaviour.Config.use360Camera) ? CustomEnableStyle : CustomDisableStyle))
                    {
                        parentBehaviour.Config.thirdPerson = true;
                        parentBehaviour.Config.use360Camera = false;
                        parentBehaviour.ThirdPersonPos = parentBehaviour.Config.Position;
                        parentBehaviour.ThirdPersonRot = parentBehaviour.Config.Rotation;

                        parentBehaviour.CreateScreenRenderTexture();
                        parentBehaviour.Config.Save();
                    }
                    if (GUI.Button(new Rect(menuPos.x + 205, menuPos.y + 210, 95, 30), new GUIContent("360 degree"), (parentBehaviour.Config.thirdPerson && parentBehaviour.Config.use360Camera) ? CustomEnableStyle : CustomDisableStyle))
                    {
                        parentBehaviour.Config.thirdPerson = true;
                        parentBehaviour.Config.use360Camera = true;
                        parentBehaviour.ThirdPersonPos = parentBehaviour.Config.Position;
                        parentBehaviour.ThirdPersonRot = parentBehaviour.Config.Rotation;

                        parentBehaviour.CreateScreenRenderTexture();
                        parentBehaviour.Config.Save();
                    }

                    if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 250, 145, 40), new GUIContent("Display Object")))
                        MenuMode = MenuState.DisplayObject;
                    if (GUI.Button(new Rect(menuPos.x + 150, menuPos.y + 250, 145, 40), new GUIContent("Layout")))
                        MenuMode = MenuState.Layout;
                    if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 295, 145, 40), new GUIContent("Multiplayer")))
                        MenuMode = MenuState.Multiplayer;
                    if (GUI.Button(new Rect(menuPos.x + 150, menuPos.y + 295, 145, 40), new GUIContent("Greenscreen Mode")))
                        MenuMode = MenuState.GreenScreen;
                    if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 340, 145, 40), new GUIContent("Profile Saver")))
                        MenuMode = MenuState.Profile;
                    if (GUI.Button(new Rect(menuPos.x + 150, menuPos.y + 340, 145, 40), new GUIContent("MovementScript")))
                    {
                        MenuMode = MenuState.MovementScript;
                        scriptName = CameraUtilities.MovementScriptList();
                    }
                    if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 385, 145, 40), new GUIContent("Camera2 Converter")))
                    {
                        MenuMode = MenuState.Camera2Converter;
                        Camera2ConfigExporter.Init();
                    }
                    if (GUI.Button(new Rect(menuPos.x + 150, menuPos.y + 385, 145, 40), new GUIContent("External linkage")))
                        MenuMode = MenuState.ExternalLink;
                    /*
                    if (GUI.Button(new Rect(menuPos.x, menuPos.y + 345, 300, 30), new GUIContent(parentBehaviour.Config.Orthographics ? "Perspective" : "Orthographics")))
                    {
                        parentBehaviour.Config.Orthographics = !parentBehaviour.Config.Orthographics;
                        parentBehaviour.Config.Save();
                        parentBehaviour.CloseContextMenu();
                        parentBehaviour.CreateScreenRenderTexture();
                    }
                    */
                    /*
                    if (GUI.Button(new Rect(menuPos.x, menuPos.y + 385, 300, 30), new GUIContent("Spawn 38 Cameras")))
                    {
                        parentBehaviour.StartCoroutine(CameraUtilities.Spawn38Cameras());
                        parentBehaviour.CloseContextMenu();
                    }*/
                    if (GUI.Button(new Rect(menuPos.x, menuPos.y + 430, 300, 30), new GUIContent("Close Menu")))
                        parentBehaviour.CloseContextMenu();
                }
                else if (MenuMode == MenuState.DisplayObject)
                    menuDisplayObject.DiplayMenu(parentBehaviour, this, menuPos);
                else if (MenuMode == MenuState.Layout)
                    menuLayout.DiplayMenu(parentBehaviour, this, menuPos);
                else if (MenuMode == MenuState.Multiplayer)
                    menuMultiplayer.DiplayMenu(parentBehaviour, this, menuPos);
                else if (MenuMode == MenuState.GreenScreen)
                    menuGreenScreen.DisplayMenu(parentBehaviour, this, menuPos);
                else if (MenuMode == MenuState.Profile)
                    menuProfile.DiplayMenu(parentBehaviour, this, menuPos);
                else if (MenuMode == MenuState.MovementScript)
                    menuMovementScript.DiplayMenu(parentBehaviour, this, menuPos);
                else if (MenuMode == MenuState.Camera2Converter)
                    menuCamera2Converter.DiplayMenu(parentBehaviour, this, menuPos);
                else if (MenuMode == MenuState.ExternalLink)
                    menuExternalLink.DiplayMenu(parentBehaviour, this, menuPos);
                GUI.matrix = originalMatrix;
            }
        }
    }
}
