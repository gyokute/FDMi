
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiFloataccumulator : FDMiBehaviour
    {
        public FDMiFloat[] output;
        public FDMiFloat[] data;
        public float multiply, min, max;

        void Update()
        {
            float outs = 0f;
            foreach (FDMiFloat d in data) outs += d.data[0];
            foreach (FDMiFloat d in output) d.Data = Mathf.Clamp(d.data[0] + outs * multiply * Time.deltaTime, min, max);
        }
    }
}