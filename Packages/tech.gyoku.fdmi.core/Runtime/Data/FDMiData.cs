
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.core
{
    public class FDMiData : FDMiEvent
    {
        protected bool isPlayerJoined = false;
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal) isPlayerJoined = true;
        }
    }
}