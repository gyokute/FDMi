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
        public VRCPlayerApi localPlayer;
        [SerializeField] public bool isMine;
        private bool isPlayerJoined = false;
        private bool isUserInVR;

        public void useSeat()
        {
            if (!Utilities.IsValid(localPlayer)) return;
            station.UseStation(localPlayer);
        }

        public override void handleParentIndex(short value)
        {
            FDMiReferencePoint prevPRP = parentRefPoint;
            base.handleParentIndex(value);
            if (!gameObject.activeSelf || !isMine)
            {
                SendCustomEventDelayedFrames("windupPositionAndRotation", 2);
                return;
            }
            if (value < 0) return;
            Vector3 teleportPos = prevPRP._rotation * _position + prevPRP._position - parentRefPoint._position;
            Vector3 teleportKmPos = (prevPRP._rotation * _kmPosition + prevPRP._kmPosition - parentRefPoint._kmPosition);
            // teleportPos += (prevPRP._rotation * _kmPosition + prevPRP._kmPosition - parentRefPoint._kmPosition) * 1000f;
            teleportPos = Quaternion.Inverse(parentRefPoint._rotation) * teleportPos;
            Quaternion teleportRot = Quaternion.Inverse(parentRefPoint._rotation) * prevPRP._rotation * _rotation;
            Vector3 playerVelocity = prevPRP._velocity + Quaternion.Inverse(parentRefPoint._rotation) * prevPRP._rotation * localPlayer.GetVelocity();
            playerVelocity += prevPRP._velocity - parentRefPoint._velocity;
            localPlayer.TeleportTo(teleportPos, teleportRot, VRC_SceneDescriptor.SpawnOrientation.Default, false);
            localPlayer.SetVelocity(playerVelocity);
            _position = teleportPos;
            _rotation = teleportRot;
            _kmPosition = teleportKmPos;

            TrySerialize();
        }

        #region station events
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.GetOwner(gameObject) != player) return;
            if (Networking.GetOwner(gameObject).isLocal)
            {
                localPlayer = player;
                isPlayerJoined = true;
                isMine = true;
                station.PlayerMobility = VRCStation.Mobility.Mobile;
                syncManager.localPlayerPosition = this;
                syncManager.changeRootRefPoint(syncManager);
                ResetSyncTime();
                initReferencePoint();
                SendCustomEventDelayedFrames(nameof(RespawnLocalPlayer), 1);
            }
            else
            {
                station.PlayerMobility = VRCStation.Mobility.ImmobilizeForVehicle;
            }

        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (isMine) RespawnLocalPlayer();
        }
        public void RespawnLocalPlayer()
        {
            ParentIndex = syncManager.rootRefPoint.index;
            localPlayer.TeleportTo(syncManager.rootRefPoint.respawnPos, syncManager.rootRefPoint.respawnRot);
            _kmPosition = Vector3.zero;
            _position = syncManager.rootRefPoint.respawnPos;
            _rotation = syncManager.rootRefPoint.respawnRot;
            syncManager.onChangeLocalPlayerKMPosition();
            RequestSerialization();
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            Debug.Log("StationEntered>" + player.playerId);
            if (isMine) gameObject.SetActive(true);
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            Debug.Log("StationExit>" + player.playerId);
            if (isMine)
            {
                gameObject.SetActive(false);
                SendCustomEventDelayedFrames(nameof(SetActive), 5);
            }
        }
        public void SetActive()
        {
            gameObject.SetActive(true);
        }

        private Collider[] localOverlaps = new Collider[32];
        public LayerMask playerLocalLayer;
        public bool IsPlayerInStation()
        {
            VRCPlayerApi.TrackingData avatarRoot = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot);
            int overlapCount = Physics.OverlapSphereNonAlloc(avatarRoot.position, 1f, localOverlaps, playerLocalLayer);
            for (int i = 0; i < overlapCount; i++)
            {
                if (!Utilities.IsValid(localOverlaps[i])) return false;
            }
            return true;
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
        public Vector3 getViewPositionInterpolated()
        {
            if (Networking.IsOwner(gameObject) || _kmPosition != syncedKmPos) return getViewPosition();
            // _kmPosition = syncedKmPos;
            _position = Vector3.Lerp(_position, syncedPos, Time.deltaTime * 10);
            // _position = Vector3.Lerp(_position, 1000f * syncedKmPos + syncedPos, Time.deltaTime * 10);
            return 1000f * _kmPosition + _position;
        }
        public Quaternion getViewRotationInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewRotation();
            _rotation = Quaternion.Slerp(_rotation, syncedRot, Time.deltaTime * 10);
            return _rotation;
        }

        #endregion

        public void LateUpdate()
        {
            // if (!isInit || !isPlayerJoined || localPlayer == null) return;
            if (isMine)
            {
                if (!IsPlayerInStation()) useSeat();
                setPosition(localPlayer.GetPosition());
                _rotation = localPlayer.GetRotation();
                // for preventing very-far jumping
                // if (localPlayer.GetPosition().magnitude > 100000f) RespawnLocalPlayer();
                if (Vector3.Distance(syncedPos, _position) > 0.01f || Quaternion.Angle(syncedRot, _rotation) > 0.75f)
                    TrySerialize();
                return;
            }
            transform.localPosition = getViewPositionInterpolated();
            transform.localRotation = getViewRotationInterpolated();
        }
    }
}