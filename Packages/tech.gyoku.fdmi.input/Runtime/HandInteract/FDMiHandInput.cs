using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.input
{
    // csharpier-ignore
    public enum FDMiHandType { L, R, None }

    public class FDMiHandInput : FDMiBehaviour
    {
        public FDMiHandType handType;

        [Tooltip("Contact groups are kept in enter order. Set this array large enough for the scene.")]
        public FDMiHandInputGroup[] contactGroups = new FDMiHandInputGroup[8];

        [Tooltip("Grip value required to start a grab.")]
        public float grabThreshold = 0.7f;

        [HideInInspector] public Vector3 handPosition;
        [HideInInspector] public Quaternion handRotation;
        [HideInInspector] public float gripValue;
        [HideInInspector] public int contactCount;

        private FDMiHandInputGroup defaultGroup;
        private FDMiHandInputGroup activeGroup;
        private bool grabMode;
        private bool gripPressed;
        private bool gripInitialized;
        private VRCPlayerApi localPlayer;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            UpdateHandTracking();
            gripValue = ReadGrip();
            InitializeGrip(gripValue > grabThreshold);
        }

        private void Update()
        {
            if (!gripInitialized)
                return;

            UpdateHandTracking();
            gripValue = ReadGrip();
            SetGrip(gripValue > grabThreshold);
        }

        private void LateUpdate()
        {
            if (!activeGroup)
                return;

            UpdateHandTracking();
            if (grabMode)
                activeGroup.OnGrab(this);
            else
                activeGroup.OnSelect(this);
        }

        public void EnterContact(FDMiHandInputGroup group)
        {
            if (!group || !group.IsActive())
                return;

            contactGroups[contactCount] = group;
            contactCount++;
            RefreshSelection();
        }

        public void ExitContact(FDMiHandInputGroup group)
        {
            for (int i = 0; i < contactCount; i++)
            {
                if (contactGroups[i] != group)
                    continue;

                RemoveContactAt(i);
                RefreshSelection();
                return;
            }
        }

        public void SetDefault(FDMiHandInputGroup group)
        {
            defaultGroup = group;
            RefreshSelection();
        }

        public void DisableGroup(FDMiHandInputGroup group)
        {
            if (!group)
                return;

            if (defaultGroup == group)
                defaultGroup = null;

            RemoveContact(group);

            if (activeGroup != group)
                return;

            if (grabMode)
            {
                EndGrabForDisable(group);
                return;
            }

            activeGroup = null;
            group.OnLeaveSelect(this);
            RefreshSelection();
        }

        public void InitializeGrip(bool pressed)
        {
            gripPressed = pressed;
            gripInitialized = true;
        }

        public void SetGrip(bool pressed)
        {
            if (!gripInitialized || gripPressed == pressed)
                return;

            gripPressed = pressed;
            if (pressed)
                BeginGrab();
            else
                EndGrab();
        }

        public void OnGrabDelegated(FDMiHandInputGroup group)
        {
            if (activeGroup == group)
                activeGroup = null;
        }

        private void BeginGrab()
        {
            FDMiHandInputGroup group = activeGroup;
            activeGroup = null;
            if (group)
                group.OnLeaveSelect(this);

            grabMode = true;
            if (!group)
                return;

            FDMiHandInput previousHand = group.grabHand;
            if (previousHand && previousHand != this)
                previousHand.OnGrabDelegated(group);

            activeGroup = group;
            group.grabHand = this;
            UpdateHandTracking();
            group.OnGrabStart(this);
        }

        private void EndGrab()
        {
            FDMiHandInputGroup group = activeGroup;
            if (group)
            {
                activeGroup = null;
                if (group.grabHand == this)
                    group.grabHand = null;
                group.OnGrabEnd(this);
            }

            grabMode = false;
            RefreshSelection();
        }

        private void EndGrabForDisable(FDMiHandInputGroup group)
        {
            activeGroup = null;
            if (group.grabHand == this)
                group.grabHand = null;
            group.OnGrabEnd(this);
            grabMode = false;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (grabMode)
                return;

            FDMiHandInputGroup nextGroup = GetNextSelection();
            if (activeGroup == nextGroup)
                return;

            FDMiHandInputGroup previousGroup = activeGroup;
            if (previousGroup)
                previousGroup.OnLeaveSelect(this);

            activeGroup = nextGroup;
            if (activeGroup)
            {
                UpdateHandTracking();
                activeGroup.OnEnterSelect(this);
            }
        }

        private FDMiHandInputGroup GetNextSelection()
        {
            while (contactCount > 0)
            {
                FDMiHandInputGroup candidate = contactGroups[contactCount - 1];
                if (candidate && candidate.IsActive())
                    return candidate;

                RemoveContactAt(contactCount - 1);
            }

            if (defaultGroup && defaultGroup.IsActive())
                return defaultGroup;

            return null;
        }

        private void RemoveContact(FDMiHandInputGroup group)
        {
            for (int i = 0; i < contactCount; i++)
            {
                if (contactGroups[i] != group)
                    continue;

                RemoveContactAt(i);
                return;
            }
        }

        private void RemoveContactAt(int index)
        {
            for (int i = index; i < contactCount - 1; i++)
                contactGroups[i] = contactGroups[i + 1];

            contactCount--;
            contactGroups[contactCount] = null;
        }

        private float ReadGrip()
        {
            if (handType == FDMiHandType.L)
                return Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
            if (handType == FDMiHandType.R)
                return Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
            return 0f;
        }

        private void UpdateHandTracking()
        {
            if (handType == FDMiHandType.None)
                return;

            if (localPlayer == null)
                localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
                return;

            VRCPlayerApi.TrackingDataType trackingType = handType == FDMiHandType.L
                ? VRCPlayerApi.TrackingDataType.LeftHand
                : VRCPlayerApi.TrackingDataType.RightHand;
            VRCPlayerApi.TrackingData tracking = localPlayer.GetTrackingData(trackingType);
            handPosition = tracking.position;
            handRotation = tracking.rotation;
        }
    }
}
