using CameraPlus.Behaviours;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraPlus.VMCProtocol
{
    public class ExternalReciver : MonoBehaviour
    {
        private OscServer server = null;
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public float fov = 0f;
        private int _receivePort = 0;

        public void Initialize(int receivePort)
        {
            _receivePort = receivePort;
            if (Plugin.cameraController.usedPort.Contains(receivePort))
            {
                Plugin.Log.Error($"External Receiver : Port {receivePort} uses the same port.");
                Destroy(this);
                return;
            }

            server = new OscServer(receivePort);
            Plugin.cameraController.usedPort.Add(receivePort);
            Plugin.Log.Notice($"Instance of OscServer {receivePort} Started.");
            server.MessageDispatcher.AddCallback(
                "/VMC/Ext/Cam", // OSC address
                (string address, OscDataHandle data) => {
                    position.x = data.GetElementAsFloat(1);
                    position.y = data.GetElementAsFloat(2);
                    position.z = data.GetElementAsFloat(3);

                    rotation.x = data.GetElementAsFloat(4);
                    rotation.y = data.GetElementAsFloat(5);
                    rotation.z = data.GetElementAsFloat(6);
                    rotation.w = data.GetElementAsFloat(7);

                    fov = data.GetElementAsFloat(8);
                }
            );
        }

        private void OnDestroy()
        {
            if (server != null)
            {
                server?.Dispose();
                Plugin.cameraController.usedPort.Remove(_receivePort);

                Plugin.Log.Notice($"Instance of OscServer {_receivePort} Stopped.");
            }
        }
    }
}
