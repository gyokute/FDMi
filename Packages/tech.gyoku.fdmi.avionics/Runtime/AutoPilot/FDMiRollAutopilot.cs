using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // ロール軸の追従則。FD のロール差分に比例する補正へ、ロールレートの制動と
    // レート積分を足し、操縦軸へ書く。
    // 追従則は correction = clamp(kp·Delta + rateCum - kd·q, ±1)。式と rateCum の
    // 扱いはピッチと同じ（ADR-0033）。ただしロールはトリムが無く、CMD と CWS で
    // 必要な rateCum 権威が違うため、上限を CMD 0.2 / CWS 0.5 に分ける（ADR-0038）。
    // ロールには実機のトリムが無いため、ピッチのようなトリムレートは持たない。
    // CMD は kp·Delta を足し、CWS は Delta を 0 として rateCum とレート制動だけで
    // バンクを保持する。保持基準は rateCum を最後にリセットした時点（解除時）の姿勢で、
    // wings level の 5°/6° 則は持たない。
    // Why not 解除時のバンクで wings level を判定する: rateCum のリセット時点の姿勢を
    // 暗黙の保持基準にしており、5° 則を足すには解除時のバンクを別に保存して目標を
    // 差し替える必要がある。状態が二重になる（ADR-0032、ADR-0033）。
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
        [Tooltip("ロール軸へ足す補正（PD＋レート累積）。FDMiFlightControl がヨークと加算する")]
        public string apOutputPath = "~/Input/AutoPilot/AP1/Roll";

        [FDMiDataPath(nameof(apOutputPath))]
        public FDMiFloat apOutput;

        [Header("追従則")]
        [Tooltip("ロール誤差の比例ゲイン [1/deg]")]
        public float kp = 0.2f;

        [Tooltip("ロールレートの積分ゲイン [1/deg]")]
        public float kiRate = 0.2f;

        [Tooltip("ロールレートの制動ゲイン [1/(deg/s)]")]
        public float kd = 0.1f;

        [Tooltip("補正出力の変化速度 [1/s]")]
        public float rateLimit = 1f;

        // CMD では rateCum が大きいと旋回中に風袋が残り、FD のバンク指令を
        // 押し戻して機首方位の整定が遅れる。0.175 で 150–350 kt の最終方位偏差が
        // 最大 0.35°、尾部の振動 0.0001° 以下。0.2 では 350 kt で 0.43° 残る。
        [Tooltip("CMD のロールレート累積項の制限値（無次元）")]
        public float rateCumLimit = 0.175f;

        // CWS は定常モーメントを rateCum だけで保持する。0.2 では 150 kt の
        // ±5 deg/s 相当のバイアスでバンクが 160° ドリフトする。0.5 で保持できる。
        [Tooltip("CWS のロールレート累積項の制限値（無次元）")]
        public float rateCumLimitCws = 0.5f;

        [Tooltip("ロールレートの LPF 時定数 [s]")]
        public float rateTau = 0.3f;

        [Header("CWS")]
        [Tooltip("ヨーク override に入る入力 [1]。ヒステリシスの上の値")]
        public float cwsForceThreshold = 0.08f;

        [Tooltip("ヨーク override を解除する入力 [1]。この値を下回ると AP 出力を再開する")]
        public float cwsReleaseThreshold = 0.03f;

        [Tooltip("未使用。wings level 則は実装していない")]
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
            float cumLimit = (sw == 2) ? rateCumLimit : rateCumLimitCws;
            _rollRateCum = Mathf.Clamp(_rollRateCum - _rollRate * kiRate * dt, -cumLimit, cumLimit);

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
