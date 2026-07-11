using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiInt : FDMiData
    {
        public int[] data = new int[1];
        public int Data
        {
            get => data[0];
            set => Set(value);
        }

        public override void Set(int src)
        {
            data[0] = src;
            TriggerCallbacks();
        }

        public override void Set(float i) => Set((int)i);

        public override void Set(uint i) => Set((int)i);

        public override void Set(short i) => Set((int)i);

        public override void Set(ushort i) => Set((int)i);

        public override void Set(sbyte i) => Set((int)i);

        public override void Set(byte i) => Set((int)i);

        public override float GetFloat() => (float)data[0];

        public override int GetInt() => data[0];

        public override uint GetUInt() => (uint)data[0];

        public override short GetShort() => (short)data[0];

        public override ushort GetUShort() => (ushort)data[0];

        public override sbyte GetSByte() => (sbyte)data[0];

        public override byte GetByte() => (byte)data[0];
    }
}
