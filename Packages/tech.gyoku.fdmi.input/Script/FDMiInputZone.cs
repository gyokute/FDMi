
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiInputZone : UdonSharpBehaviour
    {
        public FDMiInputPage input;
        private FDMiFingerTracker fingerTracker;
        [SerializeField]private GameObject highlightObject;

        void OnDisable(){
            if (highlightObject) InputManager.EnableObjectHighlight(highlightObject, false);
        }
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
            if (highlightObject) InputManager.EnableObjectHighlight(highlightObject, true);
        }

        private void disconnectFromFinger(Collider other)
        {
            input.OnFingerLeave(fingerTracker);
            if (highlightObject) InputManager.EnableObjectHighlight(highlightObject, false);
        }

    }
}