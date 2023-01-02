
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace SaccFlight_FDMi
{
    public class FDMi_ActuatorInput : FDMi_ControlFilter
    {
        public FDMi_SyncObject Input;
        public float mul;

        public override float filter(float input)
        {
            return Input.val * mul;
        }
    }
}