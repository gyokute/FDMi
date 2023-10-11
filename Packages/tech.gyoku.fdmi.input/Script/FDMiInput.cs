
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum InputButton { Grab, Trigger, Jump, PadTouch, Length }
    public enum InputAxis { Grab, Trigger, PadV, PadH, Length }
    public class FDMiInput : UdonSharpBehaviour
    {
        public FDMiInputManager inputManager;
        public FDMiInputAddon[] InputAddons = new FDMiInputAddon[(int)InputButton.Length];
        VRCPlayerApi.TrackingData track;
        [System.NonSerializedAttribute] public FDMiFingerTrackerType holdingHandType;
        [System.NonSerializedAttribute] public float[] inputAxis = new float[(int)InputAxis.Length];
        VRCPlayerApi.TrackingDataType handType;
        int[] handKeyCode = { (int)KeyCode.None, (int)KeyCode.None, (int)KeyCode.None, (int)KeyCode.None };
        int[] handKeyCodeL = { (int)KeyCode.JoystickButton4, (int)KeyCode.JoystickButton14, (int)KeyCode.JoystickButton3, (int)KeyCode.JoystickButton16 };
        int[] handKeyCodeR = { (int)KeyCode.JoystickButton5, (int)KeyCode.JoystickButton15, (int)KeyCode.JoystickButton1, (int)KeyCode.JoystickButton17 };
        protected bool grabNow = false, fingerInZone = false;


        public virtual void OnDisable()
        {
            grabNow = false;
            fingerInZone = false;
            holdingHandType = FDMiFingerTrackerType.None;
        }
        void LateUpdate()
        {
            if (!grabNow && !fingerInZone) gameObject.SetActive(false);

            if (holdingHandType == FDMiFingerTrackerType.L) getLeftHandStatus();
            if (holdingHandType == FDMiFingerTrackerType.R) getRightHandStatus();

            for (int i = 0; i < (int)InputButton.Length; i++)
            {
                if (!InputAddons[i]) continue;
                if (Input.GetKeyDown((KeyCode)handKeyCode[i])) InputAddons[i].OnCalled((KeyCode)handKeyCode[i], handType);
            }
            grabNow = Input.GetKey((KeyCode)handKeyCode[(int)InputButton.Grab]) || Input.GetKey((KeyCode)handKeyCode[(int)InputButton.Trigger]);
        }
        void getLeftHandStatus()
        {
            inputAxis[(int)InputAxis.Grab] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
            inputAxis[(int)InputAxis.Trigger] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            inputAxis[(int)InputAxis.PadH] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
            inputAxis[(int)InputAxis.PadV] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
            handKeyCode = handKeyCodeL;
            handType = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        void getRightHandStatus()
        {
            inputAxis[(int)InputAxis.Grab] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
            inputAxis[(int)InputAxis.Trigger] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            inputAxis[(int)InputAxis.PadH] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
            inputAxis[(int)InputAxis.PadV] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
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