using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // 縦モード。番号は FMA の表示インデックスそのもの。
    // PitchMode は人間の選択と FD の自動遷移が共有する単一ノードで、FD はこの値に従って制御する。
    public enum FDMiPitchMode
    {
        Cws = 0,
        Vs = 1,
        AltCap = 2,
        AltHold = 3,
        GsCap = 4,
        GsTrk = 5,
    }

    // ピッチ軸の飛行誘導。モードごとの誘導則を持ち、姿勢指令と現在ピッチ角の差だけを出す。
    // Why not 絶対姿勢指令を出す: 読み手（AP と EADI）はどちらも現在姿勢を既に持っており、
    // 差にすると引き算が1箇所で済む（ADR-0032）。
    // Why not AP スイッチを読む: CWS は縦モード 0 で表す。AP スイッチは AP の状態であり、
    // FD のモードを変えると要求モードと実作動モードが分裂する（ADR-0032 改訂）。
    // Why not モードを別ノードで publish する: FMA が読むモードと FD が使うモードが二重になる。
    // PitchMode 1本を人間と FD の両方が書き、FMA はそれを直読みする（ADR-0032 改訂）。
    [DefaultExecutionOrder(-400)]
    public class FDMiPitchFlightDirector : FDMiBehaviour
    {
        [Header("モード")]
        [Tooltip("縦モード。人間の選択と FD の自動遷移が同じノードを書く")]
        public string pitchModeSelectPath = "~/Input/PitchMode";

        [FDMiDataPath(nameof(pitchModeSelectPath))]
        public FDMiInt pitchModeSelect;

        [Header("航法（自系統の受信機と 1:1）")]
        [Tooltip("GS 偏差 [deg]。正=パス上方")]
        public string gsDeviationPath = "~/Avionics/NAV1/GSDev";

        [FDMiDataPath(nameof(gsDeviationPath))]
        public FDMiFloat gsDeviation;

        [Tooltip("航法源（0=なし、1=VOR、2=ILS）")]
        public string navTypePath = "~/Avionics/NAV1/Type";

        [FDMiDataPath(nameof(navTypePath))]
        public FDMiInt navType;

        [Tooltip("航法有効")]
        public string navValidPath = "~/Avionics/NAV1/Valid";

        [FDMiDataPath(nameof(navValidPath))]
        public FDMiBool navValid;

        [Header("指令")]
        [Tooltip("VS 指令 [100 ft/min]")]
        public string vsCommandPath = "~/Input/VS/Value";

        [FDMiDataPath(nameof(vsCommandPath))]
        public FDMiInt vsCommand;

        [Tooltip("高度指令 [ft]")]
        public string altitudeCommandPath = "~/Input/ALT/Value";

        [FDMiDataPath(nameof(altitudeCommandPath))]
        public FDMiInt altitudeCommand;

        [Header("機体状態")]
        [Tooltip("ピッチ角 [deg]。正=機首上げ")]
        public string pitchPath = "~/Physics/Pitch";

        [FDMiDataPath(nameof(pitchPath))]
        public FDMiFloat pitch;

        [Tooltip("高度 [m]")]
        public string altitudePath = "~/Physics/Altitude";

        [FDMiDataPath(nameof(altitudePath))]
        public FDMiFloat altitude;

        [Tooltip("鉛直速度 [m/s]")]
        public string verticalSpeedPath = "~/Physics/VS";

        [FDMiDataPath(nameof(verticalSpeedPath))]
        public FDMiFloat verticalSpeed;

        [Header("出力")]
        [Tooltip("ピッチ姿勢指令と現在ピッチ角の差 [deg]。正=機首上げ方向の補正")]
        public string pitchDeltaPath = "~/Avionics/FD/L/PitchDelta";

        [FDMiDataPath(nameof(pitchDeltaPath))]
        public FDMiFloat pitchDelta;

        [Header("誘導則")]
        [Tooltip("VS 誤差の比例ゲイン [deg/(m/s)/s]")]
        public float vsGainP = 0.8f;

        [Tooltip("VS 誤差の積分ゲイン [deg/(m/s)/s]")]
        public float vsGainI = 0.12f;

        [Tooltip("高度誤差→VS のゲイン [1/s]")]
        public float altGain = 0.05f;

        [Tooltip("ALT CAP の VS 上限 [m/s]")]
        public float vsMax = 5.08f;

        [Tooltip("ALT HOLD と GS CAP 待ちの VS 上限 [m/s]")]
        public float vsHoldMax = 2.54f;

        [Tooltip("VS から ALT CAP へ移る残高度 [m]")]
        public float altAcqBand = 304.8f;

        [Tooltip("ALT CAP から ALT HOLD へ移る残高度 [m]")]
        public float altCaptureBand = 30.48f;

        [Tooltip("GS 誤差の比例ゲイン [deg/deg/s]")]
        public float gsGainP = 0.5f;

        [Tooltip("GS 誤差の積分ゲイン")]
        public float gsGainI = 0.05f;

        [Tooltip("GS 偏差変化率のゲイン [deg/(deg/s)/s]")]
        public float gsGainD = 0.2f;

        [Tooltip("GS 偏差変化率の LPF 時定数 [s]")]
        public float gsRateTau = 0.5f;

        [Tooltip("GS 捕捉の偏差 [deg]")]
        public float gsCaptureDeg = 0.108f;

        [Tooltip("GS TRK を失う偏差 [deg]")]
        public float gsTrackLossDeg = 2f;

        [Tooltip("ピッチ指令の下限 [deg]")]
        public float pitchMinDeg = -10f;

        [Tooltip("ピッチ指令の上限 [deg]")]
        public float pitchMaxDeg = 20f;

        int[] _pitchModeSelect, _vsCommand, _altitudeCommand, _navType;
        float[] _pitch, _altitude, _verticalSpeed, _gsDeviation, _pitchDelta;
        bool[] _navValid;

        FDMiPitchMode _pitchMode;
        int _prevAltCmd;
        float _pitchCmd,
            _vsCmd,
            _vsIntegral,
            _gsIntegral,
            _gsRate,
            _prevGsDev,
            _altRef;

        void Start()
        {
            _pitchModeSelect = pitchModeSelect.data;
            _vsCommand = vsCommand.data;
            _altitudeCommand = altitudeCommand.data;
            _gsDeviation = gsDeviation.data;
            _navType = navType.data;
            _navValid = navValid.data;
            _pitch = pitch.data;
            _altitude = altitude.data;
            _verticalSpeed = verticalSpeed.data;
            _pitchDelta = pitchDelta.data;
            _prevAltCmd = _altitudeCommand[0];
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float pitchNow = _pitch[0];
            float altNow = _altitude[0];
            float vsNow = _verticalSpeed[0];
            float gsDev = _gsDeviation[0];
            int navTypeNow = _navType[0];
            bool navValidNow = _navValid[0];
            float altCmd = (float)_altitudeCommand[0] * 0.3048f;
            float vsSel = (float)_vsCommand[0] * 0.508f;
            bool altCmdChanged = _altitudeCommand[0] != _prevAltCmd;
            bool gsAvailable = navTypeNow == 2 && navValidNow;

            // モードノードは両席の FD が共有する。他席の受信機が捕捉した GsTrk に自席の
            // 受信機が追従できないとき、拒否をノードへ書き戻すと捕捉した席まで巻き戻る。
            // そのときは GS CAP の待ちとして留まり、書き戻さない。
            FDMiPitchMode requested = (FDMiPitchMode)_pitchModeSelect[0];
            if (requested == FDMiPitchMode.GsTrk && !gsAvailable)
                requested = FDMiPitchMode.GsCap;
            if (requested != _pitchMode)
                EnterPitchMode(requested, pitchNow, altNow, altCmd, gsDev);

            UpdatePitchMode(pitchNow, altNow, vsSel, altCmd, altCmdChanged, gsDev, gsAvailable);
            _prevAltCmd = _altitudeCommand[0];

            ComputePitchGuidance(dt, pitchNow, altNow, vsNow, gsDev);

            _pitchCmd = Mathf.Clamp(_pitchCmd, pitchMinDeg, pitchMaxDeg);
            _pitchDelta[0] = _pitchCmd - pitchNow;
        }

        // 人間がモードを選び直したときだけ、そのモードの初期状態へ入る。
        // FD 自身の遷移は SetPitchMode でノードを書き換え、遷移地点で必要な積分器だけを
        // リセットする。_pitchCmd を現在姿勢へ戻すのは GsCap→GsTrk の捕捉時だけ。
        void EnterPitchMode(FDMiPitchMode mode, float pitchNow, float altNow, float altCmd, float gsDev)
        {
            _pitchMode = mode;
            _pitchCmd = pitchNow;
            _vsIntegral = 0f;
            _gsIntegral = 0f;
            _gsRate = 0f;
            _prevGsDev = gsDev;

            switch (mode)
            {
                case FDMiPitchMode.Cws:
                case FDMiPitchMode.Vs:
                    break;
                case FDMiPitchMode.AltCap:
                    _altRef = altCmd;
                    break;
                case FDMiPitchMode.AltHold:
                case FDMiPitchMode.GsCap:
                case FDMiPitchMode.GsTrk:
                    _altRef = altNow;
                    break;
            }
        }

        // モードノードは人間と FD の共有状態。同じ値なら書かない（コールバックの無駄打ちを避ける）。
        void SetPitchMode(FDMiPitchMode mode)
        {
            if (_pitchMode == mode)
                return;
            _pitchMode = mode;
            if (_pitchModeSelect[0] != (int)mode)
                pitchModeSelect.Data = (int)mode;
        }

        void UpdatePitchMode(
            float pitchNow,
            float altNow,
            float vsSel,
            float altCmd,
            bool altCmdChanged,
            float gsDev,
            bool gsAvailable
        )
        {
            switch (_pitchMode)
            {
                case FDMiPitchMode.Cws:
                    return;
                case FDMiPitchMode.AltHold:
                    // ALT HOLD は _altRef を保持し、選択高度の変更は追わない。
                    // 高度を変えるときは ALT CAP を選ぶ。
                    return;
                case FDMiPitchMode.Vs:
                    UpdateVs(altNow, vsSel, altCmd);
                    return;
                case FDMiPitchMode.AltCap:
                    UpdateAltCap(altNow, altCmd, altCmdChanged);
                    return;
                case FDMiPitchMode.GsCap:
                    UpdateGsCap(pitchNow, gsDev, gsAvailable);
                    return;
                case FDMiPitchMode.GsTrk:
                    UpdateGsTrk(altNow, gsDev, gsAvailable);
                    return;
            }
        }

        // VS は選択 VS を保持する。選択高度が進行方向にあり捕捉帯へ入ったら、
        // ノードを ALT CAP / ALT HOLD へ書き換える。armed を別状態に持たない。
        void UpdateVs(float altNow, float vsSel, float altCmd)
        {
            _vsCmd = vsSel;
            float altDiff = altCmd - altNow;
            if (vsSel == 0f || altDiff * vsSel <= 0f)
                return;

            if (Mathf.Abs(altDiff) <= altCaptureBand)
            {
                _altRef = altCmd;
                _vsIntegral = 0f;
                SetPitchMode(FDMiPitchMode.AltHold);
                return;
            }
            if (Mathf.Abs(altDiff) <= altAcqBand)
            {
                _altRef = altCmd;
                _vsIntegral = 0f;
                SetPitchMode(FDMiPitchMode.AltCap);
            }
        }

        // ALT CAP は選択高度を捕捉する。高度指令が変わったら捕捉先も変える。
        void UpdateAltCap(float altNow, float altCmd, bool altCmdChanged)
        {
            if (altCmdChanged)
            {
                _altRef = altCmd;
                _vsIntegral = 0f;
            }
            if (Mathf.Abs(_altRef - altNow) <= altCaptureBand)
            {
                _altRef = altCmd;
                _vsIntegral = 0f;
                SetPitchMode(FDMiPitchMode.AltHold);
            }
        }

        // GS CAP は捕捉待ち。現在高度の ALT HOLD で待ち、ビームに入ったら GS TRK へ。
        void UpdateGsCap(float pitchNow, float gsDev, bool gsAvailable)
        {
            float prevDev = _prevGsDev;
            _prevGsDev = gsDev;
            if (!gsAvailable)
                return;
            if (Mathf.Abs(gsDev) < gsCaptureDeg && Mathf.Abs(gsDev) <= Mathf.Abs(prevDev))
            {
                _pitchCmd = pitchNow;
                _gsIntegral = 0f;
                _gsRate = 0f;
                SetPitchMode(FDMiPitchMode.GsTrk);
            }
        }

        void UpdateGsTrk(float altNow, float gsDev, bool gsAvailable)
        {
            if (!gsAvailable || Mathf.Abs(gsDev) > gsTrackLossDeg)
            {
                _altRef = altNow;
                _vsIntegral = 0f;
                _gsIntegral = 0f;
                _prevGsDev = gsDev;
                SetPitchMode(FDMiPitchMode.AltHold);
            }
        }

        void ComputePitchGuidance(float dt, float pitchNow, float altNow, float vsNow, float gsDev)
        {
            switch (_pitchMode)
            {
                case FDMiPitchMode.Cws:
                    _pitchCmd = pitchNow;
                    return;
                case FDMiPitchMode.Vs:
                    // _vsCmd は UpdateVs が選択値を書いている。
                    ApplyVsLoop(dt, vsNow);
                    return;
                case FDMiPitchMode.AltCap:
                    _vsCmd = Mathf.Clamp(altGain * (_altRef - altNow), -vsMax, vsMax);
                    ApplyVsLoop(dt, vsNow);
                    return;
                case FDMiPitchMode.AltHold:
                case FDMiPitchMode.GsCap:
                    _vsCmd = Mathf.Clamp(altGain * (_altRef - altNow), -vsHoldMax, vsHoldMax);
                    ApplyVsLoop(dt, vsNow);
                    return;
                case FDMiPitchMode.GsTrk:
                    ApplyGsLoop(dt, gsDev);
                    return;
            }
        }

        void ApplyVsLoop(float dt, float vsNow)
        {
            float vsErr = _vsCmd - vsNow;
            _pitchCmd += (vsGainP * vsErr + vsGainI * _vsIntegral) * dt;
            _vsIntegral += vsErr * dt;
            _vsIntegral = Mathf.Clamp(_vsIntegral, -5f, 5f);
        }

        void ApplyGsLoop(float dt, float gsDev)
        {
            float rawRate = (gsDev - _prevGsDev) / dt;
            _gsRate += (rawRate - _gsRate) * (dt / (gsRateTau + dt));
            _prevGsDev = gsDev;

            // 正の GSDev はパス上方。機首下げ方向（負）へ比例・微分・積分する。
            float gsErr = -gsDev;
            _pitchCmd += (gsGainP * gsErr - gsGainD * _gsRate + gsGainI * _gsIntegral) * dt;
            _gsIntegral += gsErr * dt;
            _gsIntegral = Mathf.Clamp(_gsIntegral, -10f, 10f);
        }
    }
}
