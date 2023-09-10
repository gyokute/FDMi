
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedQuaternion : FDMiData
    {
        [UdonSynced, FieldChangeCallback(nameof(Data))] public Quaternion[] data = new Quaternion[1];
        public Quaternion[] Data
        {
            get => data;
            set
            {
                data = value;
                trigger();
            }
        }

        public void set(Quaternion src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Data[0] = src;
            RequestSerialization();
        }
    }
}