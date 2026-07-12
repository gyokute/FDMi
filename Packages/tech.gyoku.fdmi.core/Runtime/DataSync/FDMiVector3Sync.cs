using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiVector3Sync : FDMiDataSync
    {
        [UdonSynced, HideInInspector]
        public Vector3 syncedValue;

        public override void OnDeserialization()
        {
            whenDeserializing = true;
            data.Set(syncedValue);
        }

        public override void OnDataChanged()
        {
            if (whenDeserializing)
            {
                whenDeserializing = false;
            }
            else
            {
                syncedValue = data.GetVector3();
                TrySerialize();
            }
        }
    }
}
