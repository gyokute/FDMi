using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public enum FDMiAxisBehaviourType
    {
        Momentum,
        Alternate,
        Force,
        Addition,
    }

    public class FDMiHandAxisInput : FDMiHandInputElement
    {
        public string outputPath = "Value";

        [FDMiDataPath(nameof(outputPath))]
        public FDMiData output;
        public FDMiHandAxisType inputAxisType = FDMiHandAxisType.Trigger;
        public FDMiAxisBehaviourType behaviourType;
        public float multiply = 1f;
        public float min;
        public float max = 1f;
        public float initial;
        public float threshold = 0.5f;
        private bool alternateLatch;

        public override void WhileGrab(FDMiHandInputGroup group)
        {
            float input = group.axes[(int)inputAxisType] * multiply;
            if (behaviourType == FDMiAxisBehaviourType.Momentum)
            {
                output.Set(Mathf.Clamp(input, min, max));
            }
            else if (behaviourType == FDMiAxisBehaviourType.Force)
            {
                if (input > threshold)
                    output.Set(max);
            }
            else if (behaviourType == FDMiAxisBehaviourType.Alternate)
            {
                if (input > threshold)
                {
                    if (!alternateLatch)
                    {
                        output.Set(Mathf.Approximately(max, output.GetFloat()) ? min : max);
                        alternateLatch = true;
                    }
                }
                else
                    alternateLatch = false;
            }
            else if (behaviourType == FDMiAxisBehaviourType.Addition)
            {
                output.Set(Mathf.Clamp(output.GetFloat() + input * Time.deltaTime, min, max));
            }
        }

        public override void OnGrabEnd()
        {
            alternateLatch = false;
            if (behaviourType == FDMiAxisBehaviourType.Momentum)
                output.Set(initial);
        }
    }
}
