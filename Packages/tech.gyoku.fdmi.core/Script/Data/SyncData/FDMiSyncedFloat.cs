﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedFloat : FDMiFloat
    {
        [UdonSynced, FieldChangeCallback(nameof(SyncedData))] public float syncedData;
        public float SyncedData
        {
            get => syncedData;
            set
            {
                syncedData = value;
                data[0] = value;
                trigger();
            }
        }

        public override void set(float src)
        {
            if (Mathf.Approximately(SyncedData, src)) return;
            SyncedData = src;
            if (!isPlayerJoined) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            TrySerialize();
        }
    }
}
