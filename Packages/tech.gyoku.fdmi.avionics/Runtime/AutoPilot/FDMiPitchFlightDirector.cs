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

        [Tooltip("真対気速度 [m/s]")]
        public string tasPath = "~/Physics/TAS";

        [FDMiDataPath(nameof(tasPath))]
        public FDMiFloat tas;

        [Header("出力")]
        [Tooltip("ピッチ姿勢指令と現在ピッチ角の差 [deg]。正=機首上げ方向の補正")]
        public string pitchDeltaPath = "~/Avionics/FD/L/PitchDelta";

        [FDMiDataPath(nameof(pitchDeltaPath))]
        public FDMiFloat pitchDelta;

        [Header("誘導則")]
        // 3DOF 検証で旧 0.1 は 4000 fpm の 90% 到達が 150/250/350 kt で 13.6/10.1/9.6 s。
        // 0.2 + pitchCmdRateMax 2.0 で 9.0/6.3/5.5 s（約 1.5〜1.8 倍速）。
        // 一時的なオーバーシュートは約 325 fpm まで許容し、定常誤差は 0 にする。
        [Tooltip("VS 誤差の比例ゲイン [deg/(m/s)/s]")]
        public float vsGainP = 0.2f;

        // 外側の _pitchCmd 積分が定常分を担うため、内側のレート積分は既定で切る。
        // 降下反転時の windup を避ける（ADR-0033 の rateCum バイアスは外側で吸収する）。
        [Tooltip("VS 誤差の積分ゲイン [deg/(m/s)/s]。0 で内側積分なし")]
        public float vsGainI = 0f;

        [Tooltip("VS ゲインを正規化する基準 TAS [m/s]")]
        public float tasRef = 150f;

        [Tooltip("TAS の下限 [m/s]。0 除算を避ける")]
        public float tasMin = 30f;

        [Tooltip("ピッチ指令の変化率上限 [deg/s]")]
        public float pitchCmdRateMax = 2f;

        // 500 ft からの捕捉を高速化する。0.08 + vsMax 5.08 では 250 kt で
        // ±50 ft 整定に 23 s かかった。0.2 + vsMax 18 では 7.8 s（約 1/3）。
        // 150 kt は 7.7 s（約 1/2.3）で、30 ft の一時オーバーシュートに収まる。
        [Tooltip("高度誤差→VS のゲイン [1/s]")]
        public float altGain = 0.2f;

        // 5.08 m/s（1000 fpm）では 4000 fpm の上昇から早期に減速しすぎて
        // 捕捉が遅かった。18 m/s（3543 fpm）で初期の上昇をほぼ維持し、
        // 残高度が減ってから減速する。20.32 m/s（4000 fpm）では 150 kt の
        // オーバーシュートが 78 ft まで増えるため、18 を上限にする。
        [Tooltip("ALT CAP の VS 上限 [m/s]")]
        public float vsMax = 18f;

        [Tooltip("ALT HOLD と GS CAP 待ちの VS 上限 [m/s]")]
        public float vsHoldMax = 2.54f;

        [Tooltip("VS から ALT CAP へ移る残高度 [m]。500 ft")]
        public float altAcqBand = 152.4f;

        [Tooltip("ALT CAP から ALT HOLD へ移る残高度 [m]")]
        public float altCaptureBand = 30.48f;

        // GS 偏差は 1/x で感度が上がる幾何量で、_pitchCmd はその積分器になる。
        // 旧 0.5 / 0.05 / 0.2 は 350 kt で発散した（最大 51°、定常振幅 29°）。
        // 比例を下げ、微分を上げ、積分を小さくして 150–350 kt で安定させる。
        // 定常 GS 偏差は ±0.1° 以内、捕捉中の最大偏差は 0.45° 以内。
        [Tooltip("GS 誤差の比例ゲイン [deg/deg/s]")]
        public float gsGainP = 0.25f;

        // 0.05 は水平飛行からパスへ入る間の積分が巻き上がり、定常偏差を残す。
        // 0.001 でオフセットを消しつつ、捕捉時の振動を出さない。
        [Tooltip("GS 誤差の積分ゲイン")]
        public float gsGainI = 0.001f;

        // GS 偏差の変化率に対する強い制動。gsRateTau で高周波を落としてから使う。
        [Tooltip("GS 偏差変化率のゲイン [deg/(deg/s)/s]")]
        public float gsGainD = 8f;

        [Tooltip("GS 偏差変化率の LPF 時定数 [s]")]
        public float gsRateTau = 0.3f;

        [Tooltip("GS 捕捉の偏差 [deg]")]
        public float gsCaptureDeg = 0.108f;

        [Tooltip("GS TRK を失う偏差 [deg]")]
        public float gsTrackLossDeg = 2f;

        // 4000 fpm を 150 kt で成立させるには 20° では足りない。
        // 実際の機体姿勢は空力が制限するため、指令側は余裕を持たせる。
        [Tooltip("ピッチ指令の下限 [deg]")]
        public float pitchMinDeg = -15f;

        [Tooltip("ピッチ指令の上限 [deg]")]
        public float pitchMaxDeg = 30f;

        int[] _pitchModeSelect, _vsCommand, _altitudeCommand, _navType;
        float[] _pitch, _altitude, _verticalSpeed, _tas, _gsDeviation, _pitchDelta;
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
            _tas = tas.data;
            _pitchDelta = pitchDelta.data;
            _prevAltCmd = _altitudeCommand[0];
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float pitchNow = _pitch[0];
            float altNow = _altitude[0];
            float vsNow = _verticalSpeed[0];
            float tasNow = _tas[0];
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

            ComputePitchGuidance(dt, pitchNow, altNow, vsNow, tasNow, gsDev);

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

        void ComputePitchGuidance(
            float dt,
            float pitchNow,
            float altNow,
            float vsNow,
            float tasNow,
            float gsDev
        )
        {
            switch (_pitchMode)
            {
                case FDMiPitchMode.Cws:
                    _pitchCmd = pitchNow;
                    return;
                case FDMiPitchMode.Vs:
                    // _vsCmd は UpdateVs が選択値を書いている。
                    ApplyVsLoop(dt, vsNow, tasNow);
                    return;
                case FDMiPitchMode.AltCap:
                    _vsCmd = Mathf.Clamp(altGain * (_altRef - altNow), -vsMax, vsMax);
                    ApplyVsLoop(dt, vsNow, tasNow);
                    return;
                case FDMiPitchMode.AltHold:
                case FDMiPitchMode.GsCap:
                    _vsCmd = Mathf.Clamp(altGain * (_altRef - altNow), -vsHoldMax, vsHoldMax);
                    ApplyVsLoop(dt, vsNow, tasNow);
                    return;
                case FDMiPitchMode.GsTrk:
                    ApplyGsLoop(dt, gsDev);
                    return;
            }
        }

        // VS のプラントゲインは TAS に比例する。ゲインを TAS で正規化しないと、
        // 250 kt で合わせた値が 150 kt で鈍く、350 kt で振動する。
        // Why not 高度・VS 誤差から直接ピッチ角を出す: 既存の外側積分（_pitchCmd）を
        // 残し、AP の rateCum バイアスを吸収する役割を1箇所に閉じる。
        // 指令の変化率を制限するのは、内側 AP の応答より速く目標を動かさないため。
        void ApplyVsLoop(float dt, float vsNow, float tasNow)
        {
            float vsErr = _vsCmd - vsNow;
            float tasScale = tasRef / Mathf.Max(tasNow, tasMin);
            float rate = (vsGainP * tasScale) * vsErr + (vsGainI * tasScale) * _vsIntegral;
            rate = Mathf.Clamp(rate, -pitchCmdRateMax, pitchCmdRateMax);

            float next = _pitchCmd + rate * dt;
            // 指令が上限・下限に張り付いている間は積分を止める。
            // 降下側で _pitchCmd が下限に達したまま積分が回り、復帰時に
            // オーバーシュートするのを防ぐ。
            bool saturating = (next > pitchMaxDeg && rate > 0f) || (next < pitchMinDeg && rate < 0f);
            if (!saturating)
                _vsIntegral += vsErr * dt;
            _vsIntegral = Mathf.Clamp(_vsIntegral, -5f, 5f);
            _pitchCmd = Mathf.Clamp(next, pitchMinDeg, pitchMaxDeg);
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
