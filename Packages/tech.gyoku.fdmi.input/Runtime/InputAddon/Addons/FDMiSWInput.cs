
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{

    public class FDMiSWInput : FDMiInputAddon
    {
        public FDMiBool Output;
        [SerializeField] AxisBehaviourType behaviourType;
        [SerializeField] bool reg = true;

        public override void OnGrab(FDMiFingerTracker finger)
        {
            base.OnGrab(finger);
            if (behaviourType == AxisBehaviourType.alternate)
            {
                Output.set(!Output.Data);
            }
            if (behaviourType == AxisBehaviourType.momentum || behaviourType == AxisBehaviourType.force)
            {
                Output.set(reg);
            }
        }
        public override void OnRelease(FDMiFingerTracker finger)
        {
            base.OnRelease(finger);
            if (behaviourType == AxisBehaviourType.momentum)
                Output.set(!reg);
        }
    }
}