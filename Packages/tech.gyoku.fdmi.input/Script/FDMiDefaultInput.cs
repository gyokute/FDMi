
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.input
{
    public class FDMiDefaultInput : FDMiInput
    {
        [SerializeField] FDMiFingerTrackerType fingerTrackerType;
        void Start()
        {
            fingerInZone = true;
            holdingHandType = fingerTrackerType;
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
            base.OnDisable();
            fingerInZone = false;
            if (inputManager.defaultGrabObject[(int)fingerTrackerType] == this)
                inputManager.defaultGrabObject[(int)fingerTrackerType] = null;
            if(inputManager.fingerTrackers[(int)fingerTrackerType].targetInput == this)
            inputManager.fingerTrackers[(int)fingerTrackerType].targetInput = null;
        }
        #region Finger Input
        public override void OnFingerEnter(FDMiFingerTracker finger)
        {
            fingerInZone = true;
            finger.targetInput = this;
            gameObject.SetActive(true);
        }
        public override void OnFingerLeave(FDMiFingerTracker finger)
        {
            fingerInZone = false;
            gameObject.SetActive(false);
        }
        #endregion
    }
}