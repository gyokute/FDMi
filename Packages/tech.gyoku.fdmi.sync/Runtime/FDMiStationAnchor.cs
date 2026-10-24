using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FDMi.sync
{
    // §9.5。席の絶対姿勢を車両アンカーから導出する。同期フィールドを持たない。
    // 席の占有は VRCStation が同期する。FDMi が占有を [UdonSynced] にしない (§1.3)。
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FDMiStationAnchor : UdonSharpBehaviour
    {
        public FDMiSyncManager manager;
        public FDMiSyncAnchor vehicleAnchor;
        public FDMiSyncAnchor exitAnchor;
        public bool switchOnEnter = true;
        public bool switchOnExit = true;
        public bool keepRootOnExit = true;
        public bool isPilotSeat;
        // 車両アンカーの owner を取る唯一の経路。Editor 焼きが isPilotSeat と同値にする (§9.5)。
        public bool takeOwnershipOnEnter;
        public int seatIndex;

        // Editor 焼き E-04。車両基準の席姿勢。Runtime は読むだけで書かない (§9.5)。
        // Why-not: 実行時に stationEnterPlayerLocation から再計算しない。焼きと同一の式であり、
        // 席位相対の状態を Runtime に持つと late join と席替えで 2 つの真実ができる (§10.2)。
        public int[] _bakedSeatRelMm = new int[3];
        public Quaternion _bakedSeatRelRot = Quaternion.identity;

        // §9.5 / §10.2。
        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (!player.isLocal) return;

            // Why-not: Networking.IsOwner を確認しない。プレイヤーアンカーの所有権は本人に固定される (§9.4)。
            FDMiSyncPlayer lp = manager.GetLocalPlayerAnchor();
            if (lp != null) { lp._seatIndex = seatIndex; lp.RequestSerialization(); }

            // Why-not: TeleportTo を呼ばない。呼ぶと station から降ろされる (§8.4)。
            if (switchOnEnter) manager.SwitchRootSeated(vehicleAnchor);

            if (isPilotSeat && takeOwnershipOnEnter && !Networking.IsOwner(vehicleAnchor.gameObject))
                Networking.SetOwner(player, vehicleAnchor.gameObject);
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!player.isLocal) return;

            FDMiSyncPlayer lp = manager.GetLocalPlayerAnchor();
            if (lp != null) { lp._seatIndex = -1; lp.RequestSerialization(); }

            // VRCStation がプレイヤー位置を確定してから絶対姿勢を捕まえるため 1 フレーム待つ (§10.3)。
            SendCustomEventDelayedFrames(nameof(_ExitSwitch), 1);
        }

        // §10.3。文字列配送なので public かつ引数なし。`_` 始まりなので legacy ネットワーク配送の対象外 (§9)。
        public void _ExitSwitch()
        {
            if (!switchOnExit) return;
            FDMiSyncAnchor next = keepRootOnExit
                ? vehicleAnchor
                : (exitAnchor != null ? exitAnchor : manager.defaultAnchor);
            manager.SwitchRoot(next);
        }

        // §9.5。dst = P_vehicle + Rotate(q_vehicle, p_seat)。
        public void GetSeatAbs(long[] dst)
        {
            vehicleAnchor.GetAbsoluteMm(dst);
            Quaternion q = vehicleAnchor.GetAbsoluteRotation();
            Vector3 r = FDMiSyncMath.QRotate(q, new Vector3(
                (float)_bakedSeatRelMm[0], (float)_bakedSeatRelMm[1], (float)_bakedSeatRelMm[2]));
            dst[0] = dst[0] + FDMiSyncMath.RoundHalfAwayFromZero(r.x);
            dst[1] = dst[1] + FDMiSyncMath.RoundHalfAwayFromZero(r.y);
            dst[2] = dst[2] + FDMiSyncMath.RoundHalfAwayFromZero(r.z);
        }

        public Quaternion GetSeatRot()
        {
            return FDMiSyncMath.QNormalize(FDMiSyncMath.QMul(vehicleAnchor.GetAbsoluteRotation(), _bakedSeatRelRot));
        }
    }
}
