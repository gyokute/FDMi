﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.aerodynamics
{
    public class FDMiAtomosphere : FDMiAttribute
    {
        public FDMiFloat Altitude, SAT, Pressure, Rho, SonicSpeed, GroundPressure, GroundTemperature;
        public FDMiVector3 Position, KmPosition;
        float[] alt, sat, p, rho, sonic, gndPress, gndTemp;
        Vector3[] pos, kmPos;
        void Start()
        {
            alt = Altitude.data;
            sat = SAT.data;
            p = Pressure.data;
            rho = Rho.data;
            sonic = SonicSpeed.data;
            gndPress = GroundPressure.data;
            gndTemp = GroundTemperature.data;

            pos = Position.data;
            kmPos = KmPosition.data;
        }

        void Update()
        {
            Altitude.Data = pos[0].y + kmPos[0].y * 1000f;
            SAT.Data = Mathf.Max(gndTemp[0] - 0.0065f * alt[0], 216.65f);
            float scaleHeight = 29.2985f * gndTemp[0];
            float pressureScale = Mathf.Exp(-alt[0] / scaleHeight);
            Pressure.Data = gndPress[0] * pressureScale;
            Rho.Data = 1.2f * pressureScale;
            SonicSpeed.Data = 164.8785f + 0.61f * sat[0];
        }
    }
}