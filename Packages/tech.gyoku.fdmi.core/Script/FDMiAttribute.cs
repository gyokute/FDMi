
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
        #endregion

        #region FDMi Event Method
        public virtual void init()
        {
            localplayer = Networking.LocalPlayer;
            body = objectManager.body;
        }
        
        #endregion 
    }
}