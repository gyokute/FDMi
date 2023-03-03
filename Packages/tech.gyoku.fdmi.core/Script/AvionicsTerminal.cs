
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class AvionicsTerminal : UdonSharpBehaviour
    {
        public int busId;
        public float[] register = new float[128];

    }
}
