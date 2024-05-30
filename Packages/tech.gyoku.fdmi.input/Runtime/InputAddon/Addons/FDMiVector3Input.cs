
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
        VRCPlayerApi.TrackingData handTrack, bodyTrack;

        // public override void OnCalled(VRCPlayerApi.TrackingDataType trackType)
        // {
        //     base.OnCalled(trackType);
        //     handTrack = Networking.LocalPlayer.GetTrackingData(trackType);
        //     bodyTrack = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
        //     initialValue = Output.data[0];
        //     if (useInSeatAdjuster) adjustedOrigin = handTrack.position - bodyTrack.position;
        // }
        // public override void OnReleased()
        // {
        //     base.OnReleased();
        // }
        // Vector3 p;
        // protected void LateUpdate()
        // {
        //     if (handType == VRCPlayerApi.TrackingDataType.Head) return;
        //     if (!Input.GetKey(triggeredKey)) OnReleased();
        //     if (useInSeatAdjuster)
        //     {
        //         handTrack = Networking.LocalPlayer.GetTrackingData(handType);
        //         bodyTrack = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
        //         p = Vector3.ClampMagnitude(handTrack.position - bodyTrack.position - adjustedOrigin, limitMagnitude);
        //     }
        //     else
        //     {
        //         p = Vector3.ClampMagnitude(handPos - handStartPos, limitMagnitude);
        //     }
        //     Output.set(initialValue + multiplier * p);
        // }

    }
}