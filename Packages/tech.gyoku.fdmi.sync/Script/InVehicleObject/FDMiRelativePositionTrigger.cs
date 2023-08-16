
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativePositionTrigger : UdonSharpBehaviour
    {
        FDMiRelativeObjectSyncManager syncManager;
        public FDMiReferencePoint refPoint;
        bool enableOnExit = true;

        void Start()
        {
            syncManager = refPoint.syncManager;
        }
        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal && enableOnExit && !refPoint.isRoot)
            {
                syncManager.changeRootRefPoint(refPoint);
                enableOnExit = false;
                SendCustomEventDelayedSeconds(nameof(turnOnExit), 0.5f);
            }
        }

        public void turnOnExit()
        {
            enableOnExit = true;
        }
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal && refPoint.isRoot && enableOnExit)
            {
                syncManager.changeRootRefPoint(syncManager);
                enableOnExit = false;
                SendCustomEventDelayedSeconds(nameof(turnOnExit), 0.5f);
            }

        }

    }
}