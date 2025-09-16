
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedSBytePacked : FDMiBehaviour
    {
        [UdonSynced, HideInInspector] public uint syncedData;
        public FDMiSByte[] data = new FDMiSByte[4];
        private sbyte[][] _data = new sbyte[4][];
        private int dataLength = 0;

        void Start()
        {
            dataLength = Mathf.Min(data.Length, 4);
            for (int i = 0; i < dataLength; i++)
            {
                if (data[i])
                {
                    data[i].subscribe(this, "OnChange");
                    _data[i] = data[i].data;
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
            UnpackBytes(dataLength, syncedData, ref _data);
            isDeserialized = false;
        }
        #endregion

        private static void PackBytes(int len, ref sbyte[][] bytes, out uint packed)
        {
            packed = 0;
            sbyte[] packedByteArray = new sbyte[4] { 0, 0, 0, 0 };
            for (int i = 0; i < len; i++)
            {
                packedByteArray[i] = bytes[i][0];
            }
            packed = BitConverter.ToUInt32((byte[])(Array)packedByteArray, 0);
        }
        private static void UnpackBytes(int len, uint packed, ref sbyte[][] bytes)
        {
            sbyte[] packedByteArray = (sbyte[])(Array)BitConverter.GetBytes(packed);
            for (int i = 0; i < len; i++)
            {
                bytes[i][0] = packedByteArray[i];
            }
        }

        public void OnChange()
        {
            if (isDeserialized) return;
            PackBytes(dataLength, ref _data, out syncedData);
            TrySerialize();
        }
    }
}
