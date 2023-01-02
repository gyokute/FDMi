
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public enum ATS_Mode { IAS, N1 };
    public class FDMi_AutoThrottle : FDMi_ControlFilter
    {
        public FDMi_InputObject SW, Speed, Throttle;
        public ATS_Mode mode = ATS_Mode.IAS;
        // ki = 0.5*Ti[s]/kp
        public float kp = 0.01f, ki, a = 1f, min = 0f, max = 1f;
        private float pOut, output, IAS, err, pErr, ppErr, omega, pSW;

        public override void SFEXT_O_PilotEnter() => Networking.SetOwner(player, this.gameObject);
        public override float filter(float input)
        {
            output = input;
            if (!sharedBool[(int)SharedBool.isPilot]) return output;
            if (SW.val > 0.8f)
            {
                if (pSW != SW.val) Networking.SetOwner(player, Throttle.gameObject);
                // IAS LPF
                omega = 2 * Mathf.PI * a;
                omega = omega / (omega + 1);
                IAS = airData[(int)AirData.IAS] * omega + (1f - omega) * IAS;
                // Turn OFF when pilot move throttle or copilot grab throttle
                if (!Mathf.Approximately(input, pOut) || !Networking.IsOwner(Throttle.gameObject))
                {
                    Networking.SetOwner(player, SW.gameObject);
                    SW.Val = 0f;
                    SW.RequestSerialization();
                }
                if (mode == ATS_Mode.IAS)
                {
                    // err = Mathf.Clamp((Speed.val * 0.514444f - IAS), -2f, 2f);
                    float tgtAcc = Mathf.Clamp((Speed.val * 0.514444f - IAS) * 0.1f, -1f, 1f);
                    err = Mathf.Clamp(tgtAcc - airData[(int)AirData.IASAcc], -1f, 1f);
                    output = kp * (err - pErr) + ki * err;
                    // Debug.Log("ATS Acc:" + airData[(int)AirData.IASAcc] + ",err:" + err + ",out:" + output);
                    output = Mathf.Clamp(pOut + output * Time.deltaTime, min, max);
                    pErr = err;
                }
                Throttle.Val = output;
                Throttle.RequestSerialization();
            }
            pSW = SW.val;
            pOut = output;
            return output;
        }
    }
}