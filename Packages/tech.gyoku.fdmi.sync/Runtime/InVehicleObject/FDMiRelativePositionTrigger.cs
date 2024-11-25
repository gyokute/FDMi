﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tech.gyoku.FDMi.core;

namespace tech.gyoku.FDMi.sync
{
    public class FDMiRelativePositionTrigger : FDMiAttribute
    {
        FDMiRelativeObjectSyncManager syncManager;
        public FDMiReferencePoint refPoint;
        public FDMiBool InZone;
        [SerializeField] bool detectEnter = true, detectExit = true;
        bool enableOnExit = true, enableDetecting = false;

        void Start()
        {
            syncManager = refPoint.syncManager;
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            base.OnPlayerJoined(player);
            if (!player.isLocal) return;
            SendCustomEventDelayedSeconds(nameof(EnableDetecting), 1);
        }

        public void EnableDetecting()
        {
            enableDetecting = true;
        }
        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!detectEnter || !enableDetecting) return;
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
            if (!detectExit || !enableDetecting) return;
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
            syncManager.localPlayerPosition._position = teleportPosition.position;
            syncManager.localPlayerPosition._kmPosition = refPoint._kmPosition;
            syncManager.changeRootRefPoint(refPoint);
            InZone.Data = true;
            localPlayer.TeleportTo(teleportPosition.position, teleportPosition.rotation);
        }
        public void ExitLocalPlayer()
        {
            OnPlayerTriggerExit(localPlayer);
        }
    }
}