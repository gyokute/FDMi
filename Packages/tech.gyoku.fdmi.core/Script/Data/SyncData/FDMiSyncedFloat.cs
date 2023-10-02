
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedFloat : FDMiFloat
    {
        [UdonSynced] public float syncedData;
        public override void OnDeserialization()
        {
            Data = syncedData;
        }

        public void set(float src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            syncedData = src;
            Data = src;
            RequestSerialization();
        }
    }
}