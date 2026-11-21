using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // ロール軸の追従サーボ。CWS の手放し捕捉はピッチと同じ規則。
    [DefaultExecutionOrder(-300)]
    public class FDMiRollAutopilot : FDMiBehaviour
    {
        [Header("スイッチ")]
        [Tooltip("AP スイッチ（0=OFF、1=CWS、2=CMD）")]
        public string apSwitchPath = "~/Input/AP1SW/Value";

        [FDMiDataPath(nameof(apSwitchPath))]
        public FDMiInt apSwitch;

        [Header("入力")]
        [Tooltip("FD のバンク指令 [deg]")]
        public string fdRollPath = "~/Avionics/FD/L/Roll";

        [FDMiDataPath(nameof(fdRollPath))]
        public FDMiFloat fdRoll;

        [Tooltip("バンク角 [deg]")]
        public string rollPath = "~/Physics/Roll";

        [FDMiDataPath(nameof(rollPath))]
        public FDMiFloat roll;

        [Tooltip("左ヨークのロール入力")]
        public string rollInputLPath = "~/Input/YokeL/RollInput";

        [FDMiDataPath(nameof(rollInputLPath))]
        public FDMiFloat rollInputL;

        [Tooltip("右ヨークのロール入力")]
        public string rollInputRPath = "~/Input/YokeR/RollInput";

        [FDMiDataPath(nameof(rollInputRPath))]
        public FDMiFloat rollInputR;

        [Header("出力")]
        [Tooltip("ロール軸へ足すバイアス")]
        public string apOutputPath = "~/Input/AutoPilot/AP1/Roll";

        [FDMiDataPath(nameof(apOutputPath))]
        public FDMiFloat apOutput;

        [Header("追従則")]
        [Tooltip("姿勢誤差の比例ゲイン [1/deg/s]")]
        public float kp = 1f;

        [Tooltip("姿勢レートの制動ゲイン [1/s]")]
        public float kd = 0.25f;

        [Tooltip("サーボの最大速度 [1/s]")]
        public float rateMax = 1f;

        [Tooltip("姿勢レートの LPF 時定数 [s]")]
        public float rateTau = 0.3f;

        [Tooltip("CWS の手放し判定")]
        public float cwsInputThreshold = 0.02f;

        int[] _apSwitch;
        float[] _fdRoll,
            _roll,
            _rollInputL,
            _rollInputR,
            _apOutput;

        float _servo;
        float _prevRoll;
        float _rollRate;
        float _prevYoke;
        bool _cwsHeld;

        void Start()
        {
            _apSwitch = apSwitch.data;
            _fdRoll = fdRoll.data;
            _roll = roll.data;
            _rollInputL = rollInputL.data;
            _rollInputR = rollInputR.data;
            _apOutput = apOutput.data;
            _prevRoll = _roll[0];
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            int sw = _apSwitch[0];
            float yoke = Mathf.Clamp(_rollInputL[0] + _rollInputR[0], -1f, 1f);
            float rollNow = _roll[0];

            float rawRate = (rollNow - _prevRoll) / dt;
            _rollRate += (rawRate - _rollRate) * (dt / (rateTau + dt));
            _prevRoll = rollNow;

            if (sw == 0)
            {
                _servo = Mathf.MoveTowards(_servo, 0f, rateMax * dt);
                _cwsHeld = false;
            }
            else if (sw == 1)
            {
                if (Mathf.Abs(yoke) > cwsInputThreshold)
                {
                    _cwsHeld = true;
                }
                else if (_cwsHeld)
                {
                    _servo = Mathf.Clamp(_servo + _prevYoke, -1f, 1f);
                    _cwsHeld = false;
                }
            }
            else
            {
                float err = _fdRoll[0] - rollNow;
                float servoRate = Mathf.Clamp(kp * err - kd * _rollRate, -rateMax, rateMax);
                _servo = Mathf.Clamp(_servo + servoRate * dt, -1f, 1f);
                _cwsHeld = false;
            }

            _prevYoke = yoke;
            _apOutput[0] = _servo;
        }
    }
}
