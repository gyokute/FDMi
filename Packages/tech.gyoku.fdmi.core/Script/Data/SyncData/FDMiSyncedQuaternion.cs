
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedQuaternion : FDMiQuaternion
    {
        [UdonSynced] public Quaternion syncedData;
        public override void OnDeserialization()
        {
            data[0] = syncedData;
            trigger();
        }

        public override void set(Quaternion src)
        {
            base.set(src);
            syncedData = src;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}