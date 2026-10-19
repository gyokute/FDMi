using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;

namespace FDMi.sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiPlayerPosition : FDMiReferencePoint
    {
        [UdonSynced] public int[] syncedPosition = new int[3];
        [UdonSynced] public short[] syncedChunkPosition = new short[3];
        [UdonSynced] public short[] syncedRotation = new short[4];
        private Quaternion syncedRot = Quaternion.identity;

        private Vector3 prevPostedPos = Vector3.zero;
        private Quaternion prevPostedRot = Quaternion.identity;

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

        public override void handleParentIndex(int value)
        {
            FDMiReferencePoint prevPRP = parentRefPoint;
            base.handleParentIndex(value);
            if (value >= 0) transform.SetParent(parentRefPoint.transform);
            else transform.SetParent(null);

            if (!gameObject.activeSelf || !isMine)
            {
                SendCustomEventDelayedFrames("windupPositionAndRotation", 2);
                return;
            }
            if (value < 0) return;
            Vector3 prevTotalM = prevPRP.getTotalPositionM();
            Vector3 newTotalM = parentRefPoint.getTotalPositionM();
            Vector3 teleportPos = prevPRP._rotation * _position + prevTotalM - newTotalM;
            teleportPos = Quaternion.Inverse(parentRefPoint._rotation) * teleportPos;
            Vector3 ownCoarseM = MmToM(_posMm) + _chunkPosition * M_PER_CHUNK;
            Vector3 teleportCoarseM = Quaternion.Inverse(parentRefPoint._rotation)
                * (prevPRP._rotation * ownCoarseM + (prevTotalM - prevPRP._position) - (newTotalM - parentRefPoint._position));
            Quaternion teleportRot = Quaternion.Inverse(parentRefPoint._rotation) * prevPRP._rotation * _rotation;
            Vector3 playerVelocity = prevPRP._velocity + Quaternion.Inverse(parentRefPoint._rotation) * prevPRP._rotation * localPlayer.GetVelocity();
            playerVelocity += prevPRP._velocity - parentRefPoint._velocity;
            localPlayer.TeleportTo(teleportPos, teleportRot, VRC_SceneDescriptor.SpawnOrientation.Default, false);
            localPlayer.SetVelocity(playerVelocity);
            decomposeTotalM(teleportPos + teleportCoarseM);

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
            _chunkPosition = Vector3.zero;
            _posMm[0] = 0;
            _posMm[1] = 0;
            _posMm[2] = 0;
            _position = syncManager.rootRefPoint.respawnPos;
            _rotation = syncManager.rootRefPoint.respawnRot;
            syncManager.onChangeLocalPlayerChunkPosition();
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
            Vector3 pChunk = _chunkPosition;
            int p0 = _posMm[0], p1 = _posMm[1], p2 = _posMm[2];
            if (base.setPosition(pos))
            {
                Vector3 dM = (_chunkPosition - pChunk) * M_PER_CHUNK
                    + new Vector3(_posMm[0] - p0, _posMm[1] - p1, _posMm[2] - p2) * 0.001f;
                localPlayer.TeleportTo(pos - dM, localPlayer.GetRotation());
                syncManager.onChangeLocalPlayerChunkPosition();
                return true;
            }
            return false;
        }
        public override Vector3 getViewPosition()
        {
            if (isRoot) return Vector3.zero;
            if (!Networking.IsOwner(gameObject))
            {
                for (int i = 0; i < 3; i++)
                {
                    _chunkPosition[i] = syncedChunkPosition[i];
                    _posMm[i] = syncedPosition[i];
                }
                _position = Vector3.zero;
            }
            return getTotalPositionM();
        }

        public override Quaternion getViewRotation()
        {
            if (!Networking.IsOwner(gameObject)) syncedRot = UnpackQuaternion(syncedRotation);
            else syncedRot = _rotation;
            _rotation = syncedRot;
            return _rotation;
            // if (isRoot) return Quaternion.identity;
            // return Quaternion.Inverse(rootRefPoint.direction) * direction;
        }
        public override Vector3 getViewPositionInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewPosition();
            bool chunkChanged = false;
            for (int i = 0; i < 3; i++)
            {
                if (syncedChunkPosition[i] != _chunkPosition[i]) chunkChanged = true;
            }
            // Chunk 跨ぎはスナップ (旧 _kmPosition != syncedKmPos の等価)。
            if (chunkChanged) return getViewPosition();
            _position = Vector3.Lerp(_position, MmToM(syncedPosition) - MmToM(_posMm), Time.deltaTime * 10);
            return getTotalPositionM();
        }
        public override Quaternion getViewRotationInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewRotation();
            syncedRot = UnpackQuaternion(syncedRotation);
            _rotation = Quaternion.Slerp(_rotation, syncedRot, Time.deltaTime * 10);
            return _rotation;
        }

        public override void windupPositionAndRotation()
        {
            prevPos = getViewPosition();
            prevRot = getViewRotation();
            transform.localPosition = getViewPosition();
            transform.localRotation = getViewRotation();
        }
        #endregion

        public void LateUpdate()
        {
            // if (!isInit || !isPlayerJoined || localPlayer == null) return;
            if (isMine)
            {
                if (!IsPlayerInStation()) useSeat();
                bool coarseChanged = setPosition(localPlayer.GetPosition());
                _rotation = localPlayer.GetRotation();
                // for preventing very-far jumping
                // if (localPlayer.GetPosition().magnitude > 100000f) RespawnLocalPlayer();
                if (coarseChanged || Vector3.Distance(prevPostedPos, _position) > 0.01f || Quaternion.Angle(prevPostedRot, _rotation) > 0.75f)
                {
                    TrySerialize();
                    prevPostedPos = _position;
                    prevPostedRot = _rotation;
                }
                return;
            }
            transform.localPosition = getViewPositionInterpolated();
            transform.localRotation = getViewRotationInterpolated();
        }

        protected override void TrySerialize()
        {
            // Try Serialize.
            if (Time.time > nextUpdateTime && !Networking.IsClogged)
            {
                for (int i = 0; i < 3; i++)
                {
                    syncedPosition[i] = _posMm[i] + (int)Mathf.Round(_position[i] * 1000f);
                    syncedChunkPosition[i] = (short)_chunkPosition[i];
                }
                syncedRotation = PackQuaternion(_rotation);
                RequestSerialization();
                nextUpdateTime = Time.time + updateInterval;
            }
        }
    }
}
