
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedVector3 : FDMiData
    {
        [UdonSynced, FieldChangeCallback(nameof(Data))] public Vector3[] data = new Vector3[1];
        public Vector3[] Data
        {
            get => data;
            set
            {
                data = value;
                trigger();
            }
        }

        public void set(Vector3 src)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Data[0] = src;
            RequestSerialization();
        }
    }
}