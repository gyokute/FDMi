
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedFloat : FDMiData
    {
        public float[] data = new float[1];
        [UdonSynced, FieldChangeCallback(nameof(Data))] public float syncedData;
        public float Data
        {
            get => data[0];
            set
            {
                syncedData = value;
                data[0] = value;
                trigger();
            }
        }

        public void set(float src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            data[0] = src;
            syncedData = src;
            RequestSerialization();
        }
    }
}