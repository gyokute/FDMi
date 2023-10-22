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
                        SendCustomEventDelayedFrames(nameof(useSeat), 1);
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
        private bool isPlayerJoined = false;
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
            if (tgtPlayer == null)
            {
                gameObject.SetActive(false);
                return;
            }
            if (tgtPlayer.isLocal)
            {
                isMine = true;
                syncManager.localPlayerPosition = this;
                SendCustomEventDelayedFrames(nameof(useSeat), 1);
                syncManager.changeRootRefPoint(syncManager);
                transform.position = syncManager.respawnPoint;
                localPlayer.TeleportTo(syncManager.respawnPoint, Quaternion.identity);
                station.PlayerMobility = VRCStation.Mobility.Mobile;
                RequestSerialization();
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
            if (inVehicle) return;
            station.UseStation(Player);
#if !UNITY_EDITOR
            SendCustomEventDelayedSeconds(nameof(useSeat), 10f);
#endif
        }

        public override void handleParentIndex(int value)
        {
            FDMiReferencePoint prevPRP = parentRefPoint;
            base.handleParentIndex(value);
            if (!gameObject.activeSelf || !isMine) return;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (value < 0) return;
            Vector3 teleportPos = prevPRP.direction * Position + prevPRP.Position - parentRefPoint.Position;
            teleportPos += (prevPRP.direction * kmPosition + prevPRP.kmPosition - parentRefPoint.kmPosition) * 1000f;
            teleportPos = Quaternion.Inverse(parentRefPoint.direction) * teleportPos;
            Quaternion teleportRot = Quaternion.Inverse(parentRefPoint.direction) * prevPRP.direction * direction;
            localPlayer.TeleportTo(teleportPos, teleportRot, VRC_SceneDescriptor.SpawnOrientation.Default, false);
            Position = teleportPos;
            direction = teleportRot;
            kmPosition = Vector3.zero;
        }

        #region station events
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            // if (Player != null) if (Player.isLocal) useSeat();
            if (!player.isLocal) return;
            transform.localPosition = getViewPosition();
            transform.localRotation = getViewRotation();
            isPlayerJoined = true;
            SendCustomEventDelayedSeconds(nameof(RespawnLocalPlayer), 1f);
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Networking.IsMaster) return;
            if (Player == null) return;
            if (Player.playerId == player.playerId)
            {
                gameObject.SetActive(false);
                ParentIndex = syncManager.index;
                kmPosition = Vector3.zero;
                transform.position = syncManager.respawnPoint;
                Position = syncManager.respawnPoint;
            }
        }
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (isMine) RespawnLocalPlayer();
        }
        void RespawnLocalPlayer()
        {
            ParentIndex = syncManager.rootRefPoint.index;
            localPlayer.TeleportTo(syncManager.rootRefPoint.respawnPoint, Quaternion.identity);
            inVehicle = false;
            kmPosition = Vector3.zero;
            Position = syncManager.rootRefPoint.respawnPoint;
            RequestSerialization();
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (Player.isLocal) gameObject.SetActive(true);
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!Player.isLocal) return;
            if (!inVehicle)
            {
                SendCustomEventDelayedFrames(nameof(useSeat), 1);
                return;
            }
            if (Player.isLocal) gameObject.SetActive(false);
        }
        #endregion

        #region RelativePosition
        // public override bool setPosition(Vector3 pos)
        // {
        //     Position = pos;
        //     return false;
        // }
        public override Vector3 getViewPosition()
        {
            if (isRoot) return Vector3.zero;
            return 1000f * kmPosition + Position;
        }

        public override Quaternion getViewRotation()
        {
            return direction;
            // if (isRoot) return Quaternion.identity;
            // return Quaternion.Inverse(rootRefPoint.direction) * direction;
        }
        public override Vector3 getViewPositionInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewPosition();
            prevPos = Vector3.Lerp(prevPos, getViewPosition(), Time.deltaTime * 10);
            return prevPos;
        }
        public override Quaternion getViewRotationInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewRotation();
            prevRot = Quaternion.Slerp(prevRot, getViewRotation(), Time.deltaTime * 10);
            return prevRot;
        }

        public override void windupPositionAndRotation()
        {
            prevPos = getViewPosition();
            prevRot = Quaternion.identity;
        }
        #endregion
        public void Update()
        {
            if (!isInit || !isPlayerJoined) return;
            if (Networking.IsOwner(gameObject))
            {
                setPosition(localPlayer.GetPosition());
                direction = localPlayer.GetRotation();
                RequestSerialization();
                return;
            }
            if (!inVehicle)
            {
                transform.localPosition = getViewPositionInterpolated();
                transform.localRotation = getViewRotationInterpolated();
            }
        }
    }
}