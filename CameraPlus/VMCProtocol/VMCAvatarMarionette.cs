using System;
using System.Reflection;
using UnityEngine;

namespace CameraPlus.VMCProtocol
{
    public class VMCAvatarMarionette : MonoBehaviour
    {
        public Vector3 position;
        public Quaternion rotate;
        public float fov;
        public bool receivedData = false;
        public virtual void OnEnable()
        {
            if (Plugin.cameraController._vmcAvatar != null)
            {
                var vmcProtocol = GameObject.Find("VMCProtocol");
                if (vmcProtocol != null)
                {
                    var marionette = vmcProtocol.GetComponent("Marionette");
                    EventInfo eventInfo = Plugin.cameraController._vmcAvatar.Assembly.GetType("VMCProtocol.Marionette").GetEvent("CameraTransformAndFov");
                    MethodInfo methodInfo = typeof(VMCAvatarMarionette).GetMethod("OnCameraPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                    Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, methodInfo);
                    eventInfo.AddEventHandler(marionette, handler);
                }
            }
            else
            {
                //To Do : VMCProtocol Receiver
            }
        }

        private void OnCameraPosition(Pose _pose, float _fov)
        {
            position = _pose.position;
            rotate = _pose.rotation;
            fov = _fov;
            receivedData = true;
        }
    }
}
