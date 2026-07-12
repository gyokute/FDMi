using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiQuaternion : FDMiData
    {
        public Quaternion[] data = new Quaternion[1];
        public Quaternion Data
        {
            get => data[0];
            set => Set(value);
        }

        public override void Set(Quaternion src)
        {
            data[0] = src;
            TriggerCallbacks();
        }

        public override Quaternion GetQuaternion() => data[0];
    }
}
