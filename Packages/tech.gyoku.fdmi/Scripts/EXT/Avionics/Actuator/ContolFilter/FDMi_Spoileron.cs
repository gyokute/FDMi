
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_Spoileron : FDMi_ControlFilter
    {
        public FDMi_Lever Spoiler, Flap;
        public float rollMul;
        private float val;
        private int i;

        public override float filter(float input)
        {
            val = Mathf.Clamp(rollMul * input, 0, rollMul);
            val *= Mathf.Clamp01(1f - Flap.val);
            return val + Spoiler.val;
        }
    }
}