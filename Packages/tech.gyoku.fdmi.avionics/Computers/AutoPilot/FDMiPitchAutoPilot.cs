using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public enum AutoPilotSWMode { OFF, CWS, CMD };
    public enum PitchAutoPilotMode { CWS, Alt, VS, IAS, Mach, VNAV };
    public class FDMiPitchAutoPilot : FDMiBehaviour
    {
        public FDMiFloat APSW, PitchCommand, PitchInputL, PitchInputR;
        public FDMiFloat Pitch, VerticalSpeed, Altitude;
        private AutoPilotSWMode apswMode = AutoPilotSWMode.OFF;
        private PitchAutoPilotMode pitchMode = PitchAutoPilotMode.CWS;

        [SerializeField] float kp, ki, kInput = 0.1f, kq, a = 1f;
        float output, pOut, cmd, err, pErr, omega;
        float pitchRate, prevPitch;
        void Start()
        {
            APSW.subscribe(this, "OnChangeAPSW");
            omega = 2 * Mathf.PI * a;
            omega = omega / (omega + 1);
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
                pitchMode = PitchAutoPilotMode.CWS;
            }
            if (Mathf.Approximately(APSW.data[0], 1f))
            {
                apswMode = AutoPilotSWMode.CMD;
                pitchMode = PitchAutoPilotMode.CWS;
            }
        }
        void AutoPitchTrim()
        {

        }
        void Update()
        {
            float pitchInput = Mathf.Clamp(PitchInputL.Data + PitchInputR.Data, -1f, 1f);
            if (apswMode == AutoPilotSWMode.OFF) { PitchCommand.Data = pitchInput; return; }
            pitchRate = Mathf.Lerp((Pitch.Data - prevPitch) / Time.deltaTime, pitchRate, omega);

            if (pitchMode == PitchAutoPilotMode.CWS)
            {
                output = -kq * pitchRate;
            }
            // output = kp * (err - pErr) + ki * err;
            // pOut = Mathf.Clamp(pOut + output * Time.deltaTime, -1, 1);
            pOut = Mathf.Clamp(pOut + output * Time.deltaTime + pitchInput, -1, 1);
            pErr = err;
            PitchCommand.Data = pOut;
            prevPitch = Pitch.Data;
        }
    }
}