using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // 縦モード。番号は FMA の表示インデックスそのもの。Cws が 0 なのは「モードが無い」状態を
    // 持たないため。FD OFF はモードではなくスイッチであり、表示器と AP が持つ（ADR-0031）。
    public enum FDMiPitchMode
    {
        Cws = 0,
        Vs = 1,
        AltCap = 2,
        AltHold = 3,
        GsCap = 4,
        GsTrk = 5,
    }

    // ピッチ軸の飛行誘導。姿勢指令と現在ピッチ角の差だけを出す。
    // Why not 絶対姿勢指令を出す: 読み手（AP と EADI）はどちらも現在姿勢を既に持っており、
    // 差にすると引き算が1箇所で済む（ADR-0032）。
    // Why not 横の誘導と同じクラス: 軸をまたぐ状態（セレクタ変更時の一括リセット、APP 選択に
    // よる GS の armed）が残り、横の FD が無い席で縦が動かなくなる（ADR-0032）。
    // Why not 横モード選択子を読む: GS の armed は縦の選択子と航法源だけで決まる。読むと横の
    // FD と AP スイッチが縦の前提になり、完全分離が崩れる。
    //
    // FD は FD スイッチを読まない。スイッチ OFF は「誘導を止める」ではなく「表示と AP を止める」
    // であり、FD 自身は常に計算し続ける。Why not スイッチ OFF を Off モードにする: モードに
    // 「無い」を持たせると Cws と衝突し、FMA のインデックスに穴が空く。
    //
    // 受信機とは席 1:1 で、自系統の NAV だけを読む（ADR-0032 で 1:1 を「受信機 : 席」に読み替え）。
    [DefaultExecutionOrder(-400)]
    public class FDMiPitchFlightDirector : FDMiBehaviour
    {
        [Header("スイッチ")]
        [Tooltip("AP スイッチ（0=OFF、1=CWS、2=CMD）。CWS では差分 0 を出す")]
        public string apSwitchPath = "~/Input/AP1SW/Value";

        [FDMiDataPath(nameof(apSwitchPath))]
        public FDMiInt apSwitch;

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

        [Header("モード選択")]
        [Tooltip("縦モード選択（0=VS、1=ALT、2以上=GS/APP）")]
        public string pitchModeSelectPath = "~/Input/PitchMode";

        [FDMiDataPath(nameof(pitchModeSelectPath))]
        public FDMiInt pitchModeSelect;

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

        [Tooltip("ALT HOLD/CAP の VS 上限 [m/s]")]
        public float vsMax = 5.08f;

        [Tooltip("ALT CAP を ALT HOLD に切り替える残高度 [m]")]
        public float altCaptureBand = 30.48f;

        [Tooltip("GS 誤差の比例ゲイン [deg/deg/s]")]
        public float gsGainP = 0.5f;

        [Tooltip("GS 誤差の積分ゲイン")]
        public float gsGainI = 0.05f;

        [Tooltip("GS 捕捉の偏差 [deg]")]
        public float gsCaptureDeg = 0.108f;

        [Tooltip("ピッチ指令の下限 [deg]")]
        public float pitchMinDeg = -10f;

        [Tooltip("ピッチ指令の上限 [deg]")]
        public float pitchMaxDeg = 20f;

        int[] _apSwitch, _vsCommand, _altitudeCommand, _pitchModeSelect, _navType;
        float[] _pitch, _altitude, _verticalSpeed, _gsDeviation, _pitchDelta;
        bool[] _navValid;

        FDMiPitchMode _pitchMode;
        int _pitchArmed,
            _prevPitchSel = -1,
            _prevAltCmd;
        float _pitchCmd,
            _vsCmd,
            _vsIntegral,
            _gsIntegral,
            _altRef;
        bool _initialized;

        void Start()
        {
            _apSwitch = apSwitch.data;
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
            // CWS では差分 0 を出す。姿勢の保持と手放しの捕捉は AP のサーボが担う（ADR-0032）。
            // 指令は現在姿勢から再シードさせるので、CWS から CMD に戻っても舵が跳ばない。
            if (_apSwitch[0] == 1)
            {
                _pitchDelta[0] = 0f;
                _initialized = false;
                return;
            }

            float dt = Time.fixedDeltaTime;
            float pitchNow = _pitch[0];
            float altNow = _altitude[0];
            float vsNow = _verticalSpeed[0];
            float gsDev = _gsDeviation[0];
            int navTypeNow = _navType[0];
            bool navValidNow = _navValid[0];

            int pitchSel = _pitchModeSelect[0];
            float altCmd = (float)_altitudeCommand[0] * 0.3048f;
            float vsSel = (float)_vsCommand[0] * 0.508f;
            bool altCmdChanged = _altitudeCommand[0] != _prevAltCmd;
            bool pitchSelChanged = !_initialized || pitchSel != _prevPitchSel;

            if (pitchSelChanged)
            {
                _pitchCmd = pitchNow;
                _vsIntegral = 0f;
                _gsIntegral = 0f;
                _vsCmd = vsSel;
                _altRef = altCmd;
                _pitchArmed = 0;
                _initialized = true;

                if (pitchSel == 0)
                {
                    _pitchMode = FDMiPitchMode.Vs;
                }
                else if (pitchSel == 1)
                {
                    _pitchMode =
                        Mathf.Abs(altCmd - altNow) > altCaptureBand ? FDMiPitchMode.AltCap : FDMiPitchMode.AltHold;
                }
                else
                {
                    // GS 捕捉までは現在高度の ALT HOLD で待つ。GS は armed として出す。
                    _pitchMode = FDMiPitchMode.AltHold;
                    _altRef = altNow;
                }
            }

            _prevPitchSel = pitchSel;

            // APP 選択かつ ILS のときだけ GS を armed にする。縦の選択子 2 以上が APP の要求で
            // あり、横の選択子は読まない（ADR-0032）。
            bool gsAvailable = navTypeNow == 2 && navValidNow;
            if (_pitchArmed == (int)FDMiPitchMode.GsCap && !gsAvailable)
                _pitchArmed = 0;
            if (pitchSel >= 2 && gsAvailable && _pitchMode != FDMiPitchMode.GsTrk && _pitchArmed == 0)
                _pitchArmed = (int)FDMiPitchMode.GsCap;

            // 縦モード。選択子の変更時だけ初期化し、捕捉・遷移は毎tick評価する。
            if (pitchSel == 0)
            {
                _vsCmd = vsSel;
                // 選択高度が進行方向にあり、まだ捕捉帯の外なら ALT CAP を armed にする。
                if (Mathf.Abs(altCmd - altNow) > altCaptureBand && (altCmd - altNow) * vsSel > 0f)
                {
                    _pitchArmed = (int)FDMiPitchMode.AltCap;
                }
            }
            else if (pitchSel == 1 && altCmdChanged)
            {
                _altRef = altCmd;
                _pitchMode = Mathf.Abs(altCmd - altNow) > altCaptureBand ? FDMiPitchMode.AltCap : FDMiPitchMode.AltHold;
                _pitchArmed = 0;
            }
            if (altCmdChanged)
                _prevAltCmd = _altitudeCommand[0];

            // ALT CAP の捕捉。残高度が帯に入ったら ALT HOLD へ。
            if (_pitchMode == FDMiPitchMode.AltCap && Mathf.Abs(_altRef - altNow) <= altCaptureBand)
            {
                _pitchMode = FDMiPitchMode.AltHold;
                _pitchArmed = 0;
                _vsIntegral = 0f;
            }
            if (_pitchArmed == (int)FDMiPitchMode.AltCap && Mathf.Abs(altCmd - altNow) <= altCaptureBand)
            {
                _pitchMode = FDMiPitchMode.AltHold;
                _altRef = altCmd;
                _pitchArmed = 0;
                _vsIntegral = 0f;
            }

            // GS 捕捉・喪失。
            if (_pitchArmed == (int)FDMiPitchMode.GsCap)
            {
                if (gsAvailable && Mathf.Abs(gsDev) < gsCaptureDeg)
                {
                    _pitchMode = FDMiPitchMode.GsTrk;
                    _pitchArmed = 0;
                    _pitchCmd = pitchNow;
                    _gsIntegral = 0f;
                }
            }
            else if (_pitchMode == FDMiPitchMode.GsTrk)
            {
                if (!gsAvailable || Mathf.Abs(gsDev) > 2f)
                {
                    _pitchMode = FDMiPitchMode.AltHold;
                    _altRef = altNow;
                    _pitchArmed = 0;
                    _vsIntegral = 0f;
                }
            }

            if (_pitchMode == FDMiPitchMode.GsTrk)
            {
                // 正の GSDev はパス上方。機首下げ方向（負）へ積分する。
                float gsErr = -gsDev;
                _pitchCmd += (gsGainP * gsErr + gsGainI * _gsIntegral) * dt;
                _gsIntegral += gsErr * dt;
                _gsIntegral = Mathf.Clamp(_gsIntegral, -10f, 10f);
            }
            else
            {
                if (
                    _pitchMode == FDMiPitchMode.AltHold
                    || _pitchMode == FDMiPitchMode.AltCap
                    || _pitchMode == FDMiPitchMode.GsCap
                )
                    _vsCmd = Mathf.Clamp(altGain * (_altRef - altNow), -vsMax, vsMax);

                float vsErr = _vsCmd - vsNow;
                _pitchCmd += (vsGainP * vsErr + vsGainI * _vsIntegral) * dt;
                _vsIntegral += vsErr * dt;
                _vsIntegral = Mathf.Clamp(_vsIntegral, -5f, 5f);
            }

            _pitchCmd = Mathf.Clamp(_pitchCmd, pitchMinDeg, pitchMaxDeg);
            _pitchDelta[0] = _pitchCmd - pitchNow;
        }
    }
}
