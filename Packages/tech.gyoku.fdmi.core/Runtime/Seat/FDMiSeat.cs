using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiSeat : FDMiBehaviour
    {
        public VRCStation station;
        public int pilotPriority = -1;

        public VRCPlayerApi seatedPlayer;
        public UdonSharpBehaviour[] InSeatBehaviours;

        [FDMiDataPathConst("~/IsPilot")]
        public FDMiBool IsPilot;

        [FDMiDataPathConst("~/HasPilot")]
        public FDMiBool HasPilot;

        [FDMiDataPathConst("~/PilotID")]
        public FDMiInt PilotID;

        [FDMiDataPathConst("~/CurrentPilotPriority")]
        public FDMiInt CurrentPilotPriority;

        public override void Interact()
        {
            Networking.LocalPlayer.UseAttachedStation();
        }

        public void ExitStation()
        {
            station.ExitStation(Networking.LocalPlayer);
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            seatedPlayer = player;
            if (player.isLocal)
            {
                TryTakePilot();
                foreach (UdonSharpBehaviour usb in InSeatBehaviours)
                    if (usb)
                        usb.SendCustomEvent("FDMiOnSeatEnter");
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            seatedPlayer = null;
            if (player.isLocal)
            {
                foreach (UdonSharpBehaviour usb in InSeatBehaviours)
                    if (usb)
                        usb.SendCustomEvent("FDMiOnSeatExit");
            }
        }

        #region Pilot Management
        public void TryTakePilot()
        {
            if (seatedPlayer.isLocal != true)
                return;
            if (pilotPriority >= CurrentPilotPriority.Data)
            {
                IsPilot.Data = true;
                HasPilot.Data = true;
                CurrentPilotPriority.Data = pilotPriority;
            }
        }
        #endregion
    }
}
