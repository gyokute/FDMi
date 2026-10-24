using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // §9.2。動的アンカーと静的アンカーに共通する、絶対姿勢の保持と view への写像。
    // 送受信・積分・補間は FDMiDynamicObjectSync、Editor 焼きの入力と焼き値の固定は FDMiStaticObjectSync が持つ。
    // Why-not: 基底に同期フィールドを置いて isStatic で分岐しない。動的は Manual、静的は NoVariableSync であり、
    // [UdonSynced] を持つ型は NoVariableSync を宣言できない (§5.1 / UdonSharp)。同期モードの違いが型の違いである。
    // Why-not: 具象にしない。直接付けると _InitLocal が空のまま _Tick も送受信もしないアンカーになる。
    // Why-not: 緯度経度高度 yaw を基底に置かない。Runtime が読まず、Editor 焼きの入力は静的アンカーだけが使う (§9.2.2)。
    public abstract class FDMiSyncAnchor : UdonSharpBehaviour
    {
        public FDMiSyncManager manager;
        public Rigidbody body;
        public Transform view;

        // 現在の絶対姿勢。動的は積分・受信サンプル、静的は焼き値が確定させる。
        // view への写像はこの 2 つだけを見る (§9.2)。
        public long[] _localAbs = new long[3];
        public Quaternion _localRot = Quaternion.identity;

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

        // §9.2。view を書く。毎フレーム書くかは Manager が決める (§9.1 / §12.3)。型ごとの実装を持つ。
        public abstract void _ApplyView();

        // §9.1。view を毎フレーム書く必要があるか。Manager が Start で 1 回だけ問い合わせる。
        // Why-not: Manager に isStatic の焼き値を持たせない。型が唯一の真実であり、型と焼き値が食い違う余地を作らない。
        public abstract bool _WritesViewEveryFrame();

        // §7.4 手順 4 / §8.2 手順 8。body を kinematic にせず位置・回転だけを写す。
        // Why-not: isKinematic を立てない。動的アンカーの owner は次の物理ステップで積分を続ける。
        public abstract void _ShiftToView();

        // body を kinematic にしてから絶対姿勢を書く。静的アンカーと他者が駆動する動的アンカーの経路。
        public void _ApplyPose()
        {
            if (body != null) body.isKinematic = true;
            _WriteViewPose();
        }

        // body があれば body を、無ければ view を書く。
        // Why-not: body と view の両方に書かない。view は body.transform の子 (VehicleVisual) であり (§11.5)、
        // 子の world 姿勢を親の移動後にもう一度書くと二重に動く。body が無いアンカーだけ view を持つ。
        public void _WriteViewPose()
        {
            Vector3 p = manager.ViewFromAbs(_localAbs);
            Quaternion q = manager.ViewRotFromAbs(_localRot);
            if (body != null)
            {
                body.position = p;
                body.rotation = q;
            }
            else
            {
                view.position = p;
                view.rotation = q;
            }
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
