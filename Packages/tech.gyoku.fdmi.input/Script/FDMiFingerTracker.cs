
using UdonSharp;
using UnityEngine;
using VRC.SDK3;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum FDMiFingerTrackerType { L, R, None }
    public class FDMiFingerTracker : UdonSharpBehaviour
    {
        public FDMiFingerTrackerType fingerType;
        [SerializeField] private Rigidbody body;
        public FDMiInput targetInput;
        private VRCPlayerApi localPlayer;
        private HumanBodyBones finger = HumanBodyBones.LeftIndexDistal;
        [System.NonSerializedAttribute] public Vector3 fingerPos;
        [System.NonSerializedAttribute] public Quaternion fingerAxis;
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
    }
}