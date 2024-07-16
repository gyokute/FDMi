
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiDefaultInput : FDMiInputPage
    {
        public FDMiBool IsPilot;
        public FDMiFingerTrackerType fingerTrackerType;

        void Start()
        {
            IsPilot.subscribe(this, "OnChangeInZone");
        }
        public void OnChangeInZone()
        {
            foreach (var fingerTracker in inputManager.fingerTrackers)
            {
                if (fingerTracker.fingerType == fingerTrackerType)
                {
                    fingerTracker.setDefaultPage(IsPilot.Data ? this : null);
                    return;
                }
            }
        }

    }
}