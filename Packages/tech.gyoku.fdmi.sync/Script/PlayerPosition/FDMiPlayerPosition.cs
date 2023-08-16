﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;

namespace tech.gyoku.FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiPlayerPosition : FDMiReferencePoint
    {
        public VRCStation station;
        [UdonSynced, FieldChangeCallback(nameof(inVehicle))] public bool _inVehicle = false;
        public bool inVehicle
        {
            set
            {
                _inVehicle = value;
                if (Networking.IsOwner(gameObject))
                {
                    if (!value)
                    {
                        gameObject.SetActive(true);
                        station.UseStation(Player);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                }
            }
            get => _inVehicle;
        }
        public VRCPlayerApi localPlayer;
        public VRCPlayerApi Player;
        [SerializeField] private bool isMine;
        private bool isUserInVR;
        void Start()
        {

            gameObject.SetActive(false);
        }

        public void attachPlayer(VRCPlayerApi tgtPlayer)
        {
            Player = tgtPlayer;
            localPlayer = Networking.LocalPlayer;
            isUserInVR = localPlayer.IsUserInVR();
            if (tgtPlayer.isLocal)
            {
                isMine = true;
                syncManager.localPlayerPosition = this;
                SendCustomEventDelayedFrames(nameof(useSeat), 1);
                kmPosition = Vector3.zero;
                transform.position = syncManager.respawnPoint;
                Position = syncManager.respawnPoint;
                station.PlayerMobility = VRCStation.Mobility.Mobile;

            }
            else
            {
                isMine = false;
                station.PlayerMobility = VRCStation.Mobility.ImmobilizeForVehicle;
            }
            gameObject.SetActive(true);
        }

        public void useSeat()
        {
            if (!inVehicle) station.UseStation(Player);
        }

        public override void handleParentIndex(int value)
        {
            FDMiReferencePoint prevPRP = parentRefPoint;
            base.handleParentIndex(value);
            if (!gameObject.activeSelf || !isMine) return;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (value < 0) return;
            Vector3 teleportPos = prevPRP.direction * (Position + 1000f * kmPosition);
            teleportPos += prevPRP.Position - parentRefPoint.Position;
            teleportPos = Quaternion.Inverse(parentRefPoint.direction) * teleportPos;
            Quaternion teleportRot = Quaternion.Inverse(parentRefPoint.direction) * prevPRP.direction * direction;
            localPlayer.TeleportTo(teleportPos, teleportRot, VRC_SceneDescriptor.SpawnOrientation.Default, false);
        }

        #region station events
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            transform.localPosition = getViewPosition();
            transform.localRotation = getViewRotation();
        }
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (isMine)
            {
                kmPosition = Vector3.zero;
                Position = syncManager.respawnPoint;
                useSeat();
            }
        }
        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (Player.playerId == player.playerId) gameObject.SetActive(true);
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!inVehicle)
            {
                SendCustomEventDelayedFrames(nameof(useSeat), 1);
                return;
            }
            if (Player.playerId == player.playerId) gameObject.SetActive(false);
        }
        #endregion

        public override Vector3 getViewPosition()
        {
            if (isRoot) return Vector3.zero;
            Vector3 kmDiff = kmPosition;
            Vector3 diff = Position;
            Quaternion dir = Quaternion.identity;
            diff += 1000f * kmDiff;
            return dir * diff;
        }

        public override Quaternion getViewRotation()
        {
            if (isRoot) return Quaternion.identity;
            return direction;
        }

        public void Update()
        {
            if (isMine)
            {
                setPosition(localPlayer.GetPosition());
                direction = localPlayer.GetRotation();
                RequestSerialization();
                return;
            }

            transform.localPosition = getViewPosition();
            transform.localRotation = getViewRotation();
        }
    }
}