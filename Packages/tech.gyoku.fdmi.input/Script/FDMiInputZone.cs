
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInputZone : UdonSharpBehaviour
    {
        public FDMiInput input;
        private FDMiFingerTracker fingerTracker;

        private void OnTriggerEnter(Collider other)
        {
            fingerTracker = other.GetComponent<FDMiFingerTracker>();
            if (fingerTracker)
                connectToFinger(other);
        }
        private void OnTriggerExit(Collider other)
        {
            fingerTracker = other.GetComponent<FDMiFingerTracker>();
            if (fingerTracker)
                disconnectFromFinger(other);
        }

        private void connectToFinger(Collider other)
        {
            input.OnFingerEnter(fingerTracker);
            fingerTracker.targetInput = input;
        }

        private void disconnectFromFinger(Collider other)
        {
            input.OnFingerLeave(fingerTracker);
            if (fingerTracker.targetInput == input)
                fingerTracker.targetInput = null;
        }

    }
}