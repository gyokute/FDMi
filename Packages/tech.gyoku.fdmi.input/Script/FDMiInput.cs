
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum LeverAxis { x, y, z }
    public class FDMiInput : UdonSharpBehaviour
    {
        public FDMiInputManager inputManager;

        [System.NonSerializedAttribute] VRCPlayerApi.TrackingData track;
        [System.NonSerializedAttribute] public FDMiFingerTrackerType holdingHandType;
        [System.NonSerializedAttribute] public float Grab, Trigger, stickX, stickY;
        [System.NonSerializedAttribute] public float pGrab;
        [System.NonSerializedAttribute] public Vector3 handPos, handStartPos;
        [System.NonSerializedAttribute] public Quaternion handAxis, handStartAxis;
        protected bool grabNow = false, fingerInZone = false;


        private void OnDisable()
        {
            grabNow = false;
            fingerInZone = false;
            holdingHandType = FDMiFingerTrackerType.None;
        }
        void LateUpdate()
        {
            if (!grabNow && !fingerInZone) gameObject.SetActive(false);

            pGrab = Grab;
            if (holdingHandType == FDMiFingerTrackerType.L) getLeftHandStatus();
            if (holdingHandType == FDMiFingerTrackerType.R) getRightHandStatus();
            handPos = transform.InverseTransformPoint(track.position);
            handAxis = Quaternion.Inverse(transform.rotation) * track.rotation;
            
            if (!grabNow && (Grab - pGrab) > 0.5f) OnStartGrab();
            if (grabNow && Grab < 0.25f) OnDropGrab();
        }

        void getLeftHandStatus()
        {
            Grab = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
            Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            stickX = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
            stickY = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
            track = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
        }
        void getRightHandStatus()
        {
            Grab = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
            Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            stickX = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
            stickY = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
            track = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
        }
        public virtual void OnStartGrab()
        {
            grabNow = true;
            inputManager.OnStartGrab(holdingHandType, this);
            handStartPos = handPos;
            handStartAxis = handAxis;
        }

        public virtual void OnDropGrab()
        {
            grabNow = false;
            inputManager.OnStartGrab(holdingHandType, this);
        }

        #region Finger Input
        public void OnFingerEnter(FDMiFingerTracker finger)
        {
            gameObject.SetActive(true);
            fingerInZone = true;
            holdingHandType = finger.fingerType;
        }
        public void OnFingerLeave(FDMiFingerTracker finger)
        {
            if (holdingHandType != finger.fingerType) return;
            fingerInZone = false;
            holdingHandType = FDMiFingerTrackerType.None;
        }
        #endregion
    }
}