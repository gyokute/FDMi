using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public enum FDMiAutopilotMode
    {
        Off = 0,
        Cws = 1,
        Cmd = 2,
    }

    // ピッチ軸の追従サーボ。FD の姿勢指令を追従し、操縦軸に足すバイアスだけを書く。
    // Why not 舵角を直接書く: 操舵則の書き手を FDMiFlightControl 1人に保つ（ADR-0012）。
    // Why not 出力を絶対値にする: 手動入力との合成ができず、engage/解除で舵が跳ぶ。
    [DefaultExecutionOrder(-300)]
    public class FDMiPitchAutopilot : FDMiBehaviour
    {
        [Header("スイッチ")]
        [Tooltip("AP スイッチ（0=OFF、1=CWS、2=CMD）")]
        public string apSwitchPath = "~/Input/AP2SW/Value";

        [FDMiDataPath(nameof(apSwitchPath))]
        public FDMiFloat apSwitch;

        [Tooltip("FD スイッチ。OFF なら AP は解除する")]
        public string fdSwitchPath = "~/Input/FD.L/Value";

        [FDMiDataPath(nameof(fdSwitchPath))]
        public FDMiFloat fdSwitch;

        [Header("入力")]
        [Tooltip("FD のピッチ姿勢指令 [deg]")]
        public string fdPitchPath = "~/Autopilot/FDPitch";

        [FDMiDataPath(nameof(fdPitchPath))]
        public FDMiFloat fdPitch;

        [Tooltip("ピッチ角 [deg]")]
        public string pitchPath = "~/Physics/Pitch";

        [FDMiDataPath(nameof(pitchPath))]
        public FDMiFloat pitch;

        [Tooltip("左ヨークのピッチ入力")]
        public string pitchInputLPath = "~/Input/YokeL/PitchInput";

        [FDMiDataPath(nameof(pitchInputLPath))]
        public FDMiFloat pitchInputL;

        [Tooltip("右ヨークのピッチ入力")]
        public string pitchInputRPath = "~/Input/YokeR/PitchInput";

        [FDMiDataPath(nameof(pitchInputRPath))]
        public FDMiFloat pitchInputR;

        [Header("出力")]
        [Tooltip("ピッチ軸へ足すバイアス。FDMiFlightControl がヨークと加算する")]
        public string apOutputPath = "~/Input/AutoPilot/AP2/Pitch";

        [FDMiDataPath(nameof(apOutputPath))]
        public FDMiFloat apOutput;

        [Tooltip("ピッチ AP の状態（0=OFF、1=CWS、2=CMD）。FMA 用")]
        public string apModePath = "~/Autopilot/PitchAPMode";

        [FDMiDataPath(nameof(apModePath))]
        public FDMiInt apModeOut;

        [Header("追従則")]
        [Tooltip("姿勢誤差の比例ゲイン [1/deg/s]")]
        public float kp = 0.5f;

        [Tooltip("姿勢レートの制動ゲイン [1/s]")]
        public float kd = 0.15f;

        [Tooltip("サーボの最大速度 [1/s]")]
        public float rateMax = 0.8f;

        [Tooltip("姿勢レートの LPF 時定数 [s]")]
        public float rateTau = 0.3f;

        [Tooltip("CWS の手放し判定")]
        public float cwsInputThreshold = 0.02f;

        float[] _apSwitch,
            _fdSwitch,
            _fdPitch,
            _pitch,
            _pitchInputL,
            _pitchInputR,
            _apOutput;
        int[] _apModeOut;

        float _servo;
        float _prevPitch;
        float _pitchRate;
        float _prevYoke;
        bool _cwsHeld;
        int _prevMode = -1;

        void Start()
        {
            _apSwitch = apSwitch.data;
            _fdSwitch = fdSwitch.data;
            _fdPitch = fdPitch.data;
            _pitch = pitch.data;
            _pitchInputL = pitchInputL.data;
            _pitchInputR = pitchInputR.data;
            _apOutput = apOutput.data;
            _apModeOut = apModeOut.data;
            _prevPitch = _pitch[0];
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            int sw = Mathf.RoundToInt(_apSwitch[0]);
            bool fdOn = _fdSwitch[0] >= 0.5f;
            float yoke = Mathf.Clamp(_pitchInputL[0] + _pitchInputR[0], -1f, 1f);
            float pitchNow = _pitch[0];

            float rawRate = (pitchNow - _prevPitch) / dt;
            _pitchRate += (rawRate - _pitchRate) * (dt / (rateTau + dt));
            _prevPitch = pitchNow;

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
                // 保持中はサーボを凍結する。離した瞬間に最後の入力をサーボへ移すと、
                // ヨーク入力が 0 に戻っても舵の合計が連続になる。
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
                float err = _fdPitch[0] - pitchNow;
                float servoRate = Mathf.Clamp(kp * err - kd * _pitchRate, -rateMax, rateMax);
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
