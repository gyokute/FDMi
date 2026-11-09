using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // ロール軸の追従サーボ。CWS の手放し捕捉はピッチと同じ規則。
    [DefaultExecutionOrder(-300)]
    public class FDMiRollAutopilot : FDMiBehaviour
    {
        [Tooltip("AP スイッチ（0=OFF、1=CWS、2=CMD）")]
        public string apSwitchPath = "~/Input/AP2SW/Value";

        [FDMiDataPath(nameof(apSwitchPath))]
        public FDMiFloat apSwitch;

        [Tooltip("FD スイッチ。OFF なら AP は解除する")]
        public string fdSwitchPath = "~/Input/FD.L/Value";

        [FDMiDataPath(nameof(fdSwitchPath))]
        public FDMiFloat fdSwitch;

        [Tooltip("FD のバンク指令 [deg]")]
        public string fdRollPath = "~/Autopilot/FDRoll";

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

        [Tooltip("ロール軸へ足すバイアス")]
        public string apOutputPath = "~/Input/AutoPilot/AP2/Roll";

        [FDMiDataPath(nameof(apOutputPath))]
        public FDMiFloat apOutput;

        [Tooltip("ロール AP の状態（0=OFF、1=CWS、2=CMD）。FMA 用")]
        public string apModePath = "~/Autopilot/RollAPMode";

        [FDMiDataPath(nameof(apModePath))]
        public FDMiInt apModeOut;

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

        float[] _apSwitch,
            _fdSwitch,
            _fdRoll,
            _roll,
            _rollInputL,
            _rollInputR,
            _apOutput;
        int[] _apModeOut;

        float _servo;
        float _prevRoll;
        float _rollRate;
        float _prevYoke;
        bool _cwsHeld;
        int _prevMode = -1;

        void Start()
        {
            _apSwitch = apSwitch.data;
            _fdSwitch = fdSwitch.data;
            _fdRoll = fdRoll.data;
            _roll = roll.data;
            _rollInputL = rollInputL.data;
            _rollInputR = rollInputR.data;
            _apOutput = apOutput.data;
            _apModeOut = apModeOut.data;
            _prevRoll = _roll[0];
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            int sw = Mathf.RoundToInt(_apSwitch[0]);
            bool fdOn = _fdSwitch[0] >= 0.5f;
            float yoke = Mathf.Clamp(_rollInputL[0] + _rollInputR[0], -1f, 1f);
            float rollNow = _roll[0];

            float rawRate = (rollNow - _prevRoll) / dt;
            _rollRate += (rawRate - _rollRate) * (dt / (rateTau + dt));
            _prevRoll = rollNow;

            int mode;
            if (sw == 0 || !fdOn)
            {
                mode = (int)FDMiAutopilotMode.Off;
                _servo = Mathf.MoveTowards(_servo, 0f, rateMax * dt);
                _cwsHeld = false;
            }
            else if (sw == 1)
            {
                mode = (int)FDMiAutopilotMode.Cws;
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
                mode = (int)FDMiAutopilotMode.Cmd;
                float err = _fdRoll[0] - rollNow;
                float servoRate = Mathf.Clamp(kp * err - kd * _rollRate, -rateMax, rateMax);
                _servo = Mathf.Clamp(_servo + servoRate * dt, -1f, 1f);
                _cwsHeld = false;
            }

            _prevYoke = yoke;
            _apOutput[0] = _servo;
            if (mode != _prevMode)
            {
                _apModeOut[0] = mode;
                _prevMode = mode;
            }
        }
    }
}
