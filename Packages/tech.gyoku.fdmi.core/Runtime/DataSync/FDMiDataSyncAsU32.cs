using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiDataSyncAsU32 : FDMiDataSync
    {
        [UdonSynced, HideInInspector]
        public uint syncedValue;

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
                syncedValue = data.GetUInt();
                TrySerialize();
            }
        }
    }
}
