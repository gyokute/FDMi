
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public enum RollAutoPilotMode { CWS, HDGHOLD, HDGSEL, FMSCAP, FMSTRK, LOCCAP, LOCTRK, ILSCAP, ILSTRK };
    public class FDMiRollAutoPilot : FDMiAutoPilot
    {
        public FDMiFloat APSW, _RollMode, APOutput, RollInputL, RollInputR, _FDRoll;
        public FDMiFloat Roll, HDG, HDGCommand, RollLimit, _LOC, _CRS;
        public FDMiBool IsPilot;
        private AutoPilotSWMode apswMode;
        private float[] mode, inL, inR, roll, hdg, hdgcmd, rollLim, loc, crs;
        private bool[] isPilot;
        [SerializeField] float kpHDG;
        [SerializeField] float kpLOC, kiLOC, locTRKTransition = 10f, ilsTRKTransition = 2f;
        [SerializeField] float kp, ki, kq, a = 1f, FDMoveSpeed = 2f;
        float tau, rollRate, prevRoll;
        float rollInput, holdHDG, holdRoll;
        bool isYokeHold;

        void Start()
        {
            mode = _RollMode.data;
            inL = RollInputL.data;
            inR = RollInputR.data;
            roll = Roll.data;
            hdg = HDG.data;
            hdgcmd = HDGCommand.data;
            rollLim = RollLimit.data;
            loc = _LOC.data;
            crs = _CRS.data;
            isPilot = IsPilot.data;

            APSW.subscribe(this, "OnChangeAPSW");
            _RollMode.subscribe(this, "OnChange_RollMode");
            tau = LPFTau(a);
            OnChangeAPSW();
        }
        public void OnChangeAPSW()
        {
            if (Mathf.Approximately(APSW.data[0], 0f))
            {
                apswMode = AutoPilotSWMode.OFF;
                APOutput.Data = 0f;
            }
            if (Mathf.Approximately(APSW.data[0], 0.5f))
            {
                apswMode = AutoPilotSWMode.CWS;
                holdRoll = roll[0];
            }
            if (Mathf.Approximately(APSW.data[0], 1f))
            {
                apswMode = AutoPilotSWMode.CMD;
                OnChange_RollMode();
            }
        }
        public void OnChange_RollMode()
        {
            output = 0f;
            rollErr = 0f;
            pRollErr = 0f;
            pHdgErr = 0f;
            switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case RollAutoPilotMode.CWS:
                    holdRoll = roll[0];
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
                    iLoc = 0;
                    break;
            }
        }

        float hdgRepeat(float c) { return (Mathf.Abs(c) >= 180f) ? c - 360f * Mathf.Sign(c) : c; }

        float ret, hdgErr, pHdgErr, locErr, plocErr, pLoc, iLoc;
        float rollCommand()
        {
            if (isPilot[0])
            {
                switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
                {
                    case RollAutoPilotMode.LOCCAP:
                        locErr = PControl(loc[0], kpLOC);
                        if (Mathf.Abs(loc[0]) < locTRKTransition && Mathf.Abs(locErr) < Mathf.Abs(crs[0] - hdg[0]))
                            _RollMode.Data = (float)RollAutoPilotMode.LOCTRK;
                        break;
                    case RollAutoPilotMode.ILSCAP:
                        locErr = PControl(loc[0], kpLOC);
                        if (Mathf.Abs(loc[0]) < ilsTRKTransition) _RollMode.Data = (float)RollAutoPilotMode.ILSTRK;
                        break;
                }
            }

            switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case RollAutoPilotMode.CWS:
                    if (isYokeHold)
                    {
                        holdRoll = roll[0];
                        return rollRate;
                    }
                    return holdRoll - roll[0];
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
                    iLoc = IControl(locErr, plocErr, iLoc, kiLOC);
                    locErr = PControl(loc[0], kpLOC) + iLoc;
                    // locErr = PIControl(loc[0], pLoc, locErr, kpLOC, kiLOC);
                    hdgErr = hdgRepeat(crs[0] + locErr - hdg[0]);
                    pLoc = loc[0];
                    plocErr = locErr;
                    break;
            }
            ret = Mathf.Clamp(PControl(hdgErr, kpHDG), -rollLim[0], rollLim[0]);
            // ret = PIControl(hdgErr, pHdgErr, ret, kpHDG, kiHDG);
            pHdgErr = hdgErr;
            return ret;
        }
        float rollErr, pRollErr, output;
        void FixedUpdate()
        {
            rollInput = Mathf.Clamp01(Mathf.Abs(inL[0] + inR[0]));
            isYokeHold = rollInput > 0.05f;
            rollRate = LPF((roll[0] - prevRoll) / Time.fixedDeltaTime, rollRate, tau);
            prevRoll = roll[0];

            rollErr = rollCommand() - roll[0];
            pRollErr = rollErr;

            float nextOutput = PControl(rollErr, kp) + PControl(-rollRate, kq);
            output = Mathf.MoveTowards(output, nextOutput, Time.fixedDeltaTime * FDMoveSpeed);
            _FDRoll.Data = output;
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
                    nextOutput = PControl(rollErr, kp) + PControl(-rollRate, kq);
                }
                APOutput.Data = Mathf.MoveTowards(APOutput.Data, nextOutput, Time.fixedDeltaTime * FDMoveSpeed);
            }
        }
    }
}