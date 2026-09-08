using FDMi.core;

namespace FDMi.input
{
    public class FDMiHandInputGroup : FDMiBehaviour
    {
        [UnityEngine.HideInInspector] public FDMiHandInput grabHand;

        public FDMiHandInput leftHandInput;
        public FDMiHandInput rightHandInput;

        public bool defaultForLeftHand;
        public bool defaultForRightHand;

        private bool groupActive;

        private void OnEnable()
        {
            groupActive = true;

            if (defaultForLeftHand && leftHandInput)
                leftHandInput.SetDefault(this);

            if (defaultForRightHand && rightHandInput && rightHandInput != leftHandInput)
                rightHandInput.SetDefault(this);
        }

        private void OnDisable()
        {
            groupActive = false;

            if (leftHandInput)
                leftHandInput.DisableGroup(this);

            if (rightHandInput && rightHandInput != leftHandInput)
                rightHandInput.DisableGroup(this);
        }

        public bool IsActive()
        {
            return groupActive;
        }

        public virtual void OnEnterSelect(FDMiHandInput hand) { }

        public virtual void OnSelect(FDMiHandInput hand) { }

        public virtual void OnLeaveSelect(FDMiHandInput hand) { }

        public virtual void OnGrabStart(FDMiHandInput hand) { }

        public virtual void OnGrab(FDMiHandInput hand) { }

        public virtual void OnGrabEnd(FDMiHandInput hand) { }
    }
}
