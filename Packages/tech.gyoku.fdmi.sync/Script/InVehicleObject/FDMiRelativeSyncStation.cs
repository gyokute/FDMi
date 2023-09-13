
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeSyncStation : FDMiStation
    {
        public FDMiRelativeObjectSyncManager syncManager;
        public FDMiReferencePoint refPoint;
        public override void OnStationEntered(VRCPlayerApi player)
        {
            base.OnStationEntered(player);
            if (player.isLocal)
            {
                FDMiPlayerPosition lpp = syncManager.localPlayerPosition;
                lpp.inVehicle = true;
                if (!refPoint.isRoot) syncManager.changeRootRefPoint(refPoint);
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            base.OnStationExited(player);
            if (player.isLocal)
            {
                FDMiPlayerPosition lpp = syncManager.localPlayerPosition;
                lpp.inVehicle = false;
                if (!refPoint.isRoot) syncManager.changeRootRefPoint(refPoint);
            }
        }

    }
}