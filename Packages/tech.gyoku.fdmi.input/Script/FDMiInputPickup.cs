
using UdonSharp;
using UnityEngine;
using VRC.SDK3;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInputPickup : UdonSharpBehaviour
    {
        public VRC.SDK3.Components.VRCPickup pickup;
        public FDMiInput input;
        public Vector3 initPos;
        public Quaternion initRot;
        void Start()
        {
            input.gameObject.SetActive(false);
            initPos = transform.localPosition;
            initRot = transform.localRotation;
        }
        public override void OnDrop()
        {
            input.gameObject.SetActive(false);
            input.OnDropGrab();
            input.handType = (int)VRC_Pickup.PickupHand.None;
            transform.localPosition = initPos;
            transform.localRotation = initRot;
            input.isGrab = false;
        }
        public override void OnPickup()
        {
            input.handType = (int)pickup.currentHand;
            input.OnStartGrab();
            input.gameObject.SetActive(true);
            input.isGrab = true;
        }

    }
}