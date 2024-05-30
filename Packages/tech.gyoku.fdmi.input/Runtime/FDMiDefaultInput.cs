
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.input
{
    public class FDMiDefaultInput : FDMiInputPage
    {
        public FDMiFingerTrackerType fingerTrackerType;

        // void Update()
        // {
        //     if (!inputManager) return;
        //     if (inputManager.defaultGrabObject[(int)fingerTrackerType] == this) return;
        //     inputManager.defaultGrabObject[(int)fingerTrackerType] = this;
        //     inputManager.fingerTrackers[(int)fingerTrackerType].targetInput = this;
        // }

    }
}