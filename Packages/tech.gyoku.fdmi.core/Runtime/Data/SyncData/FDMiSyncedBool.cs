
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedBool : FDMiBool
    {
        [UdonSynced, FieldChangeCallback(nameof(SyncedData)), HideInInspector] public bool syncedData;
        public bool SyncedData
        {
            get => syncedData;
            set
            {
                syncedData = value;
                data[0] = value;
                trigger();
            }
        }

        public override void set(bool src)
        {
            if (SyncedData == src) return;
            SyncedData = src;
            if (!isPlayerJoined) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            TrySerialize();
        }
    }
}

