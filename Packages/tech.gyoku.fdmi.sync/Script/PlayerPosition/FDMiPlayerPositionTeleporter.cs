
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiPlayerPositionTeleporter : UdonSharpBehaviour
    {
        public FDMiRelativePositionTrigger teleportTarget, teleportFrom;
        public Transform teleportPosition;
        public void ExecTeleport(){
            if(teleportFrom) teleportFrom.ExitLocalPlayer();
            if(teleportTarget) teleportTarget.TeleportLocalPlayer(teleportPosition);
        }
    }
}