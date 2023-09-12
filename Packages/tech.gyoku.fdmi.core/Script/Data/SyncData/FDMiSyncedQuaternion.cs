
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedQuaternion : FDMiData
    {
        public Quaternion[] data = new Quaternion[1];
        [UdonSynced, FieldChangeCallback(nameof(Data))] public Quaternion syncedData;
        public Quaternion Data
        {
            get => data[0];
            set
            {
                syncedData = value;
                data[0] = value;
                trigger();
            }
        }

        public void set(Quaternion src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            syncedData = src;
            RequestSerialization();
        }
    }
}