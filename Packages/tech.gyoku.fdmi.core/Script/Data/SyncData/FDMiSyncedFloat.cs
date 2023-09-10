
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedFloat : FDMiData
    {
        [UdonSynced, FieldChangeCallback(nameof(Data))] public float[] data = new float[1];
        public float[] Data
        {
            get => data;
            set
            {
                data = value;
                trigger();
            }
        }

        public void set(float src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Data[0] = src;
            RequestSerialization();
        }
    }
}