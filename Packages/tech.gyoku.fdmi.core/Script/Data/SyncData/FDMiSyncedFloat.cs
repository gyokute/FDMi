
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncedFloat : FDMiFloat
    {
        [UdonSynced] public float syncedData;
        
        public override void OnDeserialization()
        {
            data[0] = syncedData;
            trigger();
        }

        public override void set(float src)
        {
            base.set(src);
            syncedData = src;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}
