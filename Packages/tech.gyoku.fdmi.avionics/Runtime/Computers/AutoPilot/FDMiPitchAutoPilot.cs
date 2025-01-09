﻿using UdonSharp;
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
        public FDMiFloat APSW, _PitchMode, APPitch, PitchInputL, PitchInputR, FDPitch, TrimCommand;
        public FDMiFloat VSCommand, AltCommand, _GS, _LOC;
        public FDMiFloat Pitch, Roll, VerticalSpeed, Altitude, TAS, Alpha;
        public FDMiBool IsPilot;
        private AutoPilotSWMode apswMode = AutoPilotSWMode.OFF;
        private float[] mode, inL, inR, pitch, roll, vs, alt, tas, alpha, vscmd, altcmd, gs, loc, fdPitch;
        private bool[] isPilot;
        [SerializeField] float kp, kq, a = 1f, FDMoveSpeed = 2f, pitchRateLimit = 2.5f;
        [SerializeField] float kpGS, kiGS, GSbias = 2.54f;
        [SerializeField] float kpalt, kialt;
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
            fdPitch = FDPitch.data;
            APSW.subscribe(this, "OnChangeAPSW");
            VSCommand.subscribe(this, "OnChangeVSCommand");
            _PitchMode.subscribe(this, "OnChange_PitchMode");
            IsPilot.subscribe(this, "OnChange_PitchMode");
            omega = LPFTau(a);
            OnChangeAPSW();
        }
        public void OnChangeAPSW()
        {
            pitchErr = 0f;
            pitchCmd_i = 0f;
            if (Mathf.Approximately(APSW.data[0], 0f))
            {
                apswMode = AutoPilotSWMode.OFF;
                APPitch.Data = 0f;
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
            if (Mathf.Approximately(vscmd[0], 0f))
            {
                if (_PitchMode.Data != (int)PitchAutoPilotMode.ALTHOLD) _PitchMode.Data = (int)PitchAutoPilotMode.ALTHOLD;
                return;
            }
            if (_PitchMode.Data != (int)PitchAutoPilotMode.VS) _PitchMode.Data = (int)PitchAutoPilotMode.VS;

        }
        public void OnChange_PitchMode()
        {
            pitchErr = 0f;
            pitchCmd_i = 0f;
            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.CWS:
                    holdPitch = pitch[0];
                    break;
                case PitchAutoPilotMode.ALTHOLD:
                case PitchAutoPilotMode.GSCAP:
                    holdAlt = alt[0];
                    holdAlt = Mathf.Abs(altcmd[0] * 0.3048f - alt[0]) < 5f ? altcmd[0] * 0.3048f : alt[0];
                    altErr = holdAlt - alt[0];
                    break;
                case PitchAutoPilotMode.ALTCAP:
                    altErr = altcmd[0] * 0.3048f - alt[0];
                    break;
            }
        }

        float holdPitch, holdAlt;
        float altErr, pAltErr, pGSErr, pitchCmd_i;
        float pitchCommand()
        {
            float pitchCmd = 0f;
            if (isPilot[0])
            {
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
            }

            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.CWS:
                    return pitch[0] - alpha[0];
                case PitchAutoPilotMode.GSTRK:
                    pitchCmd_i = IControl(gs[0], pGSErr, pitchCmd_i, kiGS);
                    pitchCmd = PControl(gs[0], kpGS) + pitchCmd_i + GSbias;
                    pGSErr = gs[0];
                    break;
                case PitchAutoPilotMode.ALTHOLD:
                case PitchAutoPilotMode.GSCAP:
                    vscmd[0] = 0f;
                    // if vert speed is high, ease target altitude
                    if (Mathf.Abs(vs[0]) > 2.54f) holdAlt = alt[0];
                    pAltErr = altErr;
                    altErr = holdAlt - alt[0];
                    pitchCmd_i = Mathf.Clamp(IControl(altErr, pAltErr, pitchCmd_i, kialt), -1, 1);
                    pitchCmd = PControl(altErr, kpalt) + pitchCmd_i;
                    pitchCmd = pitchCmd * 0.508f / (tas[0] * Mathf.Deg2Rad);
                    break;
                case PitchAutoPilotMode.ALTCAP:
                    pAltErr = altErr;
                    altErr = altcmd[0] * 0.3048f - alt[0];
                    pitchCmd_i = Mathf.Clamp(IControl(altErr, pAltErr, pitchCmd_i, kialt), -1, 1);
                    vscmd[0] = PControl(altErr, kpalt) + pitchCmd_i;
                    pitchCmd = vscmd[0] * 0.508f / (tas[0] * Mathf.Deg2Rad);
                    break;
                case PitchAutoPilotMode.VS:
                    pitchCmd = vscmd[0] * 0.508f / (tas[0] * Mathf.Deg2Rad);
                    break;
            }
            float rollRad = Mathf.Abs(roll[0]) * Mathf.Deg2Rad;
            return pitchCmd + (rollRad * rollRad * 0.5f);
        }
        float pitchRate, prevPitch, pitchErr, output;

        void Update()
        {
            // pitchRate = LPF((pitch[0] - prevPitch) / Time.deltaTime, pitchRate, omega);
            pitchRate = Mathf.Lerp(pitchRate, (pitch[0] - prevPitch) / Time.deltaTime, Time.deltaTime / a);
            prevPitch = pitch[0];

            vsLPF = LPF(vs[0], vsLPF, omega);
            if (Mathf.RoundToInt(mode[0]) < (int)PitchAutoPilotMode.VS || Mathf.RoundToInt(mode[0]) > (int)PitchAutoPilotMode.ALTHOLD)
            {
                vscmd[0] = vsLPF * 1.9685039f;
            }
            pitchErr = pitchCommand() + alpha[0] - pitch[0];
            FDPitch.Data = Mathf.MoveTowards(FDPitch.Data, pitchErr, Time.deltaTime * FDMoveSpeed);

            if (apswMode == AutoPilotSWMode.OFF) return;
            // if (apswMode == AutoPilotSWMode.CMD) APPitch.Data = output;
            if (apswMode == AutoPilotSWMode.CWS || Mathf.RoundToInt(mode[0]) == (int)PitchAutoPilotMode.CWS)
            {
                float pitchInput = Mathf.Clamp(inL[0] + inR[0], -1, 1);
                if (Mathf.Abs(pitchInput) > 0.05f)
                {
                    holdPitch = pitch[0];
                    APPitch.Data = 0f;
                    output = 0f;
                    return;
                }
                pitchErr = holdPitch - pitch[0];
            }

            pitchErr = Mathf.Clamp(pitchErr, -pitchRateLimit, pitchRateLimit);
            output = PControl(pitchErr, kp) + PControl(-pitchRate, kq);
            output = Mathf.Clamp(output, -1f, 1f);
            APPitch.Data = output;

            // autotrim
            if (Mathf.Abs(output) > autoTrimThreshold) TrimCommand.Data += output * autoTrimGain * Time.deltaTime;
        }
    }
}