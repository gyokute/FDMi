
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiVector3 : FDMiData
    {
        [FieldChangeCallback(nameof(Data))] public Vector3[] data = new Vector3[1];
        public Vector3[] Data
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
