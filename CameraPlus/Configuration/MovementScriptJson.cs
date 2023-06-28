using Newtonsoft.Json;

namespace CameraPlus.Configuration
{
    public class AxizWithFoVElements
    {
        public string x { get; set; }
        public string y { get; set; }
        public string z { get; set; }
        public string FOV { get; set; }
    }

    public class AxisElements
    {
        public string x { get; set; }
        public string y { get; set; }
        public string z { get; set; }
    }

    public class PositionEelemnts
    {
        public string x { get; set; }
        public string y { get; set; }
    }

    public class WindowControlElements
    {
        public string Target { get; set; }
        public bool? Visible { get; set; }

        public PositionEelemnts StartPos { get; set; }
        public PositionEelemnts EndPos { get; set; }
    }

    public class ColorElements
    {
        public string r { get; set; }
        public string g { get; set; }
        public string b { get; set; }
    }
    public class wipeCenterElements
    {
        public string x { get; set; }
        public string y { get; set; }
    }
    public class VisibleObject
    {
        public bool? avatar { get; set; }
        public bool? ui { get; set; }
        public bool? wall { get; set; }
        public bool? wallFrame { get; set; }
        public bool? saber { get; set; }
        //public bool cutParticles { get; set; }
        public bool? notes { get; set; }
        public bool? debris { get; set; }
    }

    public class DoFParamsElements
    {
        public string dofFocusDistance { get; set; }
        public string dofFocusRange { get; set; }
        public string dofBlurRadius { get; set; }
    }
    public class WipeParamsElements
    {
        public string wipeProgress { get; set; }
        public wipeCenterElements wipeCircleCenter { get; set; }
    }
    public class OutlineParamsElements
    {
        public string outlineEffectOnly { get; set; }
        public ColorElements outlineColor { get; set; }
        public ColorElements outlineBackgroundColor { get; set; }
    }

    public class GlitchParmsElements
    {
        public string glitchLineSpeed { get; set; }
        public string glitchLineSize { get; set; }
        public string glitchColorGap { get; set; }
        public string glitchFrameRate { get; set; }
        public string glitchFrequency { get; set; }
        public string glitchScale { get; set; }
    }

    public class EffectObject
    {
        public string enableDoF { get; set; }
        public string dofAutoDistance { get; set; }
        public DoFParamsElements StartDoF { get; set; }
        public DoFParamsElements EndDoF { get; set; }

        public string wipeType { get; set; }
        public WipeParamsElements StartWipe { get; set; }
        public WipeParamsElements EndWipe { get; set; }

        public string enableOutlineEffect { get; set; }
        public OutlineParamsElements StartOutlineEffect { get; set; }
        public OutlineParamsElements EndOutlineEffect { get; set; }

        public string enableGlitchEffect { get; set; }
        public GlitchParmsElements StartGlitchEffect { get; set; }
        public GlitchParmsElements EndGlitchEffect { get; set; }

    }

    [JsonObject("Movements")]
    public class JSONMovement
    {
        [JsonProperty("StartPos")]
        public AxizWithFoVElements startPos { get; set; }
        [JsonProperty("StartRot")]
        public AxisElements startRot { get; set; }
        [JsonProperty("StartHeadOffset")]
        public AxisElements startHeadOffset { get; set; }

        [JsonProperty("EndPos")]
        public AxizWithFoVElements endPos { get; set; }
        [JsonProperty("EndRot")]
        public AxisElements endRot { get; set; }
        [JsonProperty("EndHeadOffset")]
        public AxisElements endHeadOffset { get; set; }
        [JsonProperty("VisibleObject")]
        public VisibleObject visibleObject { get; set; }

        [JsonProperty("CameraEffect")]
        public EffectObject cameraEffect { get; set; }
        public string TurnToHead { get; set; }
        public string TurnToHeadHorizontal { get; set; }
        public string Duration { get; set; }
        public string Delay { get; set; }
        public string EaseTransition { get; set; }

        [JsonProperty("WindowControl")]
        public WindowControlElements[] windowControl { get; set; }
    }

    public class MovementScriptJson
    {
        public string ActiveInPauseMenu { get; set; }
        public string TurnToHeadUseCameraSetting { get; set; }
        [JsonProperty("Movements")]
        public JSONMovement[] Jsonmovement { get; set; }
    }
}
