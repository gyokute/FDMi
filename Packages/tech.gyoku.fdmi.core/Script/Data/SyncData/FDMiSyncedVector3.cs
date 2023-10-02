
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedVector3 : FDMiVector3
    {
        [UdonSynced] public Vector3 syncedData;
        public override void OnDeserialization()
        {
            Data = syncedData;
        }

        public void set(Vector3 src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            syncedData = src;
            Data = src;
            RequestSerialization();
        }
    }
}