using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiDataSyncAsF32 : FDMiDataSync
    {
        [UdonSynced, HideInInspector]
        public float syncedValue;

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
                syncedValue = data.GetFloat();
                TrySerialize();
            }
        }
    }
}
