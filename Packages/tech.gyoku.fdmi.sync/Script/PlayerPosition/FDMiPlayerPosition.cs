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
                ResetSyncTime();
                TrySerialize();
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
            Vector3 teleportPos = prevPRP._rotation * _position + prevPRP._position - parentRefPoint._position;
            Vector3 teleportKmPos = (prevPRP._rotation * _kmPosition + prevPRP._kmPosition - parentRefPoint._kmPosition);
            // teleportPos += (prevPRP._rotation * _kmPosition + prevPRP._kmPosition - parentRefPoint._kmPosition) * 1000f;
            teleportPos = Quaternion.Inverse(parentRefPoint._rotation) * teleportPos;
            Quaternion teleportRot = Quaternion.Inverse(parentRefPoint._rotation) * prevPRP._rotation * _rotation;
            Vector3 playerVelocity = Quaternion.Inverse(parentRefPoint._rotation) * prevPRP._rotation * localPlayer.GetVelocity();
            playerVelocity += prevPRP._velocity - parentRefPoint._velocity;
            localPlayer.TeleportTo(teleportPos, teleportRot, VRC_SceneDescriptor.SpawnOrientation.Default, false);
            localPlayer.SetVelocity(playerVelocity);
            _position = teleportPos;
            _rotation = teleportRot;
            _kmPosition = teleportKmPos;
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
                _kmPosition = Vector3.zero;
                transform.position = syncManager.respawnPoint;
                _position = syncManager.respawnPoint;
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
            _kmPosition = Vector3.zero;
            _position = syncManager.rootRefPoint.respawnPoint;
            syncManager.onChangeLocalPlayerKMPosition();
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
        public override bool setPosition(Vector3 pos)
        {
            Vector3 pKmPos = _kmPosition;
            if (base.setPosition(pos))
            {
                localPlayer.TeleportTo(pos - 1000f * (_kmPosition - pKmPos), localPlayer.GetRotation());
                syncManager.onChangeLocalPlayerKMPosition();
                return true;
            }
            return false;
        }
        public override Vector3 getViewPosition()
        {
            if (isRoot) return Vector3.zero;
            if (!Networking.IsOwner(gameObject))
            {
                _kmPosition = syncedKmPos;
                _position = syncedPos;
            }
            return 1000f * _kmPosition + _position;
            // return 1000f * (_kmPosition - syncManager.localPlayerPosition._kmPosition) + _position;
        }

        public override Quaternion getViewRotation()
        {
            if (!Networking.IsOwner(gameObject)) _rotation = syncedRot;
            return _rotation;
            // if (isRoot) return Quaternion.identity;
            // return Quaternion.Inverse(rootRefPoint.direction) * direction;
        }
        public override Vector3 getViewPositionInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewPosition();
            _position = Vector3.Lerp(_position, 1000f * syncedKmPos + syncedPos, Time.deltaTime * 10);
            return _position;
        }
        public override Quaternion getViewRotationInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewRotation();
            _rotation = Quaternion.Slerp(_rotation, syncedRot, Time.deltaTime * 10);
            return _rotation;
        }

        public override void windupPositionAndRotation()
        {
            prevPos = getViewPosition();
            prevRot = getViewRotation();
        }
        #endregion

        public void LateUpdate()
        {
            if (!isInit || !isPlayerJoined || localPlayer == null) return;
            if (Networking.IsOwner(gameObject))
            {
                setPosition(localPlayer.GetPosition());
                _rotation = localPlayer.GetRotation();
                // for preventing very-far jumping
                if (localPlayer.GetPosition().magnitude > 100000f) RespawnLocalPlayer();
                if (Vector3.Distance(prevPos, _position) > 0.01f || Quaternion.Angle(prevRot, _rotation) > 0.75f)
                    TrySerialize();
                prevPos = _position;
                prevRot = _rotation;
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