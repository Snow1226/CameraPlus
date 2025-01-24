using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Globalization;
using CameraPlus.HarmonyPatches;
using CameraPlus.Configuration;
using CameraPlus.Utilities;

namespace CameraPlus.Behaviours
{
    public class CameraMovement : MonoBehaviour
    {
        protected CameraPlusBehaviour _cameraPlus;
        protected bool dataLoaded = false;
        protected CameraData data;
        protected Vector3 StartPos = Vector3.zero;
        protected Vector3 EndPos = Vector3.zero;
        protected Vector3 StartRot = Vector3.zero;
        protected Vector3 EndRot = Vector3.zero;
        protected Vector3 StartHeadOffset = Vector3.zero;
        protected Vector3 EndHeadOffset = Vector3.zero;
        protected float StartFOV = 0;
        protected float EndFOV = 0;
        protected bool easeTransition = true;
        protected float movePerc;
        protected int eventID;
        protected float movementStartTime, movementEndTime, movementNextStartTime;
        protected DateTime movementStartDateTime, movementEndDateTime, movementDelayEndDateTime;
        protected bool _paused = false;
        protected DateTime _pauseTime;
        protected CameraEffectStruct[] CameraEffect = {new CameraEffectStruct(), new CameraEffectStruct()};
        protected WindowControlElements[] WindowControl = null;

        private VisibleObject _visibleLayer = null;
        public class Movements
        {
            public Vector3 StartPos;
            public Vector3 StartRot;
            public Vector3 StartHeadOffset;
            public float StartFOV;
            public Vector3 EndPos;
            public Vector3 EndRot;
            public Vector3 EndHeadOffset;
            public float EndFOV;
            public float Duration;
            public float Delay;
            public VisibleObject SectionVisibleObject;
            public bool TurnToHead = false;
            public bool TurnToHeadHorizontal = false;
            public bool EaseTransition = true;
            public CameraEffectStruct[] CameraEffect;
            public WindowControlElements[] WindowControl;
        }
        public class CameraData
        {
            public bool ActiveInPauseMenu = true;
            public bool TurnToHeadUseCameraSetting = false;
            public List<Movements> Movements = new List<Movements>();
            private CameraPlusBehaviour _cameraPlus;
            public CameraData(CameraPlusBehaviour cameraPlus)
            {
                _cameraPlus = cameraPlus;
            }
            public bool LoadFromJson(string jsonString)
            {
                Movements.Clear();
                MovementScriptJson movementScriptJson=null;
                string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                string sepCheck = (sep == "." ? "," : ".");
                try
                {
                    movementScriptJson = JsonConvert.DeserializeObject<MovementScriptJson>(jsonString);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"JSON file syntax error. {ex.Message}");
                }
                if (movementScriptJson != null && movementScriptJson.Jsonmovement !=null)
                {
                    if (movementScriptJson.ActiveInPauseMenu != null)
                        ActiveInPauseMenu = System.Convert.ToBoolean(movementScriptJson.ActiveInPauseMenu);
                    if (movementScriptJson.TurnToHeadUseCameraSetting != null)
                        TurnToHeadUseCameraSetting = System.Convert.ToBoolean(movementScriptJson.TurnToHeadUseCameraSetting);

                    foreach (JSONMovement jsonmovement in movementScriptJson.Jsonmovement)
                    {
                        Movements newMovement = new Movements();

                        AxizWithFoVElements startPos = jsonmovement.startPos;
                        AxisElements startRot = new AxisElements();
                        AxisElements startHeadOffset = new AxisElements();
                        if (jsonmovement.startRot != null) startRot = jsonmovement.startRot;
                        if (jsonmovement.startHeadOffset != null) startHeadOffset = jsonmovement.startHeadOffset;

                        if (startPos.x != null) newMovement.StartPos = new Vector3(float.Parse(startPos.x.Contains(sepCheck) ? startPos.x.Replace(sepCheck, sep) : startPos.x), 
                                                                                    float.Parse(startPos.y.Contains(sepCheck) ? startPos.y.Replace(sepCheck, sep) : startPos.y), 
                                                                                    float.Parse(startPos.z.Contains(sepCheck) ? startPos.z.Replace(sepCheck, sep) : startPos.z));
                        if (startRot.x != null) newMovement.StartRot = new Vector3(float.Parse(startRot.x.Contains(sepCheck) ? startRot.x.Replace(sepCheck, sep) : startRot.x),
                                                                                    float.Parse(startRot.y.Contains(sepCheck) ? startRot.y.Replace(sepCheck, sep) : startRot.y),
                                                                                    float.Parse(startRot.z.Contains(sepCheck) ? startRot.z.Replace(sepCheck, sep) : startRot.z));
                        else
                            newMovement.StartRot = Vector3.zero;

                        if (startHeadOffset.x != null) newMovement.StartHeadOffset = new Vector3(float.Parse(startHeadOffset.x.Contains(sepCheck) ? startHeadOffset.x.Replace(sepCheck, sep) : startHeadOffset.x),
                                                                                    float.Parse(startHeadOffset.y.Contains(sepCheck) ? startHeadOffset.y.Replace(sepCheck, sep) : startHeadOffset.y),
                                                                                    float.Parse(startHeadOffset.z.Contains(sepCheck) ? startHeadOffset.z.Replace(sepCheck, sep) : startHeadOffset.z));
                        else
                            newMovement.StartHeadOffset = Vector3.zero;

                        if (startPos.FOV != null)
                            newMovement.StartFOV = float.Parse(startPos.FOV.Contains(sepCheck) ? startPos.FOV.Replace(sepCheck, sep) : startPos.FOV);
                        else
                            newMovement.StartFOV = 0;

                        AxizWithFoVElements endPos = jsonmovement.endPos;
                        AxisElements endRot = new AxisElements();
                        AxisElements endHeadOffset = new AxisElements();
                        if (jsonmovement.endRot != null) endRot = jsonmovement.endRot;
                        if (jsonmovement.endHeadOffset != null) endHeadOffset = jsonmovement.endHeadOffset;

                        if (endPos.x != null) newMovement.EndPos = new Vector3(float.Parse(endPos.x), float.Parse(endPos.y), float.Parse(endPos.z));
                        if (endRot.x != null) newMovement.EndRot = new Vector3(float.Parse(endRot.x), float.Parse(endRot.y), float.Parse(endRot.z));
                        if (endPos.x != null) newMovement.EndPos = new Vector3(float.Parse(endPos.x.Contains(sepCheck) ? endPos.x.Replace(sepCheck, sep) : endPos.x),
                                                                                    float.Parse(endPos.y.Contains(sepCheck) ? endPos.y.Replace(sepCheck, sep) : endPos.y),
                                                                                    float.Parse(endPos.z.Contains(sepCheck) ? endPos.z.Replace(sepCheck, sep) : endPos.z));
                        if (endRot.x != null) newMovement.EndRot = new Vector3(float.Parse(endRot.x.Contains(sepCheck) ? endRot.x.Replace(sepCheck, sep) : endRot.x),
                                                                                    float.Parse(endRot.y.Contains(sepCheck) ? endRot.y.Replace(sepCheck, sep) : endRot.y),
                                                                                    float.Parse(endRot.z.Contains(sepCheck) ? endRot.z.Replace(sepCheck, sep) : endRot.z));
                        else
                            newMovement.EndRot = Vector3.zero;
                        if (endHeadOffset.x != null) newMovement.EndHeadOffset = new Vector3(float.Parse(endHeadOffset.x.Contains(sepCheck) ? endHeadOffset.x.Replace(sepCheck, sep) : endHeadOffset.x),
                                                            float.Parse(endHeadOffset.y.Contains(sepCheck) ? endHeadOffset.y.Replace(sepCheck, sep) : endHeadOffset.y),
                                                            float.Parse(endHeadOffset.z.Contains(sepCheck) ? endHeadOffset.z.Replace(sepCheck, sep) : endHeadOffset.z));
                        else
                            newMovement.EndHeadOffset = Vector3.zero;


                        if (endPos.FOV != null)
                            newMovement.EndFOV = float.Parse(endPos.FOV.Contains(sepCheck) ? endPos.FOV.Replace(sepCheck, sep) : endPos.FOV);
                        else
                            newMovement.EndFOV = 0;

                        newMovement.CameraEffect = ConvertEffectObject(jsonmovement.cameraEffect);
                        
                        if (jsonmovement.visibleObject != null) newMovement.SectionVisibleObject = jsonmovement.visibleObject;
                        if (jsonmovement.TurnToHead != null) newMovement.TurnToHead = System.Convert.ToBoolean(jsonmovement.TurnToHead);
                        if (jsonmovement.TurnToHeadHorizontal != null) newMovement.TurnToHeadHorizontal = System.Convert.ToBoolean(jsonmovement.TurnToHeadHorizontal);
                        if (jsonmovement.Delay != null) newMovement.Delay = float.Parse(jsonmovement.Delay.Contains(sepCheck) ? jsonmovement.Delay.Replace(sepCheck,sep) : jsonmovement.Delay);
                        if (jsonmovement.Duration != null) newMovement.Duration = Mathf.Clamp(float.Parse(jsonmovement.Duration.Contains(sepCheck) ? jsonmovement.Duration.Replace(sepCheck, sep) : jsonmovement.Duration), 0.01f, float.MaxValue); // Make sure duration is at least 0.01 seconds, to avoid a divide by zero error
                        
                        if (jsonmovement.EaseTransition != null)
                            newMovement.EaseTransition = System.Convert.ToBoolean(jsonmovement.EaseTransition);

                        if(jsonmovement.windowControl != null)
                            newMovement.WindowControl = jsonmovement.windowControl;
                        Movements.Add(newMovement);
                    }
                    return true;
                }
                return false;
            }
            private CameraEffectStruct[] ConvertEffectObject(EffectObject InputEffect)
            {
                CameraEffectStruct[] cameraEffects = { _cameraPlus.Config.InitializeCameraEffect(), _cameraPlus.Config.InitializeCameraEffect() };

                if (InputEffect != null)
                {
                    if (InputEffect.enableDoF != null) cameraEffects[0].enableDOF = cameraEffects[1].enableDOF = System.Convert.ToBoolean(InputEffect.enableDoF);
                    if (InputEffect.dofAutoDistance != null) cameraEffects[0].dofAutoDistance = cameraEffects[1].dofAutoDistance = System.Convert.ToBoolean(InputEffect.dofAutoDistance);
                    if(InputEffect.StartDoF != null)
                    {
                        if (InputEffect.StartDoF.dofFocusDistance != null) cameraEffects[0].dofFocusDistance = System.Convert.ToSingle(InputEffect.StartDoF.dofFocusDistance);
                        if (InputEffect.StartDoF.dofFocusRange != null) cameraEffects[0].dofFocusRange = System.Convert.ToSingle(InputEffect.StartDoF.dofFocusRange);
                        if (InputEffect.StartDoF.dofBlurRadius != null) cameraEffects[0].dofBlurRadius = System.Convert.ToSingle(InputEffect.StartDoF.dofBlurRadius);
                    }
                    if (InputEffect.EndDoF != null)
                    {
                        if (InputEffect.EndDoF.dofFocusDistance != null) cameraEffects[1].dofFocusDistance = System.Convert.ToSingle(InputEffect.EndDoF.dofFocusDistance);
                        if (InputEffect.EndDoF.dofFocusRange != null) cameraEffects[1].dofFocusRange = System.Convert.ToSingle(InputEffect.EndDoF.dofFocusRange);
                        if (InputEffect.EndDoF.dofBlurRadius != null) cameraEffects[1].dofBlurRadius = System.Convert.ToSingle(InputEffect.EndDoF.dofBlurRadius);
                    }

                    if (InputEffect.wipeType != null) cameraEffects[0].wipeType = cameraEffects[1].wipeType = InputEffect.wipeType;
                    if (InputEffect.StartWipe != null)
                    {
                        if (InputEffect.StartWipe.wipeProgress != null) cameraEffects[0].wipeProgress = System.Convert.ToSingle(InputEffect.StartWipe.wipeProgress);
                        if (InputEffect.StartWipe.wipeCircleCenter != null) cameraEffects[0].wipeCircleCenter = new Vector4(System.Convert.ToSingle(InputEffect.StartWipe.wipeCircleCenter.x), System.Convert.ToSingle(InputEffect.StartWipe.wipeCircleCenter.y), 0, 0);
                    }
                    if (InputEffect.EndWipe != null)
                    {
                        if (InputEffect.EndWipe.wipeProgress != null) cameraEffects[1].wipeProgress = System.Convert.ToSingle(InputEffect.EndWipe.wipeProgress);
                        if (InputEffect.EndWipe.wipeCircleCenter != null) cameraEffects[1].wipeCircleCenter = new Vector4(System.Convert.ToSingle(InputEffect.EndWipe.wipeCircleCenter.x), System.Convert.ToSingle(InputEffect.EndWipe.wipeCircleCenter.y), 0, 0);
                    }

                    if (InputEffect.enableOutlineEffect != null) cameraEffects[0].enableOutline = cameraEffects[1].enableOutline = System.Convert.ToBoolean(InputEffect.enableOutlineEffect);
                    if (InputEffect.StartOutlineEffect != null)
                    {
                        if (InputEffect.StartOutlineEffect.outlineEffectOnly != null) cameraEffects[0].outlineOnly = System.Convert.ToSingle(InputEffect.StartOutlineEffect.outlineEffectOnly);
                        if (InputEffect.StartOutlineEffect.outlineColor != null) cameraEffects[0].outlineColor 
                                = new Color(System.Convert.ToSingle(InputEffect.StartOutlineEffect.outlineColor.r), System.Convert.ToSingle(InputEffect.StartOutlineEffect.outlineColor.g), System.Convert.ToSingle(InputEffect.StartOutlineEffect.outlineColor.b), 0);
                        if (InputEffect.StartOutlineEffect.outlineBackgroundColor != null) cameraEffects[0].outlineBGColor 
                                = new Color(System.Convert.ToSingle(InputEffect.StartOutlineEffect.outlineBackgroundColor.r), System.Convert.ToSingle(InputEffect.StartOutlineEffect.outlineBackgroundColor.g), System.Convert.ToSingle(InputEffect.StartOutlineEffect.outlineBackgroundColor.b), 0);
                    }
                    if (InputEffect.EndOutlineEffect != null)
                    {
                        if (InputEffect.EndOutlineEffect.outlineEffectOnly != null) cameraEffects[1].outlineOnly = System.Convert.ToSingle(InputEffect.EndOutlineEffect.outlineEffectOnly);
                        if (InputEffect.EndOutlineEffect.outlineColor != null) cameraEffects[1].outlineColor
                                = new Color(System.Convert.ToSingle(InputEffect.EndOutlineEffect.outlineColor.r), System.Convert.ToSingle(InputEffect.EndOutlineEffect.outlineColor.g), System.Convert.ToSingle(InputEffect.EndOutlineEffect.outlineColor.b), 0);
                        if (InputEffect.EndOutlineEffect.outlineBackgroundColor != null) cameraEffects[1].outlineBGColor
                                = new Color(System.Convert.ToSingle(InputEffect.EndOutlineEffect.outlineBackgroundColor.r), System.Convert.ToSingle(InputEffect.EndOutlineEffect.outlineBackgroundColor.g), System.Convert.ToSingle(InputEffect.EndOutlineEffect.outlineBackgroundColor.b), 0);
                    }

                    if (InputEffect.enableGlitchEffect != null) cameraEffects[0].enableGlitch= cameraEffects[1].enableGlitch = System.Convert.ToBoolean(InputEffect.enableGlitchEffect);
                    if (InputEffect.StartGlitchEffect != null)
                    {
                        if (InputEffect.StartGlitchEffect.glitchLineSpeed != null) cameraEffects[0].glitchLineSpeed = System.Convert.ToSingle(InputEffect.StartGlitchEffect.glitchLineSpeed);
                        if (InputEffect.StartGlitchEffect.glitchLineSize != null) cameraEffects[0].glitchLineSize = System.Convert.ToSingle(InputEffect.StartGlitchEffect.glitchLineSize);
                        if (InputEffect.StartGlitchEffect.glitchColorGap != null) cameraEffects[0].glitchColorGap = System.Convert.ToSingle(InputEffect.StartGlitchEffect.glitchColorGap);
                        if (InputEffect.StartGlitchEffect.glitchFrameRate != null) cameraEffects[0].glitchFrameRate = System.Convert.ToSingle(InputEffect.StartGlitchEffect.glitchFrameRate);
                        if (InputEffect.StartGlitchEffect.glitchFrequency != null) cameraEffects[0].glitchFrequency = System.Convert.ToSingle(InputEffect.StartGlitchEffect.glitchFrequency);
                        if (InputEffect.StartGlitchEffect.glitchScale != null) cameraEffects[0].glitchScale = System.Convert.ToSingle(InputEffect.StartGlitchEffect.glitchScale);
                    }
                    if (InputEffect.EndGlitchEffect != null)
                    {
                        if (InputEffect.EndGlitchEffect.glitchLineSpeed != null) cameraEffects[1].glitchLineSpeed = System.Convert.ToSingle(InputEffect.EndGlitchEffect.glitchLineSpeed);
                        if (InputEffect.EndGlitchEffect.glitchLineSize != null) cameraEffects[1].glitchLineSize = System.Convert.ToSingle(InputEffect.EndGlitchEffect.glitchLineSize);
                        if (InputEffect.EndGlitchEffect.glitchColorGap != null) cameraEffects[1].glitchColorGap = System.Convert.ToSingle(InputEffect.EndGlitchEffect.glitchColorGap);
                        if (InputEffect.EndGlitchEffect.glitchFrameRate != null) cameraEffects[1].glitchFrameRate = System.Convert.ToSingle(InputEffect.EndGlitchEffect.glitchFrameRate);
                        if (InputEffect.EndGlitchEffect.glitchFrequency != null) cameraEffects[1].glitchFrequency = System.Convert.ToSingle(InputEffect.EndGlitchEffect.glitchFrequency);
                        if (InputEffect.EndGlitchEffect.glitchScale != null) cameraEffects[1].glitchScale = System.Convert.ToSingle(InputEffect.EndGlitchEffect.glitchScale);
                    }
                }
                return cameraEffects;
            }
        }

        public virtual void OnActiveSceneChanged(Scene from, Scene to)
        {
            if (to.name == "GameCore")
            {
#if DEBUG
                Plugin.Log.Notice($"Script SceneChanged");
#endif
                if (_cameraPlus.Config.movementScript.useAudioSync)
                {
                    movementNextStartTime = 0;
                    eventID = 0;
                }
                if (from.name == "MainMenu" && _cameraPlus.Config.movementScript.songSpecificScript && SongScriptBeatmapPatch.customLevelPath != String.Empty)
                {
                    LoadCameraData(SongScriptBeatmapPatch.customLevelPath);
                }

                var gp = Resources.FindObjectsOfTypeAll<PauseController>().FirstOrDefault();
                if (gp && dataLoaded && !data.ActiveInPauseMenu)
                {
                    gp.didResumeEvent += Resume;
                    gp.didPauseEvent += Pause;
                }
            }
        }

        protected void Update()
        {
            if (!dataLoaded || _paused) return;

            if (_cameraPlus.Config.movementScript.useAudioSync)
            {
                if (AudioTimeSyncControllerPatch.Instance == null)
                    return;
                while (movementNextStartTime <= AudioTimeSyncControllerPatch.Instance.songTime)
                    UpdatePosAndRot();

                float difference = movementEndTime - movementStartTime;
                float current = AudioTimeSyncControllerPatch.Instance.songTime - movementStartTime;
                if (difference != 0)
                    movePerc = Mathf.Clamp(current / difference, 0, 1);
            }
            else
            {
                if (movePerc == 1 && movementDelayEndDateTime <= DateTime.Now)
                    UpdatePosAndRot();

                long differenceTicks = (movementEndDateTime - movementStartDateTime).Ticks;
                long currentTicks = (DateTime.Now - movementStartDateTime).Ticks;
                movePerc = Mathf.Clamp((float)currentTicks / (float)differenceTicks, 0, 1);
            }

            // Effect Section
            _cameraPlus.effectElements.enableDOF = CameraEffect[0].enableDOF;
            _cameraPlus.effectElements.dofAutoDistance = CameraEffect[0].dofAutoDistance;
            _cameraPlus.effectElements.dofFocusDistance = Mathf.Lerp(CameraEffect[0].dofFocusDistance, CameraEffect[1].dofFocusDistance, Ease(movePerc));
            _cameraPlus.effectElements.dofFocusRange = Mathf.Lerp(CameraEffect[0].dofFocusRange, CameraEffect[1].dofFocusRange, Ease(movePerc));
            _cameraPlus.effectElements.dofBlurRadius = Mathf.Lerp(CameraEffect[0].dofBlurRadius, CameraEffect[1].dofBlurRadius, Ease(movePerc));

            _cameraPlus.effectElements.wipeType = CameraEffect[0].wipeType;
            _cameraPlus.effectElements.wipeProgress = Mathf.Lerp(CameraEffect[0].wipeProgress, CameraEffect[1].wipeProgress, Ease(movePerc));
            _cameraPlus.effectElements.wipeCircleCenter = LerpVector4(CameraEffect[0].wipeCircleCenter, CameraEffect[1].wipeCircleCenter, Ease(movePerc));

            _cameraPlus.effectElements.enableOutline = CameraEffect[0].enableOutline;
            _cameraPlus.effectElements.outlineOnly = Mathf.LerpAngle(CameraEffect[0].outlineOnly, CameraEffect[1].outlineOnly, Ease(movePerc));
            _cameraPlus.effectElements.outlineColor = LerpColor(CameraEffect[0].outlineColor, CameraEffect[1].outlineColor, Ease(movePerc));
            _cameraPlus.effectElements.outlineBGColor = LerpColor(CameraEffect[0].outlineBGColor, CameraEffect[1].outlineBGColor, Ease(movePerc));

            _cameraPlus.effectElements.enableGlitch = CameraEffect[0].enableGlitch;
            _cameraPlus.effectElements.glitchLineSpeed = Mathf.Lerp(CameraEffect[0].glitchLineSpeed, CameraEffect[1].glitchLineSpeed, Ease(movePerc));
            _cameraPlus.effectElements.glitchLineSize = Mathf.Lerp(CameraEffect[0].glitchLineSize, CameraEffect[1].glitchLineSize, Ease(movePerc));
            _cameraPlus.effectElements.glitchColorGap = Mathf.Lerp(CameraEffect[0].glitchColorGap, CameraEffect[1].glitchColorGap, Ease(movePerc));
            _cameraPlus.effectElements.glitchFrameRate = Mathf.Lerp(CameraEffect[0].glitchFrameRate, CameraEffect[1].glitchFrameRate, Ease(movePerc));
            _cameraPlus.effectElements.glitchFrequency = Mathf.Lerp(CameraEffect[0].glitchFrequency, CameraEffect[1].glitchFrequency, Ease(movePerc));
            _cameraPlus.effectElements.glitchScale = Mathf.Lerp(CameraEffect[0].glitchScale, CameraEffect[1].glitchScale, Ease(movePerc));

            // Window Control
            if (_cameraPlus._isMainCamera && WindowControl != null)
            {
                foreach(WindowControlElements windowControl in WindowControl)
                {
                    var otherCameraPlus = CameraUtilities.TargetCameraPlus(windowControl.Target, Plugin.cameraController.CurrentProfile);
                    if (otherCameraPlus != null)
                    {
                        otherCameraPlus.renderScreen = windowControl.Visible.Value;
                        if (windowControl.StartPos != null && windowControl.EndPos != null)
                        {
                            otherCameraPlus.ScreenPosition = LeapVector2(
                                new Vector2(float.Parse(windowControl.StartPos.x), float.Parse(windowControl.StartPos.y)),
                                new Vector2(float.Parse(windowControl.EndPos.x), float.Parse(windowControl.EndPos.y)), Ease(movePerc));
                        }
                        else
                            otherCameraPlus.ScreenRect = otherCameraPlus.Config.rect;
                    }
                }
            }

            _cameraPlus.ThirdPersonPos = LerpVector3(StartPos, EndPos, Ease(movePerc));
            _cameraPlus.ThirdPersonRot = LerpVector3Angle(StartRot, EndRot, Ease(movePerc));
            _cameraPlus.turnToHeadOffset = LerpVector3(StartHeadOffset, EndHeadOffset, Ease(movePerc));
            _cameraPlus.FOV=Mathf.Lerp(StartFOV,EndFOV,Ease(movePerc));

        }

        protected Vector2 LeapVector2(Vector2 from, Vector2 to, float percent)
        {
            return new Vector2(Mathf.Lerp(from.x, to.x, percent), Mathf.Lerp(from.y, to.y, percent));
        }
        protected Vector3 LerpVector3(Vector3 from, Vector3 to, float percent)
        {
            return new Vector3(Mathf.Lerp(from.x, to.x, percent), Mathf.Lerp(from.y, to.y, percent), Mathf.Lerp(from.z, to.z, percent));
        }
        protected Vector3 LerpVector3Angle(Vector3 from, Vector3 to, float percent)
        {
            return new Vector3(Mathf.LerpAngle(from.x, to.x, percent), Mathf.LerpAngle(from.y, to.y, percent), Mathf.LerpAngle(from.z, to.z, percent));
        }
        protected Vector4 LerpVector4(Vector4 from, Vector4 to, float percent)
        {
            return new Vector4(Mathf.Lerp(from.x, to.x, percent), Mathf.Lerp(from.y, to.y, percent), Mathf.Lerp(from.z, to.z, percent), Mathf.Lerp(from.w, to.w, percent));
        }

        protected Color LerpColor(Color from, Color to, float percent)
        {
            return new Color(Mathf.Lerp(from.r, to.r, percent), Mathf.Lerp(from.g, to.g, percent), Mathf.Lerp(from.b, to.b, percent), Mathf.Lerp(from.a, to.a, percent));
        }

        public virtual bool Init(CameraPlusBehaviour cameraPlus, string scriptPath)
        {
            _cameraPlus = cameraPlus;

            Plugin.cameraController.ActiveSceneChanged += OnActiveSceneChanged;
            return LoadCameraData(scriptPath);
        }

        public virtual void Shutdown()
        {
            Plugin.cameraController.ActiveSceneChanged -= OnActiveSceneChanged;
            Destroy(this);
        }

        public void Pause()
        {
            if (_paused) return;

            _paused = true;
            _pauseTime = DateTime.Now;
        }

        public void Resume()
        {
            if (!_paused) return;

            _paused = false;
        }

        protected bool LoadCameraData(string pathFile)
        {
            string path= pathFile;

            if (File.Exists(path))
            {
                string jsonText = File.ReadAllText(path);
                data = new CameraData(_cameraPlus);
                if (data.LoadFromJson(jsonText))
                {
                    if (data.Movements.Count == 0)
                    {
                        Plugin.Log.Notice("No movement data!");
                        return false;
                    }
                    eventID = 0;
                    UpdatePosAndRot();
                    dataLoaded = true;

                    Plugin.Log.Notice($"Found {data.Movements.Count} entries in: {path}");
                    return true;
                }
            }
            return false;
        }

        protected void FindShortestDelta(ref Vector3 from, ref Vector3 to)
        {
            if(Mathf.DeltaAngle(from.x, to.x) < 0)
                from.x += 360.0f;
            if (Mathf.DeltaAngle(from.y, to.y) < 0)
                from.y += 360.0f;
            if (Mathf.DeltaAngle(from.z, to.z) < 0)
                from.z += 360.0f;
        }

        protected void UpdatePosAndRot()
        {
            if (eventID >= data.Movements.Count)
                eventID = 0;

            _cameraPlus.turnToHead = data.TurnToHeadUseCameraSetting ? _cameraPlus.Config.cameraExtensions.turnToHead : data.Movements[eventID].TurnToHead;
            _cameraPlus.turnToHeadHorizontal = data.Movements[eventID].TurnToHeadHorizontal;
            easeTransition = data.Movements[eventID].EaseTransition;

            StartRot = new Vector3(data.Movements[eventID].StartRot.x, data.Movements[eventID].StartRot.y, data.Movements[eventID].StartRot.z);
            StartPos = new Vector3(data.Movements[eventID].StartPos.x, data.Movements[eventID].StartPos.y, data.Movements[eventID].StartPos.z);

            EndRot = new Vector3(data.Movements[eventID].EndRot.x, data.Movements[eventID].EndRot.y, data.Movements[eventID].EndRot.z);
            EndPos = new Vector3(data.Movements[eventID].EndPos.x, data.Movements[eventID].EndPos.y, data.Movements[eventID].EndPos.z);

            CameraEffect[0] = data.Movements[eventID].CameraEffect[0];
            CameraEffect[1] = data.Movements[eventID].CameraEffect[1];

            if (data.Movements[eventID].SectionVisibleObject != null)
            {
                CopyVisibleLayer(data.Movements[eventID].SectionVisibleObject);
                if (_cameraPlus.Config.movementScript.ignoreScriptUIDisplay)
                    _visibleLayer.ui = _cameraPlus.Config.layerSetting.ui;
                _cameraPlus.Config.SetCullingMask(_visibleLayer);
            }
            else
            {
                int beforID = eventID > 0 ? eventID - 1 : data.Movements.Count - 1;
                if(data.Movements[beforID].SectionVisibleObject != null && data.Movements[eventID].SectionVisibleObject == null)
                    _cameraPlus.Config.SetCullingMask(_cameraPlus.Config.visibleObject);
            }

            StartHeadOffset = new Vector3(data.Movements[eventID].StartHeadOffset.x, data.Movements[eventID].StartHeadOffset.y, data.Movements[eventID].StartHeadOffset.z);
            EndHeadOffset = new Vector3(data.Movements[eventID].EndHeadOffset.x, data.Movements[eventID].EndHeadOffset.y, data.Movements[eventID].EndHeadOffset.z);

            if (data.Movements[eventID].StartFOV != 0)
                StartFOV = data.Movements[eventID].StartFOV;
            else
                StartFOV = _cameraPlus.Config.fov;
            if (data.Movements[eventID].EndFOV != 0)
                EndFOV = data.Movements[eventID].EndFOV;
            else
                EndFOV = _cameraPlus.Config.fov;

            FindShortestDelta(ref StartRot, ref EndRot);

            if (_cameraPlus.Config.movementScript.useAudioSync)
            {
                movementStartTime = movementNextStartTime;
                movementEndTime = movementNextStartTime + data.Movements[eventID].Duration;
                movementNextStartTime = movementEndTime + data.Movements[eventID].Delay;
            }
            else
            {
                movementStartDateTime = DateTime.Now;
                movementEndDateTime = movementStartDateTime.AddSeconds(data.Movements[eventID].Duration);
                movementDelayEndDateTime = movementStartDateTime.AddSeconds(data.Movements[eventID].Duration + data.Movements[eventID].Delay);
            }

            WindowControl = data.Movements[eventID].WindowControl;

            eventID++;
        }

        private void CopyVisibleLayer(VisibleObject scriptVisibleObject)
        {
            if (_visibleLayer == null) _visibleLayer = new VisibleObject();
            _visibleLayer.avatar = scriptVisibleObject.avatar;
            _visibleLayer.notes = scriptVisibleObject.notes;
            _visibleLayer.ui = scriptVisibleObject.ui;
            _visibleLayer.saber = scriptVisibleObject.saber;
            _visibleLayer.debris = scriptVisibleObject.debris;
            _visibleLayer.wall = scriptVisibleObject.wall;
            _visibleLayer.wallFrame = scriptVisibleObject.wallFrame;
        }
        protected float Ease(float p)
        {
            if (!easeTransition)
                return p;

            if (p < 0.5f) //Cubic Hopefully
            {
                return 4 * p * p * p;
            }
            else
            {
                float f = ((2 * p) - 2);
                return 0.5f * f * f * f + 1;
            }
        }
    }
}
