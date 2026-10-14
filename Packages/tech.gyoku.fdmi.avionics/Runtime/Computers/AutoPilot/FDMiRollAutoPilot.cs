using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public enum RollAutoPilotMode { CWS, HDGHOLD, HDGSEL, FMSCAP, FMSTRK, LOCCAP, LOCTRK, ILSCAP, ILSTRK }

    public class FDMiRollAutoPilot : FDMiAutoPilot
    {
        public string APSWPath = "APSW";

        [FDMiDataPath(nameof(APSWPath))]
        [FDMiRegisterCallback(nameof(OnChangeAPSW))]
        public FDMiFloat APSW;
        public string RollModePath = "RollMode";

        [FDMiDataPath(nameof(RollModePath))]
        [FDMiRegisterCallback(nameof(OnChange_RollMode))]
        public FDMiFloat RollMode;
        public string APOutputPath = "APOutput";

        [FDMiDataPath(nameof(APOutputPath))]
        public FDMiFloat APOutput;
        public string RollInputLPath = "RollInputL";

        [FDMiDataPath(nameof(RollInputLPath))]
        public FDMiFloat RollInputL;
        public string RollInputRPath = "RollInputR";

        [FDMiDataPath(nameof(RollInputRPath))]
        public FDMiFloat RollInputR;
        public string FDRollPath = "FDRoll";

        [FDMiDataPath(nameof(FDRollPath))]
        public FDMiFloat FDRoll;
        public string RollPath = "Roll";

        [FDMiDataPath(nameof(RollPath))]
        public FDMiFloat Roll;
        public string HDGPath = "HDG";

        [FDMiDataPath(nameof(HDGPath))]
        public FDMiFloat HDG;
        public string HDGCommandPath = "HDGCommand";

        [FDMiDataPath(nameof(HDGCommandPath))]
        public FDMiFloat HDGCommand;
        public string RollLimitPath = "RollLimit";

        [FDMiDataPath(nameof(RollLimitPath))]
        public FDMiFloat RollLimit;
        public string LOCPath = "LOC";

        [FDMiDataPath(nameof(LOCPath))]
        public FDMiFloat LOC;
        public string CRSPath = "CRS";

        [FDMiDataPath(nameof(CRSPath))]
        public FDMiFloat CRS;
        public string IsPilotPath = "IsPilot";

        [FDMiDataPath(nameof(IsPilotPath))]
        public FDMiBool IsPilot;

        private AutoPilotSWMode apswMode;
        [SerializeField] float kpHDG;
        [SerializeField] float kpLOC, kiLOC, locTRKTransition = 10f, ilsTRKTransition = 2f;
        [SerializeField] float kp, ki, kq, a = 1f, FDMoveSpeed = 2f;
        float tau, rollRate, prevRoll;
        float rollInput, holdHDG, holdRoll;
        bool isYokeHold;
        float ret, hdgErr, pHdgErr, locErr, pLoc;
        float rollErr, output;

        void Start()
        {
            mode = RollMode.data;
            inL = RollInputL.data;
            inR = RollInputR.data;
            roll = Roll.data;
            hdg = HDG.data;
            hdgcmd = HDGCommand.data;
            rollLim = RollLimit.data;
            loc = LOC.data;
            crs = CRS.data;
            isPilot = IsPilot.data;
            apsw = APSW.data;
            tau = LPFTau(a);
            OnChangeAPSW();
        }

        public void OnChangeAPSW()
        {
            if (Mathf.Approximately(apsw[0], 0f))
            {
                apswMode = AutoPilotSWMode.OFF;
                APOutput.Data = 0f;
            }
            if (Mathf.Approximately(apsw[0], 0.5f))
            {
                apswMode = AutoPilotSWMode.CWS;
                holdRoll = roll[0];
            }
            if (Mathf.Approximately(apsw[0], 1f))
            {
                apswMode = AutoPilotSWMode.CMD;
                OnChange_RollMode();
            }
        }

        public void OnChange_RollMode()
        {
            output = 0f;
            rollErr = 0f;
            pHdgErr = 0f;
            switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case RollAutoPilotMode.CWS:
                    holdRoll = roll[0];
                    break;
                case RollAutoPilotMode.FMSCAP:
                case RollAutoPilotMode.FMSTRK:
                    // FMS未実装: モードのみ受付
                    break;
                case RollAutoPilotMode.LOCCAP:
                case RollAutoPilotMode.ILSCAP:
                case RollAutoPilotMode.HDGHOLD:
                    holdHDG = hdg[0];
                    pHdgErr = hdgRepeat(holdHDG - hdg[0]);
                    break;
                case RollAutoPilotMode.HDGSEL:
                    pHdgErr = hdgRepeat(hdgcmd[0] - hdg[0]);
                    break;
                case RollAutoPilotMode.LOCTRK:
                case RollAutoPilotMode.ILSTRK:
                    pLoc = loc[0];
                    break;
            }
        }

        float hdgRepeat(float c) { return Mathf.Repeat(c + 180f, 360f) - 180f; }

        float rollCommand()
        {
            if (isPilot[0])
            {
                switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
                {
                    case RollAutoPilotMode.FMSCAP:
                    case RollAutoPilotMode.FMSTRK:
                        // FMS未実装: モード遷移なし
                        break;
                    case RollAutoPilotMode.LOCCAP:
                        locErr = PControl(loc[0], kpLOC);
                        if (Mathf.Abs(loc[0]) < locTRKTransition && Mathf.Abs(locErr) < Mathf.Abs(crs[0] - hdg[0]))
                            RollMode.Data = (float)RollAutoPilotMode.LOCTRK;
                        break;
                    case RollAutoPilotMode.ILSCAP:
                        locErr = PControl(loc[0], kpLOC);
                        if (Mathf.Abs(loc[0]) < ilsTRKTransition) RollMode.Data = (float)RollAutoPilotMode.ILSTRK;
                        break;
                }
            }

            switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case RollAutoPilotMode.CWS:
                    // CWSのみゲイン非対称（P制御入力をそのまま出力）。意図的な挙動
                    if (isYokeHold)
                    {
                        holdRoll = roll[0];
                        return rollRate;
                    }
                    return holdRoll - roll[0];
                case RollAutoPilotMode.FMSCAP:
                case RollAutoPilotMode.FMSTRK:
                    // FMS未実装: hdgErr未更新
                    break;
                case RollAutoPilotMode.HDGHOLD:
                case RollAutoPilotMode.LOCCAP:
                case RollAutoPilotMode.ILSCAP:
                    hdgErr = hdgRepeat(holdHDG - hdg[0]);
                    break;
                case RollAutoPilotMode.HDGSEL:
                    hdgErr = hdgRepeat(hdgcmd[0] - hdg[0]);
                    break;
                case RollAutoPilotMode.LOCTRK:
                case RollAutoPilotMode.ILSTRK:
                    locErr = PIControl(loc[0], pLoc, locErr, kpLOC, kiLOC);
                    hdgErr = hdgRepeat(crs[0] + locErr - hdg[0]);
                    pLoc = loc[0];
                    break;
            }
            ret = Mathf.Clamp(PControl(hdgErr, kpHDG), -rollLim[0], rollLim[0]);
            pHdgErr = hdgErr;
            return ret - roll[0];
        }

        void FixedUpdate()
        {
            rollInput = Mathf.Clamp01(Mathf.Abs(inL[0]) + Mathf.Abs(inR[0])); // 左右逆入力でも相殺しない
            isYokeHold = rollInput > 0.05f;
            rollRate = LPF((roll[0] - prevRoll) / Time.fixedDeltaTime, rollRate, tau);
            prevRoll = roll[0];

            rollErr = rollCommand();

            float nextOutput = Mathf.Clamp(PControl(rollErr - rollRate, kq), -1f, 1f);
            output = Mathf.MoveTowards(output, nextOutput, Time.fixedDeltaTime * FDMoveSpeed);
            FDRoll.Data = output;
            if (apswMode == AutoPilotSWMode.OFF) return;
            if (apswMode == AutoPilotSWMode.CMD) APOutput.Data = output;
            if (apswMode == AutoPilotSWMode.CWS)
            {
                if (isYokeHold)
                {
                    holdRoll = roll[0];
                    nextOutput = 0f;
                }
                else
                {
                    rollErr = holdRoll - roll[0];
                    nextOutput = Mathf.Clamp(PControl(rollErr - rollRate, kq), -1f, 1f);
                }
                APOutput.Data = Mathf.MoveTowards(APOutput.Data, nextOutput, Time.fixedDeltaTime * FDMoveSpeed);
            }
        }

        float[] mode, inL, inR, roll, hdg, hdgcmd, rollLim, loc, crs, apsw;
        bool[] isPilot;
    }
}
