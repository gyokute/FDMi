using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiBool : FDMiData
    {
        public bool[] data = new bool[1];
        public bool Data
        {
            get => data[0];
            set => Set(value);
        }

        public void Set(bool src)
        {
            data[0] = src;
            TriggerCallbacks();
        }
        public override void Set(float src) => Set(src >= 0.5f);
        public override void Set(short i) => Set(i >= 1);
        public override void Set(sbyte i) => Set(i >= 1);
        public override float GetFloat() => data[0] ? 1f : 0f;
        public override short GetShort() => (short)(data[0] ? 1 : 0);
        public override sbyte GetSByte() => (sbyte)(data[0] ? 1 : 0);
    }
}
