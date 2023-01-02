
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlight_FDMi
{

    public class FDMi_ObjectSwitcher : FDMi_Attributes
    {
        [SerializeField] private bool Local_Start, G_PilotEnter, O_PilotEnter, P_PassengerEnter;
        public override void FDMi_Local_Start() { if (Local_Start) gameObject.SetActive(false); }
        public override void SFEXT_G_PilotEnter() { if (G_PilotEnter) gameObject.SetActive(true); }
        public override void SFEXT_G_PilotExit() { if (G_PilotEnter) gameObject.SetActive(false); }
        public override void SFEXT_O_PilotEnter() { if (O_PilotEnter) gameObject.SetActive(true); }
        public override void SFEXT_O_PilotExit() { if (O_PilotEnter) gameObject.SetActive(false); }
        public override void SFEXT_P_PassengerEnter() { if (P_PassengerEnter) gameObject.SetActive(true); }
        public override void SFEXT_P_PassengerExit() { if (P_PassengerEnter) gameObject.SetActive(false); }
    }
}