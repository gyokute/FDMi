
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedBool : FDMiBool
    {
        [UdonSynced] public bool syncedData;

        public override void OnDeserialization()
        {
            Data = syncedData;
        }

        public void set(bool src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            syncedData = src;
            Data = src;
            RequestSerialization();
        }
    }
}

