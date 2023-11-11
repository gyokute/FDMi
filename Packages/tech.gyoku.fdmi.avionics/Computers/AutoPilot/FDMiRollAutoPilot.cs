
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.avionics
{
    public enum RollAutoPilotMode { CWS };
    public class FDMiRollAutoPilot : FDMiBehaviour
    {
        public FDMiFloat APSW, RollCommand, RollInputL, RollInputR;
        public FDMiFloat Roll, HDG;
        private AutoPilotSWMode apswMode = AutoPilotSWMode.OFF;
        private RollAutoPilotMode RollMode = RollAutoPilotMode.CWS;

        [SerializeField] float kp, ki, kInput = 0.1f, kq, a = 1f;
        float output, pOut, cmd, err, pErr, omega;
        float RollRate, prevRoll;
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
                RollMode = RollAutoPilotMode.CWS;
            }
            if (Mathf.Approximately(APSW.data[0], 1f))
            {
                apswMode = AutoPilotSWMode.CMD;
                RollMode = RollAutoPilotMode.CWS;
            }
        }

        void Update()
        {
            float RollInput = Mathf.Clamp(RollInputL.Data + RollInputR.Data, -1f, 1f);
            if (apswMode == AutoPilotSWMode.OFF) { RollCommand.Data = RollInput; return; }
            RollRate = Mathf.Lerp((Roll.Data - prevRoll) / Time.deltaTime, RollRate, omega);

            if (RollMode == RollAutoPilotMode.CWS)
            {
                output = RollInput - kq * RollRate;
            }
            // output = kp * (err - pErr) + ki * err;
            // pOut = Mathf.Clamp(pOut + output * Time.deltaTime, -1, 1);
            pOut = Mathf.Clamp(pOut + output * Time.deltaTime, -1, 1);
            pErr = err;
            RollCommand.Data = pOut;
            prevRoll = Roll.Data;
        }
    }
}