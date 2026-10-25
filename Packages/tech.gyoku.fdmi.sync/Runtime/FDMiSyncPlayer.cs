using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    // §9.3。プレイヤーの絶対姿勢。owner は「そのプレイヤー本人」だけ (§6.2)。
    // 同一性は VRCPlayerObject の所有権で確定する。Start の 1 回だけ代入し、再代入の経路を持たない (§9.4)。
    // Why-not: 基底の [UdonBehaviourSyncMode] は継承されないため、派生クラスにも明示する (§5.1)。
    // ExecOrder 1000: 動的アンカーと同じ段で積分する (§5.2)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(1000)]
    public class FDMiSyncPlayer : FDMiDynamicObjectSync
    {
        // VRCPlayerApi.playerId。Start が所有権から確定する。−1 は未確定。
        public int playerId = -1;
        public VRCPlayerApi player;

        // player.isLocal のキャッシュ。Start 以降変わらない。
        public bool isLocalPlayer;

        // stations[] の添字。-1 は自由移動 (§8.4)。着席の真実はこの 1 つだけで、seated フラグを持たない。
        [UdonSynced]
        public int _seatIndex = -1;

        // §9.3。プレイヤーの定位置に使う VRCStation。FDMiSyncPlayer と同じ GameObject に置く (①)。
        // Editor 焼き E-09 で配線する。
        // Why-not: Runtime で GetComponent しない。コピーは実行時に生成されるが、コピーは同一階層内の
        // 参照を Unity の Instantiate 意味論で張り替えるため、焼き値が各コピーの station に解決される (§9.4)。
        public VRCStation station;

        // §9.3。着席判定の物理プローブ。VRChat 予約レイヤ PlayerLocal (10) の自分のコライダを数える。
        // VRCStation に座ると 1 個減るため、站立時より少なければ着席中とみなす (②)。
        const int PLAYER_LOCAL_LAYER_MASK = 1 << 10;
        const int PROBE_CAPACITY = 8;
        const float PROBE_RADIUS_M = 5f;
        const float PROBE_INTERVAL_SEC = 0.2f;
        const float SEAT_RETRY_SEC = 1f;
        Collider[] _probeColliders = new Collider[PROBE_CAPACITY];

        // 直近のプローブで数えた PlayerLocal コライダ数。
        int _probeColliderCount;

        // 站立時のコライダ数。観測した最大値を保持する。
        int _standingColliderCount;
        float _nextProbeTime;
        float _nextSeatTime;

        // 自分の station に座っているか。VRChat の着席イベントで更新する (①)。
        bool _inOwnStation;

        // 姿勢を 1 回でも書いたか。station が自分の足元に来る前に着席すると TEMPLATE の位置へ飛ぶ (①)。
        bool _poseWritten;

        // §9.8。respawn の絶対位置の受け皿。root の絶対姿勢 + 回転した局所 offset。
        long[] _tmpRespawnAbs = new long[3];

        // §9.3 の 6 段。テンプレートは VRChat が無効化するため Start を実行しない想定 (U-03)。
        public void Start()
        {
            // 自由移動中は常時 2 Hz。人数による周期変更を持たない (§10.1)。
            // Why-not: _sendIntervalMs / _renderDelayMs を直接書かない。ms 系は _InitLocal が
            // sendIntervalSec から導出する 1 経路だけを持つ (§9.2)。先に設定してから _Init を呼ぶ。
            sendIntervalSec = 0.5f;
            _Init(manager);

            // Why-not: Start では所有権が正しい (§9.4)。プレイヤーアンカーの所有者は本人に固定され移譲できない。
            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            player = owner;
            playerId = owner.playerId;
            isLocalPlayer = owner.isLocal;

            // Why-not: Manager に登録しない。実行時に生成されるコピーなので登録表は必要ない (§9.4)。
            // 原点が変わったことは manager._viewVersion で各アンカーが知る (§9.1)。
            if (isLocalPlayer)
            {
                manager._SetLocalPlayerAnchor(this);
                station.PlayerMobility = VRCStation.Mobility.Mobile;
                // §9.8。参加時は manager root の respawn point へ置く。1 秒待つのは、
                // Manager.Start の root 確定と VRChat の spawn 確定の後に Teleport するため (v2 と同じ)。
                SendCustomEventDelayedSeconds(nameof(_RespawnLocalPlayer), 1f);
            }
            else
            {
                station.PlayerMobility = VRCStation.Mobility.ImmobilizeForVehicle;
            }
        }

        public override void _Tick(int frameStamp, int nowMs)
        {
            // 席姿勢は車両アンカーから導出する。送信しない (§10.2)。
            // Why-not: null ガードと `_seatIndex = -1` の代入を置かない。stations[] と _seatIndex の整合は
            // V-06 が焼きの時点で保証する (§1.2 正常系)。ここで書くと非 owner が同期フィールドを書く経路になる。
            if (_seatIndex >= 0)
            {
                FDMiStationAnchor seat = manager.GetStation(_seatIndex);
                seat.GetSeatAbs(_localAbs);
                _localRot = FDMiSyncMath.QNormalize(seat.vehicleAnchor.GetAbsoluteRotation() * seat._bakedSeatRelRot);
                _UpdateStationMode();
                return;
            }

            if (!isLocalPlayer)
            {
                _IntegrateReceive(nowMs);
                _ComputeViewPose(nowMs);
                _UpdateStationMode();
                return;
            }

            // §9.3。どの VRCStation にも座っていないときだけ、自分の station に座らせる (①・②)。
            _KeepSeated();

            Quaternion originRot = manager.GetOriginRot();
            // §7.2 の自由移動行: originMm + Rotate(originRot, GetPosition() × 1000)。
            manager.AbsFromView(player.GetPosition(), _localAbs);
            _localRot = originRot * player.GetRotation();
            _localVelFef = originRot * player.GetVelocity();
            // 送信は _accMm を読む。
            _accMm[0] = _localAbs[0];
            _accMm[1] = _localAbs[1];
            _accMm[2] = _localAbs[2];

            _UpdateStationMode();
            _TrySend(frameStamp);
        }

        // §9.3。station のモードを決める (③)。
        // Why-not: Manager に持たせる。プレイヤーのコピーは実行時に生成されるので、
        // Manager から辿れない (§9.4)。各コピーが自分の station を自分で管理する。
        void _UpdateStationMode()
        {
            if (station == null)
                return;

            bool sameRoot = false;
            bool sameKmCell = false;
            if (!isLocalPlayer)
            {
                // 同じ Root は、そのプレイヤーの FDMi 席の vehicleAnchor と現在の root の比較で決める (§9.3)。
                if (_seatIndex >= 0 && _seatIndex < manager.stations.Length)
                {
                    FDMiStationAnchor seat = manager.stations[_seatIndex];
                    if (seat != null && seat.vehicleAnchor == manager.GetRoot())
                        sameRoot = true;
                }

                // 同じ km 圏内は、絶対座標の 1 km セルで比べる (§9.3)。
                FDMiSyncPlayer local = manager.GetLocalPlayerAnchor();
                if (local != null)
                    sameKmCell = FDMiSyncMath.SameKmCell(_localAbs, local._localAbs);
            }

            // ③。自分は Seated=false。remote は同じ Root かつ同じ km 圏内のときだけ Seated=false。
            bool seated = FDMiSyncMath.StationSeatedMode(isLocalPlayer, sameRoot, sameKmCell);

            // Why-not: VRCStation.seated を実行時に書かない。Udon 公開の extern には __get_seated__ しか無く、
            // 書き込む経路が存在しない (VRC.Udon.VRCWrapperModules の ExternVRCSDK3ComponentsVRCStation)。
            // 同じ意図は PlayerMobility で表す。Mobile は移動もテポストも許し、ImmobilizeForVehicle は席に留まる。
            // main ブランチの FDMiPlayerPosition も同じ 2 値で立場を分けていた。
        }

        // §9.3。どの VRCStation にも座っていないときだけ、自分の station に座らせる (①・②)。
        // 他の VRCStation に座っているときはそちらを優先する (②)。
        // Why-not: OnStationEntered だけで判定しない。FDMi 管理外の station にはイベントが来ない。
        // VRCStation に座ると PlayerLocal のコライダが 1 個減るので、その差で判定する。
        void _KeepSeated()
        {
            if (Time.time >= _nextProbeTime)
            {
                _nextProbeTime = Time.time + PROBE_INTERVAL_SEC;
                Vector3 root = player.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).position;
                _probeColliderCount = Physics.OverlapSphereNonAlloc(
                    root,
                    PROBE_RADIUS_M,
                    _probeColliders,
                    PLAYER_LOCAL_LAYER_MASK
                );
                if (_probeColliderCount > _standingColliderCount)
                    _standingColliderCount = _probeColliderCount;
            }

            // 自分の station に座っていれば何もしない (①)。
            if (_inOwnStation)
                return;

            // 他の VRCStation に座っているときはそちらの挙動を優先する (②)。
            // Why-not: FDMi 管理の席は _seatIndex が持つ。管理外の席は物理プローブが持つ。
            if (_seatIndex >= 0)
                return;
            if (_standingColliderCount > 0 && _probeColliderCount < _standingColliderCount)
                return;

            // station が自分の足元に来る前に座ると TEMPLATE の位置へ飛ぶ。姿勢を 1 回書いてから座る (①)。
            if (!_poseWritten)
                return;

            if (Time.time < _nextSeatTime)
                return;
            _nextSeatTime = Time.time + SEAT_RETRY_SEC;
            station.UseStation(player);
        }

        // §9.8。root の respawn point へ自分の絶対姿勢を作り直す。
        // Why-not: localPlayer.Respawn() を呼ばない。descriptor spawn は root frame に追従せず、
        // 任意の respawnPoint へ飛べない。TeleportTo が唯一の経路 (§9.8 / ADR-0002)。
        public void _RespawnLocalPlayer()
        {
            FDMiSyncAnchor root = manager.GetRoot();
            if (root == null) return;

            root.GetAbsoluteMm(_tmpRespawnAbs);
            FDMiSyncMath.RespawnAbs(_tmpRespawnAbs, root.GetAbsoluteRotation(),
                root._bakedRespawnRelMm, root._bakedRespawnRelRot, _localAbs, out _localRot);

            // 着席中は _Tick が _localAbs を書かず送信もしない。席を出して自由移動に戻す (§9.3)。
            _seatIndex = -1;
            // 次の _Tick が Teleport 後の GetPosition() から同じ姿勢を作り、次フレームに送る (§6.3)。
            _nextSendTime = 0f;

            player.TeleportTo(manager.ViewFromAbs(_localAbs), manager.ViewRotFromAbs(_localRot));
            player.SetVelocity(Vector3.zero);
        }

        // §9.8。VRChat の respawn (落下・Respawn()) を root の respawn point へ置き直す。
        // Why-not: remote のコピーでも発火する。位置の真実は本人が送るので local だけが処理する (§6.2)。
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            _RespawnLocalPlayer();
        }

        // §9.3。自分の station への着席。VRChat が通知する (①)。
        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
            _inOwnStation = true;
        }

        // §9.3。自分の station からの退出。VRChat が通知する。
        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
            _inOwnStation = false;
        }

        // §9.3。owner でも view を書く。自分の station が自分の位置へ追従する (①)。
        // Why-not: 基底の owner 除外を維持する。player アンカーは body を持たないので二重動悸は起こらない。
        public override void _ApplyView(bool shift)
        {
            _WriteViewPose();
            _poseWritten = true;
        }
    }
}
