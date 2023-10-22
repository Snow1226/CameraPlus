using UnityEngine;
using CameraPlus.Behaviours;
using CameraPlus.Utilities;

namespace CameraPlus.Utilities
{
    public static class PostEffect
    {
        public static void DepthOfField(CameraPlusBehaviour cameraPlus, RenderTexture renderTexture, Material material)
        {
            if (cameraPlus == null) return;
            if (material == null) material = new Material(Plugin.cameraController.Shaders["Effect/DepthOfFieldGauss"]);
            if (cameraPlus._cam.depthTextureMode != (DepthTextureMode.Depth))
                cameraPlus._cam.depthTextureMode = DepthTextureMode.Depth;

            RenderTexture temp1 = RenderTexture.GetTemporary(cameraPlus._cam.pixelWidth, cameraPlus._cam.pixelHeight, 0, RenderTextureFormat.Default);

            material.SetFloat(ShaderPropertyID.FocusDistance, cameraPlus.effectElements.dofFocusDistance);
            material.SetFloat(ShaderPropertyID.FocusRange, cameraPlus.effectElements.dofFocusRange);
            material.SetFloat(ShaderPropertyID.BlurRadius, cameraPlus.effectElements.dofBlurRadius);

            Graphics.Blit(renderTexture, temp1, material, 0);
            Graphics.Blit(temp1, renderTexture, material, 1);
            RenderTexture.ReleaseTemporary(temp1);
        }

        /*
        public static void Glitch(CameraPlusBehaviour cameraPlus, RenderTexture renderTexture, Material material)
        {
            if (cameraPlus == null) return;
            if (material == null) material = new Material(Plugin.cameraController.Shaders["Effect/Glitch"]);
            if (cameraPlus._cam.depthTextureMode != (DepthTextureMode.Depth))
                cameraPlus._cam.depthTextureMode = DepthTextureMode.Depth;

            material.SetFloat(ShaderPropertyID.LineSpeed, cameraPlus.effectElements.glitchLineSpeed);
            material.SetFloat(ShaderPropertyID.LineSize, cameraPlus.effectElements.glitchLineSize);
            material.SetFloat(ShaderPropertyID.ColorGap, cameraPlus.effectElements.glitchColorGap);
            material.SetFloat(ShaderPropertyID.FrameRate, cameraPlus.effectElements.glitchFrameRate);
            material.SetFloat(ShaderPropertyID.Frequency, cameraPlus.effectElements.glitchFrequency);
            material.SetFloat(ShaderPropertyID.GlitchScale, cameraPlus.effectElements.glitchScale);

            Graphics.Blit(renderTexture, renderTexture, material);
        }
        */
        public static void Outline(CameraPlusBehaviour cameraPlus, RenderTexture renderTexture, Material material)
        {
            if (cameraPlus == null) return;
            if (cameraPlus._cam.depthTextureMode != DepthTextureMode.DepthNormals)
                cameraPlus._cam.depthTextureMode = DepthTextureMode.DepthNormals;

            if (material == null) material = new Material(Plugin.cameraController.Shaders["Effect/Outline"]);
            material.SetFloat(ShaderPropertyID.EdgeOnly, cameraPlus.effectElements.outlineOnly);
            material.SetColor(ShaderPropertyID.EdgeColor, cameraPlus.effectElements.outlineColor);
            material.SetColor(ShaderPropertyID.BackgroundColor, cameraPlus.effectElements.outlineBGColor);

            Graphics.Blit(renderTexture, renderTexture, material);
        }
        public static void Wipe(CameraPlusBehaviour cameraPlus, RenderTexture srcRenderTexture, RenderTexture destRenderTexture, Material material)
        {
            if (cameraPlus == null) return;
            if (material == null) material = new Material(Plugin.cameraController.Shaders["Effect/Wipe"]);
            material.SetFloat(ShaderPropertyID.Progress, cameraPlus.effectElements.wipeProgress);
            material.SetVector(ShaderPropertyID.Center, cameraPlus.effectElements.wipeCircleCenter);
            Graphics.Blit(srcRenderTexture, destRenderTexture, material,
                cameraPlus.effectElements.wipeType == "Circle" ? 0 :
                cameraPlus.effectElements.wipeType == "Left" ? 1 :
                cameraPlus.effectElements.wipeType == "Right" ? 2 :
                cameraPlus.effectElements.wipeType == "Top" ? 3 : 4);
        }

    }
}
