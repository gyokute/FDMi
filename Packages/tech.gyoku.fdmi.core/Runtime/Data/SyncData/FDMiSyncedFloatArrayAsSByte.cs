
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedFloatArrayAsSByte : FDMiBehaviour
    {
        [UdonSynced, HideInInspector] public sbyte[] syncedData;
        public FDMiFloat[] data = new FDMiFloat[0];

        [SerializeField]int serializedStep = 10;

        int dataLength = 0;

        void Start()
        {
            dataLength = Mathf.Min(data.Length, syncedData.Length);
            syncedData = new sbyte[dataLength];
            for (int i = 0; i < dataLength; i++)
            {
                if (data[i])
                {
                    data[i].subscribe(this, string.Format("OnChange{0}", i));
                    syncedData[i] = (sbyte)(data[i].Data * serializedStep);
                }
            }
        }

        #region serialize
        protected bool isPlayerJoined = false;
        protected bool isDeserialized = false;
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal) isPlayerJoined = true;
        }
        public void TrySerialize()
        {
            if (!isPlayerJoined) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
        public void OnDeserialization()
        {
            isDeserialized = true;
            for (int i = 0; i < dataLength; i++)
            {
                if (data[i]) data[i].Data = (float)syncedData[i] / serializedStep;
            }
            isDeserialized = false;
        }
        #endregion

        public void OnChange0()
        {
            if (isDeserialized) return;
            syncedData[0] = (sbyte)(data[0].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange1()
        {
            if (isDeserialized) return;
            syncedData[1] = (sbyte)(data[1].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange2()
        {
            if (isDeserialized) return;
            syncedData[2] = (sbyte)(data[2].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange3()
        {
            if (isDeserialized) return;
            syncedData[3] = (sbyte)(data[3].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange4()
        {
            if (isDeserialized) return;
            syncedData[4] = (sbyte)(data[4].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange5()
        {
            if (isDeserialized) return;
            syncedData[5] = (sbyte)(data[5].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange6()
        {
            if (isDeserialized) return;
            syncedData[6] = (sbyte)(data[6].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange7()
        {
            if (isDeserialized) return;
            syncedData[7] = (sbyte)(data[7].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange8()
        {
            if (isDeserialized) return;
            syncedData[8] = (sbyte)(data[8].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange9()
        {
            if (isDeserialized) return;
            syncedData[9] = (sbyte)(data[9].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange10()
        {
            if (isDeserialized) return;
            syncedData[10] = (sbyte)(data[10].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange11()
        {
            if (isDeserialized) return;
            syncedData[11] = (sbyte)(data[11].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange12()
        {
            if (isDeserialized) return;
            syncedData[12] = (sbyte)(data[12].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange13()
        {
            if (isDeserialized) return;
            syncedData[13] = (sbyte)(data[13].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange14()
        {
            if (isDeserialized) return;
            syncedData[14] = (sbyte)(data[14].Data * serializedStep);
            TrySerialize();
        }
        public void OnChange15()
        {
            if (isDeserialized) return;
            syncedData[15] = (sbyte)(data[15].Data * serializedStep);
            TrySerialize();
        }

    }
}
