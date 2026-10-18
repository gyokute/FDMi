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
        public FDMiGeodeticObjectSync vehicleSync;

        const string SaccEntityTypeName = "SaccFlightAndVehicles.SaccEntity";
        bool registered;

        void Start()
        {
            Register();
        }

        public void Register()
        {
            if (registered)
                return;
            CacheVehicle();
            if (!EntityControl)
                return;
            // `as UdonSharpBehaviour[]` だと UdonSharp binder(BinderSyntaxVisitor.VisitBinaryExpression)が
            // NullReferenceException で落ちる(SDK 3.10.5 時点)。直接 cast + null ガードで回避する。
            object extensions = EntityControl.GetProgramVariable("ExtensionUdonBehaviours");
            if (extensions == null)
                return;
            UdonSharpBehaviour[] behaviours = (UdonSharpBehaviour[])extensions;
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

        /// <summary>SaccEntity を型名で探す(Editor の AutoSetup と同じ探索)。</summary>
        void CacheVehicle()
        {
            if (EntityControl)
                return;
            UdonSharpBehaviour[] behaviours = GetComponents<UdonSharpBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i].GetUdonTypeName() == SaccEntityTypeName)
                {
                    EntityControl = behaviours[i];
                    return;
                }
            }
            Debug.LogWarning("FDMiSaccBridge: " + SaccEntityTypeName + " が同一 GameObject に無いため SFEXT 連携を登録できない: " + gameObject.name);
        }

        public void SFEXT_O_PilotEnter()
        {
            if (vehicleSync)
                vehicleSync.CaptureAndSend(true);
        }

        public void SFEXT_G_PilotEnter()
        {
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
