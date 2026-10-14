using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public enum AutoThrottle_Mode { IAS, Mach, N1, Retard, Clamp }

    public class FDMiAutoThrottle : FDMiAutoPilot
    {
        public string ATSSWPath = "ATSSW";

        [FDMiDataPath(nameof(ATSSWPath))]
        public FDMiFloat ATSSW;
        public string ATSModePath = "ATSMode";

        [FDMiDataPath(nameof(ATSModePath))]
        public FDMiFloat ATSMode;
        public string ThrottlePath = "Throttle";

        [FDMiDataPath(nameof(ThrottlePath))]
        public FDMiFloat Throttle;
        public string ATSSPDPath = "ATSSPD";

        [FDMiDataPath(nameof(ATSSPDPath))]
        public FDMiFloat ATSSPD;
        public string IASPath = "IAS";

        [FDMiDataPath(nameof(IASPath))]
        public FDMiFloat IAS;
        public string MachPath = "Mach";

        [FDMiDataPath(nameof(MachPath))]
        public FDMiFloat Mach;
        public string N1TGTPath = "N1TGT";

        [FDMiDataPath(nameof(N1TGTPath))]
        public FDMiFloat N1TGT;
        public string IASAccPath = "IASAcc";

        [FDMiDataPath(nameof(IASAccPath))]
        public FDMiFloat IASAcc;
        public string GroundAltitudePath = "GroundAltitude";

        [FDMiDataPath(nameof(GroundAltitudePath))]
        public FDMiFloat GroundAltitude;
        public string GrabThrottlePath = "GrabThrottle";

        [FDMiDataPath(nameof(GrabThrottlePath))]
        public FDMiBool GrabThrottle;

        [SerializeField] float kp, ki, a = 1f, min = 0f, max = 1f, retardThreshold = 9.144f;
        float output, lpias, err, pErr, omega;

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
            omega = LPFTau(a); // LPFTau(fixedDeltaTime基準)に統一（LPF自体はUpdate駆動のまま）
        }

        void Update()
        {
            if (sw[0] < 0.7f || grabThrottle[0]) { return; }
            lpias = LPF(ias[0], lpias, omega);
            switch ((AutoThrottle_Mode)Mathf.RoundToInt(mode[0]))
            {
                case AutoThrottle_Mode.IAS:
                    if (galt[0] < retardThreshold) ATSMode.Data = (int)AutoThrottle_Mode.Retard;
                    float tgtAcc = Mathf.Clamp((atsspd[0] * 0.514444f - lpias) * 0.1f, -1f, 1f);
                    err = Mathf.Clamp(tgtAcc - iasAcc[0], -1f, 1f);
                    output = kp * (err - pErr) + ki * err;
                    output = Mathf.Clamp(throttle[0] + output * Time.deltaTime, min, max);
                    pErr = err;
                    break;
                case AutoThrottle_Mode.Mach:
                    // 未実装: outputを保持（前回出力を維持）
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
                    output = Mathf.MoveTowards(output, Mathf.Min(output, galt[0] / retardThreshold), Time.deltaTime);
                    break;
            }
            Throttle.Data = output;
        }

        float[] sw, mode, throttle, atsspd, ias, mach, n1, iasAcc, galt;
        bool[] grabThrottle;
    }
}
