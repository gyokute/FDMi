
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedBool : FDMiData
    {
        public bool[] data = new bool[1];
        [UdonSynced, FieldChangeCallback(nameof(Data))] public bool syncedData;
        public bool Data
        {
            get => data[0];
            set
            {
                syncedData = value;
                data[0] = value;
                trigger();
            }
        }

        public void set(bool src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            syncedData = src;
            RequestSerialization();
        }
    }
}

