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
        [HideInInspector] public int playerId = -1;

        [HideInInspector] public VRCPlayerApi player;

        // player.isLocal のキャッシュ。Start 以降変わらない。
        [HideInInspector] public bool isLocalPlayer;

        // §9.3。プレイヤーの定位置に使う VRCStation。FDMiSyncPlayer と同じ GameObject に置く (①)。
        // Editor 焼き E-09 で配線する。
        // Why-not: Runtime で GetComponent しない。コピーは実行時に生成されるが、コピーは同一階層内の
        // 参照を Unity の Instantiate 意味論で張り替えるため、焼き値が各コピーの station に解決される (§9.4)。
        [Tooltip("プレイヤーの定位置に使う VRCStation。同一 GameObject に置く")]
        public VRCStation station;

        // 自分の station に座っているか。VRChat の着席イベントで更新する (①)。
        bool _inOwnStation;

        // §9.3。Foreign station 在席の判定用。PlayerLocal はローカルプレイヤーしか含まない (ADR-0004)。
        Collider[] _probeResults = new Collider[64];

        // 自分の station 在席中に数えた PlayerLocal のコライダー数。−1 は未取得。
        // Why-not: 定数で持たない。アバターのコライダー数に依存するため (ADR-0004)。
        int _unseatedColliderCount = -1;

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
            if (!isLocalPlayer)
            {
                _IntegrateReceive(nowMs);
                _ComputeViewPose(nowMs);
                return;
            }

            // §9.3。自分の station に座っていない間だけ、既定席へ座らせる。
            _KeepSeated();

            Quaternion originRot = manager.GetOriginRot();
            // §7.2 の自由移動行: originMm + Rotate(originRot, GetPosition() × 1000)。
            // 車両の席に座っている間も、VRChat が返す実際の着席位置をそのまま使う。
            manager.AbsFromView(player.GetPosition(), _localAbs);
            _localRot = originRot * player.GetRotation();
            _localVelFef = originRot * player.GetVelocity();
            // 送信は _accMm を読む。
            _accMm[0] = _localAbs[0];
            _accMm[1] = _localAbs[1];
            _accMm[2] = _localAbs[2];

            _TrySend(frameStamp);
        }

        // §9.3。自分の station に座っていないなら、既定席へ座らせる。
        // Why-not: station の登録表を作らない。Foreign station は後から動的に挿入されるため、
        // 列挙では追跡できない (ADR-0004)。
        // Why-not: プローブをスロットルしない。止める判定が遅れた分だけ Foreign station から
        // 引き剥がす。physics クエリはネイティブ呼び出しで、Udon のインタプリタ コストが乗らない。
        void _KeepSeated()
        {
            // seated=true の station に座ると、VRChat が PlayerLocal のコライダーを1つ無効化する。
            // personal station は seated=false なので、この数は自分の station 在席中だけ基準になる。
            // Why-not: 判定式を count == _unseatedColliderCount - 1 にしない。2つ以上減る場合に
            // 在席していないと誤判定し、引き剥がす (ADR-0004)。
            int count = Physics.OverlapSphereNonAlloc(
                player.GetPosition(),
                10f,
                _probeResults,
                1
                    << 10 /* PlayerLocal */
                ,
                QueryTriggerInteraction.Ignore
            );

            if (_inOwnStation)
            {
                _unseatedColliderCount = count;
                return;
            }

            // Foreign station 在席中は動かさない。退出すれば次の Tick で count が基準へ戻り、再着席する。
            if (count < _unseatedColliderCount)
                return;

            station.UseStation(player);
        }

        // §9.8。root の respawn point へ自分の絶対姿勢を作り直す。
        // Why-not: localPlayer.Respawn() を呼ばない。descriptor spawn は root frame に追従せず、
        // 任意の respawnPoint へ飛べない。TeleportTo が唯一の経路 (§9.8 / ADR-0002)。
        public void _RespawnLocalPlayer()
        {
            FDMiSyncAnchor root = manager.GetRoot();
            if (root == null)
                return;

            root.GetAbsoluteMm(_tmpRespawnAbs);
            FDMiSyncMath.RespawnAbs(
                _tmpRespawnAbs,
                root.GetAbsoluteRotation(),
                root._bakedRespawnRelMm,
                root._bakedRespawnRelRot,
                _localAbs,
                out _localRot
            );

            // 次の _Tick が Teleport 後の GetPosition() から同じ姿勢を作り、次フレームに送る (§6.3)。
            _nextSendTime = 0f;

            player.TeleportTo(manager.ViewFromAbs(_localAbs), manager.ViewRotFromAbs(_localRot));
            player.SetVelocity(Vector3.zero);
        }

        // §9.8。VRChat の respawn (落下・Respawn()) を root の respawn point へ置き直す。
        // Why-not: remote のコピーでも発火する。位置の真実は本人が送るので local だけが処理する (§6.2)。
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
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
    }
}
