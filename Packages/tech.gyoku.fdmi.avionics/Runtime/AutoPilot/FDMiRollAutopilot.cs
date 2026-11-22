using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // ロール軸の追従則。FD のロール差分を追従し、操縦軸へ足す PD 補正だけを書く。
    // ロールには実機のトリムが無いため、ピッチのようなトリムレートは持たない（ADR-0033）。
    // CWS の保持はピッチと同じ規則で、解除時のバンクが 5° 未満なら目標を 0（wings level）にする。
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
        [Tooltip("ロール軸へ足す PD 補正。FDMiFlightControl がヨークと加算する")]
        public string apOutputPath = "~/Input/AutoPilot/AP1/Roll";

        [FDMiDataPath(nameof(apOutputPath))]
        public FDMiFloat apOutput;

        [Header("追従則")]
        [Tooltip("ロール誤差の比例ゲイン [1/deg]")]
        public float kp = 0.2f;

        [Tooltip("ロールレートの積分ゲイン")]
        public float kiRate = 0.2f;

        [Tooltip("ロールレートの制動ゲイン [1/(deg/s)]")]
        public float kd = 0.1f;

        [Tooltip("ロールレートの制限値 ")]
        public float rateLimit = 2f;

        [Tooltip("ロールレート累積項の制限値 ")]
        public float rateCumLimit = 0.2f;

        [Tooltip("ロールレートの LPF 時定数 [s]")]
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

        float _prevRoll;
        float _rollRate;
        float _rollRateCum;
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
            _rollRateCum = Mathf.Clamp(_rollRateCum - _rollRate * kiRate * dt, -rateCumLimit, rateCumLimit);

            _holding = Mathf.Abs(yoke) > (_holding ? cwsReleaseThreshold : cwsForceThreshold);

            if (sw == 0 || _holding)
            {
                _apOutput[0] = 0f;
                _rollRateCum = 0f;
                return;
            }

            float rollCmd = (sw == 2) ? kp * _rollDelta[0] : 0f;

            float apOutput = _holding ? 0f : Mathf.Clamp(rollCmd + _rollRateCum - kd * _rollRate, -1f, 1f);
            _apOutput[0] = Mathf.MoveTowards(_apOutput[0], apOutput, dt * rateLimit);
        }
    }
}
