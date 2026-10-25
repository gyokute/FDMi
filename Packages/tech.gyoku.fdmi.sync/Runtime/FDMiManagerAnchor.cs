using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // §9.1。自由移動中の root。機体ではないため body を持たず、同期もしない。
    // 絶対姿勢は initialAnchor と同じ位置から始まり、rebase で Manager が整数 km だけ動かす。
    // Why-not: initialAnchor をそのまま root にしない。rebase 後も root を原点に保つには、
    // 静的アンカーの焼き値を書き換えるのではなく、view origin 用の別アンカーが必要になる。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(1000)]
    public class FDMiManagerAnchor : FDMiSyncAnchor
    {
        public override void _InitLocal()
        {
            if (manager != null && manager.initialAnchor != null)
            {
                manager.initialAnchor.GetAbsoluteMm(_localAbs);
                _localRot = manager.initialAnchor.GetAbsoluteRotation();
            }
            _writesView = false;
        }

        public override void _Tick(int frameStamp, int nowMs) { }

        // root 自身は view を持たない。Manager GameObject を動かすと配下の非アンカーも動くため。
        public override void _ShiftToView() { }

        public override void _ApplyView() { }

        public override Vector3 GetVelocity()
        {
            return Vector3.zero;
        }
    }
}
