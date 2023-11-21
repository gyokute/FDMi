
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedQuaternion : FDMiQuaternion
    {
        [UdonSynced, FieldChangeCallback(nameof(SyncedData)), HideInInspector] public Quaternion syncedData;
        public Quaternion SyncedData
        {
            get => syncedData;
            set
            {
                syncedData = value;
                data[0] = value;
                trigger();
            }
        }

        public override void set(Quaternion src)
        {
            SyncedData = src;
            if (!isPlayerJoined) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}