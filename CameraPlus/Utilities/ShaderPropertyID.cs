using UnityEngine;

namespace CameraPlus.Utilities
{
    public static class ShaderPropertyID
    {
        public static int VRCameraOnly = Shader.PropertyToID("_IsVRCameraOnly");

        public static int FocusDistance = Shader.PropertyToID("_FocusDistance");
        public static int FocusRange = Shader.PropertyToID("_FocusRange");
        public static int BlurRadius = Shader.PropertyToID("_BlurRadius");


        public static int LineSpeed = Shader.PropertyToID("_LineSpeed");
        public static int LineSize = Shader.PropertyToID("_LineSize");
        public static int ColorGap = Shader.PropertyToID("_ColorGap");
        public static int FrameRate = Shader.PropertyToID("_FrameRate");
        public static int Frequency = Shader.PropertyToID("_Frequency");
        public static int GlitchScale = Shader.PropertyToID("_GlitchScale");

        public static int EdgeOnly = Shader.PropertyToID("_EdgeOnly");
        public static int EdgeColor = Shader.PropertyToID("_EdgeColor");
        public static int BackgroundColor = Shader.PropertyToID("_BackgroundColor");

        public static int Progress = Shader.PropertyToID("_Progress");
        public static int Center = Shader.PropertyToID("_Center");

        public static int Color = Shader.PropertyToID("_Color");
        public static int CullMode = Shader.PropertyToID("_CullMode");
        public static int ChromaKeyColor = Shader.PropertyToID("_ChromaKeyColor");
        public static int ChromaKeyHueRange = Shader.PropertyToID("_ChromaKeyHueRange");
        public static int ChromaKeyBrightnessRange = Shader.PropertyToID("_ChromaKeyBrightnessRange");
    }
}
