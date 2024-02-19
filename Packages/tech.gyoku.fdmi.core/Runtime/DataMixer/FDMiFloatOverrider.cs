using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiFloatOverrider : FDMiBehaviour
    {
        public FDMiFloat[] output;
        public FDMiFloat input;
        [SerializeField] private bool overrideOnStart;
        void Start()
        {
            if (overrideOnStart) Override();
            input.subscribe(this, "OnChange");
        }
        public void Override()
        {
            foreach (FDMiFloat d in output) d.data = input.data;
        }
        public void OnChange()
        {
            foreach (FDMiFloat d in output) d.trigger();
        }
    }
}
