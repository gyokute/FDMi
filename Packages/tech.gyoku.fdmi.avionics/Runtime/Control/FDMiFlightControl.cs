using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // 操縦入力を舵角に変換する。DC-10 の一次資料に合わせ、フラップとスラットが格納されている間は
    // アウトボードエルロンをフェアに固定し、横操縦をインボードエルロンとウイングスポイラで行う。
    // Why not 面側に則を持たせる: 同じ則が翼の区間数ぶん複製され、設定が食い違う。
    [DefaultExecutionOrder(-200)]
    public class FDMiFlightControl : FDMiBehaviour
    {
        [Header("ヨーク")]
        [Tooltip("左ヨークのピッチ入力パス")]
        public string pitchLPath = "~/Input/YokeL/PitchInput";

        [FDMiDataPath(nameof(pitchLPath))]
        [Tooltip("ピッチ入力（左）[-1,1]")]
        public FDMiFloat pitchL;

        [Tooltip("右ヨークのピッチ入力パス")]
        public string pitchRPath = "~/Input/YokeR/PitchInput";

        [FDMiDataPath(nameof(pitchRPath))]
        [Tooltip("ピッチ入力（右）[-1,1]")]
        public FDMiFloat pitchR;

        [Tooltip("左ヨークのロール入力パス")]
        public string rollLPath = "~/Input/YokeL/RollInput";

        [FDMiDataPath(nameof(rollLPath))]
        [Tooltip("ロール入力（左）[-1,1]")]
        public FDMiFloat rollL;

        [Tooltip("右ヨークのロール入力パス")]
        public string rollRPath = "~/Input/YokeR/RollInput";

        [FDMiDataPath(nameof(rollRPath))]
        [Tooltip("ロール入力（右）[-1,1]")]
        public FDMiFloat rollR;

        [Tooltip("左ヨークのヨー入力パス")]
        public string yawLPath = "~/Input/YokeL/YawInput";

        [FDMiDataPath(nameof(yawLPath))]
        [Tooltip("ヨー入力（左）[-1,1]")]
        public FDMiFloat yawL;

        [Tooltip("右ヨークのヨー入力パス")]
        public string yawRPath = "~/Input/YokeR/YawInput";

        [FDMiDataPath(nameof(yawRPath))]
        [Tooltip("ヨー入力（右）[-1,1]")]
        public FDMiFloat yawR;

        [Header("レバー")]
        [Tooltip("フラップレバーのパス")]
        public string flapLeverPath = "~/Input/FlapLever/Value";

        [FDMiDataPath(nameof(flapLeverPath))]
        [Tooltip("フラップレバー [0,50]")]
        public FDMiFloat flapLever;

        [Tooltip("スラットレバーのパス")]
        public string slatLeverPath = "~/Input/SlatLever/Value";

        [FDMiDataPath(nameof(slatLeverPath))]
        [Tooltip("スラットレバー [0,35]")]
        public FDMiFloat slatLever;

        [Tooltip("スポイラレバーのパス")]
        public string airbrakeLeverPath = "~/Input/SpoilerLever/Value";

        [FDMiDataPath(nameof(airbrakeLeverPath))]
        [Tooltip("スポイラレバー [0,1.2]")]
        public FDMiFloat airbrakeLever;

        [Header("出力")]
        [Tooltip("エレベータ舵角のパス")]
        public string elevatorPath = "~/Physics/Elevator";

        [FDMiDataPath(nameof(elevatorPath))]
        [Tooltip("エレベータ舵角 [deg]")]
        public FDMiFloat elevator;

        [Tooltip("内側エルロン（左）のパス")]
        public string aileronInLPath = "~/Physics/AileronInL";

        [FDMiDataPath(nameof(aileronInLPath))]
        [Tooltip("内側エルロン舵角（左）[deg]")]
        public FDMiFloat aileronInL;

        [Tooltip("内側エルロン（右）のパス")]
        public string aileronInRPath = "~/Physics/AileronInR";

        [FDMiDataPath(nameof(aileronInRPath))]
        [Tooltip("内側エルロン舵角（右）[deg]")]
        public FDMiFloat aileronInR;

        [Tooltip("外側エルロン（左）のパス")]
        public string aileronOutLPath = "~/Physics/AileronOutL";

        [FDMiDataPath(nameof(aileronOutLPath))]
        [Tooltip("外側エルロン舵角（左）[deg]")]
        public FDMiFloat aileronOutL;

        [Tooltip("外側エルロン（右）のパス")]
        public string aileronOutRPath = "~/Physics/AileronOutR";

        [FDMiDataPath(nameof(aileronOutRPath))]
        [Tooltip("外側エルロン舵角（右）[deg]")]
        public FDMiFloat aileronOutR;

        [Tooltip("ラダー舵角のパス")]
        public string rudderPath = "~/Physics/Rudder";

        [FDMiDataPath(nameof(rudderPath))]
        [Tooltip("ラダー舵角 [deg]")]
        public FDMiFloat rudder;

        [Tooltip("フラップ舵角のパス")]
        public string flapsPath = "~/Physics/Flaps";

        [FDMiDataPath(nameof(flapsPath))]
        [Tooltip("フラップ舵角 [deg]")]
        public FDMiFloat flaps;

        [Tooltip("スラット舵角のパス")]
        public string slatPath = "~/Physics/Slat";

        [FDMiDataPath(nameof(slatPath))]
        [Tooltip("スラット舵角 [deg]")]
        public FDMiFloat slat;

        [Tooltip("スポイラ（左）のパス")]
        public string spoilerLPath = "~/Physics/SpoilerL";

        [FDMiDataPath(nameof(spoilerLPath))]
        [Tooltip("スポイラ舵角（左）[deg]")]
        public FDMiFloat spoilerL;

        [Tooltip("スポイラ（右）のパス")]
        public string spoilerRPath = "~/Physics/SpoilerR";

        [FDMiDataPath(nameof(spoilerRPath))]
        [Tooltip("スポイラ舵角（右）[deg]")]
        public FDMiFloat spoilerR;

        [Header("舵角")]
        [Tooltip("エレベータの最大上げ舵 [deg]")]
        public float elevatorUpDeg = 30f;
        [Tooltip("エレベータの最大下げ舵 [deg]")]
        public float elevatorDownDeg = 15f;
        [Tooltip("内側エルロンの最大上げ舵 [deg]")]
        public float aileronInUpDeg = 23f;
        [Tooltip("内側エルロンの最大下げ舵 [deg]")]
        public float aileronInDownDeg = 19f;
        [Tooltip("外側エルロンの最大上げ舵 [deg]")]
        public float aileronOutUpDeg = 15f;
        [Tooltip("外側エルロンの最大下げ舵 [deg]")]
        public float aileronOutDownDeg = 12f;
        [Tooltip("ラダーの最大舵角 [deg]")]
        public float rudderDeg = 30f;

        [Header("レバー変換")]
        [Tooltip("レバー位置 → フラップ舵角のカーブ")]
        public AnimationCurve flapCurve;
        [Tooltip("レバー位置 → スラット舵角のカーブ")]
        public AnimationCurve slatCurve;
        [Tooltip("レバー位置 → スポイラ舵角のカーブ")]
        public AnimationCurve airbrakeCurve;

        [Header("作動速度")]
        [Tooltip("フラップの作動速度 [deg/s]")]
        public float flapRateDeg = 2f;
        [Tooltip("スラットの作動速度 [deg/s]")]
        public float slatRateDeg = 2f;
        [Tooltip("スポイラの作動速度 [deg/s]")]
        public float airbrakeRateDeg = 30f;

        [Header("スポイラ")]
        [Tooltip("スポイラの最大舵角 [deg]")]
        public float spoilerMaxDeg = 60f;

        float[] _pitchL;
        float[] _pitchR;
        float[] _rollL;
        float[] _rollR;
        float[] _yawL;
        float[] _yawR;
        float[] _flapLever;
        float[] _slatLever;
        float[] _airbrakeLever;
        float[] _elevator;
        float[] _aileronInL;
        float[] _aileronInR;
        float[] _aileronOutL;
        float[] _aileronOutR;
        float[] _rudder;
        float[] _flaps;
        float[] _slat;
        float[] _spoilerL;
        float[] _spoilerR;

        float _flapDeg;
        float _slatDeg;
        float _airbrakeDeg;

        void Start()
        {
            _pitchL = pitchL.data;
            _pitchR = pitchR.data;
            _rollL = rollL.data;
            _rollR = rollR.data;
            _yawL = yawL.data;
            _yawR = yawR.data;
            _flapLever = flapLever.data;
            _slatLever = slatLever.data;
            _airbrakeLever = airbrakeLever.data;
            _elevator = elevator.data;
            _aileronInL = aileronInL.data;
            _aileronInR = aileronInR.data;
            _aileronOutL = aileronOutL.data;
            _aileronOutR = aileronOutR.data;
            _rudder = rudder.data;
            _flaps = flaps.data;
            _slat = slat.data;
            _spoilerL = spoilerL.data;
            _spoilerR = spoilerR.data;

            _flapDeg = flapCurve.Evaluate(_flapLever[0]);
            _slatDeg = slatCurve.Evaluate(_slatLever[0]);
            _airbrakeDeg = airbrakeCurve.Evaluate(_airbrakeLever[0]);
        }

        void FixedUpdate()
        {
            float pitch = Mathf.Clamp(_pitchL[0] + _pitchR[0], -1f, 1f);
            float roll = Mathf.Clamp(_rollL[0] + _rollR[0], -1f, 1f);
            float yaw = Mathf.Clamp(_yawL[0] + _yawR[0], -1f, 1f);

            float dt = Time.fixedDeltaTime;
            _flapDeg = Mathf.MoveTowards(_flapDeg, flapCurve.Evaluate(_flapLever[0]), flapRateDeg * dt);
            _slatDeg = Mathf.MoveTowards(_slatDeg, slatCurve.Evaluate(_slatLever[0]), slatRateDeg * dt);
            _airbrakeDeg = Mathf.MoveTowards(
                _airbrakeDeg,
                airbrakeCurve.Evaluate(_airbrakeLever[0]),
                airbrakeRateDeg * dt
            );

            _elevator[0] = -Asym(pitch, elevatorUpDeg, elevatorDownDeg);

            _aileronInR[0] = -Asym(roll, aileronInUpDeg, aileronInDownDeg);
            _aileronInL[0] = -Asym(-roll, aileronInUpDeg, aileronInDownDeg);

            if (_flapDeg > 0f || _slatDeg > 0f)
            {
                _aileronOutR[0] = -Asym(roll, aileronOutUpDeg, aileronOutDownDeg);
                _aileronOutL[0] = -Asym(-roll, aileronOutUpDeg, aileronOutDownDeg);
            }
            else
            {
                _aileronOutR[0] = 0f;
                _aileronOutL[0] = 0f;
            }

            float spoileron = roll * spoilerMaxDeg;
            _spoilerR[0] = Mathf.Clamp(_airbrakeDeg + spoileron, 0f, spoilerMaxDeg);
            _spoilerL[0] = Mathf.Clamp(_airbrakeDeg - spoileron, 0f, spoilerMaxDeg);

            _rudder[0] = yaw * rudderDeg;

            _flaps[0] = _flapDeg;
            _slat[0] = _slatDeg;
        }

        // 上げ舵と下げ舵で最大角が違う。正で上げ舵。
        static float Asym(float x, float upDeg, float downDeg)
        {
            if (x > 0f)
                return x * upDeg;
            return x * downDeg;
        }
    }
}
