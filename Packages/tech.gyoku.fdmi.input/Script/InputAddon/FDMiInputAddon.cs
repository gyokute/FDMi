
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInputAddon : FDMiBehaviour
    {
        [HideInInspector] public bool isActive = false;
        public InputButton SelectInputType = InputButton.Grab;
        protected KeyCode triggeredKey = KeyCode.None;
        VRCPlayerApi.TrackingData track;
        protected VRCPlayerApi.TrackingDataType handType = VRCPlayerApi.TrackingDataType.Head;
        protected float[] input = new float[(int)InputButton.Length];
        protected Vector3 handPos, handStartPos;
        protected Quaternion handAxis, handStartAxis;

        protected virtual void Start()
        {
            gameObject.SetActive(false);
        }
        protected virtual void Update()
        {
            // If not Init, return
            if (handType == VRCPlayerApi.TrackingDataType.Head) return;
            if (handType == VRCPlayerApi.TrackingDataType.LeftHand) getLeftHandStatus();
            if (handType == VRCPlayerApi.TrackingDataType.RightHand) getRightHandStatus();
            Debug.Log(input[(int)SelectInputType] + "," + Mathf.Approximately(input[(int)SelectInputType], 0f) + "," + isActive);
            if (Mathf.Approximately(input[(int)SelectInputType], 0f)) OnReleased();
            track = Networking.LocalPlayer.GetTrackingData(handType);
            handPos = transform.InverseTransformPoint(track.position);
            handAxis = Quaternion.Inverse(transform.rotation) * track.rotation;
        }
        void getLeftHandStatus()
        {
            input[(int)InputButton.Grab] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
            input[(int)InputButton.Trigger] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            input[(int)InputButton.PadH] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
            input[(int)InputButton.PadV] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
            input[(int)InputButton.Jump] = Input.GetKey(KeyCode.JoystickButton3) ? 1f : 0f;
            input[(int)InputButton.Menu] = Input.GetKey(KeyCode.JoystickButton2) ? 1f : 0f;
            input[(int)InputButton.PadPush] = Input.GetKey(KeyCode.JoystickButton8) ? 1f : 0f;
            input[(int)InputButton.PadTouch] = Input.GetKey(KeyCode.JoystickButton16) ? 1f : 0f;
        }
        void getRightHandStatus()
        {
            input[(int)InputButton.Grab] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
            input[(int)InputButton.Trigger] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            input[(int)InputButton.PadH] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
            input[(int)InputButton.PadV] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
            input[(int)InputButton.Jump] = Input.GetKey(KeyCode.JoystickButton1) ? 1f : 0f;
            input[(int)InputButton.Menu] = Input.GetKey(KeyCode.JoystickButton0) ? 1f : 0f;
            input[(int)InputButton.PadPush] = Input.GetKey(KeyCode.JoystickButton9) ? 1f : 0f;
            input[(int)InputButton.PadTouch] = Input.GetKey(KeyCode.JoystickButton17) ? 1f : 0f;

        }

        public virtual void OnCalled(VRCPlayerApi.TrackingDataType trackType)
        {
            Debug.Log(transform.parent.name + " ONCALLED");
            isActive = true;
            handType = trackType;
            if (trackType == VRCPlayerApi.TrackingDataType.LeftHand) getLeftHandStatus();
            if (trackType == VRCPlayerApi.TrackingDataType.RightHand) getRightHandStatus();
            track = Networking.LocalPlayer.GetTrackingData(trackType);
            handStartPos = transform.InverseTransformPoint(track.position);
            handStartAxis = Quaternion.Inverse(transform.rotation) * track.rotation;
            gameObject.SetActive(true);
        }
        public virtual void OnReleased()
        {
            Debug.Log(transform.parent.name + " ONRELEASED");
            isActive = false;
            gameObject.SetActive(false);
        }
        void OnDisable()
        {
            OnReleased();
        }
    }
}
