using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiRelativeSyncStation : FDMiBehaviour
    {
        public FDMiRelativeObjectSyncManager syncManager;
        public FDMiReferencePoint refPoint;

        public bool isChangeRootRefPoint = true;

        public void FDMiOnSeatEnter()
        {
            FDMiPlayerPosition lpp = syncManager.localPlayerPosition;
            // lpp.inVehicle = true;
            // lpp.transform.localPosition = transform.localPosition;
            // lpp.transform.localRotation = transform.localRotation;
            if (!refPoint.isRoot && isChangeRootRefPoint)
                syncManager.changeRootRefPoint(refPoint);
        }

        public void FDMiOnSeatExit()
        {
            // FDMiPlayerPosition lpp = syncManager.localPlayerPosition;
            // lpp.inVehicle = false;
            if (refPoint.isRoot && isChangeRootRefPoint)
                syncManager.changeRootRefPoint(syncManager);
            // Why-not: 降車時の傾き直しは root 切替とは切り離す。座席 exit そのものを起点に 1f 遅延で立て直す。
            FDMiPlayerPosition lpp = syncManager.localPlayerPosition;
            if (lpp) lpp.SendCustomEventDelayedFrames("LevelUprightAfterSeatExit", 1);
        }
    }
}
