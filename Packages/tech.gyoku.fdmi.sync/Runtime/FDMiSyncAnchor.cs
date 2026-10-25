using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // §9.2。動的アンカーと静的アンカーに共通する、絶対姿勢の保持と view への写像。
    // 送受信・積分・補間は FDMiDynamicObjectSync、Editor 焼きの入力と焼き値の固定は FDMiStaticObjectSync が持つ。
    // 位置の管理は「親アンカーとの差分」で行う。子の Transform は親の Transform の下に入る (§9.2)。
    // Why-not: 基底に同期フィールドを置いて isStatic で分岐しない。動的は Manual、静的は NoVariableSync であり、
    // [UdonSynced] を持つ型は NoVariableSync を宣言できない (§5.1 / UdonSharp)。同期モードの違いが型の違いである。
    // Why-not: 具象にしない。直接付けると _InitLocal が空のまま _Tick も送受信もしないアンカーになる。
    // Why-not: 緯度経度高度 yaw を基底に置かない。Runtime が読まず、Editor 焼きの入力は静的アンカーだけが使う (§9.2.2)。
    public abstract class FDMiSyncAnchor : UdonSharpBehaviour
    {
        public FDMiSyncManager manager;
        public Rigidbody body;
        public Transform view;

        // 親アンカー。null は親のいないアンカー。位置は親との差分で管理する (§9.2)。
        // Why-not: 親の Transform を直接の基準にしない。親の Transform は view 書きの順序で 1 フレーム古くになりうる。
        // 差分の基準は絶対姿勢で持つので、書かれる順番に依存しない (§3.5)。
        public FDMiSyncAnchor parent;

        // 現在の絶対姿勢。動的は積分・受信サンプル、静的は焼き値が確定させる。
        // view への写像はこの 2 つだけを見る (§9.2)。
        public long[] _localAbs = new long[3];
        public Quaternion _localRot = Quaternion.identity;

        // 最後に投影した原点バージョン。manager._viewVersion と違えば再投影する (§9.1)。
        public int _viewVersion = -1;

        // 毎フレーム view を書くか。_InitLocal で型ごとに確定する (§12.3)。
        // Why-not: 毎フレーム _WritesViewEveryFrame() を呼ばない。型ごとの問い合わせは 1 回だけにする (§12.3)。
        public bool _writesView;

        // 親アンカーの絶対位置の受け皿。
        public long[] _tmpParentAbs = new long[3];

        public void _CacheRefs()
        {
            if (view == null) view = transform;
            if (body == null) body = GetComponent<Rigidbody>();
        }

        // §5.3 手順 2。共通部だけを行い、焼き値の展開と積分器の準備は _InitLocal に任せる。
        // Why-not: 派生ごとに _Init を丸ごと書かない。manager と参照の解決を 2 箇所に置くと、
        // _Init の呼び出し順 (§5.4) が型ごとにずれる。
        public void _Init(FDMiSyncManager m)
        {
            manager = m;
            _CacheRefs();
            _InitLocal();
        }

        // 焼き値から _localAbs / _localRot を確定する。動的は積分器と送信の準備もここで行う。
        public abstract void _InitLocal();

        // §9.2。frameStamp / nowMs を受けて状態を進める。静的は空、動的は受信・積分・送信。
        // 型ごとの実装を持つ。基底に空の既定を置かない (静的の空実装は FDMiStaticObjectSync)。
        public abstract void _Tick(int frameStamp, int nowMs);

        // §9.2。view を書く。動的は owner 以外で書く。基底は静的アンカーも同じ処理を使う。
        public virtual void _ApplyView()
        {
            _ApplyPose();
        }

        // §7.4 手順 4 / §8.2 手順 8。body を kinematic にせず位置・回転だけを写す。
        // Why-not: isKinematic を立てない。動的アンカーの owner は次の物理ステップで積分を続ける。
        public virtual void _ShiftToView()
        {
            _ApplyPose();
        }

        // body を kinematic にしてから絶対姿勢を書く。静的アンカーと他者が駆動する動的アンカーの経路。
        public void _ApplyPose()
        {
            if (body != null) body.isKinematic = true;
            _WriteViewPose();
        }

        // body があれば body を、無ければ view を書く。
        // Why-not: body と view の両方に書かない。view は body.transform の子 (VehicleVisual) であり (§11.5)、
        // 子の world 姿勢を親の移動後にもう一度書くと二重に動く。body が無いアンカーだけ view を持つ。
        public virtual void _WriteViewPose()
        {
            if (body != null)
            {
                body.position = manager.ViewFromAbs(_localAbs);
                body.rotation = manager.ViewRotFromAbs(_localRot);
                return;
            }
            if (parent != null)
            {
                _WriteLocalPose();
                return;
            }
            view.position = manager.ViewFromAbs(_localAbs);
            view.rotation = manager.ViewRotFromAbs(_localRot);
        }

        // 親アンカーとの差分。子の Transform は親の Transform の下にあるので local に書く (§9.2)。
        // d = R_parent⁻¹ ⊗ (P_self − P_parent)、q = R_parent⁻¹ ⊗ q_self。
        // Why-not: 親の Transform を読まない。親の view 書きが自分より先に走ったか担保できない。
        // 差分は 2 つの絶対姿勢だけで決まるので、書かれる順番に依存しない (§3.5)。
        void _WriteLocalPose()
        {
            parent.GetAbsoluteMm(_tmpParentAbs);
            Quaternion pRot = parent.GetAbsoluteRotation();
            Quaternion invPRot = FDMiSyncMath.QInv(pRot);
            Vector3 d = new Vector3(
                (float)(_localAbs[0] - _tmpParentAbs[0]) * 0.001f,
                (float)(_localAbs[1] - _tmpParentAbs[1]) * 0.001f,
                (float)(_localAbs[2] - _tmpParentAbs[2]) * 0.001f);
            view.localPosition = FDMiSyncMath.QRotate(invPRot, d);
            view.localRotation = FDMiSyncMath.QMul(invPRot, _localRot);
        }

        // §9.2。Update 相当。積分・送受信・送信。
        // Why-not: suspend の判定を各型に書かない。suspend は Manager だけが知る (§8.3)。
        public void _TickDriver()
        {
            if (manager == null) return;
            if (manager._IsSuspended(this)) return;
            _Tick(manager._frameStampMs, manager._nowMs);
        }

        // §9.2。LateUpdate 相当。view を書く。
        // Why-not: 全アンカーの view を Manager が書かない。アンカーごとに書くので、
        // 1 アンカー 1 LateUpdate になるが、原点が変わったときだけ書き直す規則は各アンカーが持つ (§9.1)。
        public void _ApplyViewDriver()
        {
            if (manager == null) return;
            if (_viewVersion != manager._viewVersion)
            {
                _viewVersion = manager._viewVersion;
                _ShiftToView();
                return;
            }
            if (_writesView) _ApplyView();
        }

        public void GetAbsoluteMm(long[] dst)
        {
            dst[0] = _localAbs[0]; dst[1] = _localAbs[1]; dst[2] = _localAbs[2];
        }

        public Quaternion GetAbsoluteRotation()
        {
            return _localRot;
        }

        // FEF 成分の m/s。型ごとの実装を持つ (静的は 0、動的は _localVelFef)。
        public abstract Vector3 GetVelocity();

        public Vector3 GetViewPosition()
        {
            return manager.ViewFromAbs(_localAbs);
        }

        public Quaternion GetViewRotation()
        {
            return manager.ViewRotFromAbs(_localRot);
        }
    }
}
