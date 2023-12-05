﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedVector3 : FDMiVector3
    {
        [UdonSynced, FieldChangeCallback(nameof(SyncedData)), HideInInspector] public Vector3 syncedData;
        public Vector3 SyncedData
        {
            get => syncedData;
            set
            {
                syncedData = value;
                data[0] = value;
                trigger();
            }
        }

        public override void set(Vector3 src)
        {
            if (Vector3.Distance(SyncedData, src) <= Mathf.Epsilon) return;
            SyncedData = src;
            if (!isPlayerJoined) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            TrySerialize();
        }
    }
}