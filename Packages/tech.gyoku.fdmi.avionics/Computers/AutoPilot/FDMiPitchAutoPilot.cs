using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public enum AutoPilotSWMode { OFF, CWS, CMD };
    public enum PitchAutoPilotMode { CWS, IASHOLD, MACHHOLD, TURB, VS, ALTCAP, ALTHOLD, GSCAP, GSTRK, GA, TAKEOFF };
    public class FDMiPitchAutoPilot : FDMiAutoPilot
    {
        public FDMiFloat APSW, PitchMode, APOutput, PitchInputL, PitchInputR, FDPitch;
        public FDMiFloat VSCommand, AltCommand;
        public FDMiFloat Pitch, Roll, VerticalSpeed, Altitude;
        private AutoPilotSWMode apswMode = AutoPilotSWMode.OFF;
        private float[] mode, inL, inR, pitch, roll, vs, alt, vscmd, altcmd;
        [SerializeField] float kp, ki, kAlt, kq, a = 1f;
        [SerializeField] float kpVS, kiVS, VSLimit = 22.86f;
        float cmd, err, pErr, omega, vsLPF;
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
                holdPitch = pitch[0];
            }
            if (Mathf.Approximately(APSW.data[0], 1f))
            {
                apswMode = AutoPilotSWMode.CMD;
                OnChangePitchMode();
            }
        }
        public void OnChangeVSCommand()
        {
            if (Mathf.Approximately(vscmd[0], 0f))
            {
                if (PitchMode.Data != (int)PitchAutoPilotMode.ALTHOLD) PitchMode.Data = (int)PitchAutoPilotMode.ALTHOLD;
                return;
            }
            if (PitchMode.Data != (int)PitchAutoPilotMode.VS) PitchMode.Data = (int)PitchAutoPilotMode.VS;

        }
        public void OnChangePitchMode()
        {
            output = 0f;
            ret = 0f;
            vsErr = 0f;
            pPitchErr = 0f;

            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.CWS:
                    holdPitch = pitch[0];
                    break;
                case PitchAutoPilotMode.ALTHOLD:
                    holdAlt = alt[0];
                    break;
            }
        }

        float pitchInput, holdAlt, holdPitch;
        public float rate, pRate, ret, pret, vsErr, pVSErr, rateErr, pPitchErr;
        float pitchCommand()
        {
            if (apswMode == AutoPilotSWMode.CWS)
            {
                holdPitch = Mathf.Lerp(holdPitch, pitch[0], pitchInput);
                return PControl(holdPitch - pitch[0], kp);
            }
            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.CWS:
                    holdPitch = Mathf.Lerp(holdPitch, pitch[0], pitchInput);
                    return PControl(holdPitch - pitch[0], kp);

                case PitchAutoPilotMode.ALTCAP:
                    vsErr = Mathf.Clamp(kAlt * (altcmd[0] * 0.3048f - alt[0]), -22.86f, 22.86f);
                    ret = PIControl(vsErr - vs[0], pVSErr, ret, kpVS, kiVS);
                    pVSErr = vsErr;
                    break;
                    return kAlt * Mathf.Clamp(altcmd[0] * 30.48f - alt[0], -30.48f, 30.48f);
                case PitchAutoPilotMode.ALTHOLD:
                case PitchAutoPilotMode.VS:
                    vsErr = PIControl(vscmd[0] * 0.508f - vs[0], pVSErr, vsErr, kpVS, kiVS);
                    pVSErr = vscmd[0] * 0.508f - vs[0];
                    ret = PControl(vsErr - pitch[0], kp);
                    break;
            }
            return ret;
        }
        float pitchRate, prevPitch, pitchErr, output;

        void FixedUpdate()
        {
            pitchInput = Mathf.Clamp01(Mathf.Abs(inL[0] + inR[0]) * 10f);
            vsLPF = LPF(vs[0], vsLPF, omega);
            if (!Mathf.Approximately(mode[0], (int)PitchAutoPilotMode.VS) && !Mathf.Approximately(mode[0], (int)PitchAutoPilotMode.ALTHOLD))
            {
                VSCommand.data[0] = vsLPF * 1.9685039f;
            }
            pitchRate = LPF((pitch[0] - prevPitch) / Time.fixedDeltaTime, pitchRate, omega);
            prevPitch = pitch[0];

            pitchErr = pitchCommand();
            output = Mathf.Clamp(PControl(pitchErr - pitchRate, kq), -1f, 1f);
            FDPitch.Data = output;
            if (apswMode == AutoPilotSWMode.OFF) return;
            // output *= (1f - pitchInput);
            APOutput.Data = Mathf.Clamp(output, -1f, 1f);
        }
    }
}