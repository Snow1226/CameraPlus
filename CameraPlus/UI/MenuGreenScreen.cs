using UnityEngine;
using CameraPlus.Behaviours;

namespace CameraPlus.UI
{
    internal class MenuGreenScreen
    {
        internal void DisplayMenu(CameraPlusBehaviour parentBehaviour,ContextMenu contextMenu, Vector2 menuPos)
        {
            //Greenscreen Activation
            GUI.Box(new Rect(menuPos.x, menuPos.y + 25, 300, 55), "Greenscreen Mode");
            if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 45, 95, 30), new GUIContent("Off"), parentBehaviour.Config.GreenScreenMode == "off" ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenMode = "off";
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }
            if (GUI.Button(new Rect(menuPos.x + 105, menuPos.y + 45, 95, 30), new GUIContent("Transparent"), parentBehaviour.Config.GreenScreenMode == "transparent" ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenMode = "transparent";
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }

            if (GUI.Button(new Rect(menuPos.x + 205, menuPos.y + 45, 95, 30), new GUIContent("Greenscreen"), parentBehaviour.Config.GreenScreenMode == "greenscreen" ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenMode = "greenscreen";
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }

            //Debri
            GUI.Box(new Rect(menuPos.x, menuPos.y + 80, 300, 55), "Display Debri");
            if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 100, 95, 30), new GUIContent("Link InGame"), parentBehaviour.Config.GreenScreenDebri == "link" ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenDebri = "link";
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }
            if (GUI.Button(new Rect(menuPos.x + 105, menuPos.y + 100, 95, 30), new GUIContent("Forced Display"), parentBehaviour.Config.GreenScreenDebri == "show" ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenDebri = "show";
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }

            if (GUI.Button(new Rect(menuPos.x + 205, menuPos.y + 100, 95, 30), new GUIContent("Forced Hide"), parentBehaviour.Config.GreenScreenDebri == "hide" ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenDebri = "hide";
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }

            //Display Custom and VMC Avatar
            GUI.Box(new Rect(menuPos.x, menuPos.y + 135, 300, 55), "Display CustomAvatar/VMCAvatar");
            if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 155, 145, 30), new GUIContent("Show Avatar"), parentBehaviour.Config.GreenScreenAvatar ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenAvatar = true;
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }
            if (GUI.Button(new Rect(menuPos.x + 150, menuPos.y + 155, 145, 30), new GUIContent("Hide Avatar"), !parentBehaviour.Config.GreenScreenAvatar ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenAvatar = false;
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }

            //Saber
            GUI.Box(new Rect(menuPos.x, menuPos.y + 190, 300, 55), "Display Saber");
            if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 210, 145, 30), new GUIContent("Show Saber"), parentBehaviour.Config.GreenScreenSaber ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenSaber = true;
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }
            if (GUI.Button(new Rect(menuPos.x + 150, menuPos.y + 210, 145, 30), new GUIContent("Hide Saber"), !parentBehaviour.Config.GreenScreenSaber ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenSaber = false;
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }

            //Notes
            GUI.Box(new Rect(menuPos.x, menuPos.y + 245, 300, 55), "Display Notes");
            if (GUI.Button(new Rect(menuPos.x + 5, menuPos.y + 265, 145, 30), new GUIContent("Show Notes"), parentBehaviour.Config.GreenScreenNotes ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenNotes = true;
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }
            if (GUI.Button(new Rect(menuPos.x + 150, menuPos.y + 265, 145, 30), new GUIContent("Hide Notes"), !parentBehaviour.Config.GreenScreenNotes ? contextMenu.CustomEnableStyle : contextMenu.CustomDisableStyle))
            {
                parentBehaviour.Config.GreenScreenNotes = false;
                parentBehaviour.SetCullingMask();
                parentBehaviour.Config.Save();
            }

            if (GUI.Button(new Rect(menuPos.x, menuPos.y + 430, 300, 30), new GUIContent("Close GreenScreen Menu")))
            {
                contextMenu.MenuMode = ContextMenu.MenuState.MenuTop;
            }
        }
    }
}
