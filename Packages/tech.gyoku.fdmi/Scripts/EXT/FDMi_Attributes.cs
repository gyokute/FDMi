
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace SaccFlight_FDMi
{
    public class FDMi_Attributes : UdonSharpBehaviour
    {
        public FDMi_SharedParam Param;
        [System.NonSerializedAttribute] public Rigidbody VehicleRigidbody;
        [System.NonSerializedAttribute] public bool[] sharedBool = { false };
        [System.NonSerializedAttribute] public float[] airData = null;
        [System.NonSerializedAttribute] public VRCPlayerApi player;

        public virtual void FDMi_Local_Start() { }
        public virtual void FDMi_InitData() { }

        #region FDMi+SaccFlight Event Method
        public void FDMi_Init()
        {
            sharedBool = Param.sharedBool;
            airData = Param.airData;
            player = Networking.LocalPlayer;
            VehicleRigidbody = Param.VehicleRigidbody;
            FDMi_Local_Start();
        }

        public virtual void SFEXT_G_PilotEnter() { }
        public virtual void SFEXT_G_PilotExit() { }
        public virtual void SFEXT_O_PilotEnter() { }
        public virtual void SFEXT_O_PilotExit() { }
        public virtual void SFEXT_P_PassengerEnter() { }
        public virtual void SFEXT_P_PassengerExit() { }
        public virtual void SFEXT_O_TakeOwnership() { }
        public virtual void SFEXT_O_LoseOwnership() { }
        public virtual void SFEXT_G_Explode() { }
        public virtual void ResetStatus() { }
        public virtual void SFEXT_O_OnPlayerJoined() { }
        #endregion
    }
}