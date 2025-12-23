
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
        public FDMiSByte APRoll, RollInputL, RollInputR;
        public FDMiFloat APSW, _RollMode, FDRoll;
        public FDMiFloat Roll, HDG, IAS, HDGCommand, RollLimit, _LOC, _CRS;
        public FDMiBool IsPilot;
        private AutoPilotSWMode apswMode;
        private float[] mode, roll, hdg, ias, hdgcmd, rollLim, loc, crs;
        private sbyte[] apRoll, inL, inR;
        private bool[] isPilot;
        [SerializeField] float kpHDG;
        [SerializeField] float kpLOC;
        [SerializeField] float locTRKTransition = 10f, ilsTRKTransition = 2f;
        [SerializeField] float kp, ki, kd, a = 1f, FDMoveSpeed = 2f, rollRateLimit = 7.5f, yokeSpeedLimit = 1f;
        float tau, rollRate, prevRoll;
        float holdHDG, holdRoll;

        void Start()
        {
            mode = _RollMode.data;
            roll = Roll.data;
            inL = RollInputL.data;
            inR = RollInputR.data;
            hdg = HDG.data;
            ias = IAS.data;
            hdgcmd = HDGCommand.data;
            rollLim = RollLimit.data;
            loc = _LOC.data;
            crs = _CRS.data;
            apRoll = APRoll.data;
            isPilot = IsPilot.data;

            APSW.subscribe(this, "OnChangeAPSW");
            _RollMode.subscribe(this, "OnChange_RollMode");
            IsPilot.subscribe(this, "OnChange_RollMode");
            tau = LPFTau(a);
            OnChangeAPSW();
        }
        public void OnChangeAPSW()
        {
            rollOutput = 0f;
            rollErr = 0f;
            if (Mathf.Approximately(APSW.data[0], 0f))
            {
                apswMode = AutoPilotSWMode.OFF;
                apRoll[0] = 0;
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
            rollOutput = 0f;
            rollErr = 0f;
            switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case RollAutoPilotMode.CWS:
                    holdRoll = roll[0];
                    break;
                case RollAutoPilotMode.LOCCAP:
                case RollAutoPilotMode.ILSCAP:
                    prevLoc = loc[0];
                    holdHDG = hdg[0];
                    pHdgErr = hdgErr;
                    break;
                case RollAutoPilotMode.HDGHOLD:
                    holdHDG = hdg[0];
                    pHdgErr = hdgErr;
                    break;
            }
        }

        float hdgRepeat(float c) { return (Mathf.Abs(c) >= 180f) ? c - 360f * Mathf.Sign(c) : c; }

        float rollCmd, hdgErr, pHdgErr, locErr, prevLoc, dLoc, iLoc;
        float rollCommand()
        {
            if(isPilot[0]){
                switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
                {
                    case RollAutoPilotMode.LOCCAP:
                        locErr = PControl(loc[0], kpLOC);
                        if (Mathf.Abs(loc[0]) < locTRKTransition)
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
                    hdgErr = 0f;
                    return roll[0];
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
                    locErr = PControl(loc[0], kpLOC);
                    hdgErr = hdgRepeat(crs[0] + locErr - hdg[0]);
                    break;
            }
            rollCmd = PControl(hdgErr, kpHDG);
            rollCmd = Mathf.Clamp(rollCmd, -rollLim[0], rollLim[0]);
            pHdgErr = hdgErr;
            return rollCmd;
        }
        float rollErr, pRollErr, rollOutput;
        void Update()
        {
            // if (!IsPilot.data[0]) return;
            rollRate = LPF((roll[0] - prevRoll) / Time.deltaTime, rollRate, tau);
            prevRoll = roll[0];

            pRollErr = rollErr;
            rollErr = rollCommand() - roll[0];
            FDRoll.Data = Mathf.MoveTowards(FDRoll.Data, rollErr, FDMoveSpeed * Time.deltaTime);

            if (apswMode == AutoPilotSWMode.OFF) return;

            float rollInput = inL[0] + inR[0];
            if (apswMode == AutoPilotSWMode.CWS || mode[0] == (float)RollAutoPilotMode.CWS)
            {
                rollErr = holdRoll - roll[0];
                if (Mathf.Abs(rollInput) > 5)
                {
                    holdRoll = Mathf.Abs(roll[0]) <= 5f ? 0f : roll[0];
                    rollOutput = 0f;
                    apRoll[0] = 0;
                    return;
                }
            }

            rollErr -= kd * rollRate;
            rollOutput = PIControl(rollErr, pRollErr, rollOutput, kp / Mathf.Max(ias[0], 1), ki / Mathf.Max(ias[0], 1));
            rollOutput = Mathf.Clamp(rollOutput, -1f, 1f);
            apRoll[0] = (sbyte)Mathf.MoveTowards(apRoll[0], rollOutput * 127, yokeSpeedLimit * 127 * Time.deltaTime);
        }
    }
}