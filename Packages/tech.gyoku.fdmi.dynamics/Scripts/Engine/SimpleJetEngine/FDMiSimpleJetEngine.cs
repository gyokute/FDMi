﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.dynamics
{
    public class FDMiSimpleJetEngine : FDMiAttribute
    {
        public FDMiFloat FuelSW, AirSW, Input, Reverser;
        public FDMiFloat Mach, Pressure, SAT;
        public FDMiFloat FuelFlow, EGT;
        public FDMiFloat N1, N2;
        [SerializeField] private float N1StaticThrust = 120000f, N2StaticThrust = 550000f;
        [SerializeField] private float N2min = 0.6f, N2max = 1.1f, maxAirN2 = 0.4f, airN2Coef = 0.1f;
        [SerializeField] private float N1ReverserRatio = -0.5f, N2ReverserRatio = 1f;
        [SerializeField] private float ffIdle = 0.47f, ffMax = 3.145f, egtMax = 1100f;
        [SerializeField] private float N1dt = 1f, N2dt = 1f, ffDt = 1f, egtDt = 2f;
        [SerializeField] AnimationCurve thrustMachMultiplier;
        [SerializeField] AnimationCurve thrustPressureMultiplier;
        float[] sw, airsw, input, rev, mach, pressure, ff, egt, sat, n1, n2;
        float thrust;
        void Start()
        {
            sw = FuelSW.data;
            airsw = AirSW.data;
            input = Input.data;
            rev = Reverser.data;
            mach = Mach.data;
            pressure = Pressure.data;
            sat = SAT.data;
            ff = FuelFlow.data;
            egt = EGT.data;
            n1 = N1.data;
            n2 = N2.data;
        }
        void FixedUpdate()
        {
            if (!isInit) return;
            // Fuel flow(kg/s)
            ff[0] = Mathf.MoveTowards(ff[0], sw[0] * Mathf.Lerp(ffIdle, ffMax, input[0]), ffDt * Time.fixedDeltaTime);
            // EGT calculate section
            egt[0] = Mathf.MoveTowards(egt[0], Mathf.Lerp(sat[0], egtMax, ff[0] / ffMax), egtDt * Time.fixedDeltaTime);
            egt[0] = Mathf.Max(egt[0], sat[0]);

            float theta = Mathf.Sqrt(sat[0] / 288f);
            float N2tgt = Mathf.Max(maxAirN2 * airsw[0], sw[0] * Mathf.Lerp(N2min, N2max, input[0]));
            n2[0] = Mathf.MoveTowards(n2[0], N2tgt, N2dt * Time.fixedDeltaTime * theta);

            float dN1 = n2[0] * 2f - 1f - n1[0];
            n1[0] += N1dt * dN1 * Time.fixedDeltaTime * theta;
            n1[0] = Mathf.Max(n1[0], 0f);

            if (isOwner)
            {
                N2.Data = n2[0];
                thrust = Mathf.Lerp(1f, N1ReverserRatio, rev[0]) * n1[0] * n1[0] * N1StaticThrust;
                thrust += Mathf.Lerp(1f, N2ReverserRatio, rev[0]) * n2[0] * n2[0] * N2StaticThrust;
                thrust *= thrustMachMultiplier.Evaluate(mach[0]) * thrustPressureMultiplier.Evaluate(pressure[0]);
                body.AddForceAtPosition(thrust * transform.forward, transform.position);
            }
        }
    }
}