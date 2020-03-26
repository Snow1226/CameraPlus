using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using IPA.Utilities;
using LogLevel = IPA.Logging.Logger.Level;
using UnityEngine;
using UnityEngine.SceneManagement;
using CameraPlus.SimpleJSON;

namespace CameraPlus
{
    public class SongCameraMovement : CameraMovement
    {
        public override bool Init(CameraPlusBehaviour cameraPlus)
        {
            _cameraPlus = cameraPlus;
            Plugin.Instance.ActiveSceneChanged += OnActiveSceneChanged;
            return true;
        }
        
        public override void OnActiveSceneChanged(Scene from, Scene to)
        {
            if (to.name == "GameCore")
            {
                //var standardLevelSceneSetupDataSO = Resources.FindObjectsOfTypeAll<StandardLevelScenesTransitionSetupDataSO>().FirstOrDefault();
                //if(standardLevelSceneSetupDataSO)
                //{
                //    foreach(var pair in standardLevelSceneSetupDataSO.sceneInfoSceneSetupDataPairs)
                //    {
                //        if(pair.data is GameplayCoreSceneSetupData)
                //        {
                //            var sceneSetupData = (GameplayCoreSceneSetupData)pair.data;

                //            var level = sceneSetupData.difficultyBeatmap.level;
                //            Plugin.Log($"Level: {level.levelID}");
                //            if (level is SongLoaderPlugin.OverrideClasses.CustomLevel)
                //            {
                //                if (LoadCameraData(Path.Combine((level as SongLoaderPlugin.OverrideClasses.CustomLevel).customSongInfo.path, "CameraMovementData.json")))
                //                    data.ActiveInPauseMenu = false;
                //            }
                //        }
                //    }
                //}
            }
            else if(dataLoaded)
            {
                dataLoaded = false;
                _cameraPlus.ThirdPersonPos = _cameraPlus.Config.Position;
                _cameraPlus.ThirdPersonRot = _cameraPlus.Config.Rotation;
            }
            base.OnActiveSceneChanged(from, to);
        }

        public override void Shutdown()
        {
            Plugin.Instance.ActiveSceneChanged -= OnActiveSceneChanged;
            Destroy(this);
        }
    }

    public class CameraMovement : MonoBehaviour
    {
        protected CameraPlusBehaviour _cameraPlus;
        protected bool dataLoaded = false;
        protected CameraData data = new CameraData();
        protected Vector3 StartPos = Vector3.zero;
        protected Vector3 EndPos = Vector3.zero;
        protected Vector3 StartRot = Vector3.zero;
        protected Vector3 EndRot = Vector3.zero;
        protected bool easeTransition = true;
        protected float movePerc;
        protected int eventID;
        protected DateTime movementStartTime, movementEndTime, movementDelayEndTime;
        protected bool _paused = false;
        protected DateTime _pauseTime;

        public class Movements
        {
            public Vector3 StartPos;
            public Vector3 StartRot;
            public Vector3 EndPos;
            public Vector3 EndRot;
            public float Duration;
            public float Delay;
            public bool EaseTransition = true;
        }

        public class CameraData
        {
            public bool ActiveInPauseMenu = true;
            public List<Movements> Movements = new List<Movements>();
            
            public bool LoadFromJson(string jsonString)
            {
                Movements.Clear();
                JSONNode node = JSON.Parse(jsonString);

                if (node != null && !node["Movements"].IsNull)
                {
                    if (node["ActiveInPauseMenu"].IsBoolean)
                        ActiveInPauseMenu = node["ActiveInPauseMenu"].AsBool;

                    foreach (JSONObject movement in node["Movements"].AsArray)
                    {
                        Movements newMovement = new Movements();
                        var startPos = movement["StartPos"];
                        var startRot = movement["StartRot"];
                        newMovement.StartPos = new Vector3(startPos["x"].AsFloat, startPos["y"].AsFloat, startPos["z"].AsFloat);
                        newMovement.StartRot = new Vector3(startRot["x"].AsFloat, startRot["y"].AsFloat, startRot["z"].AsFloat);

                        var endPos = movement["EndPos"];
                        var endRot = movement["EndRot"];
                        newMovement.EndPos = new Vector3(endPos["x"].AsFloat, endPos["y"].AsFloat, endPos["z"].AsFloat);
                        newMovement.EndRot = new Vector3(endRot["x"].AsFloat, endRot["y"].AsFloat, endRot["z"].AsFloat);

                        newMovement.Delay = movement["Delay"].AsFloat;
                        newMovement.Duration = Mathf.Clamp(movement["Duration"].AsFloat, 0.01f, float.MaxValue); // Make sure duration is at least 0.01 seconds, to avoid a divide by zero error
                        
                        if (movement["EaseTransition"].IsBoolean)
                            newMovement.EaseTransition = movement["EaseTransition"].AsBool;

                        Movements.Add(newMovement);
                    }
                    return true;
                }
                return false;
            }
        }

        public virtual void OnActiveSceneChanged(Scene from, Scene to)
        {
            if (to.name == "GameCore")
            {
                var gp = Resources.FindObjectsOfTypeAll<GamePause>().First();
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

            if (movePerc == 1 && movementDelayEndTime <= DateTime.Now)
                UpdatePosAndRot();
                
            long differenceTicks = (movementEndTime - movementStartTime).Ticks;
            long currentTicks = (DateTime.Now - movementStartTime).Ticks;
            movePerc = Mathf.Clamp((float)currentTicks / (float)differenceTicks, 0, 1);

            _cameraPlus.ThirdPersonPos = LerpVector3(StartPos, EndPos, Ease(movePerc));
            _cameraPlus.ThirdPersonRot = LerpVector3(StartRot, EndRot, Ease(movePerc));
        }

        protected Vector3 LerpVector3(Vector3 from, Vector3 to, float percent)
        {
            return new Vector3(Mathf.LerpAngle(from.x, to.x, percent), Mathf.LerpAngle(from.y, to.y, percent), Mathf.LerpAngle(from.z, to.z, percent));
        }

        public virtual bool Init(CameraPlusBehaviour cameraPlus)
        {
            _cameraPlus = cameraPlus;
            Plugin.Instance.ActiveSceneChanged += OnActiveSceneChanged;
            return LoadCameraData(cameraPlus.Config.movementScriptPath);
        }

        public virtual void Shutdown()
        {
            Plugin.Instance.ActiveSceneChanged -= OnActiveSceneChanged;
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

            TimeSpan diff = DateTime.Now - _pauseTime;
            movementStartTime += diff;
            movementEndTime += diff;
            movementDelayEndTime += diff;
            _paused = false;
        }

        protected bool LoadCameraData(string path)
        {
            if (File.Exists(path))
            {
                string jsonText = File.ReadAllText(path);
                if (data.LoadFromJson(jsonText))
                {
                    Logger.Log("Populated CameraData");

                    if (data.Movements.Count == 0)
                    {
                        Logger.Log("No movement data!");
                        return false;
                    }
                    eventID = 0;
                    UpdatePosAndRot();
                    dataLoaded = true;

                    Logger.Log($"Found {data.Movements.Count} entries in: {path}");
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
            eventID++;
            if (eventID >= data.Movements.Count)
                eventID = 0;

            easeTransition = data.Movements[eventID].EaseTransition;

            StartRot = new Vector3(data.Movements[eventID].StartRot.x, data.Movements[eventID].StartRot.y, data.Movements[eventID].StartRot.z);
            StartPos = new Vector3(data.Movements[eventID].StartPos.x, data.Movements[eventID].StartPos.y, data.Movements[eventID].StartPos.z);

            EndRot = new Vector3(data.Movements[eventID].EndRot.x, data.Movements[eventID].EndRot.y, data.Movements[eventID].EndRot.z);
            EndPos = new Vector3(data.Movements[eventID].EndPos.x, data.Movements[eventID].EndPos.y, data.Movements[eventID].EndPos.z);

            FindShortestDelta(ref StartRot, ref EndRot);

            movementStartTime = DateTime.Now;
            movementEndTime = movementStartTime.AddSeconds(data.Movements[eventID].Duration);
            movementDelayEndTime = movementStartTime.AddSeconds(data.Movements[eventID].Duration + data.Movements[eventID].Delay);
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

        public static void CreateExampleScript()
        {
            string path = Path.Combine(UnityGame.UserDataPath, Plugin.Name, "Scripts");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string defaultScript = Path.Combine(path, "ExampleMovementScript.json");
            if (!File.Exists(defaultScript))
                File.WriteAllBytes(defaultScript, Utils.GetResource(Assembly.GetExecutingAssembly(), "CameraPlus.Resources.ExampleMovementScript.json"));
        }
    }
}
