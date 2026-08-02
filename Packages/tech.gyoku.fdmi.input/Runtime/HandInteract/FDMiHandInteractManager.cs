using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    // csharpier-ignore
    public enum FDMiHandType { L, R, None }

    // csharpier-ignore
    // public enum FDMiHandInputType { Grab, Trigger, PadV, PadH, Jump, Menu, PadTouch, PadPush, Length }
    public enum FDMiHandInputType { A0, A1, A2, A3, A4, A5, B0, B1, B2, Length }

    public class FDMiHandInteractManager : FDMiBehaviour
    {
        public FDMiHandType fingerType;

        #region Input Selection Stack
        private FDMiHandInput[] handInputStack = new FDMiHandInput[32];
        private int handInputStackCount = -1;
        private VRCPlayerApi localPlayer;
        #endregion

        #region axis input
        [HideInInspector]
        public float[] axisInput = new float[(int)FDMiHandInputType.Length];
        public float grabThreshold = 0.7f;

        public bool updateAxisInput()
        {
            if (fingerType == FDMiHandType.L)
                getLeftHandAxis();
            if (fingerType == FDMiHandType.R)
                getRightHandAxis();
            return (axisInput[(int)FDMiHandInputType.A0] > grabThreshold);
        }

        public void getLeftHandAxis()
        {
            axisInput[(int)FDMiHandInputType.A1] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
            axisInput[(int)FDMiHandInputType.A2] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            axisInput[(int)FDMiHandInputType.A3] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
            axisInput[(int)FDMiHandInputType.A4] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
            axisInput[(int)FDMiHandInputType.A5] = Input.GetAxisRaw("Oculus_CrossPlatform_Button4");
            axisInput[(int)FDMiHandInputType.B0] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstick");
            axisInput[(int)FDMiHandInputType.B1] = Input.GetKey(KeyCode.JoystickButton3) ? 1.0f : 0.0f;
            axisInput[(int)FDMiHandInputType.B2] = Input.GetKey(KeyCode.JoystickButton16) ? 1.0f : 0.0f;
        }

        public void getRightHandAxis()
        {
            axisInput[(int)FDMiHandInputType.A1] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
            axisInput[(int)FDMiHandInputType.A2] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            axisInput[(int)FDMiHandInputType.A3] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
            axisInput[(int)FDMiHandInputType.A4] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
            axisInput[(int)FDMiHandInputType.A5] = Input.GetAxisRaw("Oculus_CrossPlatform_Button2");
            axisInput[(int)FDMiHandInputType.B0] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstick");
            axisInput[(int)FDMiHandInputType.B1] = Input.GetKey(KeyCode.JoystickButton1) ? 1.0f : 0.0f;
            axisInput[(int)FDMiHandInputType.B2] = Input.GetKey(KeyCode.JoystickButton17) ? 1.0f : 0.0f;
        }
        #endregion
    }
}
