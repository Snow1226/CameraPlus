using CameraPlus.Utilities;
using UnityEngine;

namespace CameraPlus.Behaviours
{
    /// <summary>
    /// This is the monobehaviour that goes on the camera that handles
    /// displaying the actual feed from the camera to the screen.
    /// </summary>
    public class ScreenCameraBehaviour : MonoBehaviour
    {
        private Camera _cam;
        private CameraPlusBehaviour _cameraPlus;
        private RenderTexture _renderTexture;

        private Material _dofMaterial = null;
        private Material _wipeMaterial = null;
        private Material _outlineMaterial = null;
        private Material _glitchMaterial = null;
        public void SetRenderTexture(RenderTexture renderTexture, CameraPlusBehaviour cameraPlus = null)
        {
            _renderTexture = renderTexture;
            Plugin.Log.Debug($"[Rendertexture in SetRenderTexture] size w:{renderTexture.width}, h:{renderTexture.height}");
            if (cameraPlus != null)
                _cameraPlus = cameraPlus;
        }

        public void SetCameraInfo(Vector2 position, Vector2 size, int layer)
        {
            Plugin.Log.Debug($"[RenderTexture in SetCameraInfo] position x:{position.x}, y:{position.y} / size w:{size.x}, h:{size.y}");
            _cam.pixelRect = new Rect(position, size);
            _cam.depth = layer;
        }

        public void SetPosition(Vector2 position)
        {
            _cam.pixelRect = new Rect(position.x, position.y, _cam.pixelRect.width, _cam.pixelRect.height);
        }

        public void ResetPosition()
        {
            if (_cameraPlus)
                _cam.pixelRect = new Rect(_cameraPlus.Config.screenPosX, _cameraPlus.Config.screenPosY, _cameraPlus.Config.screenWidth, _cameraPlus.Config.screenHeight);
        }
        public void SetLayer(int layer)
        {
            _cam.depth = layer;
        }

        public void Awake()
        {
#if DEBUG
            Plugin.Log.Notice("Created new screen camera behaviour component!");
#endif
            _cam = gameObject.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.Nothing;
            _cam.cullingMask = 0;
            _cam.stereoTargetEye = StereoTargetEyeMask.None;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (_renderTexture == null) return;

            if (_cameraPlus)
            {
                if (_cameraPlus.effectElements.enableDOF) PostEffect.DepthOfField(_cameraPlus, _renderTexture, _dofMaterial);
                //if (_cameraPlus.effectElements.enableGlitch) PostEffect.Glitch(_cameraPlus, _renderTexture, _glitchMaterial);
                if (_cameraPlus.effectElements.enableOutline) PostEffect.Outline(_cameraPlus, _renderTexture, _outlineMaterial);
                if (_cameraPlus.effectElements.wipeProgress > 0)
                {
                    PostEffect.Wipe(_cameraPlus, _renderTexture, dest, _wipeMaterial);
                    return;
                }
            }
            Graphics.Blit(_renderTexture, dest);
        }
    }
}
