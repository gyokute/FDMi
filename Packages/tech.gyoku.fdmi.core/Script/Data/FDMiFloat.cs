
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiFloat : FDMiData
    {
        [FieldChangeCallback(nameof(data))] public float[] _data = new float[1];
        public float[] data
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