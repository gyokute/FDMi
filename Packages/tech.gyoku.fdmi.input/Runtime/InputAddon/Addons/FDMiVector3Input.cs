
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
        public FingerInputType inputAxisType = FingerInputType.Trigger;
        [SerializeField] float axisThreshold = 0.5f;
        [SerializeField] float multiplier, limitMagnitude = 0.2f;
        Vector3 initialValue, adjustedOrigin;
        [SerializeField] bool useInSeatAdjuster;
        VRCPlayerApi.TrackingData bodyTrack;

        public override void OnGrab(FDMiFingerTracker finger)
        {
            base.OnGrab(finger);
            initialValue = Output.data[0];
            bodyTrack = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            if (useInSeatAdjuster) adjustedOrigin = handPos - transform.InverseTransformPoint(bodyTrack.position);
        }
        Vector3 p;
        public override void whileGrab()
        {
            base.whileGrab();
            if (grabAxis[(int)inputAxisType] < axisThreshold) return;
            if (useInSeatAdjuster)
            {
                bodyTrack = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
                p = Vector3.ClampMagnitude(handPos - transform.InverseTransformPoint(bodyTrack.position) - adjustedOrigin, limitMagnitude);
            }
            else
            {
                p = Vector3.ClampMagnitude(handPos - handStartPos, limitMagnitude);
            }
            Output.set(initialValue + multiplier * p);
        }
    }
}