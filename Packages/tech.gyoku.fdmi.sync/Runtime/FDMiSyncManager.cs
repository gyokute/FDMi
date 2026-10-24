using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    // §9.1。登録表・stamp・原点・rebase・root 切替・全 view 更新。
    // 同期フィールドを持たない。フレーム駆動はこの 1 個の LateUpdate と FixedUpdate (stamp 記録のみ) に限る (P-05)。
    // ExecOrder 1000: 同じ LateUpdate を持つ他スクリプトより後に走らせる (§5.2)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(1000)]
    public class FDMiSyncManager : UdonSharpBehaviour
    {
        // §9.4 / U-05。プレイヤー登録表の長さ。ワールドの Maximum Capacity 以下であること (V-17)。
        public const int PLAYER_SLOTS = 80;

        [Header("Editor 焼き (E-05)")]
        public FDMiSyncAnchor[] anchors;
        public FDMiStationAnchor[] stations;
        public FDMiSyncAnchor initialAnchor;
        public FDMiSyncAnchor defaultAnchor;

        public FDMiSyncAnchor _root;
        // ローカルプレイヤーのアンカー。suspend の判定に使う (§8.3)。
        public FDMiSyncPlayer _localPlayerAnchor;
        // Networking.LocalPlayer のキャッシュ。Start で 1 回だけ読む (§5.4 手順 1b)。
        public VRCPlayerApi localPlayer;

        // §9.4。実行時のプレイヤー登録表。同期しない。各コピーの Start が追加し、OnPlayerLeft が swap-remove する。
        // Why-not: Manager の Start で初期化しない。Start の順序は Unity が保証せず、コピーの Start が
        // 先に走った登録を消してしまう (§5.4)。
        public FDMiSyncPlayer[] _playerAnchors = new FDMiSyncPlayer[PLAYER_SLOTS];
        public int _playerAnchorCount;

        // 原点。同期しない。クライアントごとに独立 (§7.1)。
        public long[] originMm = new long[3];
        public Quaternion originRot = Quaternion.identity;
        public float rebaseDistanceM = 1000f;

        public int _frameStampMs;
        public int _physicsStampMs;
        public int _fixedDeltaMs = 20;
        public bool _viewsDirty = true;
        public int _suspendUntilFrame = -1;
        public int _nowMs;

        public long[] _shiftMm = new long[3];
        public long[] _tmpOrigin = new long[3];
        public long[] _playerAbsMm = new long[3];

        // §5.4 の 5 段。手順 1 の直後に補助手順 1b を置く。
        public void Start()
        {
            _physicsStampMs = Networking.GetServerTimeInMilliseconds();
            _fixedDeltaMs = (int)FDMiSyncMath.RoundHalfAwayFromZero(Time.fixedDeltaTime * 1000f);
            localPlayer = Networking.LocalPlayer;

            for (int i = 0; i < anchors.Length; i++) anchors[i]._Init(this);

            // 手順 3 は手順 2 の後。GetAbsoluteMm は _Init が確定した _localAbs を返す。
            initialAnchor.GetAbsoluteMm(originMm);
            originRot = initialAnchor.GetAbsoluteRotation();
            _root = initialAnchor;

            _viewsDirty = true;
            _UpdateAllViews();
        }

        // §5.2 手順 1。body 位置は直前の物理ステップ終了時の値なので、fixedDeltaTime を足して時刻を合わせる (§6.8)。
        public void FixedUpdate()
        {
            _physicsStampMs = Networking.GetServerTimeInMilliseconds();
        }

        // §5.2 手順 3。now は 1 フレーム 1 回だけ読み、全ノードに同じ値を渡す (§6.5)。
        public void LateUpdate()
        {
            _nowMs = Networking.GetServerTimeInMilliseconds();

            _frameStampMs = _physicsStampMs + _fixedDeltaMs;
            if (FDMiSyncMath.DeltaInt(_frameStampMs, _nowMs) > 0) _frameStampMs = _nowMs;

            if (_ShouldRebase()) _Rebase();

            for (int i = 0; i < anchors.Length; i++) anchors[i]._Tick(_frameStampMs, _nowMs);

            // §8.3。suspend が止めるのはローカルプレイヤーのアンカーだけ。全体を early return しない。
            bool suspended = Time.frameCount <= _suspendUntilFrame;
            for (int i = 0; i < _playerAnchorCount; i++)
                if (!suspended || _playerAnchors[i] != _localPlayerAnchor) _playerAnchors[i]._Tick(_frameStampMs, _nowMs);

            _UpdateAllViews();
        }

        // §7.3。着席中も GetPosition() は席の view 位置を返すため、同じ条件でよい。
        public bool _ShouldRebase()
        {
            Vector3 v = localPlayer.GetPosition();
            float m = Mathf.Max(Mathf.Abs(v.x), Mathf.Max(Mathf.Abs(v.y), Mathf.Abs(v.z)));
            return m > rebaseDistanceM;
        }

        // §7.4。原点を 1 km の整数倍だけ動かす。_accMm は変更しない。
        public void _Rebase()
        {
            // 手順 0 は原点が変わる前に取る。
            AbsFromView(localPlayer.GetPosition(), _playerAbsMm);

            for (int i = 0; i < 3; i++)
            {
                long delta = _playerAbsMm[i] - originMm[i];
                // 整数除算は 0 方向の切り捨て。r は delta と同じ符号を持つ (§7.5)。
                long q = delta / 1000000L;
                long r = delta - q * 1000000L;
                if (r >= 500000L) q = q + 1L;
                else if (r <= -500000L) q = q - 1L;
                _shiftMm[i] = q * 1000000L;
            }

            // 全軸で shift が 0 なら手順 4〜6 を skip する (§7.4)。
            if (_shiftMm[0] == 0L && _shiftMm[1] == 0L && _shiftMm[2] == 0L) return;

            for (int i = 0; i < 3; i++) originMm[i] = originMm[i] + _shiftMm[i];

            for (int i = 0; i < anchors.Length; i++) anchors[i]._ShiftToView();
            for (int i = 0; i < _playerAnchorCount; i++) _playerAnchors[i]._ShiftToView();

            // 手順 5 は自由移動のプレイヤーだけ。着席中は TeleportTo を呼ばない (§7.4 / §8.4)。
            if ((_localPlayerAnchor == null || _localPlayerAnchor._seatIndex < 0) && localPlayer != null)
                localPlayer.TeleportTo(ViewFromAbs(_playerAbsMm), localPlayer.GetRotation());

            _viewsDirty = true;
            _UpdateAllViews();
        }

        public void SwitchRoot(FDMiSyncAnchor next)
        {
            _SwitchRootCore(next, false);
        }

        public void SwitchRootSeated(FDMiSyncAnchor next)
        {
            _SwitchRootCore(next, true);
        }

        // §8.2 の 10 段。
        public void _SwitchRootCore(FDMiSyncAnchor next, bool seated)
        {
            // §8.1。suspend 中は新しい SwitchRoot* を受け付けない。
            if (Time.frameCount <= _suspendUntilFrame) return;
            if (next == null) return;
            if (next == _root) { _UpdateAllViews(); return; }

            Quaternion oldOriginRot = originRot;                                   // 2
            Vector3 oldVelFef = FDMiSyncMath.QRotate(oldOriginRot, localPlayer.GetVelocity()); // 3

            // 手順 4 は旧原点でプレイヤーの絶対姿勢を捕獲する。手順 5 の後では view が変わり再現できない。
            AbsFromView(localPlayer.GetPosition(), _playerAbsMm);
            Quaternion qPlayer = FDMiSyncMath.QMul(originRot, localPlayer.GetRotation());

            next.GetAbsoluteMm(_tmpOrigin);                                        // 5
            originMm[0] = _tmpOrigin[0]; originMm[1] = _tmpOrigin[1]; originMm[2] = _tmpOrigin[2];
            originRot = next.GetAbsoluteRotation();
            _root = next;                                                          // 6

            if (!seated)                                                           // 7
            {
                localPlayer.TeleportTo(ViewFromAbs(_playerAbsMm), ViewRotFromAbs(qPlayer));
                localPlayer.SetVelocity(ViewVectorFromAbs(oldVelFef));
            }

            for (int i = 0; i < anchors.Length; i++) anchors[i]._ShiftToView();     // 8
            for (int i = 0; i < _playerAnchorCount; i++) _playerAnchors[i]._ShiftToView();

            for (int i = 0; i < anchors.Length; i++)                               // 9
            {
                Rigidbody b = anchors[i].body;
                if (b == null) continue;
                // 旧 frame → FEF → 新 frame の順に回す。
                Vector3 velFef = FDMiSyncMath.QRotate(oldOriginRot, b.velocity);
                b.velocity = FDMiSyncMath.QRotate(FDMiSyncMath.QInv(originRot), velFef);
            }

            _suspendUntilFrame = Time.frameCount + 1;                               // 10
            _viewsDirty = true;
            _UpdateAllViews();
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
            return FDMiSyncMath.QMul(FDMiSyncMath.QInv(originRot), absRot);
        }

        public Vector3 ViewVectorFromAbs(Vector3 v)
        {
            return FDMiSyncMath.QRotate(FDMiSyncMath.QInv(originRot), v);
        }

        public void GetOriginMm(long[] dst)
        {
            dst[0] = originMm[0]; dst[1] = originMm[1]; dst[2] = originMm[2];
        }

        public Quaternion GetOriginRot()
        {
            return originRot;
        }

        public FDMiSyncAnchor GetRoot()
        {
            return _root;
        }

        public FDMiStationAnchor GetStation(int index)
        {
            return stations[index];
        }

        public FDMiSyncPlayer GetLocalPlayerAnchor()
        {
            return _localPlayerAnchor;
        }

        public void _SetLocalPlayerAnchor(FDMiSyncPlayer a)
        {
            _localPlayerAnchor = a;
        }

        // §9.4。登録の唯一の経路。FDMiSyncPlayer.Start から呼ぶ。
        public void _RegisterPlayerAnchor(FDMiSyncPlayer a)
        {
            if (_playerAnchorCount >= PLAYER_SLOTS) return;
            _playerAnchors[_playerAnchorCount] = a;
            _playerAnchorCount = _playerAnchorCount + 1;
        }

        // §9.4 / U-04。playerId の一致だけで swap-remove する。破棄済みのコピーを触らない。
        public override void OnPlayerLeft(VRCPlayerApi p)
        {
            int id = p.playerId;
            for (int i = 0; i < _playerAnchorCount; i++)
            {
                if (_playerAnchors[i].playerId != id) continue;
                _playerAnchorCount = _playerAnchorCount - 1;
                _playerAnchors[i] = _playerAnchors[_playerAnchorCount];
                _playerAnchors[_playerAnchorCount] = null;
                return;
            }
        }

        // §9.1。静的アンカーは _viewsDirty が真のときだけ書く (§12.3)。
        public void _UpdateAllViews()
        {
            bool dirty = _viewsDirty;
            for (int i = 0; i < anchors.Length; i++)
                if (dirty || !anchors[i].isStatic) anchors[i]._ApplyView();
            for (int i = 0; i < _playerAnchorCount; i++) _playerAnchors[i]._ApplyView();
            _viewsDirty = false;
        }
    }
}
