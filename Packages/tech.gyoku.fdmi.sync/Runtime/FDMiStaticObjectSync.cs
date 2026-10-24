using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // §9.2。静的アンカー。送受信しない。絶対姿勢は Editor 焼きの 1 点で固定される (§4.4 / §6.1)。
    // 緯度経度高度 yaw はこの型だけが持つ。動的アンカーはスポーン姿勢から焼く (§7.2 / §11.1)。
    // Why-not: 基底の [UdonBehaviourSyncMode] は継承されないため、派生クラスにも明示する (§5.1)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiStaticObjectSync : FDMiSyncAnchor
    {
        [Header("Editor 焼きの入力。Runtime は読まない (E-01 / E-02 / E-07)")]
        public float latitude;
        public float longitude;
        public float height;
        // 接平面内のモデルの向き [deg]。q_abs = q_basis(lat, lon) ⊗ R_y(yaw) (§3.4)。
        public float yaw;

        // E-01 / E-02。焼いた絶対姿勢。Runtime は読むだけで書かない。
        // Why-not: [UdonSynced] を付けない。静的アンカーは送信せず、焼き値は全クライアントで同一である (§4.4)。
        // HideInInspector: 書き手は Editor 焼き (E-01 / E-02) だけであり、手で編集させない。
        [HideInInspector] public long[] _absMm = new long[3];
        [HideInInspector] public short[] _rot = new short[4];

        // _localAbs / _localRot は _InitLocal が焼き値から確定する (§9.2 手順 2)。
        public override void _InitLocal()
        {
            _localAbs[0] = _absMm[0]; _localAbs[1] = _absMm[1]; _localAbs[2] = _absMm[2];
            _localRot = FDMiSyncMath.UnpackQuat(_rot);
        }

        // §9.2 手順 2。静的アンカーは送受信しない。
        public override void _Tick(int frameStamp, int nowMs) { }

        // §9.2 / §12.3。静的 view は原点が変わったとき (_viewsDirty) だけ書けばよい。
        public override void _ApplyView()
        {
            if (!manager._viewsDirty) return;
            _ApplyPose();
        }

        // §9.1 / §12.3。静的 view は毎フレーム書かない。
        public override bool _WritesViewEveryFrame()
        {
            return false;
        }

        // §7.4 手順 4 / §8.2 手順 8。body を kinematic にせず位置・回転だけを写す。
        public override void _ShiftToView()
        {
            _WriteViewPose();
        }

        // FEF 成分の m/s。静的アンカーは動かない。
        public override Vector3 GetVelocity()
        {
            return Vector3.zero;
        }
    }
}
