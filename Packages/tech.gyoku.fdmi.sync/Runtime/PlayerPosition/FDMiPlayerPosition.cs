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

        public void useSeat()
        {
            if (!Utilities.IsValid(localPlayer)) return;
            station.UseStation(localPlayer);
        }

        public override void handleParentIndex(int value)
        {
            FDMiReferencePoint prev = parentRefPoint;
            base.handleParentIndex(value);
            if (value >= 0 && parentRefPoint) transform.SetParent(parentRefPoint.transform);
            else if (value < 0) transform.SetParent(null);
            if (!gameObject.activeSelf || !isMine)
            {
                SendCustomEventDelayedFrames("windupPositionAndRotation", 2);
                return;
            }
            if (value < 0) return;
            if (!prev) return;
            if (prev.index == value) return;
            FDMiReferencePoint nr = syncManager.localRootRefPoint;
            // Why-not: 位置 (A_P) は絶対のため親替えで書き換えない。姿勢だけは新座標系の Y に合わせて立て直す。
            Vector3 T = viewForRoot(nr);
            Quaternion TR = (Quaternion.Inverse(nr._rotation) * _rotation).normalized;
            // Why-not: 原点が変わると TR に旧座標系の傾きが残り、ワールドに戻っても機体の傾きを引きずる。
            // 上下 (X/Y 回転) は新座標系の Y に合わせ、向き (Z) は切替前を保つ。
            Vector3 fwd = TR * Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.000001f)
            {
                TR = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                _rotation = (nr._rotation * TR).normalized;
            }
            // Why-not: TeleportTo が速度を消すため SetVelocity 用の現在速度は Teleport の前に取る。Get/SetVelocity は共にワールドなので回さない。
            Vector3 v = localPlayer.GetVelocity();
            localPlayer.TeleportTo(T, TR, VRC_SceneDescriptor.SpawnOrientation.Default, false);
            localPlayer.SetVelocity(v);
            TrySerialize();
        }

        #region station events
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.GetOwner(gameObject) != player) return;
            if (Networking.GetOwner(gameObject).isLocal)
            {
                localPlayer = player;
                isMine = true;
                station.PlayerMobility = VRCStation.Mobility.Mobile;
                syncManager.localPlayerPosition = this;
                syncManager.changeRootRefPoint(syncManager);
                ResetSyncTime();
                initReferencePoint();
                SendCustomEventDelayedFrames(nameof(RespawnLocalPlayer), 1);
                // Why-not: 初回着席はこの一回限り。LateUpdate で毎フレーム再着席すると搭乗中の座席を奪い返すうえ、
                // PlayMode では exit/enter が毎フレーム繰り返されるためポーリングは持たない (v2.1.2 も一回限りだった)。
                SendCustomEventDelayedFrames(nameof(useSeat), 1);
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
            ParentIndex = syncManager.localRootRefPoint.index;
            FDMiReferencePoint R = syncManager.localRootRefPoint;
            _chunkPosition = R._chunkPosition;
            _posMm[0] = R._posMm[0];
            _posMm[1] = R._posMm[1];
            _posMm[2] = R._posMm[2];
            // Why-not: setPosition は上書きのため R._position を引数に含める。respawnPos は root-local のため C は足さない。
            setPosition(R._position + R._rotation * R.respawnPos);
            _rotation = (R._rotation * R.respawnRot).normalized;
            syncManager.localCell = coarseCellFor(this, R);
            localPlayer.TeleportTo(getViewPosition(), Quaternion.Inverse(R._rotation) * _rotation);
            syncManager.onChangeLocalPlayerChunkPosition();
            RequestSerialization();
        }

        // Why-not: OnStationEntered/Exited (自 GO の anchor 専用) で tracker の SetActive を切らない。
        // boarding 時の exit で追跡が止まるうえ、無効化した behaviour の delayed 自復帰に依存する。座席と追跡は無関係 (isMine で追う)。
        // Why-not: 降車の傾き直しは座席 exit 側 (FDMiRelativeSyncStation.FDMiOnSeatExit) が起点。root 切替とは切り離す。
        public void LevelUprightAfterSeatExit()
        {
            if (!isMine || !Utilities.IsValid(localPlayer)) return;
            // Why-not: 位置 T と保存姿勢 _rotation には触らない。直すのはアバターのワールド姿勢だけであり、次 LateUpdate が再取得して追従する。
            Vector3 p = localPlayer.GetPosition();
            Vector3 v = localPlayer.GetVelocity();
            Vector3 fwd = localPlayer.GetRotation() * Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.000001f) return;
            Quaternion level = Quaternion.LookRotation(fwd.normalized, Vector3.up);
            localPlayer.TeleportTo(p, level, VRC_SceneDescriptor.SpawnOrientation.Default, false);
            // Why-not: TeleportTo が速度を消すため事前に取って戻す。Get/SetVelocity は共にワールドなので回さない。
            localPlayer.SetVelocity(v);
        }
        #endregion

        #region RelativePosition
        public override Vector3 getViewPosition()
        {
            if (!Networking.IsOwner(gameObject))
            {
                // Why-not: 受信時に粗部を必ず上書きするため補間中の stale は許容する。
                for (int i = 0; i < 3; i++)
                {
                    _chunkPosition[i] = syncedChunkPosition[i];
                    _posMm[i] = syncedPosition[i];
                }
                // Why-not: sub-mm fine は送らない (ms 量子化として許容する)。
                _position = Vector3.zero;
            }
            return base.getViewPosition();
        }

        public override Quaternion getViewRotation()
        {
            if (!Networking.IsOwner(gameObject))
            {
                syncedRot = UnpackQuaternion(syncedRotation);
                _rotation = syncedRot;
            }
            return base.getViewRotation();
        }
        public override Vector3 getViewPositionInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewPosition();
            bool chunkChanged = false;
            for (int i = 0; i < 3; i++)
            {
                if (syncedChunkPosition[i] != _chunkPosition[i]) chunkChanged = true;
            }
            if (chunkChanged) return getViewPosition();
            _position = Vector3.Lerp(_position, MmToM(syncedPosition) - MmToM(_posMm), Time.deltaTime * 10);
            return base.getViewPosition();
        }
        public override Quaternion getViewRotationInterpolated()
        {
            if (Networking.IsOwner(gameObject)) return getViewRotation();
            syncedRot = UnpackQuaternion(syncedRotation);
            _rotation = Quaternion.Slerp(_rotation, syncedRot, Time.deltaTime * 10);
            return base.getViewRotation();
        }
        #endregion

        public void LateUpdate()
        {
            if (!isMine)
            {
                transform.SetPositionAndRotation(getViewPositionInterpolated(), getViewRotationInterpolated());
                return;
            }
            // Why-not: OnPlayerJoined 前 (localPlayer 未確定) は追跡しない。初回着席は OnPlayerJoined の useSeat 一回限り。
            if (!Utilities.IsValid(localPlayer)) return;
            FDMiReferencePoint R = syncManager.localRootRefPoint;
            Quaternion qr = R._rotation;
            Vector3 C = syncManager.localCell;
            Vector3 u = localPlayer.GetPosition();
            _chunkPosition = R._chunkPosition;
            _posMm[0] = R._posMm[0];
            _posMm[1] = R._posMm[1];
            _posMm[2] = R._posMm[2];
            // Why-not: setPosition は上書きのため R._position を引数に含める。ゼロ化してから呼ぶと fine が落ちる。
            bool coarseChanged = setPosition(R._position + C + qr * u);
            _rotation = (qr * localPlayer.GetRotation()).normalized;
            _velocity = qr * localPlayer.GetVelocity();
            Vector3 Cnew = coarseCellFor(this, R);
            if (Cnew != C)
            {
                Vector3 dC = Cnew - C;
                // Why-not: windup は getViewPosition 経由で新 C を読むため localCell を先に確定する。
                syncManager.localCell = Cnew;
                localPlayer.TeleportTo(u - Quaternion.Inverse(qr) * dC, localPlayer.GetRotation());
                syncManager.onChangeLocalPlayerChunkPosition();
            }
            else
            {
                syncManager.localCell = Cnew;
            }
            if (coarseChanged || Vector3.Distance(prevPostedPos, _position) > 0.01f || Quaternion.Angle(prevPostedRot, _rotation) > 0.75f)
            {
                TrySerialize();
                prevPostedPos = _position;
                prevPostedRot = _rotation;
            }
        }

        protected override void TrySerialize()
        {
            if (Time.time > nextUpdateTime && !Networking.IsClogged)
            {
                for (int i = 0; i < 3; i++)
                {
                    int mm = _posMm[i] + (int)Mathf.Round(_position[i] * 1000f);
                    int ch = (int)_chunkPosition[i];
                    if (mm >= MM_PER_CHUNK) { mm -= MM_PER_CHUNK; ch += 1; }
                    else if (mm <= -MM_PER_CHUNK) { mm += MM_PER_CHUNK; ch -= 1; }
                    syncedPosition[i] = mm;
                    // Why-not: chunk を short に収める。±32767 chunk を超える遠方は送れない。
                    if (ch > 32767) ch = 32767;
                    else if (ch < -32767) ch = -32767;
                    syncedChunkPosition[i] = (short)ch;
                }
                syncedRotation = PackQuaternion(_rotation);
                RequestSerialization();
                nextUpdateTime = Time.time + updateInterval;
            }
        }
    }
}
