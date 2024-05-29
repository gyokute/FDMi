
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
        public FDMiFloat data0;
        public FDMiFloat data1;
        public FDMiFloat data2;
        public FDMiFloat data3;
        public FDMiFloat data4;
        public FDMiFloat data5;
        public FDMiFloat data6;
        public FDMiFloat data7;
        public FDMiFloat data8;
        public FDMiFloat data9;
        public FDMiFloat data10;
        public FDMiFloat data11;
        public FDMiFloat data12;
        public FDMiFloat data13;
        public FDMiFloat data14;
        public FDMiFloat data15;

        void Start()
        {
            if (data0) { data0.subscribe(this, "OnChange0"); syncedData[0] = data0.Data; }
            if (data1) { data1.subscribe(this, "OnChange1"); syncedData[1] = data1.Data; }
            if (data2) { data2.subscribe(this, "OnChange2"); syncedData[2] = data2.Data; }
            if (data3) { data3.subscribe(this, "OnChange3"); syncedData[3] = data3.Data; }
            if (data4) { data4.subscribe(this, "OnChange4"); syncedData[4] = data4.Data; }
            if (data5) { data5.subscribe(this, "OnChange5"); syncedData[5] = data5.Data; }
            if (data6) { data6.subscribe(this, "OnChange6"); syncedData[6] = data6.Data; }
            if (data7) { data7.subscribe(this, "OnChange7"); syncedData[7] = data7.Data; }
            if (data8) { data8.subscribe(this, "OnChange8"); syncedData[8] = data8.Data; }
            if (data9) { data9.subscribe(this, "OnChange9"); syncedData[9] = data9.Data; }
            if (data10) { data10.subscribe(this, "OnChange10"); syncedData[10] = data10.Data; }
            if (data11) { data11.subscribe(this, "OnChange11"); syncedData[11] = data11.Data; }
            if (data12) { data12.subscribe(this, "OnChange12"); syncedData[12] = data12.Data; }
            if (data13) { data13.subscribe(this, "OnChange13"); syncedData[13] = data13.Data; }
            if (data14) { data14.subscribe(this, "OnChange14"); syncedData[14] = data14.Data; }
            if (data15) { data15.subscribe(this, "OnChange15"); syncedData[15] = data15.Data; }
        }

        #region serialize
        protected bool isPlayerJoined = false;
        [SerializeField] protected float updateInterval = 0.2f;
        protected double nextUpdateTime;
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
                if (!Networking.IsClogged)
                {
                    RequestSerialization();
                    nextUpdateTime = Time.time + updateInterval;
                }
                else { SendCustomEventDelayedSeconds("TrySerialize", updateInterval); }
            }
        }
        public void OnDeserialization()
        {
            if (data0) data0.Data = syncedData[0];
            if (data1) data1.Data = syncedData[1];
            if (data2) data2.Data = syncedData[2];
            if (data3) data3.Data = syncedData[3];
            if (data4) data4.Data = syncedData[4];
            if (data5) data5.Data = syncedData[5];
            if (data6) data6.Data = syncedData[6];
            if (data7) data7.Data = syncedData[7];
            if (data8) data8.Data = syncedData[8];
            if (data9) data9.Data = syncedData[9];
            if (data10) data10.Data = syncedData[10];
            if (data11) data11.Data = syncedData[11];
            if (data12) data12.Data = syncedData[12];
            if (data13) data13.Data = syncedData[13];
            if (data14) data14.Data = syncedData[14];
            if (data15) data15.Data = syncedData[15];
        }
        #endregion

        public void OnChange0()
        {
            syncedData[0] = data0.Data;
            TrySerialize();
        }
        public void OnChange1()
        {
            syncedData[1] = data1.Data;
            TrySerialize();
        }
        public void OnChange2()
        {
            syncedData[2] = data2.Data;
            TrySerialize();
        }
        public void OnChange3()
        {
            syncedData[3] = data3.Data;
            TrySerialize();
        }
        public void OnChange4()
        {
            syncedData[4] = data4.Data;
            TrySerialize();
        }
        public void OnChange5()
        {
            syncedData[5] = data5.Data;
            TrySerialize();
        }
        public void OnChange6()
        {
            syncedData[6] = data6.Data;
            TrySerialize();
        }
        public void OnChange7()
        {
            syncedData[7] = data7.Data;
            TrySerialize();
        }
        public void OnChange8()
        {
            syncedData[8] = data8.Data;
            TrySerialize();
        }
        public void OnChange9()
        {
            syncedData[9] = data9.Data;
            TrySerialize();
        }
        public void OnChange10()
        {
            syncedData[10] = data10.Data;
            TrySerialize();
        }
        public void OnChange11()
        {
            syncedData[11] = data11.Data;
            TrySerialize();
        }
        public void OnChange12()
        {
            syncedData[12] = data12.Data;
            TrySerialize();
        }
        public void OnChange13()
        {
            syncedData[13] = data13.Data;
            TrySerialize();
        }
        public void OnChange14()
        {
            syncedData[14] = data14.Data;
            TrySerialize();
        }
        public void OnChange15()
        {
            syncedData[15] = data15.Data;
            TrySerialize();
        }

    }
}
