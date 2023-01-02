
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_ActuatorRegister : FDMi_ControlFilter
    {
        public float mul = 1f, min, max, delay = 1f, init = 0f;
        private float deg, targetDeg;

        public override float filter(float input)
        {
            targetDeg = Mathf.Clamp(deg + input * mul, min, max);
            deg = Mathf.MoveTowards(deg, targetDeg, Time.deltaTime * delay);
            return deg;
        }
    }
}