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
        public float[] axes = new float[(int)FDMiHandAxisType.Length];

        private VRCPlayerApi localPlayer;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (!localPlayer.IsUserInVR())
                gameObject.SetActive(false);
        }

        private bool isGrab;

        private void LateUpdate()
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
            // axis input events
            // when grabbing
            if (axes[(int)FDMiHandAxisType.Grab] > grabThreshold)
            {
                if (!isGrab && contactedGroup)
                {
                    grabingGroup = contactedGroup;
                    grabingGroup.OnGrabStart(this);
                }
                isGrab = true;
                if (grabingGroup)
                    grabingGroup.OnGrab(this);
            }
            else
            {
                if (isGrab && grabingGroup)
                {
                    grabingGroup.OnGrabEnd();
                    grabingGroup = null;
                }
                isGrab = false;
                if (contactedGroup)
                    contactedGroup.OnSelect(this);
            }
        }

        #region Input Selection
        public FDMiHandInputGroup contactedGroup;
        public FDMiHandInputGroup defaultInputGroup;
        public FDMiHandInputGroup grabingGroup;

        public void SetContactInput(string contactTag, FDMiHandInputGroup group)
        {
            if (contactedGroup)
                contactedGroup.OnSelectEnd();
            if (contactTag != this.contactTag)
                return;
            contactedGroup = group;
            group.OnSelectStart(this);

            if (contactTag == "FingerIndexL")
                localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.25f, 1, 1);
            if (contactTag == "FingerIndexR")
                localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.25f, 1, 1);
        }

        public void UnsetContactInput(FDMiHandInputGroup group)
        {
            if (contactedGroup != group)
                return;

            group.OnSelectEnd();
            contactedGroup = defaultInputGroup;

            if (defaultInputGroup != null)
                defaultInputGroup.OnSelectStart(this);
        }

        public void SetDefaultInput(string contactTag, FDMiHandInputGroup group)
        {
            if (contactTag != this.contactTag)
                return;
            defaultInputGroup = group;
            if (!contactedGroup)
            {
                contactedGroup = defaultInputGroup;
                contactedGroup.OnSelectStart(this);
            }
        }

        public void UnsetDefaultInput(FDMiHandInputGroup group)
        {
            if (contactedGroup == group)
            {
                contactedGroup = null;
                contactedGroup.OnSelectEnd();
            }
            if (defaultInputGroup == group)
                defaultInputGroup = null;
        }
        #endregion
    }
}
