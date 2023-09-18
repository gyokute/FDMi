
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
        bool isGrab = false;
        public Transform NeutralPosition;
        void Start()
        {
            input.gameObject.SetActive(false);
            initPos = transform.localPosition;
        }
        public override void OnDrop()
        {
            input.OnDropGrab();
            input.gameObject.SetActive(false);
            input.currentHand = VRC_Pickup.PickupHand.None;
            transform.position = NeutralPosition.position;
            transform.rotation = NeutralPosition.rotation;
            isGrab = false;
        }
        public override void OnPickup()
        {
            input.currentHand = pickup.currentHand;
            input.OnStartGrab();
            input.gameObject.SetActive(true);
            isGrab = true;
        }

    }
}