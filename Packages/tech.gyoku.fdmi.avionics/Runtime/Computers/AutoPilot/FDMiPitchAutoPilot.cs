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
        public FDMiFloat APSW, _PitchMode, APOutput, PitchInputL, PitchInputR, _FDPitch, TrimCommand;
        public FDMiFloat VSCommand, AltCommand, _GS, _LOC;
        public FDMiFloat Pitch, Roll, VerticalSpeed, Altitude, TAS, Alpha;
        public FDMiBool IsPilot;
        private AutoPilotSWMode apswMode = AutoPilotSWMode.OFF;
        private float[] mode, inL, inR, pitch, roll, vs, alt, tas, alpha, vscmd, altcmd, gs, loc, fdPitch;
        private bool[] isPilot;
        [SerializeField] float kp, ki, kq, a = 1f, FDMoveSpeed = 2f, pitchRateLimit = 2.5f;
        [SerializeField] float kpGS, kiGS, GSbias = 2.54f;
        [SerializeField] float kpalt;
        [SerializeField] float autoTrimThreshold = 0.05f, autoTrimGain = 0.5f;
        float cmd, err, omega, vsLPF;
        void Start()
        {
            mode = _PitchMode.data;
            inL = PitchInputL.data;
            inR = PitchInputR.data;
            pitch = Pitch.data;
            roll = Roll.data;
            vs = VerticalSpeed.data;
            alt = Altitude.data;
            tas = TAS.data;
            alpha = Alpha.data;
            gs = _GS.data;
            loc = _LOC.data;
            vscmd = VSCommand.data;
            altcmd = AltCommand.data;
            isPilot = IsPilot.data;
            fdPitch = _FDPitch.data;
            APSW.subscribe(this, "OnChangeAPSW");
            VSCommand.subscribe(this, "OnChangeVSCommand");
            _PitchMode.subscribe(this, "OnChange_PitchMode");
            IsPilot.subscribe(this, "OnChange_PitchMode");
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
            }
        }
        public void OnChangeVSCommand()
        {
            if (!isPilot[0]) return;
            if (Mathf.Approximately(vscmd[0], 0f))
            {
                if (_PitchMode.Data != (int)PitchAutoPilotMode.ALTHOLD) _PitchMode.Data = (int)PitchAutoPilotMode.ALTHOLD;
                return;
            }
            if (_PitchMode.Data != (int)PitchAutoPilotMode.VS) _PitchMode.Data = (int)PitchAutoPilotMode.VS;

        }
        public void OnChange_PitchMode()
        {
            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.CWS:
                    holdPitch = pitch[0];
                    break;
                case PitchAutoPilotMode.ALTHOLD:
                case PitchAutoPilotMode.GSCAP:
                    holdAlt = alt[0];
                    break;
            }
        }

        float holdPitch, holdAlt;
        float altErr, pGSErr, pgsI;
        float pitchCommand()
        {
            float pitchCmd = 0f;
            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.GSCAP:
                    if (!Mathf.Approximately(gs[0], 0f) && Mathf.Abs(loc[0]) < 8f && Mathf.Abs(gs[0]) < 0.075f) _PitchMode.Data = (int)PitchAutoPilotMode.GSTRK;
                    break;
                case PitchAutoPilotMode.ALTCAP:
                    if (Mathf.Abs(altErr) < 3f) _PitchMode.Data = (int)PitchAutoPilotMode.ALTHOLD;
                    break;
                case PitchAutoPilotMode.VS:
                    altErr = altcmd[0] * 0.3048f - alt[0];
                    if (Mathf.Abs(altErr) * kpalt < Mathf.Abs(vscmd[0]) && altErr * vscmd[0] > 0)
                        _PitchMode.Data = (int)PitchAutoPilotMode.ALTCAP;
                    break;
            }

            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.CWS:
                    return pitch[0] - alpha[0];
                case PitchAutoPilotMode.GSTRK:
                    pgsI = IControl(gs[0], pGSErr, pgsI, kiGS);
                    pitchCmd = PControl(gs[0], kpGS) + pgsI + GSbias;
                    pGSErr = gs[0];
                    break;
                case PitchAutoPilotMode.ALTHOLD:
                case PitchAutoPilotMode.GSCAP:
                    VSCommand.Data = 0f;
                    // if vert speed is high, ease target altitude
                    if (Mathf.Abs(vs[0]) > 2.54f) holdAlt = alt[0];
                    altErr = PControl(holdAlt - alt[0], kpalt);
                    pitchCmd = altErr * 0.508f / (tas[0] * Mathf.Deg2Rad);
                    break;
                case PitchAutoPilotMode.ALTCAP:
                    altErr = altcmd[0] * 0.3048f - alt[0];
                    VSCommand.Data = PControl(altErr, kpalt);
                    pitchCmd = vscmd[0] * 0.508f / (tas[0] * Mathf.Deg2Rad);
                    break;
                case PitchAutoPilotMode.VS:
                    pitchCmd = vscmd[0] * 0.508f / (tas[0] * Mathf.Deg2Rad);
                    break;
            }
            return pitchCmd;
        }
        float pitchRate, prevPitch, prevPitchRate, prevPitchErr, pitchRateErr;

        void FixedUpdate()
        {
            float output = 0f;
            pitchRate = LPF((pitch[0] - prevPitch) / Time.fixedDeltaTime, pitchRate, omega);
            prevPitch = pitch[0];
            vsLPF = LPF(vs[0], vsLPF, omega);
            if (Mathf.RoundToInt(mode[0]) < (int)PitchAutoPilotMode.VS || Mathf.RoundToInt(mode[0]) > (int)PitchAutoPilotMode.ALTHOLD)
            {
                VSCommand.Data = vsLPF * 1.9685039f;
            }

            float pitchErr = pitchCommand() + alpha[0] - pitch[0];
            _FDPitch.Data = Mathf.MoveTowards(_FDPitch.Data, pitchErr, Time.fixedDeltaTime * FDMoveSpeed);

            if (apswMode == AutoPilotSWMode.OFF) return;
            // if (apswMode == AutoPilotSWMode.CMD) APOutput.Data = output;
            if (apswMode == AutoPilotSWMode.CWS || Mathf.RoundToInt(mode[0]) == (int)PitchAutoPilotMode.CWS)
            {
                float pitchInput = Mathf.Clamp(inL[0] + inR[0], -1, 1);
                if (Mathf.Abs(pitchInput) > 0.05f)
                {
                    holdPitch = pitch[0];
                    output = 0f;
                    APOutput.Data = 0f;
                    return;
                }
                pitchErr = holdPitch - pitch[0];
            }

            // prevPitchI = Mathf.Clamp(IControl(pitchErr, prevPitchErr, prevPitchI, ki), -1, 1);
            // float output = PControl(pitchErr, kp) + PControl(-pitchRate, kq) + prevPitchI;
            // pitchRateErr = PControl(pitchErr, kp) + IControl(pitchErr, pitchRateErr, ki);
            pitchRateErr = PIControl(pitchErr, prevPitchErr, pitchRateErr, kp, ki);
            pitchRateErr = Mathf.Clamp(pitchRateErr, -pitchRateLimit, pitchRateLimit);
            prevPitchErr = pitchErr;

            output = PControl(pitchRateErr - pitchRate, kq);
            output = Mathf.Clamp(output, -1f, 1f);

            APOutput.Data = output;
            // autotrim
            if (Mathf.Abs(output) > autoTrimThreshold) TrimCommand.Data += output * autoTrimGain * Time.fixedDeltaTime;
        }
    }
}