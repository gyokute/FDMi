
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
        public FDMiFloat BrakeInputL, BrakeInputR;

        public FDMiFloat AutoBrakeMode, Throttle, BrakeInput, GroundSpeed;
        public FDMiBool AnyIsGround;
        [SerializeField] float ki, brakeAcc = -2.19456f, LPFTimeCoef = 5f;
        float[] bl, br, absMode, throttle, brake, gs;
        bool[] isground;
        public float acc, err, tgtAcc;
        private bool throttleLatch;
        private float pGS, tau;
        float AccFilter()
        {
            float rawAcc = (gs[0] - pGS) / (Time.deltaTime);
            acc = LPF(rawAcc, acc, tau);
            pGS = gs[0];
            return acc;
        }

        void Start()
        {
            bl = BrakeInputL.data;
            br = BrakeInputR.data;
            absMode = AutoBrakeMode.data;
            throttle = Throttle.data;
            brake = BrakeInput.data;
            isground = AnyIsGround.data;
            gs = GroundSpeed.data;

            tau = 2 * Mathf.PI / LPFTimeCoef;
            tau = tau / (tau + Time.deltaTime);
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
            float manualBrakeInput = Mathf.Clamp01(bl[0] + br[0]);
            if (manualBrakeInput > 0.1f && AutoBrakeMode.Data > (float)AutobrakeMode.OFF)
                AutoBrakeMode.Data = (float)AutobrakeMode.OFF;
            if (AutoBrakeMode.Data == (float)AutobrakeMode.OFF)
            {
                brake[0] = manualBrakeInput;
                return;
            }
            if (!isground[0] || Mathf.Abs(gs[0]) < 0.01f)
            {
                brake[0] = Mathf.MoveTowards(brake[0], 0, ki * Time.deltaTime);
                return;
            }
            if (Mathf.RoundToInt(absMode[0]) == (int)AutobrakeMode.RTO)
            {
                if (throttle[0] > 0.5f) throttleLatch = true;
                if (throttle[0] < 0.02f && throttleLatch) tgtAcc = -4.2672f; // 14ft/sec
            }

            err = AccFilter() - tgtAcc;
            brake[0] = Mathf.Clamp01(IControl(err, brake[0], ki));
            if (err < 0) brake[0] = 0f;
            // Debug.Log(acc + "," + err + "," + brake[0]);
        }
    }
}