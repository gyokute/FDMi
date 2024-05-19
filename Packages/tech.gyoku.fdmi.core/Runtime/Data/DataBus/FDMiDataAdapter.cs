
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FDMiDataAdapter : UdonSharpBehaviour
    {
        public string primaryBusName;
        public string[] primaryDN, replicaDN;

    }
}