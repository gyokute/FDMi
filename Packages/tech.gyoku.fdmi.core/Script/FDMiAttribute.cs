
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiAttribute : UdonSharpBehaviour
    {
        #region paramators
        [SerializeField] private FDMiObjectManager objectManager;
        [System.NonSerializedAttribute] public Rigidbody body;
        [System.NonSerializedAttribute] public VRCPlayerApi localplayer;
        #endregion


        #region FDMi+SaccFlight Event Method
        public virtual void localStart() { }
        public virtual void initData() { }
        public void init()
        {
            localplayer = Networking.LocalPlayer;
            body = objectManager.body;
            localStart();
        }
        #endregion 
        #region events
        public virtual void EVT_O_Enter() { }
        public virtual void EVT_O_Exit() { }
        public virtual void EVT_G_PilotChanged() { }
        public virtual void EVT_G_PassengerEnter() { }
        public virtual void EVT_G_PassengerExit() { }
        public virtual void EVT_O_TakeOwnership() { }
        public virtual void EVT_O_LoseOwnership() { }
        public virtual void EVT_G_Explode() { }
        public virtual void ResetStatus() { }
        public virtual void EVT_O_OnPlayerJoined() { }
        #endregion
    }
}