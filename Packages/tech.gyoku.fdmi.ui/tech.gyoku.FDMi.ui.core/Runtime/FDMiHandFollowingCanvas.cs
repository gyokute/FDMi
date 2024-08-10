
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.ui.core
{
    public enum FDMiUIFollowingType { Body, HandL, HandR }
    public class FDMiHandFollowingCanvas : FDMiBehaviour
    {
        public Transform offsetTransform;
        public FDMiUIFollowingType followType;
        public float scale = 1f;
        public Vector3 handLInitialPos, handRInitialPos;
        Vector3 localPosition;
        public Quaternion handLInitialRot, handRInitialRot;
        Quaternion rootLocalRotation = Quaternion.identity;
        public Vector3 bodyInitialPositionMultiplier = new Vector3(0f, 0.9f, 0.3f);

        Vector3 worldReferencePosition;
        Quaternion worldReferenceRotation;

        private VRCPlayerApi localPlayer;

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            localPlayer = Networking.LocalPlayer;
            float playerHeight = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position.y;
            if (localPlayer.IsUserInVR()) followingHandR();
            else SendCustomEventDelayedSeconds(nameof(followingBody), 1f);
        }

        void Update()
        {
            if (!Utilities.IsValid(localPlayer)) return;
            VRCPlayerApi.TrackingData playerOrigin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            if (followType == FDMiUIFollowingType.HandL)
                playerOrigin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
            else if (followType == FDMiUIFollowingType.HandR)
                playerOrigin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
            worldReferencePosition = playerOrigin.position;
            worldReferenceRotation = playerOrigin.rotation;
            transform.position = playerOrigin.position;
            transform.rotation = playerOrigin.rotation * rootLocalRotation;
        }
        public void followingBody()
        {
            followType = FDMiUIFollowingType.Body;
            float playerHeight = 1f;
            if (Utilities.IsValid(localPlayer)) playerHeight = localPlayer.GetAvatarEyeHeightAsMeters();
            rootLocalRotation = Quaternion.identity;
            if (offsetTransform)
            {
                offsetTransform.localPosition = playerHeight * 0.9f * Vector3.up + playerHeight * 0.2f * Vector3.forward;
            }
        }
        public void followingHandL()
        {
            // if (!localPlayer.IsUserInVR()) return;
            followType = FDMiUIFollowingType.HandL;
            float playerHeight = 1f;
            if (Utilities.IsValid(localPlayer)) playerHeight = localPlayer.GetAvatarEyeHeightAsMeters();
            rootLocalRotation = handLInitialRot;
            if (offsetTransform)
            {
                offsetTransform.localPosition = handLInitialPos * playerHeight;
            }
        }
        public void followingHandR()
        {
            // if (!localPlayer.IsUserInVR()) return;
            followType = FDMiUIFollowingType.HandR;
            float playerHeight = 1f;
            if (Utilities.IsValid(localPlayer)) playerHeight = localPlayer.GetAvatarEyeHeightAsMeters();

            rootLocalRotation = handRInitialRot;
            if (offsetTransform)
            {
                offsetTransform.localPosition = handRInitialPos * playerHeight;
            }
        }
    }
}