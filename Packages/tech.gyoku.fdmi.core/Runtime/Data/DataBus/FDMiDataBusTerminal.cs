
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FDMiDataBusTerminal : UdonSharpBehaviour
    {
        public string[] globalName;
        public string[] privateName;
        public FDMiData[] data;
        public bool[] isGlobal;
        public FDMiDataType[] dataType;

    }
}