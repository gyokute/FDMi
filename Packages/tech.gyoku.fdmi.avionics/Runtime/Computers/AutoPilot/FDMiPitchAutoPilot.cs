using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.avionics
{
    public enum PitchAutoPilotMode { CWS, IASHOLD, MACHHOLD, TURB, VS, ALTCAP, ALTHOLD, GSCAP, GSTRK, GA, TAKEOFF }

    public class FDMiPitchAutoPilot : FDMiAutoPilot
    {
        public string APSWPath = "APSW";

        [FDMiDataPath(nameof(APSWPath))]
        [FDMiRegisterCallback(nameof(OnChangeAPSW))]
        public FDMiFloat APSW;
        public string PitchModePath = "PitchMode";

        [FDMiDataPath(nameof(PitchModePath))]
        [FDMiRegisterCallback(nameof(OnChange_PitchMode))]
        public FDMiFloat PitchMode;
        public string APOutputPath = "APOutput";

        [FDMiDataPath(nameof(APOutputPath))]
        public FDMiFloat APOutput;
        public string PitchInputLPath = "PitchInputL";

        [FDMiDataPath(nameof(PitchInputLPath))]
        public FDMiFloat PitchInputL;
        public string PitchInputRPath = "PitchInputR";

        [FDMiDataPath(nameof(PitchInputRPath))]
        public FDMiFloat PitchInputR;
        public string FDPitchPath = "FDPitch";

        [FDMiDataPath(nameof(FDPitchPath))]
        public FDMiFloat FDPitch;
        public string TrimCommandPath = "TrimCommand";

        [FDMiDataPath(nameof(TrimCommandPath))]
        public FDMiFloat TrimCommand;
        public string VSCommandPath = "VSCommand";

        [FDMiDataPath(nameof(VSCommandPath))]
        [FDMiRegisterCallback(nameof(OnChangeVSCommand))]
        public FDMiFloat VSCommand;
        public string AltCommandPath = "AltCommand";

        [FDMiDataPath(nameof(AltCommandPath))]
        public FDMiFloat AltCommand;
        public string GSPath = "GS";

        [FDMiDataPath(nameof(GSPath))]
        public FDMiFloat GS;
        public string PitchPath = "Pitch";

        [FDMiDataPath(nameof(PitchPath))]
        public FDMiFloat Pitch;
        public string RollPath = "Roll";

        [FDMiDataPath(nameof(RollPath))]
        public FDMiFloat Roll;
        public string VerticalSpeedPath = "VerticalSpeed";

        [FDMiDataPath(nameof(VerticalSpeedPath))]
        public FDMiFloat VerticalSpeed;
        public string AltitudePath = "Altitude";

        [FDMiDataPath(nameof(AltitudePath))]
        public FDMiFloat Altitude;
        public string IsPilotPath = "IsPilot";

        [FDMiDataPath(nameof(IsPilotPath))]
        public FDMiBool IsPilot;

        private AutoPilotSWMode apswMode = AutoPilotSWMode.OFF;
        [SerializeField] float kp, kq, a = 1f, pitchLimit = 20f, FDMoveSpeed = 2f;
        [SerializeField] float kpVS, kiVS, kpGS, kiGS, GSbias = 2.54f;
        [SerializeField] float kpalt, kialt;
        [SerializeField] float autoTrimThreshold = 0.05f, autoTrimGain = 0.5f;
        float omega, vsLPF;
        bool isYokeHold;
        float holdPitch, holdAlt;
        float altErr, pAltErr, ret, vsErr, pVSErr, pGSErr;
        float pitchRate, prevPitch, pitchErr, output;

        void Start()
        {
            mode = PitchMode.data;
            inL = PitchInputL.data;
            inR = PitchInputR.data;
            pitch = Pitch.data;
            roll = Roll.data;
            vs = VerticalSpeed.data;
            alt = Altitude.data;
            gs = GS.data;
            vscmd = VSCommand.data;
            altcmd = AltCommand.data;
            isPilot = IsPilot.data;
            fdPitch = FDPitch.data;
            apsw = APSW.data;
            omega = LPFTau(a);
            OnChangeAPSW();
        }

        public void OnChangeAPSW()
        {
            if (Mathf.Approximately(apsw[0], 0f))
            {
                apswMode = AutoPilotSWMode.OFF;
                APOutput.Data = 0f;
            }
            if (Mathf.Approximately(apsw[0], 0.5f))
            {
                apswMode = AutoPilotSWMode.CWS;
                holdPitch = pitch[0];
            }
            if (Mathf.Approximately(apsw[0], 1f))
            {
                apswMode = AutoPilotSWMode.CMD;
                OnChange_PitchMode();
            }
        }

        public void OnChangeVSCommand()
        {
            if (!isPilot[0]) return;
            if (Mathf.Approximately(vscmd[0], 0f))
            {
                if (PitchMode.Data != (int)PitchAutoPilotMode.ALTHOLD) PitchMode.Data = (int)PitchAutoPilotMode.ALTHOLD;
                return;
            }
            if (PitchMode.Data != (int)PitchAutoPilotMode.VS) PitchMode.Data = (int)PitchAutoPilotMode.VS;
        }

        public void OnChange_PitchMode()
        {
            output = 0f;
            ret = 0f;
            vsErr = 0f;
            pVSErr = 0f;
            pAltErr = 0f;
            pGSErr = 0f; // モード遷移では積分器もリセット
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
                    pAltErr = altcmd[0] * 0.3048f - alt[0]; // ft->m 統一
                    break;
                case PitchAutoPilotMode.VS:
                    pVSErr = vscmd[0] * 0.508f - vs[0];
                    break;
            }
        }

        float pitchCommand()
        {
            if (isPilot[0])
            {
                switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
                {
                    case PitchAutoPilotMode.GSCAP:
                        if (Mathf.Abs(gs[0]) < 0.075f) PitchMode.Data = (int)PitchAutoPilotMode.GSTRK;
                        break;
                    case PitchAutoPilotMode.ALTCAP:
                        if (Mathf.Abs(pAltErr) < 3f) PitchMode.Data = (int)PitchAutoPilotMode.ALTHOLD;
                        break;
                    case PitchAutoPilotMode.VS:
                        pAltErr = altcmd[0] * 0.3048f - alt[0];
                        if (Mathf.Abs(pAltErr) * kpalt < Mathf.Abs(vscmd[0]) && pAltErr * vscmd[0] > 0)
                            PitchMode.Data = (int)PitchAutoPilotMode.ALTCAP;
                        break;
                }
            }
            switch ((PitchAutoPilotMode)Mathf.RoundToInt(mode[0]))
            {
                case PitchAutoPilotMode.CWS:
                    if (isYokeHold)
                    {
                        holdPitch = pitch[0];
                        return pitchRate;
                    }
                    return PControl(holdPitch - pitch[0], kp);
                case PitchAutoPilotMode.GSTRK:
                    vsErr = PIControl(gs[0] - GSbias / kpGS, pGSErr, vsErr, kpGS, kiGS);
                    pGSErr = gs[0] - GSbias / kpGS;
                    break;
                case PitchAutoPilotMode.ALTHOLD:
                case PitchAutoPilotMode.GSCAP:
                    VSCommand.Data = 0f;
                    // 垂直速度が大きい時は目標高度を追従させる
                    if (Mathf.Abs(vs[0]) > 2.54f) holdAlt = alt[0];
                    altErr = PControl(holdAlt - alt[0], kpalt);
                    pAltErr = holdAlt - alt[0];
                    vsErr = PIControl(altErr * 0.508f - vs[0], pVSErr, vsErr, kpVS, kiVS);
                    pVSErr = altErr * 0.508f - vs[0];
                    break;
                case PitchAutoPilotMode.ALTCAP:
                    VSCommand.Data = PControl(altcmd[0] * 0.3048f - alt[0], kpalt);
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

        void FixedUpdate()
        {
            isYokeHold = Mathf.Abs(inL[0]) + Mathf.Abs(inR[0]) > 0.05f; // 左右逆入力でも相殺しない
            vsLPF = LPF(vs[0], vsLPF, omega);
            if (Mathf.RoundToInt(mode[0]) < (int)PitchAutoPilotMode.VS || Mathf.RoundToInt(mode[0]) > (int)PitchAutoPilotMode.ALTHOLD)
            {
                VSCommand.Data = vsLPF * 1.9685039f;
            }

            pitchRate = LPF((pitch[0] - prevPitch) / Time.fixedDeltaTime, pitchRate, omega);
            prevPitch = pitch[0];

            pitchErr = pitchCommand();
            output = Mathf.Clamp(PControl(pitchErr - pitchRate, kq), -1f, 1f);
            output = Mathf.MoveTowards(fdPitch[0], output, Time.fixedDeltaTime * FDMoveSpeed);
            FDPitch.Data = output;
            if (apswMode == AutoPilotSWMode.OFF) return;
            if (apswMode == AutoPilotSWMode.CMD) APOutput.Data = output;
            if (apswMode == AutoPilotSWMode.CWS)
            {
                if (isYokeHold)
                {
                    holdPitch = pitch[0];
                    output = 0f;
                }
                else
                {
                    pitchErr = PControl(holdPitch - pitch[0], kp);
                    output = Mathf.Clamp(PControl(pitchErr - pitchRate, kq), -1f, 1f);
                }
                APOutput.Data = Mathf.MoveTowards(APOutput.Data, output, Time.fixedDeltaTime * FDMoveSpeed);
            }

            if (isPilot[0])
            {
                // オートトリム: TrimCommand.Data += はGet+Setに展開（Set通知のため）
                if (Mathf.Abs(output) > autoTrimThreshold)
                    TrimCommand.Data = TrimCommand.Data + APOutput.Data * autoTrimGain * Time.fixedDeltaTime;
            }
        }

        float[] mode, inL, inR, pitch, roll, vs, alt, vscmd, altcmd, gs, fdPitch, apsw;
        bool[] isPilot;
    }
}
