using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public enum AutobrakeMode { RTO, OFF, ONE, TWO, THREE, MAX }

    public class FDMiAutoBrake : FDMiAutoPilot
    {
        public string BrakeInputLPath = "BrakeInputL";

        [FDMiDataPath(nameof(BrakeInputLPath))]
        public FDMiFloat BrakeInputL;
        public string BrakeInputRPath = "BrakeInputR";

        [FDMiDataPath(nameof(BrakeInputRPath))]
        public FDMiFloat BrakeInputR;

        public string AutoBrakeModePath = "AutoBrakeMode";

        [FDMiDataPath(nameof(AutoBrakeModePath))]
        [FDMiRegisterCallback(nameof(OnChangeAutoBrakeMode))]
        public FDMiFloat AutoBrakeMode;
        public string ThrottlePath = "Throttle";

        [FDMiDataPath(nameof(ThrottlePath))]
        public FDMiFloat Throttle;
        public string BrakeInputPath = "BrakeInput";

        [FDMiDataPath(nameof(BrakeInputPath))]
        public FDMiFloat BrakeInput;
        public string GroundSpeedPath = "GroundSpeed";

        [FDMiDataPath(nameof(GroundSpeedPath))]
        public FDMiFloat GroundSpeed;
        public string AnyIsGroundPath = "AnyIsGround";

        [FDMiDataPath(nameof(AnyIsGroundPath))]
        public FDMiBool AnyIsGround;

        [SerializeField] float ki, brakeAcc = -2.19456f;
        public float acc, err, tgtAcc;
        bool throttleLatch;
        float prevGS, accSum;
        int currentAccFilterIndex, maxAccQueueIndex = 5;
        float[] accQueue;

        void Start()
        {
            bl = BrakeInputL.data;
            br = BrakeInputR.data;
            absMode = AutoBrakeMode.data;
            throttle = Throttle.data;
            brake = BrakeInput.data;
            isground = AnyIsGround.data;
            gs = GroundSpeed.data;
            accQueue = new float[5]; // Udonではフィールド初期化子の配列new不可
            prevGS = gs[0]; // 初回の加速度スパイク防止
        }

        float AccFilter(float input)
        {
            accSum += input - accQueue[currentAccFilterIndex];
            accQueue[currentAccFilterIndex] = input;
            currentAccFilterIndex = (currentAccFilterIndex + 1) % maxAccQueueIndex;
            return accSum / maxAccQueueIndex;
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

        void FixedUpdate()
        {
            if (!isground[0])
            {
                BrakeInput.Data = 0f;
                return;
            }
            if (Mathf.RoundToInt(absMode[0]) == (int)AutobrakeMode.RTO)
            {
                if (throttle[0] > 0.5f) throttleLatch = true;
                if (throttle[0] < 0.02f && throttleLatch) tgtAcc = -4.2672f; // 14ft/sec
            }
            acc = AccFilter((gs[0] - prevGS) / Time.fixedDeltaTime);
            prevGS = gs[0];
            err = Mathf.Min(tgtAcc, brakeAcc * Mathf.Clamp01(bl[0] + br[0]));
            if (Mathf.Approximately(err, 0f)) { BrakeInput.Data = 0f; return; }
            err -= acc;
            BrakeInput.Data = Mathf.Clamp01(IControl(err, brake[0], ki));
        }

        float[] bl, br, absMode, throttle, brake, gs;
        bool[] isground;
    }
}
