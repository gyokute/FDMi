using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public enum AutoPilotSWMode { OFF, CWS, CMD };
    public enum PitchAutoPilotMode { IASHOLD, MACHOLD, TURB, VS, ALTCAP, ALTHOLD, CWS, GSCAP, GSTRK, GA, TAKEOFF, VNAV };
    public class FDMiPitchAutoPilot : FDMiAutoPilot
    {
        public FDMiFloat APSW, PitchMode, PitchOutput, PitchInputL, PitchInputR, FDPitch;
        public FDMiFloat VSCommand, AltCommand;
        public FDMiFloat Pitch, Roll, VerticalSpeed, Altitude;
        private AutoPilotSWMode apswMode = AutoPilotSWMode.OFF;
        private float[] mode, inL, inR, pitch, roll, vs, alt, vscmd, altcmd;
        [SerializeField] float kp, ki, kAlt, kpVS, kiVS, kq, a = 1f;
        float output, pOut, cmd, err, pErr, omega, vsLPF;
        float pitchRate, prevPitch;
        float pitchInput, holdAlt, holdPitch;
        void Start()
        {
            mode = PitchMode.data;
            inL = PitchInputL.data;
            inR = PitchInputR.data;
            pitch = Pitch.data;
            roll = Roll.data;
            vs = VerticalSpeed.data;
            alt = Altitude.data;
            vscmd = VSCommand.data;
            altcmd = AltCommand.data;
            APSW.subscribe(this, "OnChangeAPSW");
            VSCommand.subscribe(this, "OnChangeVSCommand");
            PitchMode.subscribe(this, "OnChangePitchMode");
            omega = LPFTau(a);
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
                if (isOwner) PitchMode.Data = (int)PitchAutoPilotMode.CWS;
                holdPitch = pitch[0];
            }
            if (Mathf.Approximately(APSW.data[0], 1f))
            {
                apswMode = AutoPilotSWMode.CMD;
                if (isOwner) PitchMode.Data = (int)PitchAutoPilotMode.ALTHOLD;
            }
        }
        public void OnChangeVSCommand()
        {
            mode[0] = (float)PitchAutoPilotMode.VS;
        }
        public void OnChangePitchMode()
        {
            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.ALTHOLD:
                    holdAlt = alt[0];
                    break;
            }
        }
        float ret, pret, pVSErr, pPitchErr;
        float pitchCommand()
        {
            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.ALTHOLD:
                    ret = PIControl(-vs[0], pVSErr, ret, kpVS, kiVS);
                    pVSErr = -vs[0];
                    break;
                case PitchAutoPilotMode.ALTCAP:
                    return kAlt * Mathf.Clamp(altcmd[0] * 30.48f - alt[0], -30.48f, 30.48f);
                case PitchAutoPilotMode.VS:
                    return kpVS * (vscmd[0] - vs[0] * 1.9685039f);
                case PitchAutoPilotMode.CWS:
                    holdPitch = Mathf.Lerp(holdPitch, pitch[0], Mathf.Abs(pitchInput));
                    ret = PIControl(holdPitch - pitch[0], pPitchErr, ret, kp, ki);
                    pPitchErr = holdPitch - pitch[0];
                    break;
            }
            return ret;
        }
        float rateErr, pRateErr;

        void FixedUpdate()
        {
            pitchInput = Mathf.Clamp(inL[0] + inR[0], -1f, 1f);
            vsLPF = LPF(vs[0], vsLPF, omega);
            if (!Mathf.Approximately(mode[0], (int)PitchAutoPilotMode.VS))
            {
                VSCommand.data[0] = vsLPF * 1.9685039f;
            }

            pitchRate = LPF((Pitch.Data - prevPitch) / Time.fixedDeltaTime, pitchRate, omega);
            prevPitch = pitch[0];

            output = Mathf.Clamp(pitchCommand() - kq * pitchRate, -1f, 1f);
            FDPitch.Data = output;
            PitchOutput.Data = Mathf.Clamp(output * Mathf.Round(APSW.data[0]) + pitchInput, -1f, 1f);
        }
    }
}