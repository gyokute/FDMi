using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    // §9.3。自由移動プレイヤーの絶対姿勢。owner は「そのプレイヤー本人」だけ (§6.2)。
    // 同一性は VRCPlayerObject の所有権で確定する。Start の 1 回だけ代入し、再代入の経路を持たない (§9.4)。
    // Why-not: 基底の [UdonBehaviourSyncMode] は継承されないため、派生クラスにも明示する (§5.1)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSyncPlayer : FDMiDynamicObjectSync
    {
        // VRCPlayerApi.playerId。Start が所有権から確定する。−1 は未確定。
        public int playerId = -1;
        public VRCPlayerApi player;
        // player.isLocal のキャッシュ。Start 以降変わらない。
        public bool isLocalPlayer;

        // stations[] の添字。-1 は自由移動 (§8.4)。着席の真実はこの 1 つだけで、seated フラグを持たない。
        [UdonSynced] public int _seatIndex = -1;

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

            manager._RegisterPlayerAnchor(this);
            if (isLocalPlayer) manager._SetLocalPlayerAnchor(this);
        }

        public override void _Tick(int frameStamp, int nowMs)
        {
            if (!_initialized) return;

            // 席姿勢は車両アンカーから導出する。送信しない (§10.2)。
            // Why-not: null ガードと `_seatIndex = -1` の代入を置かない。stations[] と _seatIndex の整合は
            // V-06 が焼きの時点で保証する (§1.2 正常系)。ここで書くと非 owner が同期フィールドを書く経路になる。
            if (_seatIndex >= 0)
            {
                FDMiStationAnchor seat = manager.GetStation(_seatIndex);
                seat.GetSeatAbs(_localAbs);
                _localRot = seat.GetSeatRot();
                return;
            }

            if (!isLocalPlayer)
            {
                _IntegrateReceive(nowMs);
                _ComputeViewPose(nowMs);
                return;
            }

            Quaternion originRot = manager.GetOriginRot();
            // §7.2 の自由移動行: originMm + Rotate(originRot, GetPosition() × 1000)。
            manager.AbsFromView(player.GetPosition(), _localAbs);
            _localRot = FDMiSyncMath.QMul(originRot, player.GetRotation());
            _localVelFef = FDMiSyncMath.QRotate(originRot, player.GetVelocity());
            // _SendNow は _accMm を送る。
            _accMm[0] = _localAbs[0]; _accMm[1] = _localAbs[1]; _accMm[2] = _localAbs[2];

            _TrySend(frameStamp);
        }
    }
}
