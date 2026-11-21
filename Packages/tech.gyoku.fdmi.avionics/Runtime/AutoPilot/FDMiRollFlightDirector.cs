using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
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

    // ロール軸の飛行誘導。バンク指令と現在バンク角の差だけを出す。
    // Why not 絶対バンク指令を出す: 読み手（AP と EADI）はどちらも現在姿勢を既に持っており、
    // 差にすると引き算が1箇所で済む（ADR-0032）。
    // Why not 縦の誘導と同じクラス: 軸をまたぐ状態（セレクタ変更時の一括リセット）が残り、
    // 縦の FD が無い席で横が動かなくなる（ADR-0032）。
    // Why not 縦モード選択子を読む: 横の誘導に入力は要らない。GS の armed は縦の FD の仕事。
    //
    // FD は FD スイッチを読まない。スイッチ OFF は「誘導を止める」ではなく「表示と AP を止める」
    // であり、FD 自身は常に計算し続ける。
    [DefaultExecutionOrder(-400)]
    public class FDMiRollFlightDirector : FDMiBehaviour
    {
        [Header("スイッチ")]
        [Tooltip("AP スイッチ（0=OFF、1=CWS、2=CMD）。CWS では差分 0 を出す")]
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

        [Tooltip("航法源（0=なし、1=VOR、2=ILS）")]
        public string navTypePath = "~/Avionics/NAV1/Type";

        [FDMiDataPath(nameof(navTypePath))]
        public FDMiInt navType;

        [Tooltip("航法有効")]
        public string navValidPath = "~/Avionics/NAV1/Valid";

        [FDMiDataPath(nameof(navValidPath))]
        public FDMiBool navValid;

        [Header("モード選択")]
        [Tooltip("横モード選択（0/2=HDG HOLD、1/3=HDG SEL、4/5=VOR、6/7=LOC）")]
        public string rollModeSelectPath = "~/Input/RollMode";

        [FDMiDataPath(nameof(rollModeSelectPath))]
        public FDMiInt rollModeSelect;

        [Header("指令")]
        [Tooltip("方位指令 [deg magnetic]")]
        public string headingCommandPath = "~/Input/HDG/Value";

        [FDMiDataPath(nameof(headingCommandPath))]
        public FDMiInt headingCommand;

        [Header("機体状態")]
        [Tooltip("バンク角 [deg]。正=右翼下げ")]
        public string rollPath = "~/Physics/Roll";

        [FDMiDataPath(nameof(rollPath))]
        public FDMiFloat roll;

        [Tooltip("磁方位 [deg]")]
        public string headingPath = "~/Physics/HDGMAG";

        [FDMiDataPath(nameof(headingPath))]
        public FDMiFloat heading;

        [Header("出力")]
        [Tooltip("バンク指令と現在バンク角の差 [deg]。正=右翼下げ方向の補正")]
        public string rollDeltaPath = "~/Avionics/FD/L/RollDelta";

        [FDMiDataPath(nameof(rollDeltaPath))]
        public FDMiFloat rollDelta;

        [Header("誘導則")]
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

        int[] _apSwitch, _rollModeSelect, _headingCommand, _navType;
        float[] _roll, _heading, _navCourse, _locDeviation, _rollDelta;
        bool[] _navValid;

        FDMiRollMode _rollMode;
        int _rollArmed,
            _prevRollSel = -1;
        float _hdgRef,
            _locRate,
            _prevLocDev;
        bool _initialized;

        void Start()
        {
            _apSwitch = apSwitch.data;
            _rollModeSelect = rollModeSelect.data;
            _headingCommand = headingCommand.data;
            _navCourse = navCourse.data;
            _locDeviation = locDeviation.data;
            _navType = navType.data;
            _navValid = navValid.data;
            _roll = roll.data;
            _heading = heading.data;
            _rollDelta = rollDelta.data;
        }

        void FixedUpdate()
        {
            // CWS では差分 0 を出す。姿勢の保持は AP のサーボが担う（ADR-0032）。
            // 誘導の基準（方位・捕捉状態）は現在値から再シードされるので、CWS から戻っても跳ばない。
            if (_apSwitch[0] == 1)
            {
                _rollDelta[0] = 0f;
                _initialized = false;
                return;
            }

            float dt = Time.fixedDeltaTime;
            float rollNow = _roll[0];
            float hdgNow = _heading[0];
            float locDev = _locDeviation[0];
            float refCourse = _navCourse[0];
            int navTypeNow = _navType[0];
            bool navValidNow = _navValid[0];

            int rollSel = _rollModeSelect[0];
            bool rollSelChanged = !_initialized || rollSel != _prevRollSel;

            if (rollSelChanged)
            {
                _hdgRef = hdgNow;
                _locRate = 0f;
                _prevLocDev = locDev;
                _rollArmed = 0;
                _initialized = true;
            }

            _prevRollSel = rollSel;

            // 横モード。VOR/LOC は受信中の Type で選ぶ。4/5=NAV（横のみ）、6/7=APP（縦が GS を持つ）。
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

            _prevLocDev = locDev;
            float rollCmd = Mathf.Clamp(hdgGain * hdgErr, -bankLimit, bankLimit);
            _rollDelta[0] = rollCmd - rollNow;
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
