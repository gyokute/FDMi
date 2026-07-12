using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiDataSyncAsU8 : FDMiDataSync
    {
        [UdonSynced, HideInInspector]
        public byte syncedValue;

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
                syncedValue = data.GetByte();
                TrySerialize();
            }
        }
    }
}
