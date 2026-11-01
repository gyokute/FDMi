using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    // §9.3。プレイヤーの姿勢。owner は「そのプレイヤー本人」だけ (§6.2)。
    // 姿勢は基準アンカー相対で同期する。基準アンカーはそのプレイヤーが載っているアンカーであり、
    // 送信側では自分の root (view origin) と一致する。着席中は基準フレームの姿勢が定数になるため、
    // root が対地速度を持っても受信側の位置は動かない (ADR-0014)。
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

        // §9.3。このプレイヤーの姿勢の基準になるアンカー。root 切替で変わるため同期する。
        // Why-not: FDMiSyncAnchor.parent を再利用しない。parent は焼きの Transform 階層であり、
        // プレイヤーの view は world に書くので階層化しない (§9.2)。
        // Why-not: 基底に置かない。実行時に親が変わるのはプレイヤーだけであり、
        // 地上固定のアンカーに 4 B と分岐を載せない (§4.4)。
        [UdonSynced, HideInInspector] public int _refAnchorIndex;

        // _refAnchorIndex の解決先。索引が変わったときだけ引き直す。−1 は未解決。
        [HideInInspector] public int _boundIndex = -1;

        [HideInInspector] public FDMiSyncAnchor _refAnchor;

        // 基準アンカーの絶対位置の受け皿。
        long[] _refAbs = new long[3];

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

            // §9.3。自分の root が自分の view frame なので、基準フレームの姿勢は view 姿勢そのもの。
            // 車両の席に座っている間も、VRChat が返す実際の着席位置をそのまま使う。
            // Why-not: AbsFromView を通さない。原点は FixedUpdate でしか動かないため、通すと
            // サンプルが物理ステップに量子化される (§7.2)。
            _refAnchorIndex = manager._rootIndex;
            Vector3 p = player.GetPosition() * 1000f;
            _poseMm[0] = FDMiSyncMath.RoundHalfAwayFromZero(p.x);
            _poseMm[1] = FDMiSyncMath.RoundHalfAwayFromZero(p.y);
            _poseMm[2] = FDMiSyncMath.RoundHalfAwayFromZero(p.z);
            _poseRot = player.GetRotation();

            // §6.5。基準フレームの相対速度は前回送信時からの差分で作る。
            // Why-not: player.GetVelocity() を送らない。着席中の意味が VRChat の実装依存であり、
            // 世界速度を返すなら「席に対して機体速度で流れる」同じバグを再生産する。
            if (_hasSent)
            {
                float dt = Time.time - _lastSendTime;
                if (dt > 0f)
                {
                    float inv = 1f / (dt * 1000f);
                    _poseVel = new Vector3(
                        (float)(_poseMm[0] - _lastSentAbs[0]) * inv,
                        (float)(_poseMm[1] - _lastSentAbs[1]) * inv,
                        (float)(_poseMm[2] - _lastSentAbs[2]) * inv);
                }
            }
            else
            {
                _poseVel = Vector3.zero;
            }

            _ResolveAbs(_poseMm, _poseRot);
            _TrySend(frameStamp);
        }

        // §9.3。基準アンカー相対の姿勢を絶対姿勢へ写す。
        // Why-not: 索引の比較を呼び出し側に置かない。owner 経路と受信経路の両方から呼ばれるため、
        // 束縛の判断をここ 1 箇所に閉じる。
        public override void _ResolveAbs(long[] poseMm, Quaternion poseRot)
        {
            if (_boundIndex != _refAnchorIndex)
            {
                _boundIndex = _refAnchorIndex;
                _refAnchor = manager.anchors[_refAnchorIndex];
            }
            _refAnchor.GetAbsoluteMm(_refAbs);
            FDMiSyncMath.ResolveAbs(_refAbs, _refAnchor.GetAbsoluteRotation(), poseMm, poseRot,
                _localAbs, out _localRot);
        }

        // §9.3。view を書く直前に絶対姿勢を作り直す。
        // Why-not: _Tick が作った _localAbs をそのまま書かない。_ApplyView は FixedUpdate でも走り、
        // その時点の原点は Manager が今の物理ステップで更新した値、_localAbs は前フレームの Update が
        // 作った値なので 1 固定ステップずれる。基準アンカーが root のとき絶対姿勢は原点と同じ量だけ
        // 動くため、この差がそのまま view の位置誤差 (v × fixedDelta) になる。書き込みが 2 値で
        // 交互になり、station に固定されたアバターが 2 位置の間でちらつく (残像)。
        // ここで作り直せば refAbs と origin が同じ瞬間の値になり、どちらの位相でも view は pose に一致する。
        public override void _WriteViewPose()
        {
            _ResolveAbs(_poseMm, _poseRot);
            base._WriteViewPose();
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

        // §9.8。root の respawn point へ自分の姿勢を作り直す。respawn 先は root の焼き値であり、
        // 基準アンカー (root) 相対の姿勢そのものなので変換しない。
        // Why-not: localPlayer.Respawn() を呼ばない。descriptor spawn は root frame に追従せず、
        // 任意の respawnPoint へ飛べない。TeleportTo が唯一の経路 (§9.8 / ADR-0002)。
        public void _RespawnLocalPlayer()
        {
            FDMiSyncAnchor root = manager.GetRoot();
            if (root == null)
                return;

            _poseMm[0] = root._bakedRespawnRelMm[0];
            _poseMm[1] = root._bakedRespawnRelMm[1];
            _poseMm[2] = root._bakedRespawnRelMm[2];
            _poseRot = root._bakedRespawnRelRot;
            _refAnchorIndex = manager._rootIndex;
            // Why-not: 不連続をまたいだ差分速度を送らない。0 から作り直し、次の _Tick が最初の
            // サンプルとして扱う (§6.5)。0 にしないと受信側がその速度で外挿して飛ぶ。
            _hasSent = false;
            _nextSendTime = 0f;

            player.TeleportTo(
                new Vector3(
                    (float)_poseMm[0] * 0.001f,
                    (float)_poseMm[1] * 0.001f,
                    (float)_poseMm[2] * 0.001f),
                _poseRot);
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
