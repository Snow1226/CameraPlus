using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CameraPlus.Behaviours;
using CameraPlus.HarmonyPatches;

namespace CameraPlus.VMCProtocol
{
    public class ExternalSender : MonoBehaviour
    {
        internal List<SendTask> sendTasks = new List<SendTask>();
        private Vector3 position = new Vector3();
        private Quaternion rotation = new Quaternion();

        internal class SendTask
        {
            internal CameraPlusBehaviour parentBehaviour = null;
            internal OscClient client = null;
        }

        internal void AddSendTask(CameraPlusBehaviour camplus,string address = "127.0.0.1", int port = 39540)
        {
            SendTask sendTask = new SendTask();
            sendTask.parentBehaviour = camplus;
            sendTask.client = new OscClient(address, port);
            if (sendTask.client != null)
            {
                Plugin.Log.Notice($"Instance of OscClient {address}:{port} Starting.");
                sendTasks.Add(sendTask);
            }
            else
                Plugin.Log.Error($"Instance of OscClient Not Starting.");
        }

        internal void RemoveTask(CameraPlusBehaviour camplus)
        {
            foreach(SendTask sendTask in sendTasks)
            {
                if (sendTask.parentBehaviour.name == camplus.name)
                {
                    sendTasks.Remove(sendTask);
                    break;
                }
            }
        }

        private async Task SendData()
        {
            await Task.Run(() => {
                try
                {
                    foreach(SendTask sendTask in sendTasks)
                    {
                        position = Quaternion.Inverse(RoomAdjustPatch.rotation) * (sendTask.parentBehaviour.ThirdPersonPos - RoomAdjustPatch.position);
                        rotation = Quaternion.Inverse(RoomAdjustPatch.rotation) * Quaternion.Euler(sendTask.parentBehaviour.ThirdPersonRot);

                        sendTask.client.Send("/VMC/Ext/Cam", "Camera", new float[] {
                            position.x, position.y, position.z,
                            rotation.x, rotation.y, rotation.z, rotation.w,
                            sendTask.parentBehaviour.FOV});
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.Error($"ExternalSender Thread : {e}");
                }
            });
        }

        private void Update()
        {
         　Task.Run(() => SendData());
        }
    }
}
