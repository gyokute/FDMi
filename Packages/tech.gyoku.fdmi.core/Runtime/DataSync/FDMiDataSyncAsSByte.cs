using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiDataSyncAsSByte : FDMiBehaviour
    {
        public string dataPath;

        [FDMiDataPath(nameof(dataPath)), FDMiRegisterCallback(nameof(OnDataChanged))]
        public FDMiData data;

        [UdonSynced, HideInInspector]
        public sbyte syncedValue;

        void OnDeserialization()
        {
            data.Set(syncedValue);
        }

        public void OnDataChanged()
        {
            sbyte next = data.GetSByte();
            if (syncedValue == next)
                return;
            syncedValue = next;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            TrySerialize();
        }

        [SerializeField]
        protected float updateInterval = 0.25f;
        protected double nextUpdateTime;
        public bool trySerializeLatch = false;

        public void TrySerialize()
        {
            // Try Serialize.
            if (Time.time > nextUpdateTime && !Networking.IsClogged)
            {
                RequestSerialization();
                nextUpdateTime = Time.time + updateInterval;
                trySerializeLatch = false;
            }
            else if (!trySerializeLatch)
            {
                SendCustomEventDelayedSeconds("TrySerialize", updateInterval);
                trySerializeLatch = true;
            }
        }
    }
}
