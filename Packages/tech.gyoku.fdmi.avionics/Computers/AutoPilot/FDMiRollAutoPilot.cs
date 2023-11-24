
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public enum RollAutoPilotMode { CWS, HDGHOLD, HDGSEL, FMSCAP, FMSTRK, LOCCAP, LOCTRK };
    public class FDMiRollAutoPilot : FDMiAutoPilot
    {
        public FDMiFloat APSW, RollMode, APOutput, RollInputL, RollInputR, FDRoll;
        public FDMiFloat Roll, HDG, HDGCommand, RollLimit, LOC;
        private AutoPilotSWMode apswMode;
        private float[] mode, inL, inR, roll, hdg, hdgcmd, rollLim, loc;
        [SerializeField] float kpHDG, kiHDG;
        [SerializeField] float kpLOC, kiLOC;
        [SerializeField] float kp, ki, kq, a = 1f;
        float tau, rollRate, prevRoll;
        float rollInput, holdHDG, holdRoll;

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

            APSW.subscribe(this, "OnChangeAPSW");
            RollMode.subscribe(this, "OnChangeRollMode");
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
                OnChangeRollMode();
            }
        }

        public void OnChangeRollMode()
        {
            output = 0f;
            rollErr = 0f;
            pRollErr = 0f;
            switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case RollAutoPilotMode.CWS:
                    holdRoll = roll[0];
                    break;
                case RollAutoPilotMode.HDGHOLD:
                    holdHDG = hdg[0];
                    break;
            }
        }

        float hdgRepeat(float c) { return (Mathf.Abs(c) >= 180f) ? c - 360f * Mathf.Sign(c) : c; }

        float ret, ploc;
        float rollCommand()
        {
            if (apswMode == AutoPilotSWMode.CWS)
            {
                holdRoll = Mathf.Lerp(holdRoll, roll[0], rollInput);
                return holdRoll - roll[0];
            }
            switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case RollAutoPilotMode.CWS:
                    holdRoll = Mathf.Lerp(holdRoll, roll[0], rollInput);
                    return holdRoll - roll[0];
                case RollAutoPilotMode.HDGHOLD:
                    ret = hdgRepeat(holdHDG - hdg[0]);
                    break;
                case RollAutoPilotMode.LOCCAP:
                case RollAutoPilotMode.HDGSEL:
                    ret = hdgRepeat(hdgcmd[0] - hdg[0]);
                    break;
                case RollAutoPilotMode.LOCTRK:
                    ret = PControl(loc[0], kpLOC);
                    ploc = loc[0];
                    break;

            }
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

            output = Mathf.Clamp(PControl(rollErr - rollRate, kq) * (1f - rollInput), -1f, 1f);
            FDRoll.Data = output;
            APOutput.Data = Mathf.Clamp(output * Mathf.Round(APSW.data[0] + 0.1f), -1f, 1f);
        }
    }
}