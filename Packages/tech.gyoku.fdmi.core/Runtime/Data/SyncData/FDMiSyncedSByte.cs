
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedSByte : FDMiSByte
    {
        [UdonSynced, FieldChangeCallback(nameof(SyncedData)), HideInInspector] public sbyte syncedData;
        public sbyte SyncedData
        {
            get => syncedData;
            set
            {
                syncedData = value;
                data[0] = value;
                trigger();
            }
        }

        public override void set(sbyte src)
        {
            if (SyncedData == src) return;
            SyncedData = src;
            if (!isPlayerJoined) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            TrySerialize();
        }
    }
}

