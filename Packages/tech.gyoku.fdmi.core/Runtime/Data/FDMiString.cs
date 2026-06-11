using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiString : FDMiData
    {
        public string[] data = new string[1];
        public string Data
        {
            get => data[0];
            set => set(value);
        }

        public virtual void set(string src)
        {
            data[0] = src;
            TriggerCallbacks();
        }
    }
}
