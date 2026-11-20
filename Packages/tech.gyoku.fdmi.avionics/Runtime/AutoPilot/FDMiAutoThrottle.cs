using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public enum FDMiAutoThrottleMode
    {
        Spd = 0,
        N1 = 1,
        Retard = 2,
        Clamp = 3,
    }

    // 自動推力。main の SPD 則（目標加速度クランプ＋積分1本）をそのまま移植する。
    // Why not 速度PI: main の目標加速度クランプが速度変化レートの上限を作っており、
    // 実機の感触も確認済み。構造を変える理由がない。
    // Why not レバーと別ノード: レバー入力は離している間だけ書くので、同じノードを
    // 現在値として読み書きすれば「現在値＋Δ」が自然に成立する。
    [DefaultExecutionOrder(-300)]
    public class FDMiAutoThrottle : FDMiBehaviour
    {
        [Header("スイッチ")]
        [Tooltip("A/T スイッチ")]
        public string atSwitchPath = "~/Input/ATSW/Value";

        [FDMiDataPath(nameof(atSwitchPath))]
        public FDMiBool atSwitch;

        [Tooltip("A/T モード選択（0=SPD、1=N1）")]
        public string modeSelectPath = "~/Input/ATSMode";

        [FDMiDataPath(nameof(modeSelectPath))]
        public FDMiInt modeSelect;

        [Header("指令")]
        [Tooltip("速度指令 [kt]")]
        public string speedCommandPath = "~/Input/SPD/Value";

        [FDMiDataPath(nameof(speedCommandPath))]
        public FDMiInt speedCommand;

        [Tooltip("N1 指令 [0,1]")]
        public string n1CommandPath = "~/Input/N1Command";

        [FDMiDataPath(nameof(n1CommandPath))]
        public FDMiFloat n1Command;

        [Header("状態")]
        [Tooltip("指示対気速度 [m/s]")]
        public string iasPath = "~/Physics/IAS";

        [FDMiDataPath(nameof(iasPath))]
        public FDMiFloat ias;

        [Tooltip("IAS の時間微分 [m/s²]")]
        public string iasAccPath = "~/Physics/IASAcc";

        [FDMiDataPath(nameof(iasAccPath))]
        public FDMiFloat iasAcc;

        [Tooltip("レーダ高度 [m]")]
        public string radarAltitudePath = "~/Physics/RadarAltitude";

        [FDMiDataPath(nameof(radarAltitudePath))]
        public FDMiFloat radarAltitude;

        [Tooltip("接地")]
        public string groundedPath = "~/Physics/IsGrounded";

        [FDMiDataPath(nameof(groundedPath))]
        public FDMiBool grounded;

        [Tooltip("鉛直速度 [m/s]")]
        public string verticalSpeedPath = "~/Physics/VS";

        [FDMiDataPath(nameof(verticalSpeedPath))]
        public FDMiFloat verticalSpeed;

        [Header("出力")]
        [Tooltip("スロットルレバーの値。同じノードを読み書きする")]
        public string throttlePath = "~/Input/ThrottleLever/Throttle";

        [FDMiDataPath(nameof(throttlePath))]
        public FDMiFloat throttle;

        [Header("則")]
        [Tooltip("速度誤差→目標加速度 [1/s]")]
        public float speedErrToAccel = 0.1f;

        [Tooltip("目標加速度の上限 [m/s²]。1.0 は約 1.94 kt/s")]
        public float maxAccel = 1f;

        [Tooltip("加速度誤差→スロットルレート [1/s]")]
        public float ki = 0.12f;

        [Tooltip("N1 誤差→スロットルレート [1/s]")]
        public float n1Gain = 1f;

        [Tooltip("N1 モードのスロットルレート上限 [1/s]")]
        public float n1Rate = 0.5f;

        [Tooltip("RETARD を始めるレーダ高度 [m]")]
        public float retardThreshold = 9.144f;

        [Tooltip("RETARD/CLAMP のスロットルレート [1/s]")]
        public float retardRate = 0.5f;

        bool[] _atSwitch;
        int[] _speedCommand;
        float[] _n1Command,
            _ias,
            _iasAcc,
            _radarAltitude,
            _verticalSpeed,
            _throttle;
        int[] _modeSelect;
        bool[] _grounded;
        int _prevMode = 0;

        void Start()
        {
            _modeSelect = modeSelect.data;
            _atSwitch = atSwitch.data;
            _speedCommand = speedCommand.data;
            _n1Command = n1Command.data;
            _ias = ias.data;
            _iasAcc = iasAcc.data;
            _radarAltitude = radarAltitude.data;
            _verticalSpeed = verticalSpeed.data;
            _throttle = throttle.data;
            _grounded = grounded.data;
        }

        void FixedUpdate()
        {
            if (!_atSwitch[0])
                return;

            float dt = Time.fixedDeltaTime;
            bool isGrounded = _grounded[0];

            float throttleNow = _throttle[0];
            float raderAlt = _radarAltitude[0];

            switch ((FDMiAutoThrottleMode)_modeSelect[0])
            {
                case FDMiAutoThrottleMode.Spd:
                    _prevMode = modeSelect.data[0];
                    if (raderAlt < retardThreshold)
                        modeSelect.Data = (int)FDMiAutoThrottleMode.Retard;

                    float spdErr = (float)_speedCommand[0] * 0.514444f - _ias[0];
                    float tgtAcc = Mathf.Clamp(spdErr * speedErrToAccel, -maxAccel, maxAccel);
                    float accErr = Mathf.Clamp(tgtAcc - _iasAcc[0], -maxAccel, maxAccel);
                    throttleNow += ki * accErr * Time.fixedDeltaTime;
                    break;
                case FDMiAutoThrottleMode.N1:
                    _prevMode = modeSelect.data[0];
                    if (raderAlt < retardThreshold)
                        modeSelect.Data = (int)FDMiAutoThrottleMode.Retard;
                    throttleNow = Mathf.MoveTowards(throttleNow, _n1Command[0], n1Rate * Time.fixedDeltaTime);
                    break;
                case FDMiAutoThrottleMode.Retard:
                    if (isGrounded)
                        modeSelect.Data = (int)FDMiAutoThrottleMode.Clamp;
                    else if (raderAlt >= retardThreshold)
                        modeSelect.Data = _prevMode;
                    float tgt = _verticalSpeed[0] > 0 ? _n1Command[0] : 0;
                    throttleNow = Mathf.MoveTowards(throttleNow, retardThreshold, retardRate * Time.fixedDeltaTime);
                    break;
                case FDMiAutoThrottleMode.Clamp:
                    if (!isGrounded)
                        modeSelect.Data = (int)FDMiAutoThrottleMode.Retard;
                    throttleNow = Mathf.MoveTowards(throttleNow, 0f, retardRate * Time.fixedDeltaTime);
                    break;
            }
            throttle.Data = Mathf.Clamp01(throttleNow);
        }
    }
}
