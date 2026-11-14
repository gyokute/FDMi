using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiSeat : FDMiBehaviour
    {
        [Tooltip("使用する VRCStation。同一 GameObject に置く")]
        public VRCStation station;

        [Tooltip("パイロット優先度。負ならパイロットにならない")]
        public int pilotPriority = -1;

        [Tooltip("パイロット選出を行うマネージャ。Editor が同じ名前空間から解決する")]
        public FDMiSeatManager manager;

        [Tooltip("座席位置の調整先。Editor が station の enter location から解決する")]
        public FDMiSeatAdjuster adjuster;

        // 着席中のプレイヤー。全クライアントで座席の占有状態を表す。
        [HideInInspector]
        public VRCPlayerApi seatedPlayer;

        [Tooltip("着席・退出時にイベントを送る相手。Editor が自動収集する")]
        public UdonSharpBehaviour[] InSeatBehaviours;

        public override void Interact()
        {
            Networking.LocalPlayer.UseAttachedStation();
        }

        public void ExitStation()
        {
            station.ExitStation(Networking.LocalPlayer);
        }

        // Unity UI Button の OnClick (SendCustomEvent) から呼ぶ座席位置の I/F。
        // 位置状態とその同期は adjuster が持つので、ここは転送のみにして UI の参照先を 1 つに保つ。
        public void SeatUp()
        {
            adjuster._RequestSeatUp();
        }

        public void SeatDown()
        {
            adjuster._RequestSeatDown();
        }

        public void SeatForward()
        {
            adjuster._RequestSeatForward();
        }

        public void SeatBack()
        {
            adjuster._RequestSeatBack();
        }

        public void SeatRight()
        {
            adjuster._RequestSeatRight();
        }

        public void SeatLeft()
        {
            adjuster._RequestSeatLeft();
        }

        // OnStationEntered/Exited はリモートプレイヤーでも全クライアントに届く。
        // 占有を更新したら全クライアントで選出をやり直す (着席はパイロット交代の契機)。
        public override void OnStationEntered(VRCPlayerApi player)
        {
            seatedPlayer = player;
            if (player.isLocal)
                foreach (UdonSharpBehaviour usb in InSeatBehaviours)
                    if (usb)
                        usb.SendCustomEvent("FDMiOnSeatEnter");

            if (manager != null)
                manager.DeterminePilot();
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            seatedPlayer = null;
            if (player.isLocal)
                foreach (UdonSharpBehaviour usb in InSeatBehaviours)
                    if (usb)
                        usb.SendCustomEvent("FDMiOnSeatExit");

            if (manager != null)
                manager.DeterminePilot();
        }
    }
}
