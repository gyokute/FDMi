
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiQuaternion : FDMiData
    {
        public Quaternion[] data = new Quaternion[1];
        public Quaternion Data
        {
            get => data[0];
            set
            {
                data[0] = value;
                trigger();
            }
        }
    }
}
