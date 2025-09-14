
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public class FDMiAxisInputSByte : FDMiInputAddon
    {
        public FDMiSByte Output;
        [SerializeField] FingerInputType inputAxisType;
        [SerializeField] AxisBehaviourType behaviourType;
        [SerializeField] float multiply = 1f;
        [SerializeField] sbyte min = 0, max = 127, initial = 0;
        [SerializeField] float threshold = 0.5f;
        bool alternateLatch = false;

        public override void whileGrab()
        {
            base.whileGrab();
            if (grabAxis == null) return;
            if (behaviourType == AxisBehaviourType.momentum)
                Output.Data = (sbyte)(Mathf.Clamp((multiply * grabAxis[(int)inputAxisType]), min, max));
            if (behaviourType == AxisBehaviourType.force)
            {
                if (grabAxis[(int)inputAxisType] * multiply > threshold)
                    Output.set(max);
            }
            if (behaviourType == AxisBehaviourType.alternate)
            {
                if (grabAxis[(int)inputAxisType] * multiply > threshold)
                {
                    if (!alternateLatch)
                    {
                        Output.set(Mathf.Approximately(max, Output.data[0]) ? min : max);
                        alternateLatch = true;
                    }
                }
                else
                {
                    alternateLatch = false;
                }
            }
        }

        public override void OnRelease(FDMiFingerTracker finger)
        {
            base.OnRelease(finger);
            alternateLatch = false;
            if (behaviourType == AxisBehaviourType.momentum)
                Output.set(initial);
        }
    }
}