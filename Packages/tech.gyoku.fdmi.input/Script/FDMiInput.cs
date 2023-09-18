
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInput : UdonSharpBehaviour
    {
        public FDMiInputPickup pickup;
        public VRC_Pickup.PickupHand currentHand;
        public Vector3 initialPos;
        public Quaternion initialRot;
        VRCPlayerApi.TrackingData track;

        public virtual void OnStartGrab()
        {
            initialPos = pickup.transform.position;
            initialRot = pickup.transform.rotation;
        }
        public virtual void OnDropGrab()
        {

        }
        // void Update()
        // {
        // if (currentHand == VRC_Pickup.PickupHand.Left)
        // {
        //     Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
        //     stickX = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
        //     stickY = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
        //     track = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
        // }
        // if (currentHand == VRC_Pickup.PickupHand.Right)
        // {
        //     Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
        //     stickX = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
        //     stickY = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
        //     track = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
        // }

        // }

    }
}