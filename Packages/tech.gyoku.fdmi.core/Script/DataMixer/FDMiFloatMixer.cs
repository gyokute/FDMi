
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiFloatMixer : UdonSharpBehaviour
    {
        public FDMiFloat[] output;
        public FDMiFloat[] data;
        public float multiply, min, max;
        void Start()
        {
            foreach (FDMiFloat d in data) d.subscribe(this, "OnChange");
        }
        public void OnChange()
        {
            float outs = 0f;
            foreach (FDMiFloat d in data) outs += d.data[0];
            foreach (FDMiFloat d in output) d.Data = Mathf.Clamp(outs * multiply, min, max);
        }
    }
}
