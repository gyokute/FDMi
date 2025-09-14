
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public enum FDMiDataType
    {
        FDMiData, FDMiBool, FDMiInt, FDMiFloat, FDMiVector3, FDMiQuaternion,
        FDMiSyncedBool, FDMiSyncedInt, FDMiSyncedFloat, FDMiSyncedVector3, FDMiSyncedQuaternion,
        FDMiSByte, FDMiSyncedSByte, FDMiByte, FDMiSyncedByte
    }
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FDMiDataBus : UdonSharpBehaviour
    {
        public string dataBusName;
        public string[] dn;
        public FDMiData[] data;
    }
}