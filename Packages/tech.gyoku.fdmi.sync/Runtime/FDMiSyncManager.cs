using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    // §9.1。stamp・原点・root 切替。
    // 同期フィールドを持たない。フレーム駆動は FixedUpdate (stamp 記録のみ) と Update (ExecOrder 1001) の 2 本だけ。
    // アンカーは自分の Update で積分・送受信を、自分の LateUpdate で view を書く (ExecOrder 1000)。
    // Why-not: 1 本の LateUpdate で回さない。順序が 1 フレーム内で確定しないと、
    // 「積分 → 原点移動 → view 書き」の順が崩れて view が 1 フレーム分ずれる (§7.4)。
    // Why-not: プレイヤーアンカーの登録表を持たない。実行時に生成されるコピーは登録表から辿れない。
    // 原点が変わったことは _viewVersion で知らせる (push ではなく version 引き) (§9.4)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(1001)]
    public class FDMiSyncManager : UdonSharpBehaviour
    {
        // §3.3。rebase の閾値と移動量の単位。
        public const float REBASE_DISTANCE_M = 1000f;
        public const long REBASE_QUANTUM_MM = 1000000L;

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
        // Why-not: Manager が全アンカーを並べて _ShiftToView を呼ぶ方式にしない。
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

            for (int i = 0; i < anchors.Length; i++)
                anchors[i]._Init(this);

            // 手順 3 は手順 2 の後。GetAbsoluteMm は _Init が確定した _localAbs を返す。
            initialAnchor.GetAbsoluteMm(originMm);
            originRot = initialAnchor.GetAbsoluteRotation();
            _root = initialAnchor;
            _viewVersion = 1;
        }

        // §5.2 手順 1。body 位置は直前の物理ステップ終了時の値なので、fixedDeltaTime を足して時刻を合わせる (§6.8)。
        public void FixedUpdate()
        {
            _physicsStampMs = Networking.GetServerTimeInMilliseconds();
        }

        // ExecOrder 1001。アンカーの Update (1000) より後に走る。
        // ここで rebase の判定と原点の移動を行う。アンカーの view 書き (LateUpdate) より前 (§7.4)。
        // Why-not: アンカーの _nowMs を自分で読ませない。全ノードに同じ時刻を渡す (§6.5)。
        // この 1 フレームの遅れは extrapolate の窓を 1 フレーム縮めるだけで、位置は変わらない (§6.5)。
        public void Update()
        {
            _nowMs = Networking.GetServerTimeInMilliseconds();

            _frameStampMs = _physicsStampMs + _fixedDeltaMs;
            if (FDMiSyncMath.DeltaInt(_frameStampMs, _nowMs) > 0)
                _frameStampMs = _nowMs;

            if (_ShouldRebase()) _Rebase();
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

        // §7.4 の rebase。原点を整数 km だけ動かし、全 view を再投影する。
        public void _Rebase()
        {
            // 手順 0 は手順 3 で原点が変わる前に取る。
            AbsFromView(localPlayer.GetPosition(), _playerAbsMm);

            // 手順 1〜3。移動量は整数 km。
            RebaseShiftMm(_playerAbsMm, originMm, _shiftMm);
            bool moved = false;
            for (int i = 0; i < 3; i++)
                if (_shiftMm[i] != 0) moved = true;
            if (!moved) return;

            for (int i = 0; i < 3; i++)
                originMm[i] = originMm[i] + _shiftMm[i];

            // 手順 4 の全点再投影は _viewVersion の更新で代用する (§9.1)。
            _viewVersion = _viewVersion + 1;

            // 手順 5。自由移動のプレイヤーだけ。着席中は席の body が動くので追従する (§10.2)。
            if (_localPlayerAnchor != null && _localPlayerAnchor._seatIndex >= 0) return;
            localPlayer.TeleportTo(ViewFromAbs(_playerAbsMm), localPlayer.GetRotation());
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
            Vector3 oldVelFef = FDMiSyncMath.QRotate(oldOriginRot, localPlayer.GetVelocity()); // 3

            // 手順 4 は旧原点でプレイヤーの絶対姿勢を捕獲する。手順 5 の後では view が変わり再現できない。
            AbsFromView(localPlayer.GetPosition(), _playerAbsMm);
            Quaternion qPlayer = FDMiSyncMath.QMul(originRot, localPlayer.GetRotation());

            next.GetAbsoluteMm(_tmpOrigin); // 5
            originMm[0] = _tmpOrigin[0];
            originMm[1] = _tmpOrigin[1];
            originMm[2] = _tmpOrigin[2];
            originRot = next.GetAbsoluteRotation();
            _root = next; // 6

            if (!seated) // 7
            {
                localPlayer.TeleportTo(ViewFromAbs(_playerAbsMm), ViewRotFromAbs(qPlayer));
                localPlayer.SetVelocity(ViewVectorFromAbs(oldVelFef));
            }

            // 手順 8 の全点再投影は _viewVersion の更新で代用する (§9.1)。
            // Why-not: プレイヤーアンカーの登録表を Manager が持たないため、全点を Manager から辿れない (§9.4)。
            // 各アンカーが自分の LateUpdate で版の変化に気付いて再投影する。

            for (int i = 0; i < anchors.Length; i++) // 9
            {
                Rigidbody b = anchors[i].body;
                if (b == null)
                    continue;
                // 旧 frame → FEF → 新 frame の順に回す。
                Vector3 velFef = FDMiSyncMath.QRotate(oldOriginRot, b.velocity);
                b.velocity = FDMiSyncMath.QRotate(FDMiSyncMath.QInv(originRot), velFef);
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
            return FDMiSyncMath.QMul(FDMiSyncMath.QInv(originRot), absRot);
        }

        public Vector3 ViewVectorFromAbs(Vector3 v)
        {
            return FDMiSyncMath.QRotate(FDMiSyncMath.QInv(originRot), v);
        }

        public void GetOriginMm(long[] dst)
        {
            dst[0] = originMm[0];
            dst[1] = originMm[1];
            dst[2] = originMm[2];
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

        public FDMiStationAnchor GetStation(int index)
        {
            return stations[index];
        }

        public FDMiSyncPlayer GetLocalPlayerAnchor()
        {
            return _localPlayerAnchor;
        }

        // §9.4。ローカルプレイヤーのアンカーの登録。配列は持たない。
        public void _SetLocalPlayerAnchor(FDMiSyncPlayer a)
        {
            _localPlayerAnchor = a;
        }
    }
}
