
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{

    public class FDMiVector3Input : FDMiInputAddon
    {
        public FDMiVector3 Output;
        [SerializeField] float multiplier, limitMagnitude = 0.2f;
        Vector3 initialValue, adjustedOrigin;
        [SerializeField] bool useInSeatAdjuster;
        [SerializeField] Transform SeatTransform;
        VRCPlayerApi.TrackingData bodyTrack;

        public override void OnGrab(FDMiFingerTracker finger)
        {
            base.OnGrab(finger);
            initialValue = Output.data[0];
            bodyTrack = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            if (useInSeatAdjuster) adjustedOrigin = finger.handPos - bodyTrack.position;
        }
        Vector3 p;
        public override void whileGrab()
        {
            base.whileGrab();
            if (useInSeatAdjuster)
            {
                bodyTrack = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
                p = Vector3.ClampMagnitude(handPos - bodyTrack.position - adjustedOrigin, limitMagnitude);
            }
            else
            {
                p = Vector3.ClampMagnitude(handPos - handStartPos, limitMagnitude);
            }
            Output.set(initialValue + multiplier * p);
        }
    }
}