
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum InputAxis { Grab, Trigger, PadV, PadH, Length }
    public class FDMiInputAddon : UdonSharpBehaviour
    {
        public InputButton SelectInputType = InputButton.Grab;
        protected KeyCode triggeredKey = KeyCode.None;
        VRCPlayerApi.TrackingData track;
        protected VRCPlayerApi.TrackingDataType handType = VRCPlayerApi.TrackingDataType.Head;
        protected float[] inputAxis = new float[(int)InputAxis.Length];
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
            if (!Input.GetKey(triggeredKey)) OnReleased();
            track = Networking.LocalPlayer.GetTrackingData(handType);
            handPos = transform.InverseTransformPoint(track.position);
            handAxis = Quaternion.Inverse(transform.rotation) * track.rotation;
            if (handType == VRCPlayerApi.TrackingDataType.LeftHand) getLeftHandStatus();
            if (handType == VRCPlayerApi.TrackingDataType.RightHand) getRightHandStatus();
        }
        void getLeftHandStatus()
        {
            inputAxis[(int)InputAxis.Grab] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
            inputAxis[(int)InputAxis.Trigger] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            inputAxis[(int)InputAxis.PadH] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
            inputAxis[(int)InputAxis.PadV] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
        }
        void getRightHandStatus()
        {
            inputAxis[(int)InputAxis.Grab] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
            inputAxis[(int)InputAxis.Trigger] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            inputAxis[(int)InputAxis.PadH] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
            inputAxis[(int)InputAxis.PadV] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
        }

        public virtual void OnCalled(KeyCode callKey, VRCPlayerApi.TrackingDataType trackType)
        {
            triggeredKey = callKey;
            handType = trackType;
            track = Networking.LocalPlayer.GetTrackingData(trackType);
            handStartPos = transform.InverseTransformPoint(track.position);
            handStartAxis = Quaternion.Inverse(transform.rotation) * track.rotation;
            gameObject.SetActive(true);
        }
        public virtual void OnReleased()
        {
            gameObject.SetActive(false);
        }
        void OnDisable()
        {
            OnReleased();
        }
    }
}
