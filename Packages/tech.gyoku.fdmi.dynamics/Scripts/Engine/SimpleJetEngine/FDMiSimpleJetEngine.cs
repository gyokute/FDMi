using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiSimpleJetEngine : FDMiAttribute
    {
        public FDMiBool InZone, IsPilot;
        public FDMiFloat FuelSW, AirSW, Throttle, Reverser;
        public FDMiFloat Mach, Pressure, SAT;
        public FDMiFloat FuelFlow, EGT;
        public FDMiFloat N1, N2;
        [SerializeField] private float N1StaticThrust = 120000f, N2StaticThrust = 550000f;
        [SerializeField] private float N2min = 0.6f, N2max = 1.1f, maxAirN2 = 0.4f, N2FuelThreshold = 0.1f;
        [SerializeField] private float N1ReverserRatio = -0.5f, N2ReverserRatio = 1f;
        [SerializeField] private float ffIdle = 0.47f, ffMax = 3.145f, egtMax = 1100f;
        [SerializeField] private float N1dt = 1f, N2dt = 1f, N2Airdt = 0.02f, ffDt = 1f, egtDt = 2f, inputNoise = 0.02f;
        [SerializeField] AnimationCurve thrustMachMultiplier;
        [SerializeField] AnimationCurve thrustPressureMultiplier;
        private bool[] inZone, isPilot;
        float[] sw, airsw, input, rev, mach, pressure, ff, egt, sat, n1, n2;
        float thrust;
        void Start()
        {
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
        float N2Tgt, isAirEn, isFuelEn;
        void FixedUpdate()
        {
            if (!isInit) return;
            float theta = Mathf.Sqrt(sat[0] / 288f) * Time.fixedDeltaTime;
            float noise = 1 + Mathf.Lerp(inputNoise, -inputNoise, Mathf.PerlinNoise(Time.time, 0));

            float dN1 = n2[0] * 2f - 1f - n1[0];
            n1[0] = Mathf.MoveTowards(n1[0], n1[0] + dN1 * noise, N1dt * theta);
            N1.Data = Mathf.Max(n1[0], 0f);

            if (inZone[0])
            {
                // Fuel flow(kg/s)
                FuelFlow.Data = Mathf.MoveTowards(ff[0], sw[0] * Mathf.Lerp(ffIdle, ffMax, input[0]), ffDt * theta);
                // EGT calculate section
                egt[0] = Mathf.MoveTowards(egt[0], Mathf.Lerp(sat[0], egtMax, Mathf.Sqrt(ff[0] / ffMax) * noise), egtDt * theta);
                EGT.Data = Mathf.Max(egt[0], sat[0]);
                if (isPilot[0])
                {
                    isFuelEn = sw[0] * Mathf.Ceil(n2[0] - N2FuelThreshold);
                    isAirEn = airsw[0] * Mathf.Ceil(maxAirN2 - n2[0]) * (1 - isFuelEn);
                    N2Tgt = Mathf.Lerp(isFuelEn * Mathf.Lerp(N2min, N2max, input[0]), maxAirN2 * airsw[0], isAirEn);
                    N2.Data = Mathf.MoveTowards(n2[0], N2Tgt, Mathf.Lerp(N2dt, N2Airdt, isAirEn) * theta);
                    // N2.Data = Mathf.Max(n2[0], Mathf.MoveTowards(n2[0], maxAirN2 * airsw[0] * noise, N2FuelThreshold * theta));

                    thrust = Mathf.Lerp(1f, N1ReverserRatio, rev[0]) * n1[0] * n1[0] * N1StaticThrust;
                    thrust += Mathf.Lerp(1f, N2ReverserRatio, rev[0]) * n2[0] * n2[0] * N2StaticThrust;
                    thrust *= thrustMachMultiplier.Evaluate(mach[0]) * thrustPressureMultiplier.Evaluate(pressure[0]);
                    body.AddForceAtPosition(thrust * transform.forward, transform.position);
                }
            }
        }
    }
}