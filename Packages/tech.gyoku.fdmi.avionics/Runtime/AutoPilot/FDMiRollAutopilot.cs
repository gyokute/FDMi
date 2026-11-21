using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // ロール軸の追従サーボ。CWS の保持はピッチと同じ規則で、解除時のバンクが 5° 未満なら
    // 目標を 0（wings level）にする。
    // Why not 5° 以上の判定を毎tick続ける: 実機の 6° 則は解除時のバンクで決まる規則であり、
    // 保持中に判定すると翼が勝手に水平へ落ちる（ADR-0032）。
    [DefaultExecutionOrder(-300)]
    public class FDMiRollAutopilot : FDMiBehaviour
    {
        [Header("スイッチ")]
        [Tooltip("AP スイッチ（0=OFF、1=CWS、2=CMD）")]
        public string apSwitchPath = "~/Input/AP1SW/Value";

        [FDMiDataPath(nameof(apSwitchPath))]
        public FDMiInt apSwitch;

        [Header("入力")]
        [Tooltip("FD のバンク差分 [deg]。正=右翼下げ方向の補正")]
        public string rollDeltaPath = "~/Avionics/FD/L/RollDelta";

        [FDMiDataPath(nameof(rollDeltaPath))]
        public FDMiFloat rollDelta;

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

        [Header("CWS")]
        [Tooltip("CWS へ入るヨーク入力 [1]。ヒステリシスの上の値")]
        public float cwsForceThreshold = 0.08f;

        [Tooltip("CWS を解除するヨーク入力 [1]。保持中の目標の凍結はこの値を下回ったとき")]
        public float cwsReleaseThreshold = 0.03f;

        [Tooltip("解除時に wings level へ落とすバンク角 [deg]")]
        public float wingsLevelBankDeg = 5f;

        int[] _apSwitch;
        float[] _rollDelta,
            _roll,
            _rollInputL,
            _rollInputR,
            _apOutput;

        float _ref;
        float _servo;
        float _prevRoll;
        float _rollRate;
        int _prevSwitch = -1;
        bool _holding;

        void Start()
        {
            _apSwitch = apSwitch.data;
            _rollDelta = rollDelta.data;
            _roll = roll.data;
            _rollInputL = rollInputL.data;
            _rollInputR = rollInputR.data;
            _apOutput = apOutput.data;
            _prevRoll = _roll[0];
            _ref = _roll[0];
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

            if (sw != _prevSwitch)
            {
                // モードが変わった tick は目標を現在姿勢へ置き直す。古い目標のままだと舵が跳ぶ。
                _ref = rollNow;
                _holding = false;
                _prevSwitch = sw;
            }

            if (sw == 0)
            {
                _servo = 0f;
                _ref = rollNow;
                _holding = false;
                _apOutput[0] = 0f;
                return;
            }

            bool force = Mathf.Abs(yoke) > (_holding ? cwsReleaseThreshold : cwsForceThreshold);
            if (force)
            {
                _holding = true;
                _ref = rollNow;
            }
            else if (sw == 1)
            {
                // CWS: 離した瞬間のバンクを凍結する。5° 未満なら wings level に落とす。
                if (_holding)
                {
                    _ref = Mathf.Abs(rollNow) < wingsLevelBankDeg ? 0f : rollNow;
                }
                _holding = false;
            }
            else
            {
                _holding = false;
                _ref = rollNow + _rollDelta[0];
            }

            float servoRate = Mathf.Clamp(kp * (_ref - rollNow) - kd * _rollRate, -rateMax, rateMax);
            _servo = Mathf.Clamp(_servo + servoRate * dt, -1f, 1f);
            _apOutput[0] = _servo;
        }
    }
}
