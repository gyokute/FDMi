
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiInt : FDMiData
    {
        public int[] data = new int[1];
        public int Data
        {
            get => data[0];
            set => set(value);
        }
        
        public virtual void set(int src)
        { 
            data[0] = src; 
            trigger();
        }
    }
}
