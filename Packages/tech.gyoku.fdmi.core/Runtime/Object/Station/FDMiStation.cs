
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiStation : FDMiAttribute
    {
        public VRCStation station;
        public FDMiStationManager stationManager;
        public int pilotPriority;
        public VRCPlayerApi seatedPlayer;
        public GameObject onlyInSeat;
        public UdonSharpBehaviour[] InSeatBehaviours;
        void Start()
        {
            if (onlyInSeat) onlyInSeat.SetActive(false);
        }
        public override void Interact()
        {
            localPlayer.UseAttachedStation();
        }
        public void ExitStation()
        {
            station.ExitStation(localPlayer);
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            seatedPlayer = player;
            if (!player.isLocal) return;
            if (player.isLocal && pilotPriority < 1) stationManager.TryTakePilot(pilotPriority);
            if (onlyInSeat) onlyInSeat.SetActive(true);
            foreach (UdonSharpBehaviour usb in InSeatBehaviours)
                if (usb) usb.SendCustomEvent("FDMiOnSeatEnter");
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            seatedPlayer = null;
            if (!player.isLocal) return;
            if (onlyInSeat) onlyInSeat.SetActive(false);
            foreach (UdonSharpBehaviour usb in InSeatBehaviours)
                if (usb) usb.SendCustomEvent("FDMiOnSeatExit");
        }
    }
}