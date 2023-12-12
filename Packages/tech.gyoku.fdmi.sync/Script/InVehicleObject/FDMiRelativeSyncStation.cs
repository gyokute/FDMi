﻿
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
        public bool isChangeRootRefPoint = true;
        public override void OnStationEntered(VRCPlayerApi player)
        {
            base.OnStationEntered(player);
            if (player.isLocal)
            {
                FDMiPlayerPosition lpp = syncManager.localPlayerPosition;
                lpp.inVehicle = true;
                lpp.transform.localPosition = transform.localPosition;
                lpp.transform.localRotation = transform.localRotation;
                if (!refPoint.isRoot && isChangeRootRefPoint) syncManager.changeRootRefPoint(refPoint);
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            base.OnStationExited(player);
            if (player.isLocal)
            {
                FDMiPlayerPosition lpp = syncManager.localPlayerPosition;
                lpp.inVehicle = false;
                if (!refPoint.isRoot && isChangeRootRefPoint) syncManager.changeRootRefPoint(refPoint);
            }
        }

    }
}