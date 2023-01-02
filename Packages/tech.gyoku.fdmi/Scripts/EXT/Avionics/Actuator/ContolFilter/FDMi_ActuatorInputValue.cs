
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_ActuatorInputValue : FDMi_ControlFilter
    {
        public FDMi_InputObject inputObject;

        public override float filter(float input)
        {
            return input + inputObject.val;
        }
    }
}