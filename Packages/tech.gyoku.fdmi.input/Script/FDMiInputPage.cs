
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum InputButton { Grab, Trigger, PadV, PadH, Jump, Menu, PadTouch, PadPush, Length }
    public enum InputAxis { Grab, Trigger, PadV, PadH, Length }
    public class FDMiInputPage : UdonSharpBehaviour
    {
        public bool enable = true;
        public FDMiInputManager inputManager;
        public FDMiInputAddon[] InputAddons = new FDMiInputAddon[(int)InputButton.Length];
        // public ButtonInput EneringInputType = InputButton.Grab;
        public float inputThreshold = 0.25f;
        VRCPlayerApi.TrackingData track;
        // [System.NonSerializedAttribute] public FDMiFingerTrackerType holdingHandType;
        // VRCPlayerApi.TrackingDataType handType;
        protected bool[] fingerInZone = { false, false, false };
        // protected bool[] fingerHolding = { false, false, false };


        // public virtual void OnDisable()
        // {
        // }
        void LateUpdate()
        {
            if (!fingerInZone[0] && !fingerInZone[1]) gameObject.SetActive(false);
            if (!enable) return;

            // if (fingerInZone[(int)FDMiFingerTrackerType.L]) getLeftHandStatus();
            // if (fingerInZone[(int)FDMiFingerTrackerType.R]) getRightHandStatus();

            for (int i = 0; i < InputAddons.Length; i++)
            {
                if (!InputAddons[i]) continue;
                if (!InputAddons[i].isActive)
                {
                    if (getLeftHandButton(InputAddons[i].SelectInputType))
                        InputAddons[i].OnCalled(VRCPlayerApi.TrackingDataType.LeftHand);
                    if (getRightHandButton(InputAddons[i].SelectInputType))
                        InputAddons[i].OnCalled(VRCPlayerApi.TrackingDataType.RightHand);
                }
            }
        }
        // void getLeftHandStatus()
        // {
        //     handKeyCode = handKeyCodeL;
        //     handType = VRCPlayerApi.TrackingDataType.LeftHand;
        // }
        // void getRightHandStatus()
        // {
        //     handKeyCode = handKeyCodeR;
        //     handType = VRCPlayerApi.TrackingDataType.RightHand;
        // }
        bool getLeftHandButton(InputButton i)
        {
            if (!fingerInZone[(int)FDMiFingerTrackerType.L]) return false;
            switch (i)
            {
                case InputButton.Grab:
                    return Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger") > inputThreshold;
                case InputButton.Trigger:
                    return Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger") > inputThreshold;
                case InputButton.PadH:
                    return Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal") > inputThreshold;
                case InputButton.PadV:
                    return Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical") > inputThreshold;
                case InputButton.Jump:
                    return Input.GetKey(KeyCode.JoystickButton3);
                case InputButton.Menu:
                    return Input.GetKey(KeyCode.JoystickButton2);
                case InputButton.PadPush:
                    return Input.GetKey(KeyCode.JoystickButton8);
                case InputButton.PadTouch:
                    return Input.GetKey(KeyCode.JoystickButton16);
            }
            return false;
        }
        bool getRightHandButton(InputButton i)
        {
            if (!fingerInZone[(int)FDMiFingerTrackerType.R]) return false;
            switch (i)
            {
                case InputButton.Grab:
                    return Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger") > inputThreshold;
                case InputButton.Trigger:
                    return Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger") > inputThreshold;
                case InputButton.PadH:
                    return Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal") > inputThreshold;
                case InputButton.PadV:
                    return Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical") > inputThreshold;
                case InputButton.Jump:
                    return Input.GetKey(KeyCode.JoystickButton1);
                case InputButton.Menu:
                    return Input.GetKey(KeyCode.JoystickButton0);
                case InputButton.PadPush:
                    return Input.GetKey(KeyCode.JoystickButton9);
                case InputButton.PadTouch:
                    return Input.GetKey(KeyCode.JoystickButton17);
            }
            return false;
        }

        #region Finger Input
        public virtual void OnFingerEnter(FDMiFingerTracker finger)
        {
            finger.targetInput = this;
            fingerInZone[(int)finger.fingerType] = true;
            inputManager.OnFingerEnter(finger.fingerType);
            gameObject.SetActive(true);
        }
        public virtual void OnFingerLeave(FDMiFingerTracker finger)
        {
            if (finger.targetInput == this) finger.targetInput = null;
            fingerInZone[(int)finger.fingerType] = false;
            // holdingHandType = FDMiFingerTrackerType.None;
            inputManager.OnFingerLeave(finger.fingerType);
        }
        #endregion
    }
}