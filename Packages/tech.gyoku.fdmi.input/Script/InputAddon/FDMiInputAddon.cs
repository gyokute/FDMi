
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInputAddon : UdonSharpBehaviour
    {
        public FDMiInput input;
        protected KeyCode triggeredKey = KeyCode.None;
        VRCPlayerApi.TrackingData track;
        VRCPlayerApi.TrackingDataType handType = VRCPlayerApi.TrackingDataType.Head;
        protected Vector3 handPos, handStartPos;
        protected Quaternion handAxis, handStartAxis;

        void Start()
        {
            gameObject.SetActive(false);
        }
        public virtual void Update()
        {
            // If not Init, return
            if(handType == VRCPlayerApi.TrackingDataType.Head) return;
            if (!Input.GetKey(triggeredKey)) OnReleased();
            track = Networking.LocalPlayer.GetTrackingData(handType);
            handPos = transform.InverseTransformPoint(track.position);
            handAxis = Quaternion.Inverse(transform.rotation) * track.rotation;
        }
        public virtual void OnCalled(KeyCode callKey, VRCPlayerApi.TrackingDataType trackType)
        {
            triggeredKey = callKey;
            handType = trackType;
            track = Networking.LocalPlayer.GetTrackingData(trackType);
            handStartPos = transform.InverseTransformPoint(track.position);
            handStartAxis = Quaternion.Inverse(transform.rotation) * track.rotation;
            gameObject.SetActive(true);
        }
        public virtual void OnReleased()
        {
            gameObject.SetActive(false);
        }
        void OnDisable()
        {
            OnReleased();
        }
    }
}
