using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{
    public enum SharedBool
    {
        initialized, isPilot, hasPilot, isPassenger,
        isVR, InEditor, isReset,
        Length
    }


    public class FDMi_SharedParam : UdonSharpBehaviour
    {
        // Shareing variables using in FDMi.
        public bool[] sharedBool = { false };
        [System.NonSerializedAttribute] public float[] airData = null;
        public FDMi_Attributes[] EXT;
        public SaccEntity EntityControl;
        private VRC.SDK3.Components.VRCObjectSync VehicleObjectSync;
        private Vector3 initPos;
        private Quaternion initRot;
        public Rigidbody VehicleRigidbody;
        VRCPlayerApi player;
        public Vector3 CurrentVel;
        public bool inReset = true;

        #region SFEXT Events
        public void Start()
        {
            VehicleRigidbody.isKinematic = true;
        }
        public void SFEXT_L_EntityStart()
        {
            foreach (FDMi_Attributes ext in EXT) { ext.FDMi_InitData(); }
            Init();
            foreach (FDMi_Attributes ext in EXT) { ext.FDMi_Init(); }
        }

        public void SFEXT_G_PilotEnter()
        {
            sharedBool[(int)SharedBool.hasPilot] = true;
            sharedBool[(int)SharedBool.isReset] = false;
            foreach (FDMi_Attributes ext in EXT) { ext.SFEXT_G_PilotEnter(); }
        }

        public void SFEXT_G_PilotExit()
        {
            sharedBool[(int)SharedBool.hasPilot] = false;
            foreach (FDMi_Attributes ext in EXT) { ext.SFEXT_G_PilotExit(); }
        }

        public void SFEXT_O_PilotEnter()
        {
            sharedBool[(int)SharedBool.isPilot] = true;
            sharedBool[(int)SharedBool.isReset] = false;
            VehicleRigidbody.isKinematic = false;

            foreach (FDMi_Attributes ext in EXT)
            {
                Networking.SetOwner(Networking.LocalPlayer, ext.gameObject);
                ext.SFEXT_O_PilotEnter();
            }
        }
        public void SFEXT_O_PilotExit()
        {
            sharedBool[(int)SharedBool.isPilot] = false;
            foreach (FDMi_Attributes ext in EXT) { ext.SFEXT_O_PilotExit(); }
        }
        public void SFEXT_P_PassengerEnter()
        {
            sharedBool[(int)SharedBool.isPassenger] = true;
            foreach (FDMi_Attributes ext in EXT) { ext.SFEXT_P_PassengerEnter(); }
        }
        public void SFEXT_P_PassengerExit()
        {
            sharedBool[(int)SharedBool.isPassenger] = false;
            foreach (FDMi_Attributes ext in EXT) { ext.SFEXT_P_PassengerExit(); }
        }
        public void SFEXT_G_Explode()
        {
            foreach (FDMi_Attributes ext in EXT) { ext.SFEXT_G_Explode(); }
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            foreach (FDMi_Attributes ext in EXT) { ext.SFEXT_O_OnPlayerJoined(); }
        }
        public void TakeOwnerShipOfExtensions()
        {
            foreach (FDMi_Attributes ext in EXT) { Networking.SetOwner(player, ext.gameObject); }
        }

        #endregion

        #region Shared Paramators
        public float heightOffset, headingOffset;
        private void Init()
        {
            sharedBool = new bool[(int)SharedBool.Length];
            VehicleRigidbody = EntityControl.gameObject.GetComponent<Rigidbody>();
            VehicleObjectSync = (VRC.SDK3.Components.VRCObjectSync)EntityControl.gameObject.GetComponent(typeof(VRC.SDK3.Components.VRCObjectSync));
            player = Networking.LocalPlayer;
            initPos = VehicleRigidbody.transform.localPosition;
            initRot = VehicleRigidbody.transform.localRotation;
            if (player == null)
            {
                sharedBool[(int)SharedBool.InEditor] = true;
                return;
            }
            VehicleRigidbody.isKinematic = true;
            sharedBool[(int)SharedBool.isVR] = player.IsUserInVR();
            sharedBool[(int)SharedBool.initialized] = true;
            sharedBool[(int)SharedBool.isReset] = true;
        }

        public void SFEXT_O_RespawnButton()
        {
            if (!sharedBool[(int)SharedBool.hasPilot])
            {
                Networking.SetOwner(player, EntityControl.gameObject);
                EntityControl.TakeOwnerShipOfExtensions();
                TakeOwnerShipOfExtensions();

                VehicleRigidbody.transform.localPosition = initPos;
                VehicleRigidbody.transform.localRotation = initRot;
                VehicleRigidbody.velocity = Vector3.zero;

                VehicleRigidbody.angularVelocity = Vector3.zero;//editor needs this
                VehicleRigidbody.isKinematic = true;
                sharedBool[(int)SharedBool.isReset] = true;
                foreach (FDMi_Attributes ext in EXT) { ext.ResetStatus(); }
            }
        }

        #endregion
    }
}