
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum AxisBehaviourType { momentum, alternate, force, addition }

    public class FDMiAxisInput : FDMiInputAddon
    {
        public FDMiFloat Output;
        [SerializeField] InputButton inputAxisType;
        [SerializeField] AxisBehaviourType behaviourType;
        [SerializeField] float multiply = 1f, min = 0f, max = 1f;
        [SerializeField] float threshold = 0.5f;
        bool alternateLatch = false;

        protected override void Update()
        {
            base.Update();
            if (behaviourType == AxisBehaviourType.momentum)
                Output.set(Mathf.Clamp(multiply * input[(int)inputAxisType], min, max));
            if (behaviourType == AxisBehaviourType.force)
            {
                if (input[(int)inputAxisType] * multiply > threshold)
                    Output.set(max);
            }
            if (behaviourType == AxisBehaviourType.alternate)
            {
                if (input[(int)inputAxisType] * multiply > threshold)
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
            if (behaviourType == AxisBehaviourType.addition)
            {
                Output.set(Mathf.Clamp(Output.Data + input[(int)inputAxisType] * multiply * Time.deltaTime, min, max));
            }
        }

        public override void OnReleased()
        {
            base.OnReleased();
            if (behaviourType == AxisBehaviourType.momentum)
                Output.set(min);
        }
    }
}