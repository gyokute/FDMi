using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // ピッチ軸の追従サーボ。FD の姿勢差分を追従し、操縦軸に足すバイアスだけを書く。
    // Why not 舵角を直接書く: 操舵則の書き手を FDMiFlightControl 1人に保つ（ADR-0012）。
    // Why not 出力を絶対値にする: 手動入力との合成ができず、engage/解除で舵が跳ぶ。
    // Why not C* 則（Δnz とピッチレートの混合）にする: 荷重倍数の生産者が無く、AP が重力の
    // 与え方を知ることになる（ADR-0032）。
    //
    // 状態は目標姿勢 _ref 1本。CMD は差分から絶対目標を復元し、CWS は保持中の現在姿勢・解除時の
    // 凍結値を入れる。則は両モードで同じ（ADR-0032）。
    [DefaultExecutionOrder(-300)]
    public class FDMiPitchAutopilot : FDMiBehaviour
    {
        [Header("スイッチ")]
        [Tooltip("AP スイッチ（0=OFF、1=CWS、2=CMD）")]
        public string apSwitchPath = "~/Input/AP1SW/Value";

        [FDMiDataPath(nameof(apSwitchPath))]
        public FDMiInt apSwitch;

        [Header("入力")]
        [Tooltip("FD のピッチ姿勢差分 [deg]。正=機首上げ方向の補正")]
        public string pitchDeltaPath = "~/Avionics/FD/L/PitchDelta";

        [FDMiDataPath(nameof(pitchDeltaPath))]
        public FDMiFloat pitchDelta;

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
        public string apOutputPath = "~/Input/AutoPilot/AP1/Pitch";

        [FDMiDataPath(nameof(apOutputPath))]
        public FDMiFloat apOutput;

        [Header("追従則")]
        [Tooltip("姿勢誤差の比例ゲイン [1/deg/s]")]
        public float kp = 0.5f;

        [Tooltip("姿勢レートの制動ゲイン [1/s]")]
        public float kd = 0.15f;

        [Tooltip("サーボの最大速度 [1/s]")]
        public float rateMax = 0.8f;

        [Tooltip("姿勢レートの LPF 時定数 [s]")]
        public float rateTau = 0.3f;

        [Header("CWS")]
        [Tooltip("CWS へ入るヨーク入力 [1]。ヒステリシスの上の値")]
        public float cwsForceThreshold = 0.08f;

        [Tooltip("CWS を解除するヨーク入力 [1]。保持中の目標の凍結はこの値を下回ったとき")]
        public float cwsReleaseThreshold = 0.03f;

        int[] _apSwitch;
        float[] _pitchDelta,
            _pitch,
            _pitchInputL,
            _pitchInputR,
            _apOutput;

        float _ref;
        float _servo;
        float _prevPitch;
        float _pitchRate;
        int _prevSwitch = -1;
        bool _holding;

        void Start()
        {
            _apSwitch = apSwitch.data;
            _pitchDelta = pitchDelta.data;
            _pitch = pitch.data;
            _pitchInputL = pitchInputL.data;
            _pitchInputR = pitchInputR.data;
            _apOutput = apOutput.data;
            _prevPitch = _pitch[0];
            _ref = _pitch[0];
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            int sw = _apSwitch[0];
            float yoke = Mathf.Clamp(_pitchInputL[0] + _pitchInputR[0], -1f, 1f);
            float pitchNow = _pitch[0];

            float rawRate = (pitchNow - _prevPitch) / dt;
            _pitchRate += (rawRate - _pitchRate) * (dt / (rateTau + dt));
            _prevPitch = pitchNow;

            if (sw != _prevSwitch)
            {
                // モードが変わった tick は目標を現在姿勢へ置き直す。古い目標のままだと舵が跳ぶ。
                _ref = pitchNow;
                _holding = false;
                _prevSwitch = sw;
            }

            if (sw == 0)
            {
                // OFF は増分を残さない。残すと手動操縦に AP のバイアスが乗り続ける（ADR-0032）。
                _servo = 0f;
                _ref = pitchNow;
                _holding = false;
                _apOutput[0] = 0f;
                return;
            }

            bool force = Mathf.Abs(yoke) > (_holding ? cwsReleaseThreshold : cwsForceThreshold);
            if (force)
            {
                // 操縦中は目標を現在姿勢へ引き直す。離した瞬間の姿勢が保持目標になる。
                _holding = true;
                _ref = pitchNow;
            }
            else if (sw == 1)
            {
                // CWS: 離した姿勢を凍結して保持する。次の操縦まで目標を動かさない。
                if (_holding)
                    _ref = pitchNow;
                _holding = false;
            }
            else
            {
                // CMD: 差分から絶対目標を復元する。override が解けたら CMD の指示へ戻る。
                _holding = false;
                _ref = pitchNow + _pitchDelta[0];
            }

            float servoRate = Mathf.Clamp(kp * (_ref - pitchNow) - kd * _pitchRate, -rateMax, rateMax);
            _servo = Mathf.Clamp(_servo + servoRate * dt, -1f, 1f);
            _apOutput[0] = _servo;
        }
    }
}
