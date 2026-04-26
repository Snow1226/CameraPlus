using CameraPlus.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine;
using Klak.Spout;

namespace CameraPlus.Behaviours
{
    internal class SpoutReceiverScreen : MonoBehaviour
    {
        private const int MemoryMappedCapacity = 4096;

        //internal RawImage rawImage;
        internal Canvas spoutCanvas = null;
        private RectTransform _rect;
        private Material _rawMaterial = new Material(Plugin.cameraController.Shaders["BeatSaber/Unlit/Transparent"]);

        private CameraPlusBehaviour _parentBhaviour;
        private SpoutReceiver _spoutReceiver;
        private RenderTexture _renderTexture;
        private MemoryMappedFile _memoryMappedFile;
        private MemoryMappedViewAccessor _memoryMappedViewAccessor;
        private string _memoryMappedFileName;

        private SpoutResources _spoutResources;
        
        private bool _spoutCameraEnabled;
        
        private void Update()
        {
            if (spoutCanvas && Camera.main != null)
                spoutCanvas.planeDistance = Vector3.Distance(spoutCanvas.worldCamera.transform.position, Camera.main.transform.position);
        }

        private void OnEnable()
        {
            _spoutCameraEnabled = true;
            WriteMemoryMappedData();
        }

        private void OnDisable()
        {
            _spoutCameraEnabled = false;
            WriteMemoryMappedData();
        }
        
        private void OnDestroy()
        {
            _memoryMappedViewAccessor?.Dispose();
            _memoryMappedViewAccessor = null;

            _memoryMappedFile?.Dispose();
            _memoryMappedFile = null;
        }

        internal void AddSpoutScreen(string spoutName, CameraPlusBehaviour parentBhaviour)
        {
            _parentBhaviour = parentBhaviour;
            _memoryMappedFileName = $"VMCSpout.Camera.{spoutName}";

            _spoutResources = SpoutResources.CreateInstance<SpoutResources>();
            _spoutResources.blitShader = Plugin.cameraController.Shaders["Hidden/Klak/Spout/Blit"];

            spoutCanvas = new GameObject("SpoutCanvas").gameObject.AddComponent<Canvas>();
            spoutCanvas.transform.SetParent(this.transform);
            spoutCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            spoutCanvas.worldCamera = parentBhaviour._cam;
            parentBhaviour._cam.depthTextureMode |= DepthTextureMode.Depth;

            spoutCanvas.gameObject.layer = Layer.OnlyInThirdPerson;

            spoutCanvas.planeDistance = 1;

            CanvasScaler canvasScaler = spoutCanvas.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(parentBhaviour.Config.screenWidth, parentBhaviour.Config.screenHeight);
            canvasScaler.matchWidthOrHeight = 1;

            RawImage raw = new GameObject("RawImage").AddComponent<RawImage>();
            raw.transform.SetParent(spoutCanvas.transform);
            raw.transform.localPosition = Vector3.zero;
            raw.transform.localEulerAngles = Vector3.zero;

            _rawMaterial.SetFloat("_DepthWriteThreshold", 0.9f);
            _rawMaterial.SetFloat("_AlphaClearThreshold", 0.9f);
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

        public void WriteMemoryMappedData()
        {
            if (_parentBhaviour == null)
                return;

            try
            {
                if (_memoryMappedViewAccessor == null)
                {
                    _memoryMappedFile = MemoryMappedFile.CreateOrOpen(_memoryMappedFileName, MemoryMappedCapacity, MemoryMappedFileAccess.ReadWrite);
                    _memoryMappedViewAccessor = _memoryMappedFile.CreateViewAccessor(0, MemoryMappedCapacity, MemoryMappedFileAccess.ReadWrite);
                }

                SpoutCameraData cameraData = new SpoutCameraData(_spoutCameraEnabled, _memoryMappedFileName, _parentBhaviour._cam.transform.position, _parentBhaviour._cam.transform.rotation, _parentBhaviour._cam.fieldOfView);
                byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cameraData));

                if (payload.Length + sizeof(int) > MemoryMappedCapacity)
                {
                    Plugin.Log.Error($"Memory mapped payload too large: {payload.Length}");
                    return;
                }

                _memoryMappedViewAccessor.Write(0, payload.Length);
                _memoryMappedViewAccessor.WriteArray(sizeof(int), payload, 0, payload.Length);
                _memoryMappedViewAccessor.Flush();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"WriteMemoryMappedData failed: {ex}");
            }
        }
        
        public void WriteMemoryMappedDataMovementScript(Vector3 pos, Quaternion rot, float fov)
        {
            if (_parentBhaviour == null)
                return;

            try
            {
                if (_memoryMappedViewAccessor == null)
                {
                    _memoryMappedFile = MemoryMappedFile.CreateOrOpen(_memoryMappedFileName, MemoryMappedCapacity, MemoryMappedFileAccess.ReadWrite);
                    _memoryMappedViewAccessor = _memoryMappedFile.CreateViewAccessor(0, MemoryMappedCapacity, MemoryMappedFileAccess.ReadWrite);
                }

                SpoutCameraData cameraData = new SpoutCameraData(_spoutCameraEnabled, _memoryMappedFileName, pos, rot, fov);
                byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cameraData));

                if (payload.Length + sizeof(int) > MemoryMappedCapacity)
                {
                    Plugin.Log.Error($"Memory mapped payload too large: {payload.Length}");
                    return;
                }

                _memoryMappedViewAccessor.Write(0, payload.Length);
                _memoryMappedViewAccessor.WriteArray(sizeof(int), payload, 0, payload.Length);
                _memoryMappedViewAccessor.Flush();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"WriteMemoryMappedData failed: {ex}");
            }
        }
    }

    public struct SpoutCameraData
    {
        public string DataVersion;
        public bool CameraEnabled;
        public string Name;
        public float Fov;
        public float[] Position;
        public float[] Rotation;
        public SpoutCameraData(bool cameraEnabled,string name, Vector3 pos, Quaternion rot, float fov)
        {
            this.DataVersion = "1.0";
            this.CameraEnabled = cameraEnabled;
            this.Name = name;
            this.Position = new float[] { pos.x, pos.y, pos.z };
            this.Rotation = new float[] { rot.x, rot.y, rot.z, rot.w };
            this.Fov = fov;
        }
    }
}
