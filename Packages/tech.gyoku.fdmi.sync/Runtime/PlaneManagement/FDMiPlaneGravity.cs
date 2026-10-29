using FDMi.core;
using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // 重力。地心向き (-g * normalize(p_abs)) を view へ写して加える (ADR-0011)。
    // Why-not: ~/Physics/Attitude (q_rel = QInv(q_basis) ⊗ q_abs) を使わない。機体と一緒に回り、
    // ピッチ 90° で前方・ロール 180° で上向きに引く。水平時のみ偶然合う。
    [DefaultExecutionOrder(1100)]
    public class FDMiPlaneGravity : FDMiBehaviour
    {
        public Rigidbody body;
        public FDMiSyncAnchor anchor;

        public float gravity = 9.78f;

        void FixedUpdate()
        {
            if (anchor == null || anchor.manager == null || body == null)
                return;
            long[] abs = anchor._localAbs;
            Vector3 p = new Vector3((float)abs[0] * 0.001f, (float)abs[1] * 0.001f, (float)abs[2] * 0.001f);
            Vector3 gFef = -gravity * p.normalized;
            body.AddForce(FDMiSyncMath.QInv(anchor.manager.GetOriginRot()) * gFef, ForceMode.Acceleration);
        }
    }
}
