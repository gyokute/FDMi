
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.core
{
    public class FDMiAttribute : UdonSharpBehaviour
    {
        #region paramators
        public FDMiObjectManager objectManager;
        [System.NonSerializedAttribute] public Rigidbody body;
        [System.NonSerializedAttribute] public VRCPlayerApi localplayer;
        private bool isOwner;
        #endregion

        #region Ownership
        public virtual void takeOwner()
        {
            Networking.SetOwner(localplayer, gameObject);
            isOwner = true;
        }
        #endregion

        #region FDMi Event Method
        public virtual void init()
        {
            localplayer = Networking.LocalPlayer;
            body = objectManager.body;
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player != localplayer) return;
            isOwner = Networking.IsOwner(gameObject);
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            isOwner = false;
        }
        #endregion
    }
}