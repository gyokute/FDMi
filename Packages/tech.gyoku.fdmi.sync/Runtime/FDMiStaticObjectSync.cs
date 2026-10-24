using UdonSharp;
using UnityEngine;

namespace FDMi.sync
{
    // §9.2。静的アンカー。送受信しない。絶対姿勢は Editor 焼きの 1 点で固定される (§4.4 / §6.1)。
    // Why-not: 基底の [UdonBehaviourSyncMode] は継承されないため、派生クラスにも明示する (§5.1)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiStaticObjectSync : FDMiSyncAnchor
    {
        // E-01 / E-02。焼いた絶対姿勢。Runtime は読むだけで書かない。
        // Why-not: [UdonSynced] を付けない。静的アンカーは送信せず、焼き値は全クライアントで同一である (§4.4)。
        // HideInInspector: 書き手は Editor 焼き (E-01 / E-02) だけであり、手で編集させない。
        [HideInInspector] public long[] _absMm = new long[3];
        [HideInInspector] public short[] _rot = new short[4];

        // _localAbs / _localRot は _Init が焼き値から確定する (§9.2 手順 2)。以後 _Tick は何もしない。
        public override void _InitLocal()
        {
            _localAbs[0] = _absMm[0]; _localAbs[1] = _absMm[1]; _localAbs[2] = _absMm[2];
            _localRot = FDMiSyncMath.UnpackQuat(_rot);
        }
    }
}
