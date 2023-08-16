
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiDataBus : UdonSharpBehaviour
    {
        public string busName;
        public string[] indexName;
        public float[] data;
    }
}