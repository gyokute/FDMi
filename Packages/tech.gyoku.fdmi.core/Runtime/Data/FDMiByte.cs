
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiByte : FDMiData
    {
        public byte[] data = new byte[1];
        public byte Data
        {
            get => data[0];
            set => set(value);
        }
        
        public virtual void set(byte src)
        { 
            data[0] = src; 
            trigger();
        }

    }
}
