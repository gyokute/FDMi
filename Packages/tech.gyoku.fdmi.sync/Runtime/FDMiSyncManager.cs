using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    // §9.1。stamp・原点・root 切替。
    // 同期フィールドを持たない。FixedUpdate (ExecOrder 999) で root 積分・原点更新を行い、
    // Update (ExecOrder 999) で同期時刻を渡す。アンカーは FixedUpdate (1000) で自分の view を書く。
    // Why-not: 1 本の LateUpdate で回さない。物理ステップの前に「root 積分 → 原点移動 → view 書き」を
    // 確定しないと、コライダーが 1 ステップずれる (§7.4)。
    // Why-not: プレイヤーアンカーの登録表を持たない。実行時に生成されるコピーは登録表から辿れない。
    // 原点が変わったことは _viewVersion で知らせる (push ではなく version 引き) (§9.4)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(999)]
    public class FDMiSyncManager : UdonSharpBehaviour
    {
        // §3.3。rebase の閾値と移動量の単位。
        public const float REBASE_DISTANCE_M = 1000f;
        public const long REBASE_QUANTUM_MM = 1000000L;

        [Header("Editor 焼き (E-05)")]
        public FDMiSyncAnchor[] anchors;
        public FDMiSyncAnchor initialAnchor;
        public FDMiManagerAnchor managerAnchor;
        public FDMiSyncAnchor defaultAnchor;

        public FDMiSyncAnchor _root;

        // ローカルプレイヤーのアンカー。suspend の判定に使う (§8.3)。
        public FDMiSyncPlayer _localPlayerAnchor;

        // Networking.LocalPlayer のキャッシュ。Start で 1 回だけ読む (§5.4 手順 1b)。
        public VRCPlayerApi localPlayer;

        // 原点。同期しない。クライアントごとに独立 (§7.1)。
        // root 切替で決まる。rebase (§7.4) では整数 km だけ平行移動する。
        public long[] originMm = new long[3];
        public Quaternion originRot = Quaternion.identity;

        public int _frameStampMs;
        public int _physicsStampMs;
        public int _fixedDeltaMs = 20;
        public int _nowMs;

        // rebase の閾値 (§7.3)。原点はこの距離を超えると整数 km だけ動く。
        public float rebaseDistanceM = REBASE_DISTANCE_M;

        // 原点の変更回数。アンカーは自分の _viewVersion と比べる (§9.1)。
        // Why-not: Manager が全アンカーを並べて view を書き直す方式にしない。
        // プレイヤーアンカーは実行時に生成されるので、全点を Manager から辿れない (§9.4)。
        public int _viewVersion;

        public int _suspendUntilFrame = -1;

        public long[] _tmpOrigin = new long[3];
        public long[] _playerAbsMm = new long[3];
        public long[] _shiftMm = new long[3];

        public void Start()
        {
            _physicsStampMs = Networking.GetServerTimeInMilliseconds();
            _fixedDeltaMs = (int)FDMiSyncMath.RoundHalfAwayFromZero(Time.fixedDeltaTime * 1000f);
            localPlayer = Networking.LocalPlayer;

            // root を先に確定する。初期化中に 1 つ失敗しても view origin は壊さない。
            if (managerAnchor != null) _root = managerAnchor;
            else _root = initialAnchor;
            _root.GetAbsoluteMm(originMm);
            originRot = _root.GetAbsoluteRotation();
            _root._SnapRootBodyToOrigin();
            _viewVersion = 1;

            for (int i = 0; i < anchors.Length; i++)
                if (anchors[i] != managerAnchor) anchors[i]._Init(this);

            // manager anchor は焼き値だけで動く。継承フィールドの Udon 初期化に依存させない。
            if (managerAnchor != null)
            {
                managerAnchor.manager = this;
                managerAnchor._writesView = false;
            }

            for (int i = 0; i < anchors.Length; i++)
                if (anchors[i] != null && anchors[i].IsRoot != null)
                    anchors[i].IsRoot.Data = false;
            if (_root != null && _root.IsRoot != null)
                _root.IsRoot.Data = true;
        }

        // §5.2 / §7.2。物理ステップの前に root を積分・原点を更新し、不連続を 1 ステップ止める。
        public void FixedUpdate()
        {
            _physicsStampMs = Networking.GetServerTimeInMilliseconds();

            if (_root != null) _root._IntegrateRootBody();

            if (_root == managerAnchor && _ShouldRebase()) _Rebase();

            if (_root == null) return;
            _root.GetAbsoluteMm(originMm);
            originRot = _root.GetAbsoluteRotation();
            // 自由移動中は rebase / root 切替でしか原点が変わらない。
            // 乗車中は root body が毎 FixedUpdate 動くので、毎ステップ再投影する。
            if (_root != managerAnchor)
                _viewVersion = _viewVersion + 1;
        }

        // ExecOrder 999。アンカーの Update (1000) より前に走り、全ノードへ同じ時刻を渡す。
        // 原点移動は FixedUpdate 側で行う。ここは同期時刻の更新だけ。
        public void Update()
        {
            _nowMs = Networking.GetServerTimeInMilliseconds();

            _frameStampMs = _physicsStampMs + _fixedDeltaMs;
            if (_frameStampMs - _nowMs > 0)
                _frameStampMs = _nowMs;
        }

        // §7.3。ローカルプレイヤーの view 位置が閾値を超えたら rebase する。
        // Why-not: root の位置で判定しない。自由移動のプレイヤーが原点から離れうるので、
        // プレイヤーの位置で判定する (着席中は席の位置 = 車両の位置) (§7.2)。
        public bool _ShouldRebase()
        {
            if (localPlayer == null) return false;
            Vector3 v = localPlayer.GetPosition();
            float m = Mathf.Abs(v.x);
            float a = Mathf.Abs(v.y);
            if (a > m) m = a;
            a = Mathf.Abs(v.z);
            if (a > m) m = a;
            return m > rebaseDistanceM;
        }

        // §7.4 手順 1〜3。原点の移動量。原点は整数 km だけ動くので丸め誤差が原点に入らない。
        // Why-not: 丸めを float で行う。差を long で先に取ってから整数除算する (§7.1)。
        public static void RebaseShiftMm(long[] abs, long[] origin, long[] dst)
        {
            for (int i = 0; i < 3; i++)
            {
                long delta = abs[i] - origin[i];
                long q = delta / REBASE_QUANTUM_MM;
                long r = delta - q * REBASE_QUANTUM_MM;
                if (r >= REBASE_QUANTUM_MM / 2) q = q + 1;
                else if (r <= -(REBASE_QUANTUM_MM / 2)) q = q - 1;
                dst[i] = q * REBASE_QUANTUM_MM;
            }
        }

        // §7.4 の rebase。自由移動中の manager anchor と原点を整数 km だけ動かす。
        public void _Rebase()
        {
            if (managerAnchor == null) return;

            // 手順 0 は原点が変わる前に取る。
            AbsFromView(localPlayer.GetPosition(), _playerAbsMm);

            // 手順 1〜3。移動量は整数 km。
            RebaseShiftMm(_playerAbsMm, originMm, _shiftMm);
            bool moved = false;
            for (int i = 0; i < 3; i++)
                if (_shiftMm[i] != 0) moved = true;
            if (!moved) return;

            for (int i = 0; i < 3; i++)
            {
                managerAnchor._localAbs[i] = managerAnchor._localAbs[i] + _shiftMm[i];
                originMm[i] = originMm[i] + _shiftMm[i];
            }

            _viewVersion = _viewVersion + 1;

            // 手順 5。自由移動のプレイヤーを新しい view へ移す。着席中は root が車両になるため
            // _ShouldRebase は走らない (§10.2)。
            localPlayer.TeleportTo(ViewFromAbs(_playerAbsMm), localPlayer.GetRotation());
        }

        public void SwitchRoot(FDMiSyncAnchor next)
        {
            _SwitchRootCore(next);
        }

        // §8.2 の 10 段。
        public void _SwitchRootCore(FDMiSyncAnchor next)
        {
            // §8.1。suspend 中は新しい SwitchRoot* を受け付けない。
            if (Time.frameCount <= _suspendUntilFrame)
                return;
            if (next == null)
                return;
            if (next == _root)
            {
                _viewVersion = _viewVersion + 1;
                return;
            }

            Quaternion oldOriginRot = originRot; // 2
            Vector3 oldVelFef = oldOriginRot * localPlayer.GetVelocity(); // 3

            // 手順 4 は旧原点でプレイヤーの絶対姿勢を捕獲する。手順 5 の後では view が変わり再現できない。
            AbsFromView(localPlayer.GetPosition(), _playerAbsMm);
            Quaternion qPlayer = originRot * localPlayer.GetRotation();

            FDMiSyncAnchor prevRoot = _root;

            next.GetAbsoluteMm(_tmpOrigin); // 5
            originMm[0] = _tmpOrigin[0];
            originMm[1] = _tmpOrigin[1];
            originMm[2] = _tmpOrigin[2];
            originRot = next.GetAbsoluteRotation();
            _root = next; // 6
            if (prevRoot != null && prevRoot.IsRoot != null) prevRoot.IsRoot.Data = false;
            if (next.IsRoot != null) next.IsRoot.Data = true;
            next._SnapRootBodyToOrigin();

            localPlayer.TeleportTo(ViewFromAbs(_playerAbsMm), ViewRotFromAbs(qPlayer)); // 7
            localPlayer.SetVelocity(FDMiSyncMath.QInv(originRot) * oldVelFef);

            // 手順 8 の全点再投影は _viewVersion の更新で代用する (§9.1)。
            // 各アンカーが次の FixedUpdate で版の変化に気付いて再投影する。

            for (int i = 0; i < anchors.Length; i++) // 9
            {
                Rigidbody b = anchors[i].body;
                if (b == null)
                    continue;
                // 旧 frame → FEF → 新 frame の順に回す。角速度も同じ frame 変換を受ける。
                Vector3 velFef = oldOriginRot * b.velocity;
                b.velocity = FDMiSyncMath.QInv(originRot) * velFef;
                Vector3 angFef = oldOriginRot * b.angularVelocity;
                b.angularVelocity = FDMiSyncMath.QInv(originRot) * angFef;
            }

            _suspendUntilFrame = Time.frameCount + 1; // 10
            _viewVersion = _viewVersion + 1;
        }

        // §7.1 の逆写像。
        public void AbsFromView(Vector3 view, long[] dst)
        {
            FDMiSyncMath.AbsFromView(originMm, originRot, view, dst);
        }

        // §7.1。差を long で先に取る。
        public Vector3 ViewFromAbs(long[] abs)
        {
            return FDMiSyncMath.ViewFromAbs(originMm, originRot, abs);
        }

        public Quaternion ViewRotFromAbs(Quaternion absRot)
        {
            return FDMiSyncMath.QInv(originRot) * absRot;
        }

        public Quaternion GetOriginRot()
        {
            return originRot;
        }

        public FDMiSyncAnchor GetRoot()
        {
            return _root;
        }

        // §8.3。suspend が止めるのはローカルプレイヤーのアンカーだけ。
        public bool _IsSuspended(FDMiSyncAnchor a)
        {
            if (Time.frameCount > _suspendUntilFrame)
                return false;
            return a == _localPlayerAnchor;
        }

        public FDMiSyncPlayer GetLocalPlayerAnchor()
        {
            return _localPlayerAnchor;
        }

        // §9.8。Interact / Trigger から呼べる respawn の入口。
        // Why-not: プレイヤーアンカーのコピーは実行時生成でシーンから配線できないため、Manager を入口にする (§9.4)。
        public void _RespawnLocalPlayer()
        {
            if (_localPlayerAnchor == null) return;
            _localPlayerAnchor._RespawnLocalPlayer();
        }

        // §9.4。ローカルプレイヤーのアンカーの登録。配列は持たない。
        public void _SetLocalPlayerAnchor(FDMiSyncPlayer a)
        {
            _localPlayerAnchor = a;
        }
    }
}
