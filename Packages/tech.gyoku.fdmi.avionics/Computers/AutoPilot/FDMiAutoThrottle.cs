using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public enum AutoThrottle_Mode { IAS, Mach, N1, Retard, Clamp };

    public class FDMiAutoThrottle : FDMiAutoPilot
    {
        public FDMiFloat ATSSW, ATSMode, Throttle, ATSSPD, IAS, Mach, N1TGT, IASAcc, GroundAltitude;
        public FDMiBool GrabThrottle;
        // [SerializeField] AutoThrottle_Mode mode;

        [SerializeField] float kp, ki, a = 1f, min = 0f, max = 1f, retardThreshold = 9.144f;
        float output, lpias, err, pErr, omega;
        float[] sw, mode, throttle, atsspd, ias, mach, n1, iasAcc, galt;
        bool[] grabThrottle;
        void Start()
        {
            sw = ATSSW.data;
            mode = ATSMode.data;
            throttle = Throttle.data;
            atsspd = ATSSPD.data;
            ias = IAS.data;
            mach = Mach.data;
            n1 = N1TGT.data;
            iasAcc = IASAcc.data;
            grabThrottle = GrabThrottle.data;
            galt = GroundAltitude.data;
            omega = 2 * Mathf.PI * a;
            omega = omega / (omega + 1);
            // ATSSW.subscribe(this, "OnChangeATSSW");
        }

        // public void OnChangeATSSW()
        // {
        //     if (Mathf.Approximately(sw[0], 1f))
        //         ATSMode.Data = (int)AutoThrottle_Mode.IAS;
        // }
        float prevThrottle;
        void Update()
        {
            if (sw[0] < 0.7f || grabThrottle[0]) { return; }
            // IAS LPF
            lpias = ias[0] * omega + (1f - omega) * lpias;
            switch ((AutoThrottle_Mode)Mathf.RoundToInt(mode[0]))
            {
                case AutoThrottle_Mode.IAS:
                    if (galt[0] < retardThreshold) ATSMode.Data = (int)AutoThrottle_Mode.Retard;
                    // err = Mathf.Clamp((Speed.val * 0.514444f - IAS), -2f, 2f);
                    float tgtAcc = Mathf.Clamp((atsspd[0] * 0.514444f - lpias) * 0.1f, -1f, 1f);
                    err = Mathf.Clamp(tgtAcc - iasAcc[0], -1f, 1f);
                    output = kp * (err - pErr) + ki * err;
                    // Debug.Log("ATS Acc:" + airData[(int)AirData.IASAcc] + ",err:" + err + ",out:" + output);
                    output = Mathf.Clamp(throttle[0] + output * Time.deltaTime, min, max);
                    pErr = err;
                    break;
                case AutoThrottle_Mode.N1:
                    output = Mathf.MoveTowards(output, n1[0], Time.deltaTime);
                    break;
                case AutoThrottle_Mode.Clamp:
                    if (galt[0] > retardThreshold) ATSMode.Data = (int)AutoThrottle_Mode.IAS;
                    output = Mathf.MoveTowards(output, 0f, Time.deltaTime);
                    break;
                case AutoThrottle_Mode.Retard:
                    float altscale = galt[0] / retardThreshold;
                    if (altscale < 0.02f) ATSMode.Data = (int)AutoThrottle_Mode.Clamp;
                    if (altscale > 1f) ATSMode.Data = (int)AutoThrottle_Mode.IAS;
                    output = Mathf.MoveTowards(output, galt[0] / retardThreshold, Time.deltaTime);
                    break;
            }
            Throttle.Data = output;
            prevThrottle = output;
        }
    }
}