
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public enum RollAutoPilotMode { HDGHOLD, HDGSEL, FMSCAP, FMSTRK, RADCAP, RADTRK, CWS, LOCCAP, LOCTRK };
    public class FDMiRollAutoPilot : FDMiAutoPilot
    {
        public FDMiFloat APSW, RollMode, RollOutput, RollInputL, RollInputR, FDRoll;
        public FDMiFloat Roll, HDG, HDGCommand, RollLimit, NAVRMI, NAVCRS;
        private AutoPilotSWMode apswMode;
        private float[] mode, inL, inR, roll, hdg, hdgcmd, rollLim;
        [SerializeField] float kp, ki, kInput = 0.1f, kq, a = 1f;
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

            APSW.subscribe(this, "OnChangeAPSW");
            RollMode.subscribe(this, "OnChangeRollMode");
            tau = LPFTau(a);
        }
        public void OnChangeAPSW()
        {
            if (Mathf.Approximately(APSW.data[0], 0f))
            {
                apswMode = AutoPilotSWMode.OFF;
            }
            if (Mathf.Approximately(APSW.data[0], 0.5f))
            {
                apswMode = AutoPilotSWMode.CWS;
                if (isOwner) RollMode.Data = (int)RollAutoPilotMode.CWS;
                holdRoll = roll[0];
            }
            if (Mathf.Approximately(APSW.data[0], 1f))
            {
                apswMode = AutoPilotSWMode.CMD;
                if (isOwner) RollMode.Data = (int)RollAutoPilotMode.HDGHOLD;
            }
        }
        public void OnChangeRollMode()
        {
            switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case RollAutoPilotMode.HDGHOLD:
                    holdHDG = hdg[0];
                    break;
            }
        }

        float hdgRepeat(float c) { return (Mathf.Abs(c) >= 180f) ? c - 360f * Mathf.Sign(c) : c; }

        float rollCommand()
        {
            float ret = 0f;
            switch ((RollAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case RollAutoPilotMode.HDGHOLD:
                    ret = hdgRepeat(holdHDG - hdg[0]);
                    break;
                case RollAutoPilotMode.HDGSEL:
                    ret = hdgRepeat(hdgcmd[0] - hdg[0]);
                    break;
                case RollAutoPilotMode.CWS:
                    holdRoll = Mathf.Lerp(holdRoll, roll[0], Mathf.Abs(rollInput));
                    ret = holdRoll;
                    break;
            }
            return Mathf.Clamp(ret, -rollLim[0], rollLim[0]) - roll[0];
        }
        float rollErr, pRollErr, angleOut, rateErr, output, pOut;
        void FixedUpdate()
        {
            rollInput = Mathf.Clamp(inL[0] + inR[0], -1f, 1f);
            if (apswMode == AutoPilotSWMode.OFF) { RollOutput.Data = rollInput; return; }
            rollRate = LPF((roll[0] - prevRoll) / Time.fixedDeltaTime, rollRate, tau);
            prevRoll = roll[0];

            rollErr = rollCommand() - rollRate * kq;
            rateErr = PIControl(rollErr, pRollErr, angleOut, kp, ki);
            pRollErr = rollErr;

            output = Mathf.Clamp(rateErr - kq * rollRate, -1f, 1f);
            FDRoll.Data = output;
            RollOutput.Data = Mathf.Clamp(output + rollInput, -1f, 1f);
        }
    }
}