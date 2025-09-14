
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiSByte : FDMiData
    {
        public sbyte[] data = new sbyte[1];
        public sbyte Data
        {
            get => data[0];
            set => set(value);
        }
        
        public virtual void set(sbyte src)
        { 
            data[0] = src; 
            trigger();
        }

    }
}
