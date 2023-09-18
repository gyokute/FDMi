
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInput : UdonSharpBehaviour
    {
        public FDMiInputManager inputManager;
        public FDMiInputPickup pickup;
        VRCPlayerApi.TrackingData track;
        [System.NonSerializedAttribute] public int handType = -1;
        [System.NonSerializedAttribute] public Vector3 handPos, handStartPos;
        [System.NonSerializedAttribute] public Quaternion handAxis, handStartAxis;
        [System.NonSerializedAttribute] public float Trigger, stickX, stickY;
        public bool isGrab = false;

        public virtual void OnStartGrab()
        {
            if (handType == (int)VRC_Pickup.PickupHand.Left)
                track = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
            if (handType == (int)VRC_Pickup.PickupHand.Right)
                track = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
            handStartPos = track.position;
            handStartAxis = track.rotation;
        }
        public virtual void OnDropGrab()
        {

        }
        void Update()
        {
            if (handType == (int)VRC_Pickup.PickupHand.Left)
            {
                Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                stickX = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                stickY = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                track = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
            }
            if (handType == (int)VRC_Pickup.PickupHand.Right)
            {
                Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                stickX = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                stickY = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                track = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
            }
            handPos = track.position;
            handAxis = track.rotation;

        }

    }
}