using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public enum EngineState { OFF, STARTUP, RUNNING, BROKEN };
    public class FDMiSimpleJetEngine : FDMiAttribute
    {
        public FDMiBool InZone, IsPilot;
        public FDMiFloat EngineStatus;
        public FDMiFloat FuelSW, AirSW, Throttle, Reverser;
        public FDMiFloat Mach, Pressure, SAT;
        public FDMiFloat FuelFlow, EGT;
        public FDMiFloat N1, N2;
        [SerializeField] AnimationCurve N1vsN2;
        [SerializeField] private float N1StaticThrust = 120000f, N2StaticThrust = 550000f;
        [SerializeField] private float N2min = 0.6f, N2max = 1.1f, maxAirN2 = 0.4f, N2FuelThreshold = 0.1f;
        [SerializeField] private float N1ReverserRatio = -0.5f, N2ReverserRatio = 1f;
        [SerializeField] private float ffIdle = 0.47f, ffMax = 3.145f, egtMax = 1100f;
        [SerializeField] private float N1dt = 1f, N2dt = 1f, N2Airdt = 0.02f, ffDt = 1f, egtDt = 2f, inputNoise = 0.02f;
        [SerializeField] AnimationCurve thrustMachMultiplier;
        [SerializeField] AnimationCurve thrustPressureMultiplier;
        private bool[] inZone, isPilot;
        float[] status, sw, airsw, input, rev, mach, pressure, ff, egt, sat, n1, n2;
        float thrust;
        void Start()
        {
            status = EngineStatus.data;
            sw = FuelSW.data;
            airsw = AirSW.data;
            input = Throttle.data;
            rev = Reverser.data;
            mach = Mach.data;
            pressure = Pressure.data;
            sat = SAT.data;
            ff = FuelFlow.data;
            egt = EGT.data;
            n1 = N1.data;
            n2 = N2.data;
            inZone = InZone.data;
            isPilot = IsPilot.data;
        }

        void FixedUpdate()
        {
            if (!isInit) return;
            float theta = Mathf.Sqrt(sat[0] / 288f) * Time.fixedDeltaTime;
            float noise = 1 + Mathf.Lerp(inputNoise, -inputNoise, Mathf.PerlinNoise(Time.time, 0));

            // float dN1 = n2[0] * 2f - 1f - n1[0];
            // isFuelEn = sw[0] * Mathf.Ceil(n2[0] - N2FuelThreshold);
            // isAirEn = airsw[0] * Mathf.Ceil(maxAirN2 - n2[0]) * (1 - isFuelEn);
            // N2Tgt = Mathf.Lerp(isFuelEn * Mathf.Lerp(N2min, N2max, input[0]), maxAirN2 * airsw[0], isAirEn);

            float N2Tgt = 0f;
            float N2Acc = N2dt;
            switch (status[0])
            {
                case (float)EngineState.STARTUP:
                    N2Tgt = maxAirN2 * airsw[0];
                    N2Acc = N2Airdt;
                    break;
                case (float)EngineState.RUNNING:
                    N2Tgt = Mathf.Lerp(N2min, N2max, input[0]);
                    break;
            }
            n2[0] = Mathf.MoveTowards(n2[0], N2Tgt, N2Acc * theta);

            n1[0] = Mathf.MoveTowards(n1[0], N1vsN2.Evaluate(n2[0]), N1dt * theta);
            n1[0] = Mathf.Max(n1[0], 0f);

            if (inZone[0])
            {
                // Fuel flow(kg/s)
                FuelFlow.Data = Mathf.MoveTowards(ff[0], sw[0] * Mathf.Lerp(ffIdle, ffMax, input[0]), ffDt * theta);
                // EGT calculate section
                egt[0] = Mathf.MoveTowards(egt[0], Mathf.Lerp(sat[0], egtMax, Mathf.Sqrt(ff[0] / ffMax) * noise), egtDt * theta);
                EGT.Data = Mathf.Max(egt[0], sat[0]);
            }
            if (isPilot[0])
            {
                bool isFuelEn = (sw[0] > 0.5f) && (n2[0] > N2FuelThreshold);
                switch (status[0])
                {
                    case (float)EngineState.OFF:
                        if (isFuelEn) EngineStatus.Data = (float)EngineState.RUNNING;
                        else if (airsw[0] > 0.5f) EngineStatus.Data = (float)EngineState.STARTUP;
                        break;
                    case (float)EngineState.STARTUP:
                        if (isFuelEn) EngineStatus.Data = (float)EngineState.RUNNING;
                        if (airsw[0] < 0.5f) EngineStatus.Data = (float)EngineState.OFF;
                        break;
                    case (float)EngineState.RUNNING:
                        if (sw[0] < 0.5f) EngineStatus.Data = (float)EngineState.OFF;
                        break;
                }
                // N2.Data = Mathf.Max(n2[0], Mathf.MoveTowards(n2[0], maxAirN2 * airsw[0] * noise, N2FuelThreshold * theta));
                if (airsw[0] > 0.5f && n2[0] > maxAirN2)
                {
                    AirSW.Data = 0f;
                }
                thrust = Mathf.Lerp(1f, N1ReverserRatio, rev[0]) * n1[0] * n1[0] * N1StaticThrust;
                thrust += Mathf.Lerp(1f, N2ReverserRatio, rev[0]) * n2[0] * n2[0] * N2StaticThrust;
                thrust *= thrustMachMultiplier.Evaluate(mach[0]) * thrustPressureMultiplier.Evaluate(pressure[0]);
                body.AddForceAtPosition(thrust * transform.forward, transform.position);
            }
        }
    }
}