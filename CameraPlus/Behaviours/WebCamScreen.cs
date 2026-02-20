using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Collections;
using CameraPlus.Utilities;

namespace CameraPlus.Behaviours
{
    internal class WebCamScreen : MonoBehaviour
    {
        internal RawImage rawImage;
        internal WebCamTexture webCamTexture = null;
        internal int selectedCamera = 0;
        internal Canvas webCamCanvas = null;
        private RectTransform rect;
        private Material rawMaterial = new Material(Plugin.cameraController.Shaders["ChromaKey/Unlit/Cutout"]);

        internal bool colorPickState = false;

        internal void Init(RawImage raw, string webCamName)
        {
            rawImage = raw;
            rawImage.texture = webCamTexture;
            if(webCamTexture && webCamTexture.isPlaying)
                webCamTexture.Stop();
            webCamTexture = new WebCamTexture(webCamName);
            rawImage.texture = webCamTexture;
            rawMaterial.SetColor(ShaderPropertyID.Color, new Color(1.0f, 1.0f, 1.0f, 0));
            rawMaterial.SetColor(ShaderPropertyID.ChromaKeyColor, Color.blue);
            rawImage.material = rawMaterial;
            webCamTexture.Play();
        }

        internal void ChromakeyColor(float r, float g, float b)
        {
            rawMaterial.SetColor(ShaderPropertyID.ChromaKeyColor, new Color(r, g, b, 0));
        }
        internal float ChromakeyR
        {
            get{
                Color col = rawMaterial.GetColor(ShaderPropertyID.ChromaKeyColor);
                return col.r;
            }
            set{
                Color col = rawMaterial.GetColor(ShaderPropertyID.ChromaKeyColor);
                col.r = value;
                rawMaterial.SetColor(ShaderPropertyID.ChromaKeyColor, new Color(col.r, col.g, col.b, 0));
            }
        }
        internal float ChromakeyG
        {
            get{
                Color col = rawMaterial.GetColor(ShaderPropertyID.ChromaKeyColor);
                return col.g;
            }
            set{
                Color col = rawMaterial.GetColor(ShaderPropertyID.ChromaKeyColor);
                col.g = value;
                rawMaterial.SetColor(ShaderPropertyID.ChromaKeyColor, new Color(col.r, col.g, col.b, 0));
            }
        }
        internal float ChromakeyB
        {
            get{
                Color col = rawMaterial.GetColor(ShaderPropertyID.ChromaKeyColor);
                return col.b;
            }
            set{
                Color col = rawMaterial.GetColor(ShaderPropertyID.ChromaKeyColor);
                col.b = value;
                rawMaterial.SetColor(ShaderPropertyID.ChromaKeyColor, new Color(col.r, col.g, col.b, 0));
            }
        }

        internal float ChromakeyHue
        {
            get{
                return rawMaterial.GetFloat(ShaderPropertyID.ChromaKeyHueRange);
            }
            set{
                rawMaterial.SetFloat(ShaderPropertyID.ChromaKeyHueRange, value);
            }
        }
        internal float ChromakeySaturation
        {
            get{
                return rawMaterial.GetFloat(ShaderPropertyID.ChromaKeyHueRange);
            }
            set{ 
                rawMaterial.SetFloat(ShaderPropertyID.ChromaKeyHueRange, value);
            }
        }
        internal float ChromakeyBrightness
        {
            get{
                return rawMaterial.GetFloat(ShaderPropertyID.ChromaKeyBrightnessRange);
            }
            set{
                rawMaterial.SetFloat(ShaderPropertyID.ChromaKeyBrightnessRange, value);
            }
        }
        private IEnumerator ColorPickCoroutine()
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
            Vector2 pos = Input.mousePosition;
            texture.ReadPixels(new Rect(pos.x, pos.y, 1, 1), 0, 0);
            Color color = texture.GetPixel(0, 0);
            ChromakeyColor(color.r, color.g, color.b);
            UnityEngine.Cursor.SetCursor(null, new Vector2(0, 0), CursorMode.Auto);
        }
        private void Update()
        {
            if (webCamCanvas && Camera.main != null)
                webCamCanvas.planeDistance = Vector3.Distance(webCamCanvas.worldCamera.transform.position, Camera.main.transform.position);// - 0.5f;
            if (colorPickState)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    StartCoroutine(ColorPickCoroutine());
                    colorPickState = false;
                }
            }
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
            webCamCanvas = new GameObject("WebCamCanvas").gameObject.AddComponent<Canvas>();
            webCamCanvas.transform.SetParent(transform);
            webCamCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            webCamCanvas.worldCamera = parentBhaviour._cam;
            webCamCanvas.gameObject.layer |= 1 << Layer.OnlyInThirdPerson;

            webCamCanvas.planeDistance = 1;
            CanvasScaler canvasScaler = webCamCanvas.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);
            canvasScaler.matchWidthOrHeight = 1;

            RawImage raw = new GameObject("RawImage").AddComponent<RawImage>();
            raw.transform.SetParent(webCamCanvas.transform);
            raw.transform.localPosition = Vector3.zero;
            raw.transform.localEulerAngles = Vector3.zero;

            rect = raw.GetComponent<RectTransform>();
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
            if (webCamCanvas)
                GameObject.Destroy(webCamCanvas.gameObject);
            webCamCanvas = null;
        }

        internal void ChangeWebCamRectScale(int screenHeight)
        {
            float scl = Convert.ToSingle(screenHeight) / Convert.ToSingle(Screen.height);
            rect.localScale = new Vector3(scl, scl, 1);
        }

    }
}
