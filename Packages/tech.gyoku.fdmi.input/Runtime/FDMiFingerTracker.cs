
using UdonSharp;
using UnityEngine;
using VRC.SDK3;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum FDMiFingerTrackerType { L, R, None }
    public enum FingerInputType { Grab, Trigger, PadV, PadH, Jump, Menu, PadTouch, PadPush, Length }
    public class FDMiFingerTracker : FDMiBehaviour
    {
        public FDMiFingerTrackerType fingerType;
        [SerializeField] private Rigidbody body;
        public FDMiInputPage targetInput;
        private VRCPlayerApi localPlayer;
        private HumanBodyBones finger = HumanBodyBones.LeftIndexDistal;
        VRCPlayerApi.TrackingData track;
        [System.NonSerializedAttribute] public Vector3 fingerPos, handPos;
        [System.NonSerializedAttribute] public Quaternion fingerAxis, handAxis;
        void Start()
        {
            gameObject.SetActive(false);
            localPlayer = Networking.LocalPlayer;

            if (fingerType == FDMiFingerTrackerType.L)
                finger = HumanBodyBones.LeftIndexDistal;
            if (fingerType == FDMiFingerTrackerType.R)
                finger = HumanBodyBones.RightIndexDistal;

            if (localPlayer.IsUserInVR())
                gameObject.SetActive(true);
        }

        void FixedUpdate()
        {
            fingerPos = localPlayer.GetBonePosition(finger);
            fingerAxis = localPlayer.GetBoneRotation(finger);
            body.position = fingerPos;
            body.rotation = fingerAxis;
        }

        void LateUpdate()
        {
            if (fingerType == FDMiFingerTrackerType.L)
                track = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
            if (fingerType == FDMiFingerTrackerType.R)
                track = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
            handPos = track.position;
            handAxis = track.rotation;

            bool grabState = updateAxisInput();
            if (grabState && !isGrabNow) onGrab();
            if (!grabState && isGrabNow) onRelease();
            if (!isGrabNow && touchingPage != null) touchingPage.whileTouch();
            if (isGrabNow && grabingPage != null) grabingPage.whileGrab();
        }

        #region input page select
        [System.NonSerializedAttribute] public FDMiInputPage touchingPage;
        [System.NonSerializedAttribute] public FDMiInputPage defaultPage;
        [System.NonSerializedAttribute] public FDMiInputPage grabingPage;

        bool isGrabNow = false;
        public void connect(FDMiInputPage connectPage)
        {
            if (touchingPage == connectPage) return;

            if (touchingPage) touchingPage.OnFingerLeave(this);
            connectPage.OnFingerEnter(this);
            touchingPage = connectPage;
            if (fingerType == FDMiFingerTrackerType.L)
                localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.25f, 1, 1);
            if (fingerType == FDMiFingerTrackerType.R)
                localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.25f, 1, 1);
        }
        public void disconnect(FDMiInputPage disconnectPage)
        {
            if (touchingPage != disconnectPage) return;
            disconnectPage.OnFingerLeave(this);
            if (defaultPage) defaultPage.OnFingerEnter(this);
            touchingPage = defaultPage;
        }
        public void setDefaultPage(FDMiInputPage connectPage)
        {
            if (touchingPage != null) touchingPage.OnFingerLeave(this);
            defaultPage = connectPage;
            touchingPage = connectPage;
        }

        public void onGrab()
        {
            grabingPage = touchingPage;
            isGrabNow = true;
            if (grabingPage) grabingPage.OnGrab(this);
        }
        public void onRelease()
        {
            if (grabingPage) grabingPage.OnRelease(this);
            grabingPage = null;
            isGrabNow = false;
        }
        #endregion

        #region axis input
        [HideInInspector] public float[] axisInput = new float[(int)FingerInputType.Length];
        public float grabThreshold = 0.7f;
        public bool updateAxisInput()
        {
            if (fingerType == FDMiFingerTrackerType.L)
                getLeftHandAxis();
            if (fingerType == FDMiFingerTrackerType.R)
                getRightHandAxis();
            return (axisInput[(int)FingerInputType.Grab] > grabThreshold);
        }

        public void getLeftHandAxis()
        {
            axisInput[(int)FingerInputType.Grab] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
            axisInput[(int)FingerInputType.Trigger] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            axisInput[(int)FingerInputType.PadV] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
            axisInput[(int)FingerInputType.PadH] = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");

            axisInput[(int)FingerInputType.Jump] = Input.GetKey(KeyCode.JoystickButton3) ? 1.0f : 0.0f;
            axisInput[(int)FingerInputType.Menu] = Input.GetKey(KeyCode.JoystickButton2) ? 1.0f : 0.0f;
            axisInput[(int)FingerInputType.PadPush] = Input.GetKey(KeyCode.JoystickButton8) ? 1.0f : 0.0f;
            axisInput[(int)FingerInputType.PadTouch] = Input.GetKey(KeyCode.JoystickButton16) ? 1.0f : 0.0f;
        }
        public void getRightHandAxis()
        {
            axisInput[(int)FingerInputType.Grab] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
            axisInput[(int)FingerInputType.Trigger] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            axisInput[(int)FingerInputType.PadV] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
            axisInput[(int)FingerInputType.PadH] = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");

            axisInput[(int)FingerInputType.Jump] = Input.GetKey(KeyCode.JoystickButton1) ? 1.0f : 0.0f;
            axisInput[(int)FingerInputType.Menu] = Input.GetKey(KeyCode.JoystickButton0) ? 1.0f : 0.0f;
            axisInput[(int)FingerInputType.PadPush] = Input.GetKey(KeyCode.JoystickButton9) ? 1.0f : 0.0f;
            axisInput[(int)FingerInputType.PadTouch] = Input.GetKey(KeyCode.JoystickButton17) ? 1.0f : 0.0f;
        }
        #endregion
    }
}