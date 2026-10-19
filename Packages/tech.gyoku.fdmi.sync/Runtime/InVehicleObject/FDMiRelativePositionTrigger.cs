using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.sync
{
    public class FDMiRelativePositionTrigger : FDMiBehaviour
    {
        FDMiRelativeObjectSyncManager syncManager;
        public FDMiReferencePoint refPoint;
        public FDMiBool InZone;

        [SerializeField]
        bool detectEnter = true,
            detectExit = true;
        bool enableOnExit = true,
            enableDetecting = false;

        void Start()
        {
            syncManager = refPoint.syncManager;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            base.OnPlayerJoined(player);
            if (!player.isLocal)
                return;
            SendCustomEventDelayedSeconds(nameof(EnableDetecting), 1);
        }

        public void EnableDetecting()
        {
            enableDetecting = true;
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!detectEnter || !enableDetecting)
                return;
            if (player.isLocal && enableOnExit && syncManager.isRoot)
            {
                syncManager.changeRootRefPoint(refPoint);
                enableOnExit = false;
                SendCustomEventDelayedSeconds(nameof(turnOnExit), 0.5f);
                InZone.Data = true;
            }
        }

        public void turnOnExit()
        {
            enableOnExit = true;
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!detectExit || !enableDetecting)
                return;
            if (player.isLocal && refPoint.isRoot && enableOnExit)
            {
                syncManager.changeRootRefPoint(syncManager);
                enableOnExit = false;
                SendCustomEventDelayedSeconds(nameof(turnOnExit), 0.5f);
                InZone.Data = false;
            }
        }

        public void TeleportLocalPlayer(Transform teleportPosition)
        {
            // Why-not: 旧実装はワールド座標を _position (root フレームの fine) に直接代入し、
            // refPoint の粗部をコピーしていた。再アンカーは changeRootRefPoint に任せ、
            // アバターだけをテレポートする。
            syncManager.changeRootRefPoint(refPoint);
            InZone.Data = true;
            Networking.LocalPlayer.TeleportTo(teleportPosition.position, teleportPosition.rotation);
        }

        public void ExitLocalPlayer()
        {
            OnPlayerTriggerExit(Networking.LocalPlayer);
        }
    }
}
