using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // ピッチ軸の追従則。FD の姿勢差分に比例する補正へ、姿勢レートの制動とレート積分を
    // 足し、操縦軸とピッチトリムへ書く。
    // 追従則は correction = clamp(kp·Delta + rateCum - kd·q, ±1)。
    // rateCum は LPF を通した姿勢レート q を -kiRate で積分し、±rateCumLimit で
    // クランプしたもの。定常ではリセット時点からの姿勢変化に比例する（姿勢誤差の積分器
    // ではない）。
    // Why not 舵角を直接書く: 操舵則の書き手を FDMiFlightControl 1人に保つ（ADR-0012）。
    // Why not 出力を絶対値にする: 手動入力との合成ができず、engage/解除で舵が跳ぶ。
    // Why not 積分サーボ（servo += servoRate·dt）: レート制限付きの積分器が姿勢誤差を
    // 溜め続け、目標姿勢で止まらない（ADR-0033）。
    // Why not 姿勢誤差の積分器を AP に持つ: 定常分は実機のトリム角へ移す。レート累積は
    // CWS の姿勢保持と外乱の吸収だけを担い、定常舵はトリムが持つ。
    //
    // CMD は kp·Delta を足し、CWS は Delta を 0 として rateCum とレート制動だけで姿勢を
    // 保持する。rateCum は AP OFF とヨーク override 中に 0 へ戻るので、CWS の保持基準は
    // 最後にリセットした時点（解除時）の姿勢になる。目標姿勢を陽には持たない
    // （ADR-0032、ADR-0033）。
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
        [Tooltip("ピッチ軸へ足す補正（PD＋レート累積）。FDMiFlightControl がヨークと加算する")]
        public string apOutputPath = "~/Input/AutoPilot/AP1/Pitch";

        [FDMiDataPath(nameof(apOutputPath))]
        public FDMiFloat apOutput;

        [Tooltip("ピッチトリムへ出すレート [-1,1]。FDMiTrimControl が 0.5 deg/s で積分する")]
        public string trimRatePath = "~/Input/AutoPilot/AP1/TrimRate";

        [FDMiDataPath(nameof(trimRatePath))]
        public FDMiFloat trimRate;

        [Header("追従則")]
        [Tooltip("姿勢誤差の比例ゲイン [1/deg]")]
        public float kp = 0.2f;

        [Tooltip("姿勢レートの積分ゲイン [1/deg]")]
        public float kiRate = 0.2f;

        [Tooltip("姿勢レートの制動ゲイン [1/(deg/s)]")]
        public float kd = 0.1f;

        [Tooltip("PD 補正→トリムレートのゲイン [1/s]")]
        public float kiTrim = 0.5f;

        [Tooltip("姿勢レートの LPF 時定数 [s]")]
        public float rateTau = 0.3f;

        [Tooltip("補正出力の変化速度 [1/s]")]
        public float rateLimit = 2f;

        [Tooltip("ピッチレート累積項の制限値（無次元）")]
        public float rateCumLimit = 0.2f;

        [Header("CWS")]
        [Tooltip("ヨーク override に入る入力 [1]。ヒステリシスの上の値")]
        public float cwsForceThreshold = 0.08f;

        [Tooltip("ヨーク override を解除する入力 [1]。この値を下回ると AP 出力を再開する")]
        public float cwsReleaseThreshold = 0.03f;

        int[] _apSwitch;
        float[] _pitchDelta,
            _pitch,
            _pitchInputL,
            _pitchInputR,
            _apOutput,
            _trimRate;

        float _prevPitch;
        float _pitchRate;
        float _pitchRateCum;
        bool _holding;

        void Start()
        {
            _apSwitch = apSwitch.data;
            _pitchDelta = pitchDelta.data;
            _pitch = pitch.data;
            _pitchInputL = pitchInputL.data;
            _pitchInputR = pitchInputR.data;
            _apOutput = apOutput.data;
            _trimRate = trimRate.data;
            _prevPitch = _pitch[0];
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
            _pitchRateCum = Mathf.Clamp(_pitchRateCum - _pitchRate * kiRate * dt, -rateCumLimit, rateCumLimit);

            _holding = Mathf.Abs(yoke) > (_holding ? cwsReleaseThreshold : cwsForceThreshold);

            if (sw == 0 || _holding)
            {
                _apOutput[0] = 0f;
                _trimRate[0] = 0f;
                _pitchRateCum = 0f;
                return;
            }

            float pitchCmd = (sw == 2) ? kp * _pitchDelta[0] : 0f;

            float correction = Mathf.Clamp(pitchCmd + _pitchRateCum - kd * _pitchRate, -1f, 1f);
            correction = Mathf.MoveTowards(_apOutput[0], correction, dt * rateLimit);
            _apOutput[0] = correction;

            // 正の補正（機首上げ）でトリム角を減らす向き。トリム角の正は前縁上がりで機首下げ。
            // 定常ではトリムが舵の代わりを引き受け、correction は 0 に収束する（ADR-0033）。
            _trimRate[0] = Mathf.Clamp(-kiTrim * correction, -1f, 1f);
        }
    }
}
