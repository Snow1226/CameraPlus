using Newtonsoft.Json;

namespace CameraPlus.Configuration
{
    internal enum CameraType
    {
        FirstPesron,
        ThirtdPerson
    }
    internal enum DebriVisibility
    {
        Visible,
        Hidden,
        Link
    }
    [JsonObject("cameraLock")]
    public class cameraLockElements
    {
        public bool lockScreen { get; set; }
        public bool lockCamera { get; set; }
        public bool dontSaveDrag { get; set; }
    }
    [JsonObject("visibleObjects")]
    public class visibleObjectsElements
    {
        public bool previewCamera { get; set; }
        public bool avatar { get; set; }
        public bool ui { get; set; }
        public bool wall { get; set; }
        public bool wallFrame { get; set; }
        public bool saber { get; set; }
        public bool cutParticles { get; set; }
        public bool notes { get; set; }
        public string debris { get; set; }
    }
    [JsonObject("cameraExtensions")]
    public class cameraExtensionsElements
    {
        public float positionSmooth { get; set; }
        public float rotationSmooth { get; set; }
        public float rotation360Smooth { get; set; }
        public bool firstPresonCameraForceUpRight { get; set; }
        public bool follow360map { get; set; }
        public bool follow360mapUseLegacyProcess { get; set; }
        public bool followNoodlePlayerTrack { get; set; }
        public bool turnToHead { get; set; }
    }
    [JsonObject("windowRect")]
    public class windowRectElement
    {
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
    [JsonObject("targetTransform")]
    public class targetTransformElements
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
    [JsonObject("movementScript")]
    public class movementScriptElements
    {
        public string movementScript { get; set; }
        public bool useAudioSync { get; set; }
        public bool songSpecificScript { get; set; }
    }
    [JsonObject("multiplayer")]
    public class multiplayerElemetns
    {
        public int targetPlayerNumber { get; set; }
        public bool displayPlayerInfo { get; set; }
    }

    [JsonObject("vmcProtocol")]
    public class vmcProtocolElements
    {
        public string mode { get; set; }
        public string address { get; set; }
        public int port { get; set; }
    }

    [JsonObject("cameraConfig")]
    public class cameraConfig
    {
        public string cameraType { get; set; }
        public float fieldOfView { get; set; }
        public visibleObjectsElements visibleObject { get; set; }
        public int layer { get; set; }
        public int antiAliasing { get; set; }
        public float renderScale { get; set; }

        public windowRectElement windowRect { get; set; }
        public targetTransformElements thirdPersonPos { get; set; }
        public targetTransformElements thirdPersonRot { get; set; }
        public targetTransformElements firstPersonPos { get; set; }
        public targetTransformElements firstPersonRot { get; set; }
        public targetTransformElements turnToHeadOffset { get; set; }
        public movementScriptElements movementScript { get; set; }
        public cameraLockElements cameraLock { get; set; }
        public cameraExtensionsElements cameraExtensions { get; set; }
        public multiplayerElemetns multiplayer { get; set; }
    }
}
