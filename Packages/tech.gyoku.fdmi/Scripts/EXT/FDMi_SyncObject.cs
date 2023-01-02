
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace SaccFlight_FDMi
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMi_SyncObject : UdonSharpBehaviour
    {
        // [UdonSynced(UdonSyncMode.None)] public float val;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Val))] public float val;
        public virtual float Val
        {
            get => val;
            set
            {
                val = value;
                whenUpdate();
            }
        }

        public virtual void whenUpdate() { }
    }
}