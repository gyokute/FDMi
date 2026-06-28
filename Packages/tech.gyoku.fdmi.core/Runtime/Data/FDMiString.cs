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
            set => Set(value);
        }

        public virtual void Set(string src)
        {
            data[0] = src;
            TriggerCallbacks();
        }
    }
}
