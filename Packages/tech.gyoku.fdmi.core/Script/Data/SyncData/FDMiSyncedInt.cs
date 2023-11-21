
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedInt : FDMiInt
    {
        [UdonSynced, FieldChangeCallback(nameof(SyncedData)), HideInInspector] public int syncedData;
        public int SyncedData
        {
            get => syncedData;
            set
            {
                syncedData = value;
                data[0] = value;
                trigger();
            }
        }

        public override void set(int src)
        {
            SyncedData = src;
            if (!isPlayerJoined) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}
