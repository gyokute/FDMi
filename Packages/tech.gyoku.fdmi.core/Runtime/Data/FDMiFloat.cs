using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiFloat : FDMiData
    {
        public float[] data = new float[1];
        public float Data
        {
            get => data[0];
            set => Set(value);
        }

        public override void Set(float src)
        {
            data[0] = src;
            TriggerCallbacks();
        }
        
        public override void Set(short i) => Set(i);
        public override void Set(sbyte i) => Set(i);
        public override float GetFloat() => data[0];
        public override short GetShort() => (short)(data[0]);
        public override sbyte GetSByte() => (sbyte)(data[0]);
    }
}
