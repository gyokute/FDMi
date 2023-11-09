
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum InputButton { Grab, Trigger, Jump, PadTouch, Length }
    public class FDMiInputPage : UdonSharpBehaviour
    {
        public bool enable = true;
        public FDMiInputManager inputManager;
        public FDMiInputAddon[] InputAddons = new FDMiInputAddon[(int)InputButton.Length];
        VRCPlayerApi.TrackingData track;
        [System.NonSerializedAttribute] public FDMiFingerTrackerType holdingHandType;
        VRCPlayerApi.TrackingDataType handType;
        int[] handKeyCode = { (int)KeyCode.None, (int)KeyCode.None, (int)KeyCode.None, (int)KeyCode.None };
        int[] handKeyCodeL = { (int)KeyCode.JoystickButton4, (int)KeyCode.JoystickButton14, (int)KeyCode.JoystickButton3, (int)KeyCode.JoystickButton16 };
        int[] handKeyCodeR = { (int)KeyCode.JoystickButton5, (int)KeyCode.JoystickButton15, (int)KeyCode.JoystickButton1, (int)KeyCode.JoystickButton17 };
        protected bool fingerInZone = false;


        public virtual void OnDisable()
        {
            fingerInZone = false;
            holdingHandType = FDMiFingerTrackerType.None;
        }
        void LateUpdate()
        {
            if (!fingerInZone) gameObject.SetActive(false);
            if (!enable) return;
            if (holdingHandType == FDMiFingerTrackerType.L) getLeftHandStatus();
            if (holdingHandType == FDMiFingerTrackerType.R) getRightHandStatus();

            for (int i = 0; i < InputAddons.Length; i++)
            {
                if (!InputAddons[i]) continue;
                if (Input.GetKey((KeyCode)handKeyCode[(int)InputAddons[i].SelectInputType]) && !InputAddons[i].isActive)
                    InputAddons[i].OnCalled((KeyCode)handKeyCode[(int)InputAddons[i].SelectInputType], handType);
            }
        }
        void getLeftHandStatus()
        {
            handKeyCode = handKeyCodeL;
            handType = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        void getRightHandStatus()
        {
            handKeyCode = handKeyCodeR;
            handType = VRCPlayerApi.TrackingDataType.RightHand;
        }

        #region Finger Input
        public virtual void OnFingerEnter(FDMiFingerTracker finger)
        {
            finger.targetInput = this;
            fingerInZone = true;
            holdingHandType = finger.fingerType;
            inputManager.OnFingerEnter(finger.fingerType);
            gameObject.SetActive(true);
        }
        public virtual void OnFingerLeave(FDMiFingerTracker finger)
        {
            if (finger.targetInput == this) finger.targetInput = null;
            if (holdingHandType != finger.fingerType) return;
            fingerInZone = false;
            holdingHandType = FDMiFingerTrackerType.None;
            inputManager.OnFingerLeave(finger.fingerType);
        }
        #endregion
    }
}