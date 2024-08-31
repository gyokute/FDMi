
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiDefaultInput : FDMiInputPage
    {
        public FDMiFingerTrackerType fingerTrackerType;

        public void FDMiOnSeatEnter()
        {
            foreach (var fingerTracker in inputManager.fingerTrackers)
            {
                if (fingerTracker.fingerType == fingerTrackerType)
                {
                    fingerTracker.setDefaultPage(this);
                    return;
                }
            }
        }
        public void FDMiOnSeatExit()
        {
            foreach (var fingerTracker in inputManager.fingerTrackers)
            {
                if (fingerTracker.fingerType == fingerTrackerType)
                {
                    fingerTracker.setDefaultPage(null);
                    return;
                }
            }
        }

    }
}