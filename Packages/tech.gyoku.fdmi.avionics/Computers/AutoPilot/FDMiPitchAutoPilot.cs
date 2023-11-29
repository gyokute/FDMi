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
        public FDMiFloat VSCommand, AltCommand, _GS;
        public FDMiFloat Pitch, Roll, VerticalSpeed, Altitude;
        private AutoPilotSWMode apswMode = AutoPilotSWMode.OFF;
        private float[] mode, inL, inR, pitch, roll, vs, alt, vscmd, altcmd, gs;
        [SerializeField] float kp, kq, a = 1f, pitchLimit = 20f;
        [SerializeField] float kpVS, kiVS, kpGS, kiGS;
        [SerializeField] float kpalt, kialt;
        [SerializeField] float autoTrimThreshold = 0.05f, autoTrimGain = 0.5f;
        float cmd, err, pErr, omega, vsLPF;
        void Start()
        {
            mode = _PitchMode.data;
            inL = PitchInputL.data;
            inR = PitchInputR.data;
            pitch = Pitch.data;
            roll = Roll.data;
            vs = VerticalSpeed.data;
            alt = Altitude.data;
            gs = _GS.data;
            vscmd = VSCommand.data;
            altcmd = AltCommand.data;
            APSW.subscribe(this, "OnChangeAPSW");
            VSCommand.subscribe(this, "OnChangeVSCommand");
            _PitchMode.subscribe(this, "OnChange_PitchMode");
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
                OnChange_PitchMode();
            }
        }
        public void OnChangeVSCommand()
        {
            if (!isOwner) return;
            if (Mathf.Approximately(vscmd[0], 0f))
            {
                if (_PitchMode.Data != (int)PitchAutoPilotMode.ALTHOLD) _PitchMode.Data = (int)PitchAutoPilotMode.ALTHOLD;
                return;
            }
            if (_PitchMode.Data != (int)PitchAutoPilotMode.VS) _PitchMode.Data = (int)PitchAutoPilotMode.VS;

        }
        public void OnChange_PitchMode()
        {
            output = 0f;
            ret = 0f;

            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.CWS:
                    holdPitch = pitch[0];
                    break;
                case PitchAutoPilotMode.ALTHOLD:
                case PitchAutoPilotMode.GSCAP:
                    holdAlt = alt[0];
                    pAltErr = holdAlt - alt[0];
                    pVSErr = 0;
                    break;
                case PitchAutoPilotMode.ALTCAP:
                    pAltErr = altcmd[0] - alt[0] * 3.28084f;
                    break;
                case PitchAutoPilotMode.VS:
                    pVSErr = vscmd[0] * 0.508f - vs[0];
                    break;
            }
        }

        float pitchInput, holdPitch, holdAlt;
        float altErr, pAltErr, ret, vsErr, pVSErr, gsErr, pGSErr;
        float pitchCommand()
        {
            if (apswMode == AutoPilotSWMode.CWS)
            {
                holdPitch = Mathf.Lerp(holdPitch, pitch[0], pitchInput);
                return PControl(holdPitch - pitch[0], kp);
            }
            if (isOwner)
            {
                switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
                {
                    case PitchAutoPilotMode.GSCAP:
                        if (Mathf.Abs(gs[0]) < 0.075f) _PitchMode.Data = (int)PitchAutoPilotMode.GSTRK;
                        break;
                    case PitchAutoPilotMode.ALTCAP:
                        if (Mathf.Abs(pAltErr) < 0.5f) _PitchMode.Data = (int)PitchAutoPilotMode.ALTHOLD;
                        break;
                    case PitchAutoPilotMode.VS:
                        if (apswMode != AutoPilotSWMode.CMD) break;
                        pAltErr = altcmd[0] - alt[0] * 3.28084f;
                        if (Mathf.Abs(pAltErr) * kpalt < Mathf.Abs(vscmd[0]) && pAltErr * vscmd[0] > 0)
                            _PitchMode.Data = (int)PitchAutoPilotMode.ALTCAP;
                        break;
                }
            }
            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.CWS:
                    holdPitch = Mathf.Lerp(holdPitch, pitch[0], pitchInput);
                    return PControl(holdPitch - pitch[0], kp);
                case PitchAutoPilotMode.GSTRK:
                    vsErr = PIControl(gs[0], pGSErr, vsErr, kpGS, kiGS);
                    pGSErr = gs[0];
                    break;
                case PitchAutoPilotMode.ALTHOLD:
                case PitchAutoPilotMode.GSCAP:
                    vscmd[0] = 0f;
                    altErr = PIControl(holdAlt - alt[0], pAltErr, altErr, kpalt, kialt);
                    pAltErr = holdAlt - alt[0];
                    vsErr = PIControl(altErr * 0.508f - vs[0], pVSErr, vsErr, kpVS, kiVS);
                    pVSErr = altErr * 0.508f - vs[0];
                    break;
                case PitchAutoPilotMode.ALTCAP:
                    vscmd[0] = PIControl(altcmd[0] * 0.3048f - alt[0], pAltErr, vscmd[0], kpalt, kialt);
                    pAltErr = altcmd[0] * 0.3048f - alt[0];
                    vsErr = PIControl(vscmd[0] * 0.508f - vs[0], pVSErr, vsErr, kpVS, kiVS);
                    pVSErr = vscmd[0] * 0.508f - vs[0];
                    break;
                case PitchAutoPilotMode.VS:
                    vsErr = PIControl(vscmd[0] * 0.508f - vs[0], pVSErr, vsErr, kpVS, kiVS);
                    pVSErr = vscmd[0] * 0.508f - vs[0];
                    break;
            }
            vsErr = Mathf.Clamp(vsErr, -pitchLimit, pitchLimit);
            ret = PControl(vsErr - pitch[0], kp);
            return ret;
        }
        float pitchRate, prevPitch, pitchErr, output;

        void FixedUpdate()
        {
            pitchInput = Mathf.Clamp01(Mathf.Abs(inL[0] + inR[0]) * 10f);
            vsLPF = LPF(vs[0], vsLPF, omega);
            if (Mathf.RoundToInt(mode[0]) < (int)PitchAutoPilotMode.VS || Mathf.RoundToInt(mode[0]) > (int)PitchAutoPilotMode.ALTHOLD)
            {
                vscmd[0] = vsLPF * 1.9685039f;
            }

            pitchRate = LPF((pitch[0] - prevPitch) / Time.fixedDeltaTime, pitchRate, omega);
            prevPitch = pitch[0];

            pitchErr = pitchCommand();
            output = Mathf.Lerp(Mathf.Clamp(PControl(pitchErr - pitchRate, kq), -1f, 1f), inL[0] + inR[0], pitchInput);
            _FDPitch.Data = output;
            if (apswMode == AutoPilotSWMode.OFF) return;
            APOutput.Data = Mathf.Clamp(output, -1f, 1f);
            if (isOwner)
            {
                // autotrim
                if (Mathf.Abs(output) > autoTrimThreshold) TrimCommand.Data += APOutput.Data * autoTrimGain * Time.fixedDeltaTime;
            }
        }
    }
}