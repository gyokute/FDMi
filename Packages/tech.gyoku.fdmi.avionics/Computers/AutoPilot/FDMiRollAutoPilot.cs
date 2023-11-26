
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
        private AutoPilotSWMode apswMode;
        private float[] mode, inL, inR, roll, hdg, hdgcmd, rollLim, loc, crs;
        [SerializeField] float kpHDG, kiHDG;
        [SerializeField] float kpLOC, kiLOC, locTRKTransition = 10f, ilsTRKTransition = 2f;
        [SerializeField] float kp, ki, kq, a = 1f;
        float tau, rollRate, prevRoll;
        float rollInput, holdHDG, holdRoll;

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
                    break;
                case RollAutoPilotMode.LOCTRK:
                case RollAutoPilotMode.ILSTRK:
                    pLoc = loc[0];
                    break;
            }
        }

        float hdgRepeat(float c) { return (Mathf.Abs(c) >= 180f) ? c - 360f * Mathf.Sign(c) : c; }

        float ret, hdgErr, pHdgErr, locErr, pLoc;
        float rollCommand()
        {
            if (isOwner)
            {
                switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
                {
                    case RollAutoPilotMode.LOCCAP:
                        locErr = PControl(loc[0], kpLOC);
                        if (Mathf.Abs(loc[0]) < locTRKTransition) _RollMode.Data = (float)RollAutoPilotMode.LOCTRK;
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
                    holdRoll = Mathf.Lerp(holdRoll, roll[0], rollInput);
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
                    // locErr = PControl(loc[0], kpLOC);
                    locErr = PIControl(loc[0], pLoc, locErr, kpLOC, kiLOC);
                    hdgErr = hdgRepeat(crs[0] + locErr - hdg[0]);
                    Debug.Log(locErr);
                    pLoc = loc[0];
                    break;
            }
            ret = PIControl(hdgErr, pHdgErr, ret, kpHDG, kiHDG);
            pHdgErr = hdgErr;
            return Mathf.Clamp(ret, -rollLim[0], rollLim[0]) - roll[0];
        }
        float rollErr, pRollErr, output;
        void FixedUpdate()
        {
            rollInput = Mathf.Clamp01(Mathf.Abs(inL[0] + inR[0]));
            rollRate = LPF((roll[0] - prevRoll) / Time.fixedDeltaTime, rollRate, tau);
            prevRoll = roll[0];

            rollErr = rollCommand();
            pRollErr = rollErr;

            if (apswMode == AutoPilotSWMode.CWS)
            {
                holdRoll = Mathf.Lerp(holdRoll, roll[0], rollInput);
                rollErr = holdRoll - roll[0];
            }

            output = Mathf.Clamp(PControl(rollErr - rollRate, kq) * (1f - rollInput), -1f, 1f);
            _FDRoll.Data = output;
            APOutput.Data = Mathf.Clamp(output * Mathf.Round(APSW.data[0] + 0.1f), -1f, 1f);
        }
    }
}