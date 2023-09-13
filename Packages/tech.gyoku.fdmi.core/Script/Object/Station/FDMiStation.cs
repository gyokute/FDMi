
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
        public int ownerPriority;
        public VRCPlayerApi seatedPlayer;
        public override void Interact()
        {
            localPlayer.UseAttachedStation();
        }
        public override void OnStationEntered(VRCPlayerApi player)
        {
            seatedPlayer = player;
            if(player.isLocal) stationManager.TryTakePilot();
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            seatedPlayer = null;
        }
    }
}