
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiQuaternion : FDMiData
    {
        [FieldChangeCallback(nameof(Data))] public Quaternion[] data = new Quaternion[1];
        public Quaternion[] Data
        {
            get => data;
            set
            {
                data = value;
                trigger();
            }
        }
    }
}
