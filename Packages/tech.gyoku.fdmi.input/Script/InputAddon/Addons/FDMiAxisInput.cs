
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.input
{
    public enum AxisBehaviourType { momentum, alternate, force }

    public class FDMiAxisInput : FDMiInputAddon
    {
        [SerializeField] FDMiFloat Output;
        [SerializeField] InputAxis inputAxisType;
        [SerializeField] AxisBehaviourType behaviourType;
        [SerializeField] float multiply = 1f, min = 0f, max = 1f;
        [SerializeField] float threshold = 0.5f;
        bool alternateLatch = false;

        protected override void Update()
        {
            base.Update();
            if (behaviourType == AxisBehaviourType.momentum)
                Output.set(Mathf.Clamp(multiply * inputAxis[(int)inputAxisType], min, max));
            if (behaviourType == AxisBehaviourType.force)
            {
                if (inputAxis[(int)inputAxisType] * multiply > threshold)
                    Output.set(max);
            }
            if (behaviourType == AxisBehaviourType.alternate)
            {
                if (inputAxis[(int)inputAxisType] * multiply > threshold)
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

        public override void OnReleased()
        {
            base.OnReleased();
            if (behaviourType == AxisBehaviourType.momentum)
                Output.set(min);
        }
    }
}