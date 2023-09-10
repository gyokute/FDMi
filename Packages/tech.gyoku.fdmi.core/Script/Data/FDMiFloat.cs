
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiFloat : FDMiData
    {
        [FieldChangeCallback(nameof(Data))] public float[] data = new float[1];
        public float[] Data
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