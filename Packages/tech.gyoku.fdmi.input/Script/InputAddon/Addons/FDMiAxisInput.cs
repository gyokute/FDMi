
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum AxisBehaviourType { momentum, alternate }

    public class FDMiAxisInput : FDMiInputAddon
    {
        [SerializeField] FDMiSyncedFloat Output;
        [SerializeField] InputAxis inputAxisType;
        [SerializeField] AxisBehaviourType behaviourType;
        [SerializeField] float multiply = 1f;
        protected override void Update()
        {
            base.Update();
            if (behaviourType == AxisBehaviourType.momentum)
                Output.set(multiply * inputAxis[(int)inputAxisType]);
        }
        public override void OnCalled(KeyCode callKey, VRCPlayerApi.TrackingDataType trackType)
        {
            base.OnCalled(callKey, trackType);
            if (behaviourType == AxisBehaviourType.alternate)
            {
                Output.set(Mathf.Approximately(multiply, Output.data[0]) ? 0f : multiply);
            }
        }
        public override void OnReleased()
        {
            base.OnReleased();
            if (behaviourType == AxisBehaviourType.momentum)
                Output.set(0f);
        }
    }
}