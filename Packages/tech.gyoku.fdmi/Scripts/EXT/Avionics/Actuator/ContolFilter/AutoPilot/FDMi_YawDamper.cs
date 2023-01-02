
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public class FDMi_YawDamper : FDMi_ControlFilter
    {
        public float cutOff = 1f, val;
        public float kMin, kMax, kOffset, minIAS, kIAS;
        public float maxMove = 0.25f;
        private float k, i0, i1, o0, o1, a;

        public override float filter(float input)
        {
            if (!sharedBool[0]) return input;
            if (!sharedBool[(int)SharedBool.isPilot]) return input;
            k = Mathf.Clamp(kIAS * airData[(int)AirData.IAS] + kOffset, kMin, kMax);
            k *= Mathf.Clamp01(airData[(int)AirData.IAS] - minIAS);

            i0 = airData[(int)AirData.yawRate];
            a = 2 * Mathf.PI * Time.deltaTime * cutOff + 1f;
            o0 = (o1 + i0 - i1) / a;
            i1 = i0;
            o1 = o0;
            return input + Mathf.Clamp(k * o0, -maxMove, maxMove);
        }
    }
}