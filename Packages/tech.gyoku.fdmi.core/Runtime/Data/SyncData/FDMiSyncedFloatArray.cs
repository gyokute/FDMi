
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedFloatArray : FDMiBehaviour
    {
        [UdonSynced, HideInInspector] public float[] syncedData = new float[16];
        public FDMiFloat[] data = new FDMiFloat[16];

        void Start()
        {
            for (int i = 0; i < Mathf.Min(data.Length, syncedData.Length); i++)
            {
                if (data[i])
                {
                    data[i].subscribe(this, string.Format("OnChange{0}", i));
                    syncedData[i] = data[i].Data;
                }
            }
        }

        #region serialize
        protected bool isPlayerJoined = false;
        [SerializeField] protected float updateInterval = 0.2f;
        protected double nextUpdateTime;
        bool isDeserialized = false, isWaitingNetworkSettled = false;
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal) isPlayerJoined = true;
        }
        public void TrySerialize()
        {
            if (!isPlayerJoined) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            // Try Serialize.
            if (Time.time > nextUpdateTime)
            {
                isWaitingNetworkSettled = false;
                if (!Networking.IsClogged)
                {
                    RequestSerialization();
                    nextUpdateTime = Time.time + updateInterval;
                    return;
                }
            }

            if (!isWaitingNetworkSettled)
            {
                isWaitingNetworkSettled = true;
                SendCustomEventDelayedSeconds("TrySerialize", updateInterval);
            }
        }
        public void OnDeserialization()
        {
            isDeserialized = true;
            for (int i = 0; i < Mathf.Min(data.Length, syncedData.Length); i++)
            {
                if (data[i]) data[i].Data = syncedData[i];
            }
            isDeserialized = false;
        }
        #endregion

        public void OnChange0()
        {
            if (isDeserialized) return;
            syncedData[0] = data[0].Data;
            TrySerialize();
        }
        public void OnChange1()
        {
            if (isDeserialized) return;
            syncedData[1] = data[1].Data;
            TrySerialize();
        }
        public void OnChange2()
        {
            if (isDeserialized) return;
            syncedData[2] = data[2].Data;
            TrySerialize();
        }
        public void OnChange3()
        {
            if (isDeserialized) return;
            syncedData[3] = data[3].Data;
            TrySerialize();
        }
        public void OnChange4()
        {
            if (isDeserialized) return;
            syncedData[4] = data[4].Data;
            TrySerialize();
        }
        public void OnChange5()
        {
            if (isDeserialized) return;
            syncedData[5] = data[5].Data;
            TrySerialize();
        }
        public void OnChange6()
        {
            if (isDeserialized) return;
            syncedData[6] = data[6].Data;
            TrySerialize();
        }
        public void OnChange7()
        {
            if (isDeserialized) return;
            syncedData[7] = data[7].Data;
            TrySerialize();
        }
        public void OnChange8()
        {
            if (isDeserialized) return;
            syncedData[8] = data[8].Data;
            TrySerialize();
        }
        public void OnChange9()
        {
            if (isDeserialized) return;
            syncedData[9] = data[9].Data;
            TrySerialize();
        }
        public void OnChange10()
        {
            if (isDeserialized) return;
            syncedData[10] = data[10].Data;
            TrySerialize();
        }
        public void OnChange11()
        {
            if (isDeserialized) return;
            syncedData[11] = data[11].Data;
            TrySerialize();
        }
        public void OnChange12()
        {
            if (isDeserialized) return;
            syncedData[12] = data[12].Data;
            TrySerialize();
        }
        public void OnChange13()
        {
            if (isDeserialized) return;
            syncedData[13] = data[13].Data;
            TrySerialize();
        }
        public void OnChange14()
        {
            if (isDeserialized) return;
            syncedData[14] = data[14].Data;
            TrySerialize();
        }
        public void OnChange15()
        {
            if (isDeserialized) return;
            syncedData[15] = data[15].Data;
            TrySerialize();
        }

    }
}
