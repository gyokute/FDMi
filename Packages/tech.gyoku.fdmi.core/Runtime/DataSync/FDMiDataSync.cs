using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class FDMiDataSync : FDMiBehaviour
    {
        public string dataPath;

        [FDMiDataPath(nameof(dataPath)), FDMiRegisterCallback(nameof(OnDataChanged))]
        public FDMiData data;
        protected bool whenDeserializing = false;

        public abstract void OnDataChanged();

        [SerializeField]
        protected float updateInterval = 0.25f;
        protected double nextUpdateTime;
        protected bool trySerializeLatch = false;

        protected void TrySerialize()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
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
