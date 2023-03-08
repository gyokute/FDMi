
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.v2.core;

namespace tech.gyoku.FDMi.v2.aerodynamics
{

    public class VorticityField : FDMiDynamicsBehaviour
    {
        public float[] Gamma = { 0f };
    }
}