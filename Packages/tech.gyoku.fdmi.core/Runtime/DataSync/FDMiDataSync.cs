using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class FDMiDataSync : FDMiBehaviour
    {
        [Tooltip("データの解決パス。名前空間の区切りは /、~ は機体の起点")]
        public string dataPath;

        [FDMiDataPath(nameof(dataPath)), FDMiRegisterCallback(nameof(OnDataChanged))]
        [Tooltip("同期する FDMiData。dataPath から解決する")]
        public FDMiData data;
        protected bool whenDeserializing = false;

        public abstract void OnDataChanged();

        [SerializeField]
        [Tooltip("送信間隔 [s]。混雑時はこの間隔で再試行する")]
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
