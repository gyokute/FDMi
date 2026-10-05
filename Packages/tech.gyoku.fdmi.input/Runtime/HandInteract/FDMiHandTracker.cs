using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.input
{
    public enum FDMiHandAxisType
    {
        Grab,
        Trigger,
        PadV,
        PadH,
        PadPush,
        Length,
    }

    public class FDMiHandTracker : FDMiBehaviour
    {
        public VRCPlayerApi.TrackingDataType trackingHand = VRCPlayerApi.TrackingDataType.LeftHand;
        public string contactTag = "FingerIndexL";

        [Tooltip("Grip value required to start a grab.")]
        public float grabThreshold = 0.7f;

        public string grabAxis = "Oculus_CrossPlatform_PrimaryHandTrigger";
        public string triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
        public string padVAxis = "Oculus_CrossPlatform_PrimaryThumbstickVertical";
        public string padHAxis = "Oculus_CrossPlatform_PrimaryThumbstickHorizontal";
        public string padPushAxis = "Oculus_CrossPlatform_PrimaryThumbstick";

        [HideInInspector]
        public Vector3 handPos;

        [HideInInspector]
        public Quaternion handRot;

        [HideInInspector]
        public float[] axes = new float[(int)FDMiHandAxisType.Length];

        private VRCPlayerApi localPlayer;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (!localPlayer.IsUserInVR())
                gameObject.SetActive(false);
        }

        private bool isGrab;
        private bool selectActive;
        private bool waitingForRelease;

        private void Update()
        {
            // get hand position and rotation
            VRCPlayerApi.TrackingData track = Networking.LocalPlayer.GetTrackingData(trackingHand);
            handPos = track.position;
            handRot = track.rotation;
            // get axis inputs
            axes[(int)FDMiHandAxisType.Grab] = Input.GetAxis(grabAxis);
            axes[(int)FDMiHandAxisType.Trigger] = Input.GetAxis(triggerAxis);
            axes[(int)FDMiHandAxisType.PadV] = Input.GetAxis(padVAxis);
            axes[(int)FDMiHandAxisType.PadH] = Input.GetAxis(padHAxis);
            axes[(int)FDMiHandAxisType.PadPush] = Input.GetAxis(padPushAxis);
            bool gripPressed = axes[(int)FDMiHandAxisType.Grab] > grabThreshold;
            if (gripPressed && !isGrab)
            {
                isGrab = true;
                if (contactedGroup && !waitingForRelease)
                {
                    EndSelect();
                    grabingGroup = contactedGroup;
                    grabingGroup.OnGrabStart(this);
                }
            }
            else if (!gripPressed && isGrab)
            {
                isGrab = false;
                if (grabingGroup)
                {
                    FDMiHandInputGroup group = grabingGroup;
                    grabingGroup = null;
                    group.OnGrabEnd(this);
                }
                waitingForRelease = false;
                StartSelect();
            }
        }

        private void LateUpdate()
        {
            if (grabingGroup)
                grabingGroup.OnGrab(this);
            else if (selectActive && contactedGroup)
                contactedGroup.OnSelect(this);
        }

        private void StartSelect()
        {
            if (!selectActive && !grabingGroup && !waitingForRelease && contactedGroup)
            {
                selectActive = true;
                contactedGroup.OnSelectStart(this);
            }
        }

        private void EndSelect()
        {
            if (selectActive)
            {
                selectActive = false;
                contactedGroup.OnSelectEnd();
            }
        }

        public void YieldGrab(FDMiHandInputGroup group)
        {
            if (grabingGroup != group)
                return;
            grabingGroup = null;
            waitingForRelease = true;
        }

        #region Input Selection
        public FDMiHandInputGroup contactedGroup;
        public FDMiHandInputGroup defaultInputGroup;
        public FDMiHandInputGroup grabingGroup;

        public void SetContactInput(string contactTag, FDMiHandInputGroup group)
        {
            if (contactTag != this.contactTag || contactedGroup == group)
                return;
            EndSelect();
            contactedGroup = group;
            StartSelect();

            if (contactTag == "FingerIndexL")
                localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.25f, 1, 1);
            if (contactTag == "FingerIndexR")
                localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.25f, 1, 1);
        }

        public void UnsetContactInput(FDMiHandInputGroup group)
        {
            if (contactedGroup != group)
                return;

            EndSelect();
            contactedGroup = defaultInputGroup;
            StartSelect();
        }

        public void SetDefaultInput(string contactTag, FDMiHandInputGroup group)
        {
            if (contactTag != this.contactTag)
                return;
            defaultInputGroup = group;
            if (!contactedGroup)
            {
                contactedGroup = defaultInputGroup;
                StartSelect();
            }
        }

        public void UnsetDefaultInput(FDMiHandInputGroup group)
        {
            if (grabingGroup == group)
            {
                grabingGroup = null;
                group.OnGrabEnd(this);
            }
            if (contactedGroup == group)
            {
                EndSelect();
                contactedGroup = null;
            }
            if (defaultInputGroup == group)
                defaultInputGroup = null;
            StartSelect();
        }
        #endregion
    }
}
