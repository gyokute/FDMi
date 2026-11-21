using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // FD の縦モード。番号は FMA の表示インデックスそのもの。
    // Cws が 0 なのは「モードが無い」状態を持たないため。FD OFF はモードではなくスイッチであり、
    // 表示器と AP が持つ（ADR-0031）。
    public enum FDMiPitchMode
    {
        Cws = 0,
        Vs = 1,
        AltCap = 2,
        AltHold = 3,
        GsCap = 4,
        GsTrk = 5,
    }

    // FD の横モード。
    public enum FDMiRollMode
    {
        Cws = 0,
        HdgHold = 1,
        HdgSel = 2,
        VorCap = 3,
        VorTrk = 4,
        LocCap = 5,
        LocTrk = 6,
    }

    // モードと姿勢指令を持つ飛行誘導。AP から独立して動き、AP OFF でも FD バー用の指令を出し続ける。
    // Why not AP と同じクラス: モード遷移の書き手が AP になると、FD と AP の engage 状態が混ざり、
    // main のようにモード書込が同フレームで再入する。
    // Why not 姿勢指令を AP 内で作る: FD バーと AP の追従目標が二重実装になる。
    //
    // FD は FD スイッチを読まない。スイッチ OFF は「誘導を止める」ではなく「表示と AP を止める」で
    // あり、FD 自身は常に計算し続ける。Why not スイッチ OFF を Off モードにする: モードに
    // 「無い」を持たせると Cws と衝突し、FMA のインデックスに穴が空く。
    //
    // 受信機とは 1:1 で、自系統の NAV だけを読む。系統の選択は表示器の仕事であり、誘導は
    // 受信機ごとに固定する（ADR-0031）。
    // Why not FD が FDCOM で系統を選ぶ: 誘導と表示の両方が同じ選択子に依存し、表示だけを
    // 切り替える要求と両立しない。
    [DefaultExecutionOrder(-400)]
    public class FDMiFlightDirector : FDMiBehaviour
    {
        [Header("スイッチ")]
        [Tooltip("AP スイッチ（0=OFF、1=CWS、2=CMD）。CWS では指令を 0 にする")]
        public string apSwitchPath = "~/Input/AP1SW/Value";

        [FDMiDataPath(nameof(apSwitchPath))]
        public FDMiInt apSwitch;

        [Header("航法（自系統の受信機と 1:1）")]
        [Tooltip("航法基準コース [deg magnetic]。ILS はローカライザ、VOR は選択コース")]
        public string navCoursePath = "~/Avionics/NAV1/Course";

        [FDMiDataPath(nameof(navCoursePath))]
        public FDMiFloat navCourse;

        [Tooltip("LOC 偏差 [deg]。正=コースの左（右へ旋回）")]
        public string locDeviationPath = "~/Avionics/NAV1/LOCDev";

        [FDMiDataPath(nameof(locDeviationPath))]
        public FDMiFloat locDeviation;

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

        [Tooltip("横モード選択（0/2=HDG HOLD、1/3=HDG SEL、4/5=VOR、6/7=LOC）")]
        public string rollModeSelectPath = "~/Input/RollMode";

        [FDMiDataPath(nameof(rollModeSelectPath))]
        public FDMiInt rollModeSelect;

        [Header("指令")]
        [Tooltip("VS 指令 [100 ft/min]")]
        public string vsCommandPath = "~/Input/VS/Value";

        [FDMiDataPath(nameof(vsCommandPath))]
        public FDMiInt vsCommand;

        [Tooltip("高度指令 [ft]")]
        public string altitudeCommandPath = "~/Input/ALT/Value";

        [FDMiDataPath(nameof(altitudeCommandPath))]
        public FDMiInt altitudeCommand;

        [Tooltip("方位指令 [deg magnetic]")]
        public string headingCommandPath = "~/Input/HDG/Value";

        [FDMiDataPath(nameof(headingCommandPath))]
        public FDMiInt headingCommand;

        [Header("機体状態")]
        [Tooltip("ピッチ角 [deg]。正=機首上げ")]
        public string pitchPath = "~/Physics/Pitch";

        [FDMiDataPath(nameof(pitchPath))]
        public FDMiFloat pitch;

        [Tooltip("バンク角 [deg]。正=右翼下げ")]
        public string rollPath = "~/Physics/Roll";

        [FDMiDataPath(nameof(rollPath))]
        public FDMiFloat roll;

        [Tooltip("磁方位 [deg]")]
        public string headingPath = "~/Physics/HDGMAG";

        [FDMiDataPath(nameof(headingPath))]
        public FDMiFloat heading;

        [Tooltip("高度 [m]")]
        public string altitudePath = "~/Physics/Altitude";

        [FDMiDataPath(nameof(altitudePath))]
        public FDMiFloat altitude;

        [Tooltip("鉛直速度 [m/s]")]
        public string verticalSpeedPath = "~/Physics/VS";

        [FDMiDataPath(nameof(verticalSpeedPath))]
        public FDMiFloat verticalSpeed;

        [Header("出力")]
        [Tooltip("ピッチ姿勢指令 [deg]。正=機首上げ")]
        public string fdPitchPath = "~/Avionics/FD/L/Pitch";

        [FDMiDataPath(nameof(fdPitchPath))]
        public FDMiFloat fdPitch;

        [Tooltip("バンク指令 [deg]。正=右翼下げ")]
        public string fdRollPath = "~/Avionics/FD/L/Roll";

        [FDMiDataPath(nameof(fdRollPath))]
        public FDMiFloat fdRoll;

        public string pitchModeOutPath = "~/Avionics/FD/L/PitchMode";

        [FDMiDataPath(nameof(pitchModeOutPath))]
        public FDMiInt pitchModeOut;

        public string rollModeOutPath = "~/Avionics/FD/L/RollMode";

        [FDMiDataPath(nameof(rollModeOutPath))]
        public FDMiInt rollModeOut;

        public string pitchArmedOutPath = "~/Avionics/FD/L/PitchArmed";

        [FDMiDataPath(nameof(pitchArmedOutPath))]
        public FDMiInt pitchArmedOut;

        public string rollArmedOutPath = "~/Avionics/FD/L/RollArmed";

        [FDMiDataPath(nameof(rollArmedOutPath))]
        public FDMiInt rollArmedOut;

        [Header("縦の誘導則")]
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

        [Header("横の誘導則")]
        [Tooltip("方位誤差→バンクのゲイン [deg/deg]")]
        public float hdgGain = 2f;

        [Tooltip("通常のバンク上限 [deg]")]
        public float bankMax = 25f;

        [Tooltip("capture 中のバンク上限 [deg]")]
        public float bankMaxCapture = 30f;

        [Tooltip("偏差→方位のゲイン [deg/deg]")]
        public float locGainP = 3f;

        [Tooltip("偏差変化率→方位のゲイン [deg/(deg/s)]")]
        public float locGainD = 0.5f;

        [Tooltip("インターセプト方位の上限 [deg]")]
        public float interceptMax = 30f;

        [Tooltip("LOC 捕捉の偏差 [deg]")]
        public float locCaptureDeg = 2.3f;

        [Tooltip("VOR 捕捉の偏差 [deg]")]
        public float vorCaptureDeg = 3f;

        [Tooltip("偏差変化率の LPF 時定数 [s]")]
        public float locRateTau = 0.5f;

        int[] _apSwitch,
            _vsCommand,
            _altitudeCommand,
            _headingCommand,
            _pitchModeSelect,
            _rollModeSelect,
            _navType,
            _pitchModeOut,
            _rollModeOut,
            _pitchArmedOut,
            _rollArmedOut;
        float[] _pitch,
            _roll,
            _heading,
            _altitude,
            _verticalSpeed,
            _navCourse,
            _locDeviation,
            _gsDeviation,
            _fdPitch,
            _fdRoll;
        bool[] _navValid;

        FDMiPitchMode _pitchMode;
        FDMiRollMode _rollMode;
        int _pitchArmed,
            _rollArmed,
            _prevPitchSel = -1,
            _prevRollSel = -1,
            _prevOutPitchMode = -1,
            _prevOutRollMode = -1,
            _prevOutPitchArmed = -1,
            _prevOutRollArmed = -1,
            _prevAltCmd;
        float _pitchCmd,
            _rollCmd,
            _vsCmd,
            _vsIntegral,
            _gsIntegral,
            _altRef,
            _hdgRef,
            _locRate,
            _prevLocDev;
        bool _initialized;

        void Start()
        {
            _apSwitch = apSwitch.data;
            _pitchModeSelect = pitchModeSelect.data;
            _rollModeSelect = rollModeSelect.data;
            _vsCommand = vsCommand.data;
            _altitudeCommand = altitudeCommand.data;
            _headingCommand = headingCommand.data;
            _navCourse = navCourse.data;
            _locDeviation = locDeviation.data;
            _gsDeviation = gsDeviation.data;
            _navType = navType.data;
            _navValid = navValid.data;
            _pitch = pitch.data;
            _roll = roll.data;
            _heading = heading.data;
            _altitude = altitude.data;
            _verticalSpeed = verticalSpeed.data;
            _fdPitch = fdPitch.data;
            _fdRoll = fdRoll.data;
            _pitchModeOut = pitchModeOut.data;
            _rollModeOut = rollModeOut.data;
            _pitchArmedOut = pitchArmedOut.data;
            _rollArmedOut = rollArmedOut.data;
            _prevAltCmd = _altitudeCommand[0];
        }

        void FixedUpdate()
        {
            // CWS では指令を出さない。姿勢の保持と手放しの捕捉は AP のサーボが担う（ADR-0031）。
            // Why not 現在姿勢を指令にする: FD が CWS を担うことになり、AP が凍結した姿勢と
            // 二重の基準になる。
            if (_apSwitch[0] == 1)
            {
                _fdPitch[0] = 0f;
                _fdRoll[0] = 0f;
                SetPitchState(FDMiPitchMode.Cws, 0);
                SetRollState(FDMiRollMode.Cws, 0);
                _initialized = false;
                return;
            }

            float dt = Time.fixedDeltaTime;
            float pitchNow = _pitch[0];
            float rollNow = _roll[0];
            float hdgNow = _heading[0];
            float altNow = _altitude[0];
            float vsNow = _verticalSpeed[0];
            float locDev = _locDeviation[0];
            float gsDev = _gsDeviation[0];
            float refCourse = _navCourse[0];
            int navTypeNow = _navType[0];
            bool navValidNow = _navValid[0];

            int pitchSel = _pitchModeSelect[0];
            int rollSel = _rollModeSelect[0];
            float altCmd = (float)_altitudeCommand[0] * 0.3048f;
            float vsSel = (float)_vsCommand[0] * 0.508f;
            bool altCmdChanged = _altitudeCommand[0] != _prevAltCmd;
            bool pitchSelChanged = !_initialized || pitchSel != _prevPitchSel;
            bool rollSelChanged = !_initialized || rollSel != _prevRollSel;

            if (pitchSelChanged || rollSelChanged)
            {
                _pitchCmd = pitchNow;
                _rollCmd = rollNow;
                _vsIntegral = 0f;
                _gsIntegral = 0f;
                _vsCmd = vsSel;
                _altRef = altCmd;
                _hdgRef = hdgNow;
                _locRate = 0f;
                _prevLocDev = locDev;
                _pitchArmed = 0;
                _rollArmed = 0;
                _initialized = true;
            }

            if (pitchSelChanged)
            {
                if (pitchSel == 0)
                {
                    _pitchMode = FDMiPitchMode.Vs;
                }
                else if (pitchSel == 1)
                {
                    _altRef = altCmd;
                    _pitchMode =
                        Mathf.Abs(altCmd - altNow) > altCaptureBand ? FDMiPitchMode.AltCap : FDMiPitchMode.AltHold;
                }
                else
                {
                    // GS 捕捉までは現在高度の ALT HOLD で待つ。GS は armed として出す。
                    _pitchMode = FDMiPitchMode.AltHold;
                    _altRef = altNow;
                    _pitchArmed = 0;
                }
            }

            _prevPitchSel = pitchSel;
            _prevRollSel = rollSel;

            // APP 選択かつ ILS のときだけ GS を armed にする。
            // VOR（Type=1）では GSDev が 0 のままなので、Type を見ないと即捕捉してしまう。
            bool appSelected = rollSel >= 6;
            bool gsAvailable = appSelected && navTypeNow == 2 && navValidNow;
            if (_pitchArmed == (int)FDMiPitchMode.GsCap && !gsAvailable)
                _pitchArmed = 0;
            if (pitchSel >= 2 && gsAvailable && _pitchMode != FDMiPitchMode.GsTrk && _pitchArmed == 0)
                _pitchArmed = (int)FDMiPitchMode.GsCap;

            // 縦モード。選択子の変更時だけ初期化し、捕捉・遷移は毎tick評価する。
            if (pitchSel == 0)
            {
                _vsCmd = vsSel;
                // 選択高度が進行方向にあり、まだ捕捉帯の外なら ALT CAP を armed にする。
                // 高度指令の変更時だけでなく、VS 選択直後から armed にする。
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

            // 横モード。VOR/LOC は受信中の Type で選ぶ。4/5=NAV（横のみ）、6/7=APP（GS を許可）。
            float locRate = (locDev - _prevLocDev) / dt;
            _locRate = _locRate + (locRate - _locRate) * (dt / (locRateTau + dt));

            bool isVor = navTypeNow == 1;
            if (rollSel == 0 || rollSel == 2)
            {
                _rollMode = FDMiRollMode.HdgHold;
                _rollArmed = 0;
            }
            else if (rollSel == 1 || rollSel == 3)
            {
                _rollMode = FDMiRollMode.HdgSel;
                _rollArmed = 0;
            }
            else
            {
                int armed = isVor ? (int)FDMiRollMode.VorCap : (int)FDMiRollMode.LocCap;
                int track = isVor ? (int)FDMiRollMode.VorTrk : (int)FDMiRollMode.LocTrk;
                float capture = isVor ? vorCaptureDeg : locCaptureDeg;
                if (_rollMode != (FDMiRollMode)track)
                {
                    _rollMode = FDMiRollMode.HdgSel;
                    _rollArmed = armed;
                }
                if (_rollArmed == armed)
                {
                    if (navValidNow && Mathf.Abs(locDev) < capture && Mathf.Abs(locDev) <= Mathf.Abs(_prevLocDev))
                    {
                        _rollMode = (FDMiRollMode)track;
                        _rollArmed = 0;
                    }
                }
                if (_rollMode == (FDMiRollMode)track && !navValidNow)
                {
                    _rollMode = FDMiRollMode.HdgSel;
                    _rollArmed = armed;
                }
            }

            float hdgErr;
            float bankLimit = _rollArmed != 0 ? bankMaxCapture : bankMax;
            if (_rollMode == FDMiRollMode.HdgHold)
            {
                hdgErr = Wrap180(_hdgRef - hdgNow);
            }
            else if (_rollMode == FDMiRollMode.HdgSel)
            {
                hdgErr = Wrap180((float)_headingCommand[0] - hdgNow);
            }
            else
            {
                float intercept = Mathf.Clamp(locGainP * locDev + locGainD * _locRate, -interceptMax, interceptMax);
                float desiredHdg = Wrap360(refCourse + intercept);
                hdgErr = Wrap180(desiredHdg - hdgNow);
            }

            _rollCmd = Mathf.Clamp(hdgGain * hdgErr, -bankLimit, bankLimit);
            _prevLocDev = locDev;

            _fdPitch[0] = _pitchCmd;
            _fdRoll[0] = _rollCmd;
            SetPitchState(_pitchMode, _pitchArmed);
            SetRollState(_rollMode, _rollArmed);
        }

        void SetPitchState(FDMiPitchMode mode, int armed)
        {
            int m = (int)mode;
            if (m != _prevOutPitchMode)
            {
                _pitchModeOut[0] = m;
                _prevOutPitchMode = m;
            }
            if (armed != _prevOutPitchArmed)
            {
                _pitchArmedOut[0] = armed;
                _prevOutPitchArmed = armed;
            }
        }

        void SetRollState(FDMiRollMode mode, int armed)
        {
            int m = (int)mode;
            if (m != _prevOutRollMode)
            {
                _rollModeOut[0] = m;
                _prevOutRollMode = m;
            }
            if (armed != _prevOutRollArmed)
            {
                _rollArmedOut[0] = armed;
                _prevOutRollArmed = armed;
            }
        }

        static float Wrap360(float d)
        {
            d = Mathf.Repeat(d, 360f);
            return d < 0f ? d + 360f : d;
        }

        static float Wrap180(float d)
        {
            d = Mathf.Repeat(d + 180f, 360f) - 180f;
            return d;
        }
    }
}
