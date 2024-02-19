
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{

    public class FDMiSWInput : FDMiInputAddon
    {
        [SerializeField] FDMiBool Output;
        [SerializeField] AxisBehaviourType behaviourType;
        [SerializeField] bool reg = true;

        public override void OnCalled(VRCPlayerApi.TrackingDataType trackType)
        {
            base.OnCalled(trackType);
            if (behaviourType == AxisBehaviourType.alternate)
            {
                Output.set(!Output.Data);
            }
            if (behaviourType == AxisBehaviourType.momentum || behaviourType == AxisBehaviourType.force)
            {
                Output.set(reg);
            }
        }
        public override void OnReleased()
        {
            base.OnReleased();
            if (behaviourType == AxisBehaviourType.momentum)
                Output.set(!reg);
        }
    }
}