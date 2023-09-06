
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiVector3 : FDMiData
    {
        [FieldChangeCallback(nameof(data))]private Vector3[] _data = new Vector3[1];
        public Vector3[] data{
            get => data;
            set{
                data=value;
                trigger();
            }
        }
    }
}
