
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
        [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
        public bool isOwner, isInit = false;
        #endregion

        #region Ownership
        public virtual void takeOwner()
        {
            Networking.SetOwner(localPlayer, gameObject);
            isOwner = true;
        }
        #endregion

        #region FDMi Event Method
        public virtual void init()
        {
            if (!objectManager) return;
            body = objectManager.body;
            isInit = true;
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            localPlayer = Networking.LocalPlayer;
            isOwner = Networking.IsOwner(gameObject);
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            isOwner = false;
        }
        #endregion
    }
}