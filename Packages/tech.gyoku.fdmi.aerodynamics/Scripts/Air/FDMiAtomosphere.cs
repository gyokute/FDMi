
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.aerodynamics
{
    public class FDMiAtomosphere : FDMiAttribute
    {
        public FDMiFloat Altitude, SAT, Pressure, Rho, GroundPressure, GroundTemperature;
        public FDMiVector3 Position, KmPosition;
        float[] alt, sat, p, rho, gndPress, gndTemp;
        Vector3[] pos, kmPos;
        void Start()
        {
            alt = Altitude.data;
            sat = SAT.data;
            p = Pressure.data;
            rho = Rho.data;
            gndPress = GroundPressure.data;
            gndTemp = GroundTemperature.data;

            pos = Position.data;
            kmPos = KmPosition.data;
        }

        void Update()
        {
            alt[0] = pos[0].y + kmPos[0].y * 1000f;
            sat[0] = Mathf.Max(gndTemp[0] - 0.0065f * alt[0], 216.65f);
            float scaleHeight = 29.2985f * gndTemp[0];
            float pressureScale = Mathf.Exp(-alt[0] / scaleHeight);
            p[0] = gndPress[0] * pressureScale;
            rho[0] = 1.2f * pressureScale;

        }
        // void AbsoluteAltToAtomosphere()
        // {
        //     float tmpt = 0.0065f * alt;
        //     air[0/*SAT*/] = gnd[0/*groundTemp*/] - tmpt;
        //     air[1/*pressure*/] = gnd[1/*groundPress*/] * Mathf.Pow((air[0] - tmpt) / air[0], 5.255876f);
        //     air[2/*rho*/] = air[1] / (air[0] * 287);
        //     air[4/*pressureAlt*/] = 44330.8f * (1 - Mathf.Pow(air[1] / air[4/*QNH*/], 0.190263f));
        // }

        // void velocityToIAS(Vector3 position, Vector3 velocity, Quaternion direction)
        // {
        //     Vector3 windVec = direction * Quaternion.Euler(0f, gnd[4/*windDir*/], 0f) * Vector3.forward;
        //     float TimeGustiness = Time.time * gnd[5];
        //     windVec *= gnd[2] + gnd[3] * (Mathf.PerlinNoise(TimeGustiness, 0) - .5f);
        //     Vector3 airSpeed = velocity + windVec;
        //     float machSpeed = Mathf.Sqrt(1.403068f * air[1/*pressure*/] / air[2/*rho*/]);
        //     air[5/*TAS*/] = airSpeed.z;
        //     air[6/*IAS*/] = air[5/*TAS*/] * Mathf.Sqrt(air[2/*rho*/] / 1.23f);
        //     air[7/*mach*/] = air[5/*TAS*/] / machSpeed;
        // }

    }
}