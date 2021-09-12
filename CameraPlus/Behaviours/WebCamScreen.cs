﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace CameraPlus.Behaviours
{
    internal class WebCamScreen : MonoBehaviour
    {
        internal RawImage rawImage;
        internal WebCamTexture webCamTexture = null;
        internal int selectedCamera = 0;
        internal GameObject webCamObject = null;
        internal Canvas webCamCanvas = null;
        private RectTransform rect;
        private Material rawMaterial = new Material(Plugin.cameraController.Shaders["ChromaKey/Unlit/Cutout"]);

        internal void Init(RawImage raw, string webCamName)
        {
            rawImage = raw;
            rawImage.texture = webCamTexture;
            if(webCamTexture && webCamTexture.isPlaying)
                webCamTexture.Stop();
            webCamTexture = new WebCamTexture(webCamName);
            rawImage.texture = webCamTexture;
            rawMaterial.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, 0));
            rawMaterial.SetColor("_ChromaKeyColor", Color.blue);
            rawImage.material = rawMaterial;
            webCamTexture.Play();
        }

        internal void ChromakeyColor(float r, float g, float b)
        {
            rawMaterial.SetColor("_ChromaKeyColor", new Color(r, g, b, 0));
        }
        internal float ChromakeyR
        {
            get
            {
                Color col = rawMaterial.GetColor("_ChromaKeyColor");
                return col.r;
            }
            set
            {
                Color col = rawMaterial.GetColor("_ChromaKeyColor");
                col.r = value;
                rawMaterial.SetColor("_ChromaKeyColor", new Color(col.r, col.g, col.b, 0));
            }
        }
        internal float ChromakeyG
        {
            get
            {
                Color col = rawMaterial.GetColor("_ChromaKeyColor");
                return col.g;
            }
            set
            {
                Color col = rawMaterial.GetColor("_ChromaKeyColor");
                col.g = value;
                rawMaterial.SetColor("_ChromaKeyColor", new Color(col.r, col.g, col.b, 0));
            }
        }
        internal float ChromakeyB
        {
            get
            {
                Color col = rawMaterial.GetColor("_ChromaKeyColor");
                return col.b;
            }
            set
            {
                Color col = rawMaterial.GetColor("_ChromaKeyColor");
                col.b = value;
                rawMaterial.SetColor("_ChromaKeyColor", new Color(col.r, col.g, col.b, 0));
            }
        }

        internal float ChromakeyHue
        {
            get
            {
                return rawMaterial.GetFloat("_ChromaKeyHueRange");
            }
            set
            {
                rawMaterial.SetFloat("_ChromaKeyHueRange", value);
            }
        }
        internal float ChromakeySaturation
        {
            get
            {
                return rawMaterial.GetFloat("_ChromaKeySaturationRange");
            }
            set
            {
                rawMaterial.SetFloat("_ChromaKeySaturationRange", value);
            }
        }
        internal float ChromakeyBrightness
        {
            get
            {
                return rawMaterial.GetFloat("_ChromaKeyBrightnessRange");
            }
            set
            {
                rawMaterial.SetFloat("_ChromaKeyBrightnessRange", value);
            }
        }
        private void Update()
        {
            if (webCamCanvas && Camera.main != null)
                webCamCanvas.planeDistance = Vector3.Distance(webCamCanvas.worldCamera.transform.position, Camera.main.transform.position);// - 0.5f;
        }
        internal void ChangeCamera(string webCamName)
        {
            int cameras = Plugin.cameraController.webCamDevices.Length;
            if (cameras < 1) return;

            selectedCamera++;
            if (selectedCamera >= cameras) selectedCamera = 0;

            webCamTexture.Stop();
            webCamTexture = new WebCamTexture(webCamName);
            rawImage.texture = webCamTexture;
            webCamTexture.Play();
        }

        internal void AddWebCamScreen(string webCamName, CameraPlusBehaviour parentBhaviour)
        {
            webCamObject = new GameObject("WebCamCanvas");
            webCamObject.transform.SetParent(this.transform);

            webCamCanvas = webCamObject.gameObject.AddComponent<Canvas>();
            webCamCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            webCamCanvas.worldCamera = parentBhaviour._cam;

            webCamCanvas.planeDistance = 1;
            CanvasScaler canvasScaler = webCamObject.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);
            canvasScaler.matchWidthOrHeight = 1;
            GameObject canvObj = new GameObject("RawImage");
            canvObj.transform.SetParent(webCamObject.transform);
            canvObj.transform.localPosition = Vector3.zero;
            canvObj.transform.localEulerAngles = Vector3.zero;
            RawImage raw = canvObj.AddComponent<RawImage>();

            rect = canvObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = new Vector3(1f, 1f, 1);
            rect.anchoredPosition = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(Screen.width, Screen.height);
            rect.localPosition = new Vector3(0, 0, 0);

            ChangeWebCamRectScale(parentBhaviour.Config.screenHeight);

            Init(raw, webCamName);
        }

        internal void DisconnectWebCam()
        {
            webCamTexture?.Stop();
            if (webCamObject)
                GameObject.Destroy(webCamObject);
            webCamCanvas = null;
        }

        internal void ChangeWebCamRectScale(int screenHeight)
        {
            float scl = Convert.ToSingle(screenHeight) / Convert.ToSingle(Screen.height);
            rect.localScale = new Vector3(scl, scl, 1);
        }

    }
}
