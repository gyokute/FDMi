
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.v2.aerodynamics
{
    public enum AtmData
    {
        alt, temp, rho, pressure, Length
    }
    public class Atmosphere : UdonSharpBehaviour
    {
        public float altitudeOffset;
        public float[] data = { 0f, 0f, 0f, 0f };
        void FixedUpdate()
        {
            
        }
    }
}