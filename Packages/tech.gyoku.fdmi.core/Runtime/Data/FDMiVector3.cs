
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiVector3 : FDMiData
    {
        public Vector3[] data = new Vector3[1];
        public Vector3 Data
        {
            get => data[0];
            set => set(value);
        }
        
        public virtual void set(Vector3 src)
        { 
            data[0] = src; 
            trigger();
        }
    }
}
