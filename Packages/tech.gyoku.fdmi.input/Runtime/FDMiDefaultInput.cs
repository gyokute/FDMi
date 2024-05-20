
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.input
{
    public class FDMiDefaultInput : FDMiInputPage
    {
        public FDMiFingerTrackerType fingerTrackerType;
        void Start()
        {
            fingerInZone[0] = true;
            fingerInZone[1] = true;
            // holdingHandType = fingerTrackerType;
        }
        // void Update()
        // {
        //     if (!inputManager) return;
        //     if (inputManager.defaultGrabObject[(int)fingerTrackerType] == this) return;
        //     inputManager.defaultGrabObject[(int)fingerTrackerType] = this;
        //     inputManager.fingerTrackers[(int)fingerTrackerType].targetInput = this;
        // }
        public void FDMiOnSeatEnter()
        {
            if (!inputManager) return;
            inputManager.defaultGrabObject[(int)fingerTrackerType] = this;
            inputManager.fingerTrackers[(int)fingerTrackerType].targetInput = this;
            OnFingerEnter(inputManager.fingerTrackers[(int)fingerTrackerType]);
        }

        public void FDMiOnSeatExit()
        {
            // base.OnDisable();
            fingerInZone[0] = false;
            fingerInZone[1] = false;
            if (inputManager.defaultGrabObject[(int)fingerTrackerType] == this)
                inputManager.defaultGrabObject[(int)fingerTrackerType] = null;
            if (inputManager.fingerTrackers[(int)fingerTrackerType].targetInput == this)
                inputManager.fingerTrackers[(int)fingerTrackerType].targetInput = null;
        }
        #region Finger Input
        public override void OnFingerEnter(FDMiFingerTracker finger)
        {
            fingerInZone[(int)finger.fingerType] = true;
            finger.targetInput = this;
            gameObject.SetActive(true);
        }
        public override void OnFingerLeave(FDMiFingerTracker finger)
        {
            fingerInZone[(int)finger.fingerType] = true;
            gameObject.SetActive(false);
        }
        #endregion
    }
}