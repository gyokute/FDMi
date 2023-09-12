
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedBool : FDMiData
    {
        [UdonSynced, FieldChangeCallback(nameof(Data))] public bool[] data = new bool[1];
        public bool[] Data
        {
            get => data;
            set
            {
                data = value;
                trigger();
            }
        }

        public void set(bool src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Data[0] = src;
            RequestSerialization();
        }
    }
}

