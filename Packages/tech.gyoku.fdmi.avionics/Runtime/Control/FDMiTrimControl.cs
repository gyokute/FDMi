using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // ピッチトリムの操舵則。トリムレバー・左右ヨークのトリムデルタ・AP のトリムレート
    // （いずれも [-1,1] のレート）を足し合わせ、1本のトリム角 [deg] に積分する。出力は FDMiFloat だけ。
    // Why not 絶対位置: 実機のトリムホイールは操作量に比例した速度で回る。レバーとヨークの
    // スイッチを同じレートとして足せるので、入力側に積分器を持たせずに済む。
    // Why not レバーとヨークで別レート: 1本のトリムホイールを複数の入力が回すモデルなので、
    // 合成後のレートを ±1 にクランプすれば足りる。ノブを増やすと調整箇所が増える。
    // Why not 手動トリムを優先する: AP のトリムレートも同じトリムホイールを回す入力として
    // 合算する。手動と AP が同時に動けば AP が手動を打ち消し得る（ADR-0033）。
    // 正のトリム角は前縁上がり（FDMiWing の取付角）。入力の符号は入力側の multiplier で決める。
    [DefaultExecutionOrder(-200)]
    public class FDMiTrimControl : FDMiBehaviour
    {
        [Header("入力")]
        [Tooltip("トリムレバーのパス")]
        public string trimLeverPath = "~/Input/PitchTrim/Value";

        [FDMiDataPath(nameof(trimLeverPath))]
        [Tooltip("トリムレバーのレート [-1,1]")]
        public FDMiFloat trimLever;

        [Tooltip("左ヨークのトリムデルタのパス")]
        public string trimDeltaLPath = "~/Input/YokeL/TrimDelta";

        [FDMiDataPath(nameof(trimDeltaLPath))]
        [Tooltip("トリムデルタ（左）[-1,1]")]
        public FDMiFloat trimDeltaL;

        [Tooltip("右ヨークのトリムデルタのパス")]
        public string trimDeltaRPath = "~/Input/YokeR/TrimDelta";

        [FDMiDataPath(nameof(trimDeltaRPath))]
        [Tooltip("トリムデルタ（右）[-1,1]")]
        public FDMiFloat trimDeltaR;

        [Tooltip("AP1 のピッチトリムレートのパス")]
        public string apTrim1Path = "~/Input/AutoPilot/AP1/TrimRate";

        [FDMiDataPath(nameof(apTrim1Path))]
        [Tooltip("AP1 のピッチトリムレート [-1,1]")]
        public FDMiFloat apTrim1;

        [Tooltip("AP2 のピッチトリムレートのパス")]
        public string apTrim2Path = "~/Input/AutoPilot/AP2/TrimRate";

        [FDMiDataPath(nameof(apTrim2Path))]
        [Tooltip("AP2 のピッチトリムレート [-1,1]")]
        public FDMiFloat apTrim2;

        [Header("出力")]
        [Tooltip("トリム角のパス")]
        public string trimAnglePath = "~/Physics/TrimAngle";

        [FDMiDataPath(nameof(trimAnglePath))]
        [Tooltip("トリム角 [deg]。正で前縁上がり")]
        public FDMiFloat trimAngle;

        [Header("トリム則")]
        [Tooltip("トリム角の最大変化速度 [deg/s]")]
        [Min(0f)]
        public float trimRateDeg = 0.5f;

        [Tooltip("トリム角の下限 [deg]")]
        public float trimMinDeg = -2f;

        [Tooltip("トリム角の上限 [deg]")]
        public float trimMaxDeg = 14f;

        float[] _trimLever;
        float[] _trimDeltaL;
        float[] _trimDeltaR;
        float[] _apTrim1;
        float[] _apTrim2;
        float[] _trimAngle;
        float _trimDeg;

        void Start()
        {
            _trimLever = trimLever.data;
            _trimDeltaL = trimDeltaL.data;
            _trimDeltaR = trimDeltaR.data;
            _apTrim1 = apTrim1.data;
            _apTrim2 = apTrim2.data;
            _trimAngle = trimAngle.data;

            _trimDeg = _trimAngle[0];
            if (_trimDeg > trimMaxDeg)
                _trimDeg = trimMaxDeg;
            else if (_trimDeg < trimMinDeg)
                _trimDeg = trimMinDeg;
            _trimAngle[0] = _trimDeg;
        }

        void FixedUpdate()
        {
            float rate = _trimLever[0] + _trimDeltaL[0] + _trimDeltaR[0] + _apTrim1[0] + _apTrim2[0];
            if (rate > 1f)
                rate = 1f;
            else if (rate < -1f)
                rate = -1f;

            _trimDeg += rate * trimRateDeg * Time.fixedDeltaTime;
            if (_trimDeg > trimMaxDeg)
                _trimDeg = trimMaxDeg;
            else if (_trimDeg < trimMinDeg)
                _trimDeg = trimMinDeg;

            _trimAngle[0] = _trimDeg;
        }
    }
}
