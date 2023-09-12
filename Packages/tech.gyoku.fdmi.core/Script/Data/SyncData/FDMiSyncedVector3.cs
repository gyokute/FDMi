
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedVector3 : FDMiData
    {
        public Vector3[] data = new Vector3[1];
        [UdonSynced, FieldChangeCallback(nameof(Data))] public Vector3 syncedData;
        public Vector3 Data
        {
            get => data[0];
            set
            {
                syncedData = value;
                data[0] = value;
                trigger();
            }
        }

        public void set(Vector3 src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            syncedData = src;
            RequestSerialization();
        }
    }
}