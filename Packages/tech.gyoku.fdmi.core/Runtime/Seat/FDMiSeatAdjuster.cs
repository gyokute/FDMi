using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;

namespace FDMi.core
{
    // 座席位置は状態ではなく操作イベントとして全クライアントへ送る (ADR-0019)。
    // 同期変数は持たないが、受信側の Sync Mode が None だとネットワークイベントが届かない。
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FDMiSeatAdjuster : FDMiBehaviour
    {
        [Tooltip("1 操作あたりの移動量 [m]")]
        public float seatAdjustStep = 0.1f;

        [Tooltip("基準のローカル位置。Editor が実位置から焼き付ける")]
        public Vector3 initialSeatPos;

        // 初期位置からの移動量。全クライアントが同じイベント列から積算する。
        Vector3 seatOffset;

        // UI の入口 (FDMiSeat) から呼ばれる。状態は変えずに送るだけにして、
        // All が送信者自身も実行する性質による二重適用を避ける。
        // 先頭 _ は、この入口がネットワークイベントとして呼ばれるのを防ぐ。
        public void _RequestSeatUp()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkSeatUp");
        }

        public void _RequestSeatDown()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkSeatDown");
        }

        public void _RequestSeatForward()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkSeatForward");
        }

        public void _RequestSeatBack()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkSeatBack");
        }

        public void _RequestSeatRight()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkSeatRight");
        }

        public void _RequestSeatLeft()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkSeatLeft");
        }

        [NetworkCallable]
        public void NetworkSeatUp()
        {
            MoveSeat(Vector3.up * seatAdjustStep);
        }

        [NetworkCallable]
        public void NetworkSeatDown()
        {
            MoveSeat(-Vector3.up * seatAdjustStep);
        }

        [NetworkCallable]
        public void NetworkSeatForward()
        {
            MoveSeat(-Vector3.forward * seatAdjustStep);
        }

        [NetworkCallable]
        public void NetworkSeatBack()
        {
            MoveSeat(Vector3.forward * seatAdjustStep);
        }

        [NetworkCallable]
        public void NetworkSeatRight()
        {
            MoveSeat(Vector3.right * seatAdjustStep);
        }

        [NetworkCallable]
        public void NetworkSeatLeft()
        {
            MoveSeat(-Vector3.right * seatAdjustStep);
        }

        // 退出したクライアントだけが呼ばれる。全クライアントで 0 に戻す。
        public void FDMiOnSeatExit()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkSeatReset");
        }

        [NetworkCallable]
        public void NetworkSeatReset()
        {
            seatOffset = Vector3.zero;
            Apply();
        }

        void MoveSeat(Vector3 delta)
        {
            seatOffset += delta;
            Apply();
        }

        void Apply()
        {
            transform.localPosition = initialSeatPos + seatOffset;
        }
    }
}
