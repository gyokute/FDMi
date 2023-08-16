
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativeSyncStation : UdonSharpBehaviour
    {
        public VRCStation station;
        public FDMiRelativeObjectSyncManager syncManager;
        public FDMiReferencePoint refPoint;
        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                FDMiPlayerPosition lpp = syncManager.localPlayerPosition;
                lpp.inVehicle = true;
                if (!refPoint.isRoot) syncManager.changeRootRefPoint(refPoint);
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                FDMiPlayerPosition lpp = syncManager.localPlayerPosition;
                lpp.inVehicle = false;
                if (!refPoint.isRoot) syncManager.changeRootRefPoint(refPoint);
            }
        }

    }
}