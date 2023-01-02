
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public enum FDMi_Actuator_LimitMode { IAS, GS }
    public class FDMi_ActuatorDegree : FDMi_ControlFilter
    {
        public float mainInputMul, delay = 0.1f;
        public FDMi_Actuator_LimitMode limitMode;
        [SerializeField] private float[] limit, minDegree, maxDegree;
        private float deg, targetDeg, limitVal;
        private int limitIndex;

        public override float filter(float input)
        {
            if (!sharedBool[0]) return input;

            if (limit.Length == 0) targetDeg = input * mainInputMul;
            else
            {

                // Limit
                switch (limitMode)
                {
                    case FDMi_Actuator_LimitMode.IAS:
                        limitVal = airData[(int)AirData.IAS];
                        break;
                    case FDMi_Actuator_LimitMode.GS:
                        limitVal = airData[(int)AirData.GS];
                        break;
                }

                limitIndex = limit.Length;
                for (int i = 0; i < limit.Length; i++)
                {
                    if (limitVal < limit[i])
                    {
                        limitIndex = i;
                        break;
                    }
                }
                targetDeg = Mathf.Clamp(input * mainInputMul, minDegree[limitIndex], maxDegree[limitIndex]);
            }
            deg = Mathf.MoveTowards(deg, targetDeg, Time.deltaTime * delay);
            return deg;
        }
    }
}