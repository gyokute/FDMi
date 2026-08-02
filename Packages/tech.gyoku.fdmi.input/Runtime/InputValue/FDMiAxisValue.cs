using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.input
{
    public class FDMiAxisValue : FDMiBehaviour
    {
        public string axisName;

        public float Get() => Input.GetAxisRaw(axisName);
    }
}
