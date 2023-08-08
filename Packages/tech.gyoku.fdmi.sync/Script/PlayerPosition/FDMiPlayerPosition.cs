using UdonSharp;
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
            gameObject.SetActive(true);
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
        }

        public void useSeat()
        {
            if (!inVehicle) station.UseStation(Player);
        }

        public override void handleParentIndex(int value)
        {
            base.handleParentIndex(value);
            if (!gameObject.activeSelf || !isMine) return;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (value < 0) return;
            parentRefPoint = syncManager.refPoints[value];
            Vector3 teleportPos = direction * Quaternion.Inverse(parentRefPoint.direction) * Position - parentRefPoint.transform.position;
            localPlayer.TeleportTo(teleportPos, getViewRotation(), VRC_SceneDescriptor.SpawnOrientation.Default, false);
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

        Vector3 moveVec = Vector3.zero;
        public void Update()
        {
            if (isMine)
            {
                setPosition(localPlayer.GetPosition());
                direction = localPlayer.GetRotation();
                RequestSerialization();
            }

            if (Player == null) { Player = Networking.GetOwner(gameObject); }

            if (!isMine)
            {
                transform.position = getViewPositionInterpolated();
                transform.rotation = getViewRotationInterpolated();
            }
        }
        #endregion
    }
}