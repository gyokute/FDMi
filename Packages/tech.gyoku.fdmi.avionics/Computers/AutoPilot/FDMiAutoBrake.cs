
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public enum AutobrakeMode { RTO, OFF, ONE, TWO, THREE, MAX }
    public class FDMiAutoBrake : FDMiAutoPilot
    {
        [SerializeField] FDMiFloat BrakeInputL, BrakeInputR;

        [SerializeField] FDMiFloat AutoBrakeMode, Throttle, GroundSpeed, BrakeInput;
        [SerializeField] FDMiBool AnyIsGround;

        [SerializeField] float ki, brakeAcc = -2.19456f, lpf = 1f;
        float[] bl, br, absMode, throttle, gs, brake;
        bool[] isground;
        public float tau, acc, prevGS, err, tgtAcc;
        private bool throttleLatch;

        void Start()
        {
            bl = BrakeInputL.data;
            br = BrakeInputR.data;
            absMode = AutoBrakeMode.data;
            throttle = Throttle.data;
            gs = GroundSpeed.data;
            brake = BrakeInput.data;
            isground = AnyIsGround.data;
            tau = LPFTau(lpf);
            AutoBrakeMode.subscribe(this, nameof(OnChangeAutoBrakeMode));
        }

        public void OnChangeAutoBrakeMode()
        {
            switch ((AutobrakeMode)Mathf.RoundToInt(absMode[0]))
            {
                case AutobrakeMode.OFF:
                    throttleLatch = false;
                    tgtAcc = 0f;
                    break;
                case AutobrakeMode.RTO:
                    tgtAcc = 0f;
                    if (throttle[0] > 0.5f) throttleLatch = true;
                    if (throttle[0] < 0.02f && throttleLatch) tgtAcc = -4.2672f; // 14ft/sec
                    break;
                case AutobrakeMode.ONE:
                    throttleLatch = false;
                    tgtAcc = -1.2192f; // 4ft/sec
                    break;
                case AutobrakeMode.TWO:
                    throttleLatch = false;
                    tgtAcc = -1.524f; // 5ft/sec
                    break;
                case AutobrakeMode.THREE:
                    throttleLatch = false;
                    tgtAcc = -2.19456f; // 7.2ft/sec
                    break;
                case AutobrakeMode.MAX:
                    throttleLatch = false;
                    tgtAcc = -4.2672f; // 14ft/sec
                    break;
            }
        }

        void Update()
        {
            if (!isground[0])
            {
                brake[0] = 0f;
                return;
            }
            if (Mathf.RoundToInt(absMode[0]) == (int)AutobrakeMode.RTO)
            {
                if (throttle[0] > 0.5f) throttleLatch = true;
                if (throttle[0] < 0.02f && throttleLatch) tgtAcc = -4.2672f; // 14ft/sec
            }

            acc = LPF((gs[0] - prevGS) / Time.deltaTime, acc, tau);
            prevGS = gs[0];
            err = Mathf.Min(tgtAcc, brakeAcc * Mathf.Clamp01(bl[0] + br[0]));
            if (Mathf.Approximately(err, 0f)) { brake[0] = 0f; return; }
            err -= acc;
            brake[0] = Mathf.Clamp01(IControl(err, brake[0], ki));
        }
    }
}