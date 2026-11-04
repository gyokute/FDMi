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

        [Header("自動操縦")]
        [Tooltip("AP のピッチバイアスのパス")]
        public string apPitchPath = "~/Input/AutoPilot/AP2/Pitch";

        [FDMiDataPath(nameof(apPitchPath))]
        [Tooltip("AP のピッチバイアス [-1,1]")]
        public FDMiFloat apPitch;

        [Tooltip("AP のロールバイアスのパス")]
        public string apRollPath = "~/Input/AutoPilot/AP2/Roll";

        [FDMiDataPath(nameof(apRollPath))]
        [Tooltip("AP のロールバイアス [-1,1]")]
        public FDMiFloat apRoll;

        [Tooltip("ヨーダンパのラダーバイアスのパス")]
        public string yawDamperPath = "~/Input/YawDamper/Rudder";

        [FDMiDataPath(nameof(yawDamperPath))]
        [Tooltip("ヨーダンパのラダーバイアス [-1,1]")]
        public FDMiFloat yawDamper;

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

        [Tooltip("内側スポイラ（左）のパス")]
        public string spoilerInLPath = "~/Physics/SpoilerInL";

        [FDMiDataPath(nameof(spoilerInLPath))]
        [Tooltip("内側スポイラ舵角（左）[deg]")]
        public FDMiFloat spoilerInL;

        [Tooltip("内側スポイラ（右）のパス")]
        public string spoilerInRPath = "~/Physics/SpoilerInR";

        [FDMiDataPath(nameof(spoilerInRPath))]
        [Tooltip("内側スポイラ舵角（右）[deg]")]
        public FDMiFloat spoilerInR;

        [Tooltip("外側スポイラ（左）のパス")]
        public string spoilerOutLPath = "~/Physics/SpoilerOutL";

        [FDMiDataPath(nameof(spoilerOutLPath))]
        [Tooltip("外側スポイラ舵角（左）[deg]")]
        public FDMiFloat spoilerOutL;

        [Tooltip("外側スポイラ（右）のパス")]
        public string spoilerOutRPath = "~/Physics/SpoilerOutR";

        [FDMiDataPath(nameof(spoilerOutRPath))]
        [Tooltip("外側スポイラ舵角（右）[deg]")]
        public FDMiFloat spoilerOutR;

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
        [Tooltip("レバー位置 → 内側スポイラ舵角のカーブ")]
        public AnimationCurve airbrakeInCurve;
        [Tooltip("レバー位置 → 外側スポイラ舵角のカーブ")]
        public AnimationCurve airbrakeOutCurve;

        [Header("作動速度")]
        [Tooltip("フラップの作動速度 [deg/s]")]
        public float flapRateDeg = 2f;
        [Tooltip("スラットの作動速度 [deg/s]")]
        public float slatRateDeg = 2f;
        [Tooltip("内側スポイラの作動速度 [deg/s]")]
        public float airbrakeInRateDeg = 30f;
        [Tooltip("外側スポイラの作動速度 [deg/s]")]
        public float airbrakeOutRateDeg = 30f;

        [Header("スポイラ")]
        [Tooltip("内側スポイラの最大舵角 [deg]")]
        public float spoilerInMaxDeg = 60f;
        [Tooltip("外側スポイラの最大舵角 [deg]")]
        public float spoilerOutMaxDeg = 60f;

        float[] _pitchL;
        float[] _pitchR;
        float[] _rollL;
        float[] _rollR;
        float[] _yawL;
        float[] _yawR;
        float[] _apPitch;
        float[] _apRoll;
        float[] _yawDamper;
        bool _hasApPitch;
        bool _hasApRoll;
        bool _hasYawDamper;
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
        float[] _spoilerInL;
        float[] _spoilerInR;
        float[] _spoilerOutL;
        float[] _spoilerOutR;

        float _flapDeg;
        float _slatDeg;
        float _airbrakeInDeg;
        float _airbrakeOutDeg;

        void Start()
        {
            _pitchL = pitchL.data;
            _pitchR = pitchR.data;
            _rollL = rollL.data;
            _rollR = rollR.data;
            _yawL = yawL.data;
            _yawR = yawR.data;
            if (apPitch)
            {
                _apPitch = apPitch.data;
                _hasApPitch = true;
            }
            if (apRoll)
            {
                _apRoll = apRoll.data;
                _hasApRoll = true;
            }
            if (yawDamper)
            {
                _yawDamper = yawDamper.data;
                _hasYawDamper = true;
            }
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
            _spoilerInL = spoilerInL.data;
            _spoilerInR = spoilerInR.data;
            _spoilerOutL = spoilerOutL.data;
            _spoilerOutR = spoilerOutR.data;

            _flapDeg = flapCurve.Evaluate(_flapLever[0]);
            _slatDeg = slatCurve.Evaluate(_slatLever[0]);
            _airbrakeInDeg = airbrakeInCurve.Evaluate(_airbrakeLever[0]);
            _airbrakeOutDeg = airbrakeOutCurve.Evaluate(_airbrakeLever[0]);
        }

        void FixedUpdate()
        {
            float apPitchValue = 0f;
            float apRollValue = 0f;
            float yawDamperValue = 0f;
            if (_hasApPitch)
                apPitchValue = _apPitch[0];
            if (_hasApRoll)
                apRollValue = _apRoll[0];
            if (_hasYawDamper)
                yawDamperValue = _yawDamper[0];

            float pitch = Mathf.Clamp(_pitchL[0] + _pitchR[0] + apPitchValue, -1f, 1f);
            float roll = Mathf.Clamp(_rollL[0] + _rollR[0] + apRollValue, -1f, 1f);
            float yaw = Mathf.Clamp(_yawL[0] + _yawR[0] + yawDamperValue, -1f, 1f);

            float dt = Time.fixedDeltaTime;
            _flapDeg = Mathf.MoveTowards(_flapDeg, flapCurve.Evaluate(_flapLever[0]), flapRateDeg * dt);
            _slatDeg = Mathf.MoveTowards(_slatDeg, slatCurve.Evaluate(_slatLever[0]), slatRateDeg * dt);
            _airbrakeInDeg = Mathf.MoveTowards(
                _airbrakeInDeg,
                airbrakeInCurve.Evaluate(_airbrakeLever[0]),
                airbrakeInRateDeg * dt
            );
            _airbrakeOutDeg = Mathf.MoveTowards(
                _airbrakeOutDeg,
                airbrakeOutCurve.Evaluate(_airbrakeLever[0]),
                airbrakeOutRateDeg * dt
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

            // DC-10 のスポイラはインボードもアウトボードもスポイレロンとスピードブレーキを兼ねる
            // (DC-10 FCOM Ch.9)。地上スポイラは扱わないので、両群に同じ則を適用する。
            float spoileronIn = roll * spoilerInMaxDeg;
            _spoilerInR[0] = Mathf.Clamp(_airbrakeInDeg + spoileronIn, 0f, spoilerInMaxDeg);
            _spoilerInL[0] = Mathf.Clamp(_airbrakeInDeg - spoileronIn, 0f, spoilerInMaxDeg);

            float spoileronOut = roll * spoilerOutMaxDeg;
            _spoilerOutR[0] = Mathf.Clamp(_airbrakeOutDeg + spoileronOut, 0f, spoilerOutMaxDeg);
            _spoilerOutL[0] = Mathf.Clamp(_airbrakeOutDeg - spoileronOut, 0f, spoilerOutMaxDeg);

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
