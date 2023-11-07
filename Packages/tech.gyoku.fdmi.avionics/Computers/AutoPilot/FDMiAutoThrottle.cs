using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public enum AutoThrottle_Mode { IAS, Mach, N1 };

    public class FDMiAutoThrottle : FDMiBehaviour
    {
        public FDMiFloat ATSSW, Throttle, ATSSPD, IAS, Mach, N1, IASAcc;
        public FDMiBool GrabThrottle;
        [SerializeField] AutoThrottle_Mode mode;

        [SerializeField] float kp, ki, a = 1f, min = 0f, max = 1f;
        float output, lpias, err, pErr, omega;
        float[] sw, throttle, atsspd, ias, mach, n1, iasAcc;
        bool[] grabThrottle;
        void Start()
        {
            sw = ATSSW.data;
            throttle = Throttle.data;
            atsspd = ATSSPD.data;
            ias = IAS.data;
            mach = Mach.data;
            n1 = N1.data;
            iasAcc = IASAcc.data;
            grabThrottle = GrabThrottle.data;
            omega = 2 * Mathf.PI * a;
            omega = omega / (omega + 1);
        }
        float prevThrottle;
        void Update()
        {
            if (sw[0] < 0.7f || grabThrottle[0]) { return; }
            // IAS LPF
            lpias = ias[0] * omega + (1f - omega) * lpias;
            if (mode == AutoThrottle_Mode.IAS)
            {
                // err = Mathf.Clamp((Speed.val * 0.514444f - IAS), -2f, 2f);
                float tgtAcc = Mathf.Clamp((atsspd[0] * 0.514444f - lpias) * 0.1f, -1f, 1f);
                err = Mathf.Clamp(tgtAcc - iasAcc[0], -1f, 1f);
                output = kp * (err - pErr) + ki * err;
                // Debug.Log("ATS Acc:" + airData[(int)AirData.IASAcc] + ",err:" + err + ",out:" + output);
                output = Mathf.Clamp(throttle[0] + output * Time.deltaTime, min, max);
                pErr = err;
            }
            Throttle.Data = output;
            prevThrottle = output;
        }
    }
}