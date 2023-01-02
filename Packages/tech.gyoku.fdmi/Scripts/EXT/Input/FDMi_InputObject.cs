
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
namespace SaccFlight_FDMi
{
    public class FDMi_InputObject : FDMi_SyncObject
    {
        public FDMi_InputManager im;
        public Vector3[] colliderPos;
        public float colliderRadius;
        private Vector3[] handPosition = { Vector3.zero, Vector3.zero };
        [System.NonSerializedAttribute] public float initialValue;
        [System.NonSerializedAttribute] public Vector3 handPos, handStartPos;
        [System.NonSerializedAttribute] public Quaternion handAxis, handStartAxis;
        [System.NonSerializedAttribute] public int handType = -1;
        [System.NonSerializedAttribute] public float[] Trig = { 0f, 0f }, StickX = { 0f, 0f }, StickY = { 0f, 0f };
        [System.NonSerializedAttribute] public VRCPlayerApi player;
        public FDMi_Avionics[] indicators;

        public virtual void Start()
        {
            player = Networking.LocalPlayer;
            Trig = im.Trig;
            StickX = im.StickX;
            StickY = im.StickY;
            handPosition = im.handPosition;
            initialValue = val;
        }
        public virtual void InputUpdate()
        {
            if (handType == -1) return;
            handPos = transform.InverseTransformPoint(handPosition[handType]);
            handAxis = Quaternion.Inverse(transform.rotation);
            if (handType == 0) handAxis *= player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
            if (handType == 1) handAxis *= player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
            foreach (FDMi_Avionics ind in indicators) ind.whenChange();
        }

        public void whenGrabR()
        {
            Networking.SetOwner(player, this.gameObject);
            handType = 0;
            handStartPos = transform.InverseTransformPoint(handPosition[handType]);
            handStartAxis = Quaternion.Inverse(transform.rotation) * player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
            whenGrab();
        }
        public void whenGrabL()
        {
            Networking.SetOwner(player, this.gameObject);
            handType = 1;
            handStartPos = transform.InverseTransformPoint(handPosition[handType]);
            handStartAxis = Quaternion.Inverse(transform.rotation) * player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
            whenGrab();
        }
        public virtual void whenRelease()
        {
            handType = -1;
            foreach (FDMi_Avionics ind in indicators) ind.whenChange();
        }
        public virtual void whenGrab() { }
        public virtual void whenPressUp() { }
        public virtual void whenPressDown() { }
        public virtual void whenReleaseKey() { }
        public virtual void ResetStatus()
        {
            Val = initialValue;
            foreach (FDMi_Avionics ind in indicators) ind.whenChange();
        }
        public virtual void SFEXT_O_OnPlayerJoined() => RequestSerialization();

        public override void whenUpdate()
        {
            foreach (FDMi_Avionics ind in indicators) ind.whenChange();
        }
    }
}
