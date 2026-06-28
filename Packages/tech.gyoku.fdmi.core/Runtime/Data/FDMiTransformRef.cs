using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiTransformRef : FDMiData
    {
        public Transform[] data = new Transform[1];
        public Transform Data
        {
            get => data[0];
            set => set(value);
        }

        public virtual void set(Transform src)
        {
            data[0] = src;
            TriggerCallbacks();
        }
    }
}
