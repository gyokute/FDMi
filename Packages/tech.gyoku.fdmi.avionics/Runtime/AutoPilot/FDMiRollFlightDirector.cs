using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    // 横モード。番号は FMA の表示インデックスそのもの。
    // RollMode は人間の選択と FD の自動遷移が共有する単一ノードで、FD はこの値に従って制御する。
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

    // ロール軸の飛行誘導。モードごとの誘導則を持ち、バンク指令と現在バンク角の差だけを出す。
    // Why not 絶対バンク指令を出す: 読み手（AP と EADI）はどちらも現在姿勢を既に持っており、
    // 差にすると引き算が1箇所で済む（ADR-0032）。
    // Why not AP スイッチを読む: CWS は横モード 0 で表す。AP スイッチは AP の状態であり、
    // FD のモードを変えると要求モードと実作動モードが分裂する（ADR-0032 改訂）。
    // Why not モードを別ノードで publish する: FMA が読むモードと FD が使うモードが二重になる。
    // RollMode 1本を人間と FD の両方が書き、FMA はそれを直読みする（ADR-0032 改訂）。
    [DefaultExecutionOrder(-400)]
    public class FDMiRollFlightDirector : FDMiBehaviour
    {
        [Header("モード")]
        [Tooltip("横モード。人間の選択と FD の自動遷移が同じノードを書く")]
        public string rollModeSelectPath = "~/Input/RollMode";

        [FDMiDataPath(nameof(rollModeSelectPath))]
        public FDMiInt rollModeSelect;

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

        [Tooltip("CAP 中のインターセプト方位の上限 [deg]")]
        public float interceptMax = 30f;

        [Tooltip("TRK 中のインターセプト方位の上限 [deg]")]
        public float trackInterceptMax = 10f;

        [Tooltip("LOC 捕捉の偏差 [deg]")]
        public float locCaptureDeg = 2.3f;

        [Tooltip("VOR 捕捉の偏差 [deg]")]
        public float vorCaptureDeg = 3f;

        [Tooltip("偏差変化率の LPF 時定数 [s]")]
        public float locRateTau = 0.5f;

        int[] _rollModeSelect, _headingCommand, _navType;
        float[] _roll, _heading, _navCourse, _locDeviation, _rollDelta;
        bool[] _navValid;

        FDMiRollMode _rollMode;
        float _rollCmd,
            _hdgRef,
            _locRate,
            _prevLocDev;

        void Start()
        {
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
            float dt = Time.fixedDeltaTime;
            float rollNow = _roll[0];
            float hdgNow = _heading[0];
            float locDev = _locDeviation[0];
            float refCourse = _navCourse[0];
            int navTypeNow = _navType[0];
            bool navValidNow = _navValid[0];

            // モードノードは両席の FD が共有する。他席の受信機が捕捉した TRK に自席の
            // 受信機が追従できないとき、拒否をノードへ書き戻すと捕捉した席まで巻き戻る。
            // そのときは CAP の待ちとして留まり、書き戻さない。
            FDMiRollMode requested = (FDMiRollMode)_rollModeSelect[0];
            if (requested == FDMiRollMode.LocTrk && !(navValidNow && navTypeNow == 2))
                requested = FDMiRollMode.LocCap;
            else if (requested == FDMiRollMode.VorTrk && !(navValidNow && navTypeNow == 1))
                requested = FDMiRollMode.VorCap;
            if (requested != _rollMode)
                EnterRollMode(requested, rollNow, hdgNow, locDev);

            // 偏差変化率は CAP/TRK の誘導だけが読む。他モードでは LPF を回さない。
            if (IsNavMode(_rollMode))
            {
                float rawRate = (locDev - _prevLocDev) / dt;
                _locRate += (rawRate - _locRate) * (dt / (locRateTau + dt));
            }

            UpdateRollMode(locDev, navTypeNow, navValidNow);
            bool navReady = navValidNow && NavMatches(_rollMode, navTypeNow);

            ComputeRollGuidance(rollNow, hdgNow, locDev, refCourse, navReady);
            _rollDelta[0] = _rollCmd - rollNow;
            _prevLocDev = locDev;
        }

        // 人間がモードを選び直したときだけ、そのモードの初期状態へ入る。
        // FD 自身の遷移は SetRollMode でノードを書き換えるだけにして、指令とレートを継続する。
        void EnterRollMode(FDMiRollMode mode, float rollNow, float hdgNow, float locDev)
        {
            _rollMode = mode;
            _rollCmd = rollNow;
            _hdgRef = hdgNow;
            _locRate = 0f;
            _prevLocDev = locDev;
        }

        void SetRollMode(FDMiRollMode mode)
        {
            if (_rollMode == mode)
                return;
            _rollMode = mode;
            if (_rollModeSelect[0] != (int)mode)
                rollModeSelect.Data = (int)mode;
        }

        void UpdateRollMode(float locDev, int navTypeNow, bool navValidNow)
        {
            switch (_rollMode)
            {
                case FDMiRollMode.Cws:
                case FDMiRollMode.HdgHold:
                case FDMiRollMode.HdgSel:
                    return;
                case FDMiRollMode.VorCap:
                    UpdateNavCapture(FDMiRollMode.VorTrk, 1, vorCaptureDeg, locDev, navTypeNow, navValidNow);
                    return;
                case FDMiRollMode.LocCap:
                    UpdateNavCapture(FDMiRollMode.LocTrk, 2, locCaptureDeg, locDev, navTypeNow, navValidNow);
                    return;
                case FDMiRollMode.VorTrk:
                    UpdateNavTrack(1, navTypeNow, navValidNow);
                    return;
                case FDMiRollMode.LocTrk:
                    UpdateNavTrack(2, navTypeNow, navValidNow);
                    return;
            }
        }

        // CAP は能動捕捉。ビームに近づいているときだけ TRK へ移る。
        void UpdateNavCapture(
            FDMiRollMode trackMode,
            int requiredType,
            float captureDeg,
            float locDev,
            int navTypeNow,
            bool navValidNow
        )
        {
            if (navTypeNow != requiredType || !navValidNow)
                return;
            if (Mathf.Abs(locDev) < captureDeg && Mathf.Abs(locDev) <= Mathf.Abs(_prevLocDev))
                SetRollMode(trackMode);
        }

        // TRK 中に受信が切れたら HDG SEL へ戻す。モードノードも同時に書き換える。
        void UpdateNavTrack(int requiredType, int navTypeNow, bool navValidNow)
        {
            if (navTypeNow != requiredType || !navValidNow)
                SetRollMode(FDMiRollMode.HdgSel);
        }

        bool IsNavMode(FDMiRollMode mode)
        {
            return mode == FDMiRollMode.VorCap
                || mode == FDMiRollMode.VorTrk
                || mode == FDMiRollMode.LocCap
                || mode == FDMiRollMode.LocTrk;
        }

        bool NavMatches(FDMiRollMode mode, int navTypeNow)
        {
            if (mode == FDMiRollMode.VorCap || mode == FDMiRollMode.VorTrk)
                return navTypeNow == 1;
            if (mode == FDMiRollMode.LocCap || mode == FDMiRollMode.LocTrk)
                return navTypeNow == 2;
            return false;
        }

        void ComputeRollGuidance(float rollNow, float hdgNow, float locDev, float refCourse, bool navReady)
        {
            switch (_rollMode)
            {
                case FDMiRollMode.Cws:
                    _rollCmd = rollNow;
                    return;
                case FDMiRollMode.HdgHold:
                    ComputeHeading(Wrap180(_hdgRef - hdgNow), bankMax);
                    return;
                case FDMiRollMode.HdgSel:
                    ComputeHeading(Wrap180((float)_headingCommand[0] - hdgNow), bankMax);
                    return;
                case FDMiRollMode.VorCap:
                case FDMiRollMode.LocCap:
                    if (navReady)
                        ComputeNav(locDev, refCourse, hdgNow, interceptMax, bankMaxCapture);
                    else
                    {
                        // nav 不成立の CAP は HDG SEL で待つ。バンク上限は通常の 25°。
                        ComputeHeading(Wrap180((float)_headingCommand[0] - hdgNow), bankMax);
                    }
                    return;
                case FDMiRollMode.VorTrk:
                case FDMiRollMode.LocTrk:
                    ComputeNav(locDev, refCourse, hdgNow, trackInterceptMax, bankMax);
                    return;
            }
        }

        void ComputeHeading(float hdgErr, float bankLimit)
        {
            _rollCmd = Mathf.Clamp(hdgGain * hdgErr, -bankLimit, bankLimit);
        }

        void ComputeNav(float locDev, float refCourse, float hdgNow, float interceptLimit, float bankLimit)
        {
            float intercept = Mathf.Clamp(
                locGainP * locDev + locGainD * _locRate,
                -interceptLimit,
                interceptLimit
            );
            ComputeHeading(Wrap180(Wrap360(refCourse + intercept) - hdgNow), bankLimit);
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
