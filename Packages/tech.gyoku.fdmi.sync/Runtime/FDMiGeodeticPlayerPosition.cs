using FDMi.core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    /// <summary>
    /// プレイヤー1人につき1つの参照点。非着席時はローカルアバターを追い、
    /// チャンク通過時に TeleportTo で Unity 座標を巻き戻して世界を再錨定する。
    /// 着席中(inVehicle)は車両が座標系の主役になるため本 behaviour は休止する。
    /// inVehicle を書くのは FDMiSeatAnchor だけ(状態の二重管理を避ける)。
    /// 論理位置は transform 階層に依存しないので、親の付け替えで位置を持ち替える必要はない。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiGeodeticPlayerPosition : FDMiGeodeticReferencePoint
    {
        public VRCStation station;
        [UdonSynced, FieldChangeCallback(nameof(inVehicle)), HideInInspector] public bool _inVehicle = false;
        public bool inVehicle
        {
            set
            {
                _inVehicle = value;
                if (Networking.IsOwner(gameObject))
                    gameObject.SetActive(!value);
            }
            get => _inVehicle;
        }
        public VRCPlayerApi localPlayer;
        public VRCPlayerApi Player;
        [SerializeField] private bool isMine;
        // 初回デシリアライズ前は受信値が既定値(0)なので、transform を触らない(原点への一瞬の飛びを防ぐ)
        bool received;

        public override void OnDeserialization()
        {
            received = true;
        }

        void Start()
        {
            gameObject.SetActive(false);
        }

        public void attachPlayer(VRCPlayerApi tgtPlayer)
        {
            Player = tgtPlayer;
            localPlayer = Networking.LocalPlayer;
            if (tgtPlayer == null)
            {
                gameObject.SetActive(false);
                return;
            }
            if (tgtPlayer.isLocal)
            {
                isMine = true;
                syncManager.localPlayerPosition = this;
                syncManager.changeRootRefPoint(syncManager);
                RespawnLocalPlayer();
                SendCustomEventDelayedFrames(nameof(useSeat), 1);
                if (station)
                    station.PlayerMobility = VRCStation.Mobility.Mobile;
                ResetSyncTime();
                TrySerialize();
            }
            else
            {
                isMine = false;
                if (station)
                    station.PlayerMobility = VRCStation.Mobility.ImmobilizeForVehicle;
            }
            gameObject.SetActive(!_inVehicle);
        }

        public void useSeat()
        {
            if (inVehicle || !station) return;
            station.UseStation(Player);
#if !UNITY_EDITOR
            SendCustomEventDelayedSeconds(nameof(useSeat), 10f);
#endif
        }

        #region station events
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Networking.IsMaster) return;
            if (Player == null) return;
            if (Player.playerId == player.playerId)
            {
                gameObject.SetActive(false);
                ParentIndex = syncManager.index;
                _chunk = Vector3Int.zero;
                setPosition(syncManager.respawnPoint);
                transform.position = syncManager.ToUnity(_position);
            }
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (isMine) RespawnLocalPlayer();
        }

        /// <summary>論理位置をリスポーン点(既定はワールド ECEF 原点)へ戻し、ローカルプレイヤーを置き直す。</summary>
        public void RespawnLocalPlayer()
        {
            ParentIndex = syncManager.rootRefPoint.index;
            _chunk = Vector3Int.zero;
            setPosition(syncManager.rootRefPoint.respawnPoint);
            _rotation = Quaternion.identity;
            localPlayer.TeleportTo(syncManager.ToUnity(_position), Quaternion.identity);
            syncManager.onChangeLocalPlayerKMPosition();
            RequestSerialization();
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            // 自分のステーションは「参照点に座らせておくための席」。車両の席(SeatAnchor)とは別物。
            if (!player.isLocal || _inVehicle) return;
            gameObject.SetActive(true);
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!player.isLocal || _inVehicle) return;
            SendCustomEventDelayedFrames(nameof(useSeat), 1);
        }

        /// <summary>
        /// 座標系のフレームが切り替わった瞬間(root 切替)の処理。
        /// アバターの Unity 座標は VRChat 管理なので動かさず、論理位置を「切替前のフレームでの
        /// 解釈」で書き直す。そのうえで、見えているもの全部がずれる分だけアバターを手で動かす
        /// (フレームの原点と姿勢が変わるため、ずれは位置だけでは表せない)。
        /// 着席中は VRChat が座席へ置き直すので動かさない。
        /// </summary>
        public void rebaseFrame(Vector3Int oldOriginChunk, Vector3 oldOriginFine, Quaternion oldFrameRot, Vector3 frameShift)
        {
            if (!isMine || localPlayer == null) return;
            Vector3 avatar = localPlayer.GetPosition();
            setBaseChunk(oldOriginChunk);
            base.setPosition(oldOriginFine + oldFrameRot * avatar);
            _rotation = oldFrameRot * localPlayer.GetRotation();
            if (!inVehicle && frameShift != Vector3.zero)
                localPlayer.TeleportTo(avatar + frameShift, localPlayer.GetRotation());
        }
        #endregion

        #region RelativePosition
        public override bool setPosition(Vector3 pos)
        {
            Vector3Int prevChunk = _chunk;
            if (base.setPosition(pos))
            {
                // pos は ECEF セル内成分。巻き戻し先は現在の Unity 座標から ENU 上のチャンク差分を引く
                localPlayer.TeleportTo(localPlayer.GetPosition() - syncManager.ToUnity(syncManager.MulChunk(_chunk - prevChunk)), localPlayer.GetRotation());
                syncManager.onChangeLocalPlayerKMPosition();
                return true;
            }
            return false;
        }
        #endregion

        public void LateUpdate()
        {
            if (!isInit || localPlayer == null) return;
            if (Networking.IsOwner(gameObject))
            {
                setPosition(syncManager.ToEcef(localPlayer.GetPosition()));
                _rotation = syncManager.ToEcefRot(localPlayer.GetRotation());
                // for preventing very-far jumping
                if (localPlayer.GetPosition().magnitude > 100000f) RespawnLocalPlayer();
                TrySerialize();
                return;
            }
            if (inVehicle || !received) return;
            if (_chunk != syncedChunk)
            {
                // チャンクを跨いだら補間せずスナップ(別セル間の Lerp は無意味)
                _chunk = syncedChunk;
                _position = syncedFineM;
            }
            else
            {
                _position = Vector3.Lerp(_position, syncedFineM, Time.deltaTime * 10);
            }
            _rotation = Quaternion.Slerp(_rotation, syncedRot, Time.deltaTime * 10);
            transform.position = syncManager.ToUnity(getViewPosition());
            transform.rotation = syncManager.ToUnityRot(getViewRotation());
        }
    }
}
