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
        [Tooltip("FD スイッチ（0=OFF、1以上=ON）")]
        public string fdSwitchPath = "~/Input/FD.L/Value";

        [FDMiDataPath(nameof(fdSwitchPath))]
        public FDMiFloat fdSwitch;

        [Tooltip("AP スイッチ（0=OFF、1=CWS、2=CMD）。CWS 表示の判定だけに使う")]
        public string apSwitchPath = "~/Input/AP2SW/Value";

        [FDMiDataPath(nameof(apSwitchPath))]
        public FDMiFloat apSwitch;

        [Header("モード選択")]
        [Tooltip("縦モード選択（0=VS、1=ALT、2=GS/APP）")]
        public string pitchModeSelectPath = "~/Input/AutoPilot/PitchModeSelect";

        [FDMiDataPath(nameof(pitchModeSelectPath))]
        public FDMiFloat pitchModeSelect;

        [Tooltip("横モード選択（0=HDG HOLD、1=HDG SEL、2=VOR、3=LOC）")]
        public string rollModeSelectPath = "~/Input/AutoPilot/RollModeSelect";

        [FDMiDataPath(nameof(rollModeSelectPath))]
        public FDMiFloat rollModeSelect;

        [Header("指令")]
        [Tooltip("VS 指令 [100 ft/min]")]
        public string vsCommandPath = "~/Input/VS/Value";

        [FDMiDataPath(nameof(vsCommandPath))]
        public FDMiFloat vsCommand;

        [Tooltip("高度指令 [ft]")]
        public string altitudeCommandPath = "~/Input/ALT/Value";

        [FDMiDataPath(nameof(altitudeCommandPath))]
        public FDMiFloat altitudeCommand;

        [Tooltip("方位指令 [deg magnetic]")]
        public string headingCommandPath = "~/Input/HDG/Value";

        [FDMiDataPath(nameof(headingCommandPath))]
        public FDMiFloat headingCommand;

        [Tooltip("コース指令 [deg magnetic]")]
        public string courseCommandPath = "~/Input/NAV1/Course";

        [FDMiDataPath(nameof(courseCommandPath))]
        public FDMiFloat courseCommand;

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

        [Tooltip("LOC/GS 偏差（CNS 受信機の出力）")]
        public string locDeviationPath = "~/Physics/NAV1/LOCDev";

        [FDMiDataPath(nameof(locDeviationPath))]
        public FDMiFloat locDeviation;

        [Tooltip("GS 偏差 [deg]。正=パス上方")]
        public string gsDeviationPath = "~/Physics/NAV1/GSDev";

        [FDMiDataPath(nameof(gsDeviationPath))]
        public FDMiFloat gsDeviation;

        [Tooltip("航法有効")]
        public string navValidPath = "~/Physics/NAV1/Valid";

        [FDMiDataPath(nameof(navValidPath))]
        public FDMiBool navValid;

        [Header("ヨーク（CWS の手放し検出）")]
        public string pitchInputLPath = "~/Input/YokeL/Input.Gesture/PitchInput";

        [FDMiDataPath(nameof(pitchInputLPath))]
        public FDMiFloat pitchInputL;

        public string pitchInputRPath = "~/Input/YokeR/Input.Gesture/PitchInput";

        [FDMiDataPath(nameof(pitchInputRPath))]
        public FDMiFloat pitchInputR;

        public string rollInputLPath = "~/Input/YokeL/Input.Gesture/RollInput";

        [FDMiDataPath(nameof(rollInputLPath))]
        public FDMiFloat rollInputL;

        public string rollInputRPath = "~/Input/YokeR/Input.Gesture/RollInput";

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

        float[] _fdSwitch,
            _apSwitch,
            _pitchModeSelect,
            _rollModeSelect,
            _vsCommand,
            _altitudeCommand,
            _headingCommand,
            _courseCommand,
            _pitch,
            _roll,
            _heading,
            _altitude,
            _verticalSpeed,
            _locDeviation,
            _gsDeviation,
            _pitchInputL,
            _pitchInputR,
            _rollInputL,
            _rollInputR,
            _fdPitch,
            _fdRoll;
        bool[] _navValid;
        int[] _pitchModeOut, _rollModeOut, _pitchArmedOut, _rollArmedOut;

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
            _prevLocDev,
            _prevAltCmd;
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
            _pitch = pitch.data;
            _roll = roll.data;
            _heading = heading.data;
            _altitude = altitude.data;
            _verticalSpeed = verticalSpeed.data;
            _locDeviation = locDeviation.data;
            _gsDeviation = gsDeviation.data;
            _navValid = navValid.data;
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
            _prevAltCmd = _altitudeCommand[0];
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float fdOn = _fdSwitch[0];
            if (fdOn < 0.5f)
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
            int apMode = Mathf.RoundToInt(_apSwitch[0]);

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

            int pitchSel = Mathf.RoundToInt(_pitchModeSelect[0]);
            int rollSel = Mathf.RoundToInt(_rollModeSelect[0]);
            float altCmd = _altitudeCommand[0] * 0.3048f;
            float vsSel = _vsCommand[0] * 0.508f;
            bool altCmdChanged = Mathf.Abs(_altitudeCommand[0] - _prevAltCmd) > 1f;
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
                _prevLocDev = _locDeviation[0];
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
                        Mathf.Abs(altCmd - altNow) > altCaptureBand
                            ? FDMiPitchMode.AltCap
                            : FDMiPitchMode.AltHold;
                }
                else
                {
                    // GS 捕捉までは現在高度の ALT HOLD で待つ。GS は armed として出す。
                    _pitchMode = FDMiPitchMode.AltHold;
                    _altRef = altNow;
                    _pitchArmed = (int)FDMiPitchMode.GsCap;
                }
            }

            _prevPitchSel = pitchSel;
            _prevRollSel = rollSel;

            // 縦モード。選択子の変更時だけ初期化し、捕捉・遷移は毎tick評価する。
            if (pitchSel == 0)
            {
                _vsCmd = vsSel;
                // 選択高度が進行方向にあり、まだ捕捉帯の外なら ALT CAP を armed にする。
                // 高度指令の変更時だけでなく、VS 選択直後から armed にする。
                if (
                    Mathf.Abs(altCmd - altNow) > altCaptureBand
                    && (altCmd - altNow) * vsSel > 0f
                )
                {
                    _pitchArmed = (int)FDMiPitchMode.AltCap;
                }
            }
            else if (pitchSel == 1 && altCmdChanged)
            {
                _altRef = altCmd;
                _pitchMode =
                    Mathf.Abs(altCmd - altNow) > altCaptureBand
                        ? FDMiPitchMode.AltCap
                        : FDMiPitchMode.AltHold;
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
                if (_navValid[0] && Mathf.Abs(_gsDeviation[0]) < gsCaptureDeg)
                {
                    _pitchMode = FDMiPitchMode.GsTrk;
                    _pitchArmed = 0;
                    _pitchCmd = pitchNow;
                    _gsIntegral = 0f;
                }
            }
            else if (_pitchMode == FDMiPitchMode.GsTrk)
            {
                if (!_navValid[0] || Mathf.Abs(_gsDeviation[0]) > 2f)
                {
                    _pitchMode = FDMiPitchMode.AltHold;
                    _altRef = altNow;
                    _pitchArmed = (int)FDMiPitchMode.GsCap;
                    _vsIntegral = 0f;
                }
            }

            if (_pitchMode == FDMiPitchMode.GsTrk)
            {
                // 正の GSDev はパス上方。機首下げ方向（負）へ積分する。
                float gsErr = -_gsDeviation[0];
                _pitchCmd += (gsGainP * gsErr + gsGainI * _gsIntegral) * dt;
                _gsIntegral += gsErr * dt;
                _gsIntegral = Mathf.Clamp(_gsIntegral, -10f, 10f);
            }
            else
            {
                if (_pitchMode == FDMiPitchMode.AltHold || _pitchMode == FDMiPitchMode.AltCap || _pitchMode == FDMiPitchMode.GsCap)
                    _vsCmd = Mathf.Clamp(altGain * (_altRef - altNow), -vsMax, vsMax);

                float vsErr = _vsCmd - vsNow;
                _pitchCmd += (vsGainP * vsErr + vsGainI * _vsIntegral) * dt;
                _vsIntegral += vsErr * dt;
                _vsIntegral = Mathf.Clamp(_vsIntegral, -5f, 5f);
            }

            _pitchCmd = Mathf.Clamp(_pitchCmd, pitchMinDeg, pitchMaxDeg);

            // 横モード。
            float locDev = _locDeviation[0];
            float locRate = (locDev - _prevLocDev) / dt;
            _locRate = _locRate + (locRate - _locRate) * (dt / (locRateTau + dt));

            if (rollSel == 0)
            {
                _rollMode = FDMiRollMode.HdgHold;
                _rollArmed = 0;
            }
            else if (rollSel == 1)
            {
                _rollMode = FDMiRollMode.HdgSel;
                _rollArmed = 0;
            }
            else
            {
                int armed = rollSel == 2 ? (int)FDMiRollMode.VorCap : (int)FDMiRollMode.LocCap;
                int track = rollSel == 2 ? (int)FDMiRollMode.VorTrk : (int)FDMiRollMode.LocTrk;
                float capture = rollSel == 2 ? vorCaptureDeg : locCaptureDeg;
                if (_rollMode != (FDMiRollMode)track)
                {
                    _rollMode = FDMiRollMode.HdgSel;
                    _rollArmed = armed;
                }
                if (_rollArmed == armed)
                {
                    if (
                        _navValid[0]
                        && Mathf.Abs(locDev) < capture
                        && Mathf.Abs(locDev) <= Mathf.Abs(_prevLocDev)
                    )
                    {
                        _rollMode = (FDMiRollMode)track;
                        _rollArmed = 0;
                    }
                }
                if (_rollMode == (FDMiRollMode)track && !_navValid[0])
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
                hdgErr = Wrap180(_headingCommand[0] - hdgNow);
            }
            else
            {
                float intercept = Mathf.Clamp(
                    locGainP * locDev + locGainD * _locRate,
                    -interceptMax,
                    interceptMax
                );
                float desiredHdg = Wrap360(_courseCommand[0] + intercept);
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
