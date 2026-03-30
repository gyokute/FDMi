
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiComponentData : FDMiData
    {
        public Component[] data = new Component[1];
        public Component Data
        {
            get => data[0];
            set => set(value);
        }
        
        public virtual void set(Component src)
        { 
            data[0] = src; 
            trigger();
        }

    }
}
