using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiSaccBridge : FDMiBehaviour
    {
        [Tooltip("Sacc vehicle root (SaccEntity on the same GameObject). Set by Editor; field name must stay EntityControl.")]
        public UdonSharpBehaviour EntityControl;
        [Tooltip("Sync body on the same vehicle root.")]
        public FDMiEcefSync vehicleSync;
        [Tooltip("Turn off while Sacc owns motion (pilot seated). On = FDMi follows Sacc each frame.")]
        public bool followSacc = true;

        UdonSharpBehaviour cachedVehicle;
        Transform vehicleTransform;
        bool registered;

        void Start()
        {
            vehicleTransform = vehicleSync && vehicleSync.target ? vehicleSync.target : transform;
            CacheVehicle();
            Register();
        }

        void Update()
        {
            if (!followSacc || !vehicleSync || !vehicleTransform)
                return;
            if (!vehicleSync.origin)
                return;
            if (Networking.IsOwner(vehicleSync.gameObject))
                vehicleSync.CaptureAndSend(false);
        }

        public void Register()
        {
            if (registered)
                return;
            CacheVehicle();
            if (!EntityControl)
                return;
            object extensions = EntityControl.GetProgramVariable("ExtensionUdonBehaviours");
            UdonSharpBehaviour[] behaviours = extensions as UdonSharpBehaviour[];
            if (behaviours == null)
                return;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == this)
                    return;
            }
            UdonSharpBehaviour[] grown = new UdonSharpBehaviour[behaviours.Length + 1];
            for (int i = 0; i < behaviours.Length; i++)
                grown[i] = behaviours[i];
            grown[behaviours.Length] = this;
            EntityControl.SetProgramVariable("ExtensionUdonBehaviours", grown);
            registered = true;
        }

        void CacheVehicle()
        {
            if (!EntityControl)
                EntityControl = GetComponent<UdonSharpBehaviour>();
            cachedVehicle = EntityControl;
        }

        public void SFEXT_O_PilotEnter()
        {
            followSacc = true;
            if (vehicleSync)
                vehicleSync.CaptureAndSend(true);
        }

        public void SFEXT_G_PilotEnter()
        {
            followSacc = true;
        }

        public void SFEXT_O_TakeOwnership()
        {
            if (vehicleSync)
                vehicleSync.CaptureAndSend(true);
        }

        public void SFEXT_G_RespawnButton()
        {
            if (vehicleSync)
                vehicleSync.CaptureAndSend(true);
        }

        public void SFEXT_O_PilotExit()
        {
            if (vehicleSync)
                vehicleSync.CaptureAndSend(true);
        }
    }
}
