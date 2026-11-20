using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // FD の縦モード。番号は FMA の表示インデックスそのもの。
    public enum FDMiPitchMode
    {
        Off = 0,
        Cws = 1,
        Vs = 2,
        AltCap = 3,
        AltHold = 4,
        GsCap = 5,
        GsTrk = 6,
    }

    // FD の横モード。
    public enum FDMiRollMode
    {
        Off = 0,
        Cws = 1,
        HdgHold = 2,
        HdgSel = 3,
        VorCap = 4,
        VorTrk = 5,
        LocCap = 6,
        LocTrk = 7,
    }

    // モードと姿勢指令を持つ飛行誘導。AP から独立して動き、AP OFF でも FD バー用の指令を出し続ける。
    // Why not AP と同じクラス: モード遷移の書き手が AP になると、FD と AP の engage 状態が混ざり、
    // main のようにモード書込が同フレームで再入する。
    // Why not 姿勢指令を AP 内で作る: FD バーと AP の追従目標が二重実装になる。
    [DefaultExecutionOrder(-400)]
    public class FDMiFlightDirector : FDMiBehaviour
    {
        [Header("スイッチ")]
        [Tooltip("FD スイッチ")]
        public string fdSwitchPath = "~/Input/FD.L/Value";

        [FDMiDataPath(nameof(fdSwitchPath))]
        public FDMiBool fdSwitch;

        [Tooltip("AP スイッチ（0=OFF、1=CWS、2=CMD）。CWS 表示の判定だけに使う")]
        public string apSwitchPath = "~/Input/AP2SW/Value";

        [FDMiDataPath(nameof(apSwitchPath))]
        public FDMiInt apSwitch;

        [Header("NAV系統選択")]
        [Tooltip("系統選択子（0=Both On A, 1=NORM, 2=Both On B）")]
        public string navSelectorPath = "~/Input/FDCOM/Value";

        [FDMiDataPath(nameof(navSelectorPath)), FDMiRegisterCallback(nameof(OnNavSelectorChanged))]
        public FDMiData navSelector;

        [Tooltip("右席なら true。NORM のとき B 系統を使う")]
        public bool rightSeat;

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

        [Tooltip("コース指令 [deg magnetic]")]
        public string courseCommandPath = "~/Input/CRS1/Value";

        [FDMiDataPath(nameof(courseCommandPath))]
        public FDMiInt courseCommand;

        [Tooltip("B 系統のコース指令 [deg magnetic]")]
        public string courseCommand2Path = "~/Input/CRS2/Value";

        [FDMiDataPath(nameof(courseCommand2Path))]
        public FDMiInt courseCommand2;

        [Tooltip("A 系統の航法基準コース（ILS はローカライザ、VOR は選択コース）")]
        public string navCoursePath = "~/Physics/NAV1/Course";

        [FDMiDataPath(nameof(navCoursePath))]
        public FDMiFloat navCourse;

        [Tooltip("B 系統の航法基準コース（ILS はローカライザ、VOR は選択コース）")]
        public string navCourse2Path = "~/Physics/NAV2/Course";

        [FDMiDataPath(nameof(navCourse2Path))]
        public FDMiFloat navCourse2;

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

        [Tooltip("A 系統の LOC 偏差（CNS 受信機の出力）")]
        public string locDeviationPath = "~/Physics/NAV1/LOCDev";

        [FDMiDataPath(nameof(locDeviationPath))]
        public FDMiFloat locDeviation;

        [Tooltip("A 系統の GS 偏差 [deg]。正=パス上方")]
        public string gsDeviationPath = "~/Physics/NAV1/GSDev";

        [FDMiDataPath(nameof(gsDeviationPath))]
        public FDMiFloat gsDeviation;

        [Tooltip("A 系統の TO/FROM")]
        public string tofromPath = "~/Physics/NAV1/TOFROM";

        [FDMiDataPath(nameof(tofromPath))]
        public FDMiFloat tofrom;

        [Tooltip("A 系統の航法源（0=なし、1=VOR、2=ILS）")]
        public string navTypePath = "~/Physics/NAV1/Type";

        [FDMiDataPath(nameof(navTypePath))]
        public FDMiInt navType;

        [Tooltip("A 系統の航法有効")]
        public string navValidPath = "~/Physics/NAV1/Valid";

        [FDMiDataPath(nameof(navValidPath))]
        public FDMiBool navValid;

        [Tooltip("B 系統の LOC 偏差")]
        public string locDeviation2Path = "~/Physics/NAV2/LOCDev";

        [FDMiDataPath(nameof(locDeviation2Path))]
        public FDMiFloat locDeviation2;

        [Tooltip("B 系統の GS 偏差 [deg]")]
        public string gsDeviation2Path = "~/Physics/NAV2/GSDev";

        [FDMiDataPath(nameof(gsDeviation2Path))]
        public FDMiFloat gsDeviation2;

        [Tooltip("B 系統の TO/FROM")]
        public string tofrom2Path = "~/Physics/NAV2/TOFROM";

        [FDMiDataPath(nameof(tofrom2Path))]
        public FDMiFloat tofrom2;

        [Tooltip("B 系統の航法源（0=なし、1=VOR、2=ILS）")]
        public string navType2Path = "~/Physics/NAV2/Type";

        [FDMiDataPath(nameof(navType2Path))]
        public FDMiInt navType2;

        [Tooltip("B 系統の航法有効")]
        public string navValid2Path = "~/Physics/NAV2/Valid";

        [FDMiDataPath(nameof(navValid2Path))]
        public FDMiBool navValid2;

        [Header("ヨーク（CWS の手放し検出）")]
        public string pitchInputLPath = "~/Input/YokeL/PitchInput";

        [FDMiDataPath(nameof(pitchInputLPath))]
        public FDMiFloat pitchInputL;

        public string pitchInputRPath = "~/Input/YokeR/PitchInput";

        [FDMiDataPath(nameof(pitchInputRPath))]
        public FDMiFloat pitchInputR;

        public string rollInputLPath = "~/Input/YokeL/RollInput";

        [FDMiDataPath(nameof(rollInputLPath))]
        public FDMiFloat rollInputL;

        public string rollInputRPath = "~/Input/YokeR/RollInput";

        [FDMiDataPath(nameof(rollInputRPath))]
        public FDMiFloat rollInputR;

        [Header("出力")]
        [Tooltip("ピッチ姿勢指令 [deg]。正=機首上げ")]
        public string fdPitchPath = "~/Autopilot/FDPitch";

        [FDMiDataPath(nameof(fdPitchPath))]
        public FDMiFloat fdPitch;

        [Tooltip("バンク指令 [deg]。正=右翼下げ")]
        public string fdRollPath = "~/Autopilot/FDRoll";

        [FDMiDataPath(nameof(fdRollPath))]
        public FDMiFloat fdRoll;

        public string pitchModeOutPath = "~/Autopilot/PitchMode";

        [FDMiDataPath(nameof(pitchModeOutPath))]
        public FDMiInt pitchModeOut;

        public string rollModeOutPath = "~/Autopilot/RollMode";

        [FDMiDataPath(nameof(rollModeOutPath))]
        public FDMiInt rollModeOut;

        public string pitchArmedOutPath = "~/Autopilot/PitchArmed";

        [FDMiDataPath(nameof(pitchArmedOutPath))]
        public FDMiInt pitchArmedOut;

        public string rollArmedOutPath = "~/Autopilot/RollArmed";

        [FDMiDataPath(nameof(rollArmedOutPath))]
        public FDMiInt rollArmedOut;

        [Header("選択済み NAV（計器用）")]
        [Tooltip("選択系統のコース [deg magnetic]")]
        public string navCrsOutPath = "~/Avionics/Autopilot/NAV/L/CRS";

        [FDMiDataPath(nameof(navCrsOutPath))]
        public FDMiFloat navCrsOut;

        [Tooltip("選択系統の LOC 偏差 [deg]")]
        public string navLocOutPath = "~/Avionics/Autopilot/NAV/L/LOCDev";

        [FDMiDataPath(nameof(navLocOutPath))]
        public FDMiFloat navLocOut;

        [Tooltip("選択系統の GS 偏差 [deg]")]
        public string navGsOutPath = "~/Avionics/Autopilot/NAV/L/GSDev";

        [FDMiDataPath(nameof(navGsOutPath))]
        public FDMiFloat navGsOut;

        [Tooltip("選択系統の TO/FROM")]
        public string navTofromOutPath = "~/Avionics/Autopilot/NAV/L/TOFROM";

        [FDMiDataPath(nameof(navTofromOutPath))]
        public FDMiFloat navTofromOut;

        [Tooltip("選択系統の航法源（0=なし、1=VOR、2=ILS）")]
        public string navTypeOutPath = "~/Avionics/Autopilot/NAV/L/Type";

        [FDMiDataPath(nameof(navTypeOutPath))]
        public FDMiInt navTypeOut;

        [Tooltip("選択系統の航法有効")]
        public string navValidOutPath = "~/Avionics/Autopilot/NAV/L/Valid";

        [FDMiDataPath(nameof(navValidOutPath))]
        public FDMiBool navValidOut;

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

        [Tooltip("CWS の手放し判定 [入力]")]
        public float cwsInputThreshold = 0.02f;

        bool[] _fdSwitch;
        int[] _apSwitch,
            _vsCommand,
            _altitudeCommand,
            _headingCommand,
            _courseCommand,
            _courseCommand2;
        float[] _pitch,
            _roll,
            _heading,
            _altitude,
            _verticalSpeed,
            _locDeviation,
            _locDeviation2,
            _gsDeviation,
            _gsDeviation2,
            _tofrom,
            _tofrom2,
            _pitchInputL,
            _pitchInputR,
            _rollInputL,
            _rollInputR,
            _fdPitch,
            _fdRoll,
            _navCrsOut,
            _navLocOut,
            _navGsOut,
            _navTofromOut,
            _navCourse,
            _navCourse2;
        bool[] _navValid,
            _navValid2,
            _navValidOut;
        int[] _pitchModeSelect,
            _rollModeSelect;
        int[] _navType,
            _navType2,
            _navTypeOut;
        int[] _pitchModeOut,
            _rollModeOut,
            _pitchArmedOut,
            _rollArmedOut;

        FDMiPitchMode _pitchMode;
        FDMiRollMode _rollMode;
        int _pitchArmed,
            _rollArmed;
        int _prevPitchSel = -1,
            _prevRollSel = -1,
            _prevOutPitchMode = -1,
            _prevOutRollMode = -1,
            _prevOutPitchArmed = -1,
            _prevOutRollArmed = -1;
        float _pitchCmd,
            _rollCmd,
            _vsCmd,
            _vsIntegral,
            _gsIntegral,
            _altRef,
            _hdgRef,
            _locRate,
            _prevLocDev;
        int _prevAltCmd;
        float _selCourse,
            _selRefCourse,
            _selLoc,
            _selGs,
            _selTofrom;
        int _selType;
        bool _selValid,
            _useB;
        bool _cwsPitchHeld,
            _cwsRollHeld,
            _initialized;

        void Start()
        {
            _fdSwitch = fdSwitch.data;
            _apSwitch = apSwitch.data;
            _pitchModeSelect = pitchModeSelect.data;
            _rollModeSelect = rollModeSelect.data;
            _vsCommand = vsCommand.data;
            _altitudeCommand = altitudeCommand.data;
            _headingCommand = headingCommand.data;
            _courseCommand = courseCommand.data;
            _courseCommand2 = courseCommand2.data;
            _pitch = pitch.data;
            _roll = roll.data;
            _heading = heading.data;
            _altitude = altitude.data;
            _verticalSpeed = verticalSpeed.data;
            _locDeviation = locDeviation.data;
            _locDeviation2 = locDeviation2.data;
            _gsDeviation = gsDeviation.data;
            _gsDeviation2 = gsDeviation2.data;
            _tofrom = tofrom.data;
            _tofrom2 = tofrom2.data;
            _navType = navType.data;
            _navType2 = navType2.data;
            _navValid = navValid.data;
            _navValid2 = navValid2.data;
            _pitchInputL = pitchInputL.data;
            _pitchInputR = pitchInputR.data;
            _rollInputL = rollInputL.data;
            _rollInputR = rollInputR.data;
            _fdPitch = fdPitch.data;
            _fdRoll = fdRoll.data;
            _pitchModeOut = pitchModeOut.data;
            _rollModeOut = rollModeOut.data;
            _pitchArmedOut = pitchArmedOut.data;
            _rollArmedOut = rollArmedOut.data;
            _navCrsOut = navCrsOut.data;
            _navLocOut = navLocOut.data;
            _navGsOut = navGsOut.data;
            _navTofromOut = navTofromOut.data;
            _navTypeOut = navTypeOut.data;
            _navValidOut = navValidOut.data;
            _navCourse = navCourse.data;
            _navCourse2 = navCourse2.data;
            _prevAltCmd = _altitudeCommand[0];
            OnNavSelectorChanged();
        }

        // 系統選択子の変化はイベントで受ける。FixedUpdate では読まない。
        // 0=Both On A, 1=NORM, 2=Both On B。NORM は右席だけ B 系統。
        public void OnNavSelectorChanged()
        {
            int selector = navSelector.GetInt();
            _useB = selector == 2 || (selector == 1 && rightSeat);
            _initialized = false;
        }

        // 選択系統の NAV を計器用に publish する。FD OFF でも計器は NAV を表示するため、
        // FD の早期 return より前に毎tick書く。
        // Why-not 表示コースと誘導コースを同一にする: ILS の偏差は施設ローカライザ
        // コース基準で、パイロットが OBS を合わせていなくても誘導は正しく行う必要がある。
        // EHSI のコース針は選択コース（_selCourse）のまま、誘導は受信機の基準コースを使う。
        void UpdateSelectedNav()
        {
            if (_useB)
            {
                _selCourse = (float)_courseCommand2[0];
                _selRefCourse = _navCourse2[0];
                _selLoc = _locDeviation2[0];
                _selGs = _gsDeviation2[0];
                _selTofrom = _tofrom2[0];
                _selType = _navType2[0];
                _selValid = _navValid2[0];
            }
            else
            {
                _selCourse = (float)_courseCommand[0];
                _selRefCourse = _navCourse[0];
                _selLoc = _locDeviation[0];
                _selGs = _gsDeviation[0];
                _selTofrom = _tofrom[0];
                _selType = _navType[0];
                _selValid = _navValid[0];
            }

            _navCrsOut[0] = _selCourse;
            _navLocOut[0] = _selLoc;
            _navGsOut[0] = _selGs;
            _navTofromOut[0] = _selTofrom;
            _navTypeOut[0] = _selType;
            _navValidOut[0] = _selValid;
        }

        void FixedUpdate()
        {
            UpdateSelectedNav();

            float dt = Time.fixedDeltaTime;
            if (!_fdSwitch[0])
            {
                _fdPitch[0] = 0f;
                _fdRoll[0] = 0f;
                SetPitchState(FDMiPitchMode.Off, 0);
                SetRollState(FDMiRollMode.Off, 0);
                _cwsPitchHeld = false;
                _cwsRollHeld = false;
                _initialized = false;
                return;
            }

            float pitchNow = _pitch[0];
            float rollNow = _roll[0];
            float hdgNow = _heading[0];
            float altNow = _altitude[0];
            float vsNow = _verticalSpeed[0];
            float yokePitch = Mathf.Clamp(_pitchInputL[0] + _pitchInputR[0], -1f, 1f);
            float yokeRoll = Mathf.Clamp(_rollInputL[0] + _rollInputR[0], -1f, 1f);
            int apMode = _apSwitch[0];

            // CWS 中は現在姿勢を指令にし、手放した瞬間の姿勢を凍結する。
            // Why not AP に姿勢保持を持たせる: FD バーと AP が別々の基準を持つと、手放し直後にバーと機体がずれる。
            if (apMode == 1)
            {
                if (Mathf.Abs(yokePitch) > cwsInputThreshold)
                {
                    _pitchCmd = pitchNow;
                    _cwsPitchHeld = true;
                }
                else if (_cwsPitchHeld)
                {
                    _pitchCmd = pitchNow;
                    _cwsPitchHeld = false;
                }
                if (Mathf.Abs(yokeRoll) > cwsInputThreshold)
                {
                    _rollCmd = rollNow;
                    _cwsRollHeld = true;
                }
                else if (_cwsRollHeld)
                {
                    _rollCmd = rollNow;
                    _cwsRollHeld = false;
                }
                _pitchCmd = Mathf.Clamp(_pitchCmd, pitchMinDeg, pitchMaxDeg);
                _rollCmd = Mathf.Clamp(_rollCmd, -bankMax, bankMax);
                _fdPitch[0] = _pitchCmd;
                _fdRoll[0] = _rollCmd;
                SetPitchState(FDMiPitchMode.Cws, 0);
                SetRollState(FDMiRollMode.Cws, 0);
                _initialized = false;
                return;
            }

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
                _prevLocDev = _selLoc;
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
            bool gsAvailable = appSelected && _selType == 2 && _selValid;
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
                if (gsAvailable && Mathf.Abs(_selGs) < gsCaptureDeg)
                {
                    _pitchMode = FDMiPitchMode.GsTrk;
                    _pitchArmed = 0;
                    _pitchCmd = pitchNow;
                    _gsIntegral = 0f;
                }
            }
            else if (_pitchMode == FDMiPitchMode.GsTrk)
            {
                if (!gsAvailable || Mathf.Abs(_selGs) > 2f)
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
                float gsErr = -_selGs;
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
            float locDev = _selLoc;
            float locRate = (locDev - _prevLocDev) / dt;
            _locRate = _locRate + (locRate - _locRate) * (dt / (locRateTau + dt));

            bool isVor = _selType == 1;
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
                    if (_selValid && Mathf.Abs(locDev) < capture && Mathf.Abs(locDev) <= Mathf.Abs(_prevLocDev))
                    {
                        _rollMode = (FDMiRollMode)track;
                        _rollArmed = 0;
                    }
                }
                if (_rollMode == (FDMiRollMode)track && !_selValid)
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
                float desiredHdg = Wrap360(_selRefCourse + intercept);
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
