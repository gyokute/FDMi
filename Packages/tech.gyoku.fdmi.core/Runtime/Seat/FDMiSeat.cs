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
