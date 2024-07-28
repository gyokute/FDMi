
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.aerodynamics
{
    public class FDMiWind : FDMiBehaviour
    {
        public FDMiVector3 Wind, Position;
        public FDMiVector3 WindConstant;
        public FDMiFloat WindTurbulanceScale, WindGustStrength;
        private Vector3[] pos, wind, windConst;
        private float[] turbulanceScale, gustStrength;

        void Start()
        {
            pos = Position.data;
            wind = Wind.data;
            windConst = WindConstant.data;
            turbulanceScale = WindTurbulanceScale.data;
            gustStrength = WindGustStrength.data;
        }
        void Update()
        {
            // idea from Saccflight.
            Vector3 gust = Time.time * windConst[0] + turbulanceScale[0] * pos[0];
            gust.x = Mathf.PerlinNoise(gust.x, 0f);
            gust.y = Mathf.PerlinNoise(gust.y, 0.3f);
            gust.z = Mathf.PerlinNoise(gust.z, 0.6f);
            Vector3 FinalWind = Vector3.Normalize(gust - 0.5f * Vector3.one) * gustStrength[0];
            wind[0] = (FinalWind + windConst[0]);
        }
    }
}