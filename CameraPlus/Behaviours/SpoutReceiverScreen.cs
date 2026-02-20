using CameraPlus.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using Klak.Spout;

namespace CameraPlus.Behaviours
{
    internal class SpoutReceiverScreen : MonoBehaviour
    {
        //internal RawImage rawImage;
        internal Canvas spoutCanvas = null;
        private RectTransform _rect;
        private Material _rawMaterial = new Material(Plugin.cameraController.Shaders["BeatSaber/Unlit/Transparent"]);

        private SpoutReceiver _spoutReceiver;
        private RenderTexture _renderTexture;

        private SpoutResources _spoutResources;
        private void Update()
        {
            if (spoutCanvas && Camera.main != null)
                spoutCanvas.planeDistance = Vector3.Distance(spoutCanvas.worldCamera.transform.position, Camera.main.transform.position)- 0.5f;
        }

        internal void AddSpoutScreen(string spoutName, CameraPlusBehaviour parentBhaviour)
        {

            _spoutResources = SpoutResources.CreateInstance<SpoutResources>();
            _spoutResources.blitShader = Plugin.cameraController.Shaders["Hidden/Klak/Spout/Blit"];

            spoutCanvas = new GameObject("SpoutCanvas").gameObject.AddComponent<Canvas>();
            spoutCanvas.transform.SetParent(this.transform);
            spoutCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            spoutCanvas.worldCamera = parentBhaviour._cam;

            spoutCanvas.planeDistance = 1;

            CanvasScaler canvasScaler = spoutCanvas.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);
            canvasScaler.matchWidthOrHeight = 1;

            RawImage raw = new GameObject("RawImage").AddComponent<RawImage>();
            raw.transform.SetParent(spoutCanvas.transform);
            raw.transform.localPosition = Vector3.zero;
            raw.transform.localEulerAngles = Vector3.zero;

            raw.material = _rawMaterial;

            _rect = raw.GetComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.localScale = new Vector3(1f, 1f, 1);
            _rect.anchoredPosition = new Vector2(0, 0);
            _rect.sizeDelta = new Vector2(Screen.width, Screen.height);
            _rect.localPosition = new Vector3(0, 0, 0);

            ChangeSpoutRectScale(parentBhaviour.Config.screenHeight);

            _spoutReceiver = this.gameObject.AddComponent<SpoutReceiver>();

            _spoutReceiver.SetResources(_spoutResources);

            _spoutReceiver.sourceName = spoutName;
            _renderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);

            _spoutReceiver.targetTexture = _renderTexture;
            _spoutReceiver.targetMaterialProperty = "_MainTex";
            raw.texture = _renderTexture;
        }

        internal void ChangeSpoutRectScale(int screenHeight)
        {
            float scl = Convert.ToSingle(screenHeight) / Convert.ToSingle(Screen.height);
            _rect.localScale = new Vector3(scl, scl, 1);
        }

    }
}
