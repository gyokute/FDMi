
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace SaccFlight_FDMi
{
    public enum TriggerSWType { lever, toggle }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMi_TriggerSW : FDMi_InputObject
    {
        [SerializeField] private LeverAxis movingAxis;
        [SerializeField] private TriggerSWType type;
        [SerializeField] private float inputMul = 1f, min = 0f, max = 1f;
        private float rawInput, prevParam;
        private bool latch = false;

        public override void InputUpdate()
        {
            base.InputUpdate();
            rawInput = handPos[(int)movingAxis] - handStartPos[(int)movingAxis];
            if (type == TriggerSWType.lever) Val = Mathf.Clamp(rawInput * inputMul + prevParam, min, max);
            RequestSerialization();
            callbackIndicator();
        }
        public override void whenGrab()
        {
            prevParam = val;
            if (type == TriggerSWType.toggle) Val = 1f - val;
            callbackIndicator();
        }

        public override void whenPressUp()
        {
            if (!latch) Val = 1f - val;
            latch = true;
            RequestSerialization();
            callbackIndicator();
        }
        public override void whenReleaseKey()
        {
            latch = false;
            RequestSerialization();
            callbackIndicator();
        }
        public void onSWON() { Val = 1f; RequestSerialization(); callbackIndicator(); }
        public void onSWOFF() { Val = 0f; RequestSerialization(); callbackIndicator(); }

        private void callbackIndicator()
        {
            foreach (FDMi_Avionics ind in indicators) ind.whenChange();
        }
    }
}
