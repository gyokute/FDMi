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

        // 着席中のプレイヤー。全クライアントで座席の占有状態を表す。
        [HideInInspector]
        public VRCPlayerApi seatedPlayer;

        [Tooltip("着席・退出時にイベントを送る相手。Editor が自動収集する")]
        public UdonSharpBehaviour[] InSeatBehaviours;

        [FDMiDataPathConst("~/IsPilot")]
        [Tooltip("この座席のプレイヤーがパイロットか")]
        public FDMiBool IsPilot;

        [FDMiDataPathConst("~/HasPilot")]
        [Tooltip("パイロットが存在するか")]
        public FDMiBool HasPilot;

        [FDMiDataPathConst("~/PilotID")]
        [Tooltip("パイロットのプレイヤー ID。不在は -1")]
        public FDMiInt PilotID;

        [FDMiDataPathConst("~/CurrentPilotPriority")]
        [Tooltip("現パイロットの優先度")]
        public FDMiInt CurrentPilotPriority;

        public override void Interact()
        {
            Networking.LocalPlayer.UseAttachedStation();
        }

        public void ExitStation()
        {
            station.ExitStation(Networking.LocalPlayer);
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            seatedPlayer = player;
            if (player.isLocal)
            {
                TryTakePilot();
                foreach (UdonSharpBehaviour usb in InSeatBehaviours)
                    if (usb)
                        usb.SendCustomEvent("FDMiOnSeatEnter");
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            seatedPlayer = null;
            if (player.isLocal)
            {
                foreach (UdonSharpBehaviour usb in InSeatBehaviours)
                    if (usb)
                        usb.SendCustomEvent("FDMiOnSeatExit");
            }
        }

        #region Pilot Management
        // 座席への着席によるパイロット獲得。獲得者自身のクライアントのみが共有状態に書き込む。
        // 現パイロットが存在する場合のみ優先度を比較し、現パイロットより高い PilotPriority でなければ獲得しない。
        // 同一 priority の獲得競合 (着席順の入れ替わりによる再獲得の連鎖) は無視(同じ優先度の席がないことを期待する)
        public void TryTakePilot()
        {
            if (seatedPlayer.isLocal != true || pilotPriority < 0)
                return;
            if (HasPilot.Data && pilotPriority <= CurrentPilotPriority.Data)
                return;
            IsPilot.Data = true;
            HasPilot.Data = true;
            PilotID.Data = Networking.LocalPlayer.playerId;
            CurrentPilotPriority.Data = pilotPriority;
        }
        #endregion
    }
}
