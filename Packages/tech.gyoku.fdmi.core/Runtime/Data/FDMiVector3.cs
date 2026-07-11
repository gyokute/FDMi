using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiVector3 : FDMiData
    {
        public Vector3[] data = new Vector3[1];
        public Vector3 Data
        {
            get => data[0];
            set => Set(value);
        }

        public override void Set(Vector3 src)
        {
            data[0] = src;
            TriggerCallbacks();
        }

        public override Vector3 GetVector3() => data[0];
    }
}
