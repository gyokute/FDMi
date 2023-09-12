
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiBool : FDMiData
    {
        [FieldChangeCallback(nameof(Data))] public bool[] data = new bool[1];
        public bool[] Data
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